#!/usr/bin/env python3
"""PR57 #467: lossless compression census of authentic #465 saves, offline only.

Usage: python3 pr57-467-authentic-lossless-history-probe.py pr57-465-original.zip
No simulation, synthetic citizens/events, engine changes or save-format changes.
"""
import collections
import hashlib
import json
import sys
import struct
import zipfile
import zlib

EXPECTED_ZIP_SHA = '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1'
SAVES = ['20h.json', 'A-pre/checkpoint-1.json', 'A-mid/checkpoint-1.json', 'A-post/checkpoint-1.json']


def sha(data):
    return hashlib.sha256(data).hexdigest()


def value_span(raw, key, start=0):
    """Extract raw JSON value bytes for a unique property, honoring quoted strings."""
    needle = ('"' + key + '":').encode()
    i = raw.find(needle, start)
    assert i >= 0, ('missing key', key)
    i += len(needle)
    assert raw[i:i + 1] == b'[', ('expected JSON array', key)
    depth = 0
    quoted = escaped = False
    for j in range(i, len(raw)):
        ch = raw[j]
        if quoted:
            if escaped:
                escaped = False
            elif ch == 92:
                escaped = True
            elif ch == 34:
                quoted = False
        elif ch == 34:
            quoted = True
        elif ch == 91:
            depth += 1
        elif ch == 93:
            depth -= 1
            if depth == 0:
                return raw[i:j + 1], j + 1
    raise AssertionError(('unterminated array', key))


def compress_exact(raw):
    compressed = zlib.compress(raw, 6)
    assert zlib.decompress(compressed) == raw
    return len(compressed)


def exact_append_delta(old_raw, new_raw):
    assert old_raw.startswith(b'[') and old_raw.endswith(b']')
    assert new_raw.startswith(old_raw[:-1]), 'raw historical prefix changed'
    return new_raw[len(old_raw) - 1:-1]


def measure_transition(before, after):
    old, new = json.loads(before), json.loads(after)
    assert [c['Id'] for c in old['Citizens']] == [c['Id'] for c in new['Citizens']]
    old_pos = new_pos = 0
    fragments = []
    framed = []
    for old_c, new_c in zip(old['Citizens'], new['Citizens']):
        a, old_pos = value_span(before, 'History', old_pos)
        b, new_pos = value_span(after, 'History', new_pos)
        assert new_c['History'][:len(old_c['History'])] == old_c['History']
        suffix = exact_append_delta(a, b)
        if suffix:
            fragments.append(suffix)
            framed.append((int(old_c['Id']), suffix))
    la, _ = value_span(before, 'Ledger')
    lb, _ = value_span(after, 'Ledger')
    ledger_delta = exact_append_delta(la, lb)
    ea, _ = value_span(before, 'CityEvents')
    eb, _ = value_span(after, 'CityEvents')
    event_delta = exact_append_delta(ea, eb)
    # Proof-of-concept: ordered per-citizen IDs + byte lengths + one zlib block.
    # This is a reversible sidecar witness, NOT a new production save format.
    header = struct.pack('<I', len(framed)) + b''.join(
        struct.pack('<qI', cid, len(fragment)) for cid, fragment in framed)
    block = zlib.compress(b''.join(fragments), 6) if fragments else b''
    packed = header + block
    count = struct.unpack_from('<I', packed)[0]
    assert count == len(framed)
    metadata = [struct.unpack_from('<qI', packed, 4 + 12 * i) for i in range(count)]
    restored = zlib.decompress(packed[4 + 12 * count:]) if count else b''
    pos = 0
    decoded = {}
    for cid, n in metadata:
        assert cid not in decoded
        decoded[cid] = restored[pos:pos + n]
        pos += n
    assert pos == len(restored)
    assert decoded == dict(framed)
    assert zlib.decompress(zlib.compress(ledger_delta)) == ledger_delta
    old_pos = new_pos = 0
    for old_c in old['Citizens']:
        a, old_pos = value_span(before, 'History', old_pos)
        b, new_pos = value_span(after, 'History', new_pos)
        assert a[:-1] + decoded.get(int(old_c['Id']), b'') + b']' == b
    assert la[:-1] + ledger_delta + b']' == lb
    assert ea[:-1] + event_delta + b']' == eb
    return dict(history_raw_append_bytes=sum(map(len, fragments)),
                history_framed_zlib_bytes=len(packed),
                history_append_affected_citizens=len(fragments),
                history_append_zlib_separate_bytes=sum(compress_exact(f) for f in fragments),
                history_append_zlib_joined_bytes=compress_exact(b''.join(fragments)) if fragments else 0,
                ledger_raw_append_bytes=len(ledger_delta),
                ledger_append_zlib_bytes=compress_exact(ledger_delta) if ledger_delta else 0,
                city_events_raw_append_bytes=len(event_delta),
                city_events_append_zlib_bytes=compress_exact(event_delta) if event_delta else 0)


