#!/usr/bin/env python3
"""PR57 #515: lossless history/log split on authentic #493 checkpoints.

Usage: python3 scripts/pr57-515-history-split.py pr57-493-original.zip
Observational, outside the .NET motor; not a runtime optimization or new save format.
"""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

ZIP_SHA = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
CASES = ('worksREAL47', 'congestedREAL121', 'GRID_ARTIFICIAL')
ARMS = ('A1', 'B1', 'B2', 'A2')
KEYS = ('Ledger', 'CityEvents', 'History')
DECODER = json.JSONDecoder()
PATTERN = re.compile(rb'"(Ledger|CityEvents|History)":(?=\[)')


def split_losslessly(raw):
    """Replace only exact JSON array byte spans, retaining their original bytes."""
    assert raw.isascii()
    patches, fragments, last, core_offset = [], [], 0, 0
    counts, lengths = {k: 0 for k in KEYS}, {k: 0 for k in KEYS}
    for match in PATTERN.finditer(raw):
        start = match.end()
        _, end = DECODER.raw_decode(raw[start:].decode('ascii'))
        end += start
        key = match.group(1).decode('ascii')
        prefix = raw[last:start]
        fragments.extend((prefix, b'[]'))
        core_offset += len(prefix)
        patches.append((core_offset, raw[start:end]))
        core_offset += 2
        lengths[key] += end - start
        counts[key] += 1
        last = end
    fragments.append(raw[last:])
    core = b''.join(fragments)
    restored, last = [], 0
    for offset, original in patches:
        assert core[offset:offset+2] == b'[]'
        restored.extend((core[last:offset], original))
        last = offset + 2
    restored.append(core[last:])
    assert b''.join(restored) == raw
    assert len(core) + sum(len(x) - 2 for _, x in patches) == len(raw)
    return counts, lengths, len(core), len(patches)


def event_counts(data):
    return {'Ledger': len(data['Ledger']), 'CityEvents': len(data['CityEvents']),
            'History': sum(len(c['History']) for c in data['Citizens'])}


def prefix_append_only(a, b):
    assert b['Ledger'][:len(a['Ledger'])] == a['Ledger']
    assert b['CityEvents'][:len(a['CityEvents'])] == a['CityEvents']
    old = {c['Id']: c['History'] for c in a['Citizens']}
    new = {c['Id']: c['History'] for c in b['Citizens']}
    assert all(i in new and new[i][:len(h)] == h for i, h in old.items())


def main(path):
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == ZIP_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 37
        meta = json.loads(z.read('summary.json'))
        assert meta['experiment'] == 493
        assert meta['cadence'] == 'one public Advance(2) per quantum'
        assert meta['comparison'] == 'same composed flags, fresh processes ABBA'
        for case in CASES:
            starts, ends = set(), set()
            reference = None
            for arm in ARMS:
                pair = []
                for i in (0, 1):
                    raw = z.read(f'{case}/{arm}/checkpoint-{i}.json')
                    sha = hashlib.sha256(raw).hexdigest()
                    expected = meta['cases'][case]['arms'][arm]['initialSha256' if i == 0 else 'finalSha256']
                    assert sha == expected
                    (starts if i == 0 else ends).add(sha)
                    data = json.loads(raw)
                    counts, lengths, core_bytes, patches = split_losslessly(raw)
                    assert counts['Ledger'] == counts['CityEvents'] == 1
                    assert counts['History'] == len(data['Citizens'])
                    pair.append((data, len(raw), lengths, core_bytes, patches))
                prefix_append_only(pair[0][0], pair[1][0])
                if reference is None:
                    reference = pair
            assert len(starts) == len(ends) == 1 and meta['cases'][case]['fullJsonParity']
            a, b = reference
            before, after = event_counts(a[0]), event_counts(b[0])
            assert all(after[k] >= before[k] for k in KEYS)
            print(json.dumps({
                'case': case,
                'type': 'ARTIFICIAL' if case == 'GRID_ARTIFICIAL' else 'REAL',
                'steps': 50 if case == 'GRID_ARTIFICIAL' else 100,
                'ABBA_full_checkpoint_parity': True,
                'byte_exact_split_and_reassembly': True,
                'append_only_during_window': True,
                'save_bytes': [a[1], b[1]],
                'split_core_bytes': [a[3], b[3]],
                'history_ledger_cityevents_bytes': [sum(a[2].values()), sum(b[2].values())],
                'history_ledger_cityevents_share_pct': [round(100 * sum(x[2].values()) / x[1], 2) for x in (a, b)],
                'arrays': {'before': a[2], 'after': b[2]},
                'event_counts': {'before': before, 'after': after},
                'event_deltas': {k: after[k] - before[k] for k in KEYS},
                'save_delta_bytes': b[1] - a[1],
                'note': 'full JSON byte spans, not retained heap; no extrapolation of event rate to 80 years'
            }, sort_keys=True))
    print('PR57_515_AUTHENTIC_HISTORY_SPLIT_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit('usage: pr57-515-history-split.py pr57-493-original.zip')
    main(sys.argv[1])
