#!/usr/bin/env python3
"""PR57 #516: byte-exact append-only delta on authentic REAL midnight ABBA saves.
Usage: python3 scripts/pr57-516-midnight-delta.py pr57-478-original.zip
No engine changes, no valid alternative save format, no throughput claim.
"""
import collections
import hashlib
import json
import re
import sys
import zipfile
from datetime import datetime, timedelta
from pathlib import Path

ZIP_SHA = '7130344249d2958244dd08906eea5cfc74bb0585cd14a772a5e4c9ce9aff6902'
CASE = 'midnightREAL121'
ARMS = ('A1', 'B1', 'B2', 'A2')
PATTERN = re.compile(rb'"(Ledger|CityEvents|History)":(?=\[)')
DECODER = json.JSONDecoder()


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def split(raw):
    assert raw.isascii()
    pieces, arrays, last = [], [], 0
    for match in PATTERN.finditer(raw):
        start = match.end()
        value, length = DECODER.raw_decode(raw[start:].decode('ascii'))
        assert isinstance(value, list)
        arr = raw[start:start + length]
        assert arr[:1] == b'[' and arr[-1:] == b']'
        arrays.append((match.group(1).decode(), arr, len(value)))
        pieces.extend((raw[last:start], b'[]'))
        last = start + length
    pieces.append(raw[last:])
    return b''.join(pieces), arrays


def join(core, arrays):
    pieces, last = [], 0
    matches = list(PATTERN.finditer(core))
    assert len(matches) == len(arrays)
    for match, (key, arr, _) in zip(matches, arrays):
        assert match.group(1).decode() == key
        start = match.end()
        assert core[start:start + 2] == b'[]'
        pieces.extend((core[last:start], arr))
        last = start + 2
    return b''.join(pieces + [core[last:]])


def main(path):
    assert sha(Path(path).read_bytes()) == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 13
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 478 and summary['allAdvance2']
        assert summary['comparison'] == 'same-composed-flags-ABBA'
        assert summary['cases'][CASE]['steps'] == 1800
        assert summary['cases'][CASE]['fullJsonParity']
        pairs, start_shas, end_shas, timing = [], set(), set(), {}
        for arm in ARMS:
            pair = []
            for i in (0, 1):
                raw = z.read(f'{CASE}/{arm}/checkpoint-{i}.json')
                core, arrays = split(raw)
                assert join(core, arrays) == raw
                pair.append((raw, json.loads(raw), core, arrays))
                (start_shas if i == 0 else end_shas).add(sha(raw))
            assert sha(pair[0][0]) == summary['cases'][CASE]['inputSha256']
            assert sha(pair[1][0]) == summary['cases'][CASE]['arms'][arm]['sha256']
            assert len(pair[1][0]) == summary['cases'][CASE]['arms'][arm]['bytes']
            log = z.read(f'{CASE}/{arm}/full.log').decode()
            assert 'WINDOW 1 seconds=3600' in log and 'INVARIANTS OK' in log
            assert 'PR57_AB_CHECKPOINT window=1 sha256=' + sha(pair[1][0]).upper() in log
            w = re.search(r'WINDOW 1 seconds=3600 wall=([0-9.]+)s', log)
            sl = re.search(r'SAVE [0-9.]+MB save=([0-9.]+)s load=([0-9.]+)s', log)
            assert w and sl
            timing[arm] = [float(w.group(1)), float(sl.group(1)), float(sl.group(2))]
            pairs.append(pair)
        assert len(start_shas) == len(end_shas) == 1
        a, b = pairs[0]
        old, new = a[1], b[1]
        assert datetime.fromisoformat(new['Now']) - datetime.fromisoformat(old['Now']) == timedelta(hours=1)
        assert old['Now'] == '2026-01-07T23:59:58' and new['Now'] == '2026-01-08T00:59:58'
        assert [c['Id'] for c in old['Citizens']] == [c['Id'] for c in new['Citizens']]
        assert len(old['Citizens']) == 121
        assert [k for k, _, _ in a[3]] == [k for k, _, _ in b[3]]
        assert len(a[3]) == 123
        deltas, tail_bytes, rebuilt = collections.Counter(), collections.Counter(), []
        for (key, prev, n0), (other, curr, n1) in zip(a[3], b[3]):
            assert key == other and n1 >= n0
            assert curr.startswith(prev[:-1]) and curr.endswith(b']')
            tail = curr[len(prev) - 1:-1]
            assert curr == prev[:-1] + tail + b']'
            deltas[key] += n1 - n0
            tail_bytes[key] += len(tail)
            rebuilt.append((key, prev[:-1] + tail + b']', n1))
        assert join(b[2], rebuilt) == b[0]
        by_id = {c['Id']: c for c in old['Citizens']}
        personal = [event for c in new['Citizens'] for event in c['History'][len(by_id[c['Id']]['History']):]]
        ledger = new['Ledger'][len(old['Ledger']):]
        city = new['CityEvents'][len(old['CityEvents']):]
        assert [len(personal), len(ledger), len(city)] == [deltas[x] for x in ('History', 'Ledger', 'CityEvents')]
        for event in personal + ledger + city:
            assert old['Now'] < event['At'] <= new['Now']
        midnight = '2026-01-08T00:00:00'
        at_midnight = {k: sum(e['At'] == midnight for e in events) for k, events in
                       [('History', personal), ('Ledger', ledger), ('CityEvents', city)]}
        historical = lambda arrays: sum(len(arr) for _, arr, _ in arrays)
        reuse = sum(len(arr) - 2 for _, arr, _ in a[3])
        delta_representation = len(b[2]) + sum(tail_bytes.values())
        assert delta_representation + reuse == len(b[0])
        assert len(b[0]) - len(a[0]) == len(b[2]) - len(a[2]) + sum(tail_bytes.values())
        print(json.dumps({
            'case': 'REAL congestionada, 121 vivos, 98 veiculos',
            'window': [old['Now'], new['Now']], 'steps_per_arm': 1800,
            'ABBA_byte_parity': True, 'all_123_arrays_append_only_bytes': True,
            'save_bytes': [len(a[0]), len(b[0])],
            'historical_array_bytes': [historical(a[3]), historical(b[3])],
            'history_share_final_pct': round(100 * historical(b[3]) / len(b[0]), 2),
            'core_bytes': [len(a[2]), len(b[2])],
            'new_records': dict(deltas), 'new_tail_bytes': dict(tail_bytes),
            'at_midnight': at_midnight,
            'ledger_categories': dict(sorted(collections.Counter(e['Category'] for e in ledger).items())),
            'unchanged_history_bytes_reused': reuse,
            'current_core_plus_new_tails_bytes': delta_representation,
            'save_growth_bytes': len(b[0]) - len(a[0]),
            'wall_seconds_per_arm_step_save_load': timing,
            'limit': 'byte delta only; full history retained; not .NET, GC, throughput, or 80y proof'
        }, sort_keys=True, ensure_ascii=False))
    print('PR57_516_AUTHENTIC_MIDNIGHT_DELTA_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit('usage: pr57-516-midnight-delta.py pr57-478-original.zip')
    main(sys.argv[1])