def measure(raw):
    obj = json.loads(raw)
    histories = []
    pos = 0
    for citizen in obj['Citizens']:
        h, pos = value_span(raw, 'History', pos)
        assert json.loads(h) == citizen['History']
        histories.append(h)
    assert len(histories) == len(obj['Citizens'])
    ledger, _ = value_span(raw, 'Ledger')
    events, _ = value_span(raw, 'CityEvents')
    assert json.loads(ledger) == obj['Ledger']
    assert json.loads(events) == obj['CityEvents']
    descriptions = [e['Description'] for c in obj['Citizens'] for e in c['History']]
    descriptions += [e['Description'] for e in obj['CityEvents']]
    unique = len(set(descriptions))
    repeats = sum(v for v in collections.Counter(descriptions).values() if v > 1)
    packed_individual = sum(compress_exact(h) for h in histories)
    packed_joined = compress_exact(b''.join(histories))
    packed_ledger = compress_exact(ledger)
    packed_events = compress_exact(events)
    raw_history = sum(map(len, histories))
    return dict(save_bytes=len(raw), citizens=len(histories), alive=sum(c['Alive'] for c in obj['Citizens']),
                history_entries=sum(len(c['History']) for c in obj['Citizens']),
                history_raw_bytes=raw_history,
                history_zlib_per_citizen_bytes=packed_individual,
                history_zlib_joined_bytes=packed_joined,
                history_roundtrip_ok=True,
                ledger_entries=len(obj['Ledger']), ledger_raw_bytes=len(ledger), ledger_zlib_bytes=packed_ledger,
                city_events_entries=len(obj['CityEvents']), city_events_raw_bytes=len(events),
                city_events_zlib_bytes=packed_events,
                description_total=len(descriptions), description_unique=unique,
                description_occurrences_with_repeated_value=repeats,
                save_zlib_bytes=compress_exact(raw), sha256=sha(raw))


def main(path):
    archive = open(path, 'rb').read()
    assert sha(archive) == EXPECTED_ZIP_SHA, 'original #465 artifact digest mismatch'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        results = {}
        for name in SAVES:
            a = z.read(name)
            if name.startswith('A-'):
                assert a == z.read(name.replace('A-', 'B-', 1)), ('A/B mismatch', name)
            results[name] = measure(a)
        # Validate chronological prefix of every individual history, not only totals.
        for prev, curr in zip(SAVES, SAVES[1:]):
            before = json.loads(z.read(prev))['Citizens']
            after = json.loads(z.read(curr))['Citizens']
            for old, new in zip(before, after):
                assert old['Id'] == new['Id']
                assert new['History'][:len(old['History'])] == old['History']
    with zipfile.ZipFile(path) as z:
        for prev, curr in zip(SAVES, SAVES[1:]):
            print('append_' + prev + '_to_' + curr, json.dumps(
                measure_transition(z.read(prev), z.read(curr)), sort_keys=True))
    for name, row in results.items():
        print(name, json.dumps(row, sort_keys=True, ensure_ascii=False))
    a, b = results[SAVES[0]], results[SAVES[-1]]
    print('delta_20h_to_00h02', json.dumps({k: b[k] - a[k] for k in (
        'save_bytes', 'history_entries', 'history_raw_bytes', 'history_zlib_per_citizen_bytes',
        'history_zlib_joined_bytes', 'ledger_entries', 'ledger_raw_bytes', 'ledger_zlib_bytes',
        'city_events_entries', 'city_events_raw_bytes')}, sort_keys=True))
    print('PR57_467_AUTHENTIC_LOSSLESS_HISTORY_PASS')


if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: script original-zip'
    main(sys.argv[1])
