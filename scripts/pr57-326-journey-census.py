#!/usr/bin/env python3
"""PR57 #326: independent, read-only census of #325 authentic replay logs.

Usage: python3 scripts/pr57-326-journey-census.py pr57-325.zip
Only the two CitizenRoutine work/home StartJourney call sites are timed by
PR57_239_CITIZEN name=start-journey. Other StartJourney calls (notably SeekFood)
are NOT included. walkCache missMs is inclusive and excludes cache hits.
The difference of these timers is NOT exclusive cost attribution.
"""
import hashlib
import re
import sys
import zipfile

EXPECTED = '82e08c3840ba38f1bc29f1fc35905daaeaf0b9d9c330fe6d2611299a94499b41'
PATTERNS = {
    'step': re.compile(r'^PR57_294_STEP case=\S+ call=(\d+) .*?citizensMs=([\d.]+) advanceWallMs=([\d.]+)'),
    'journey': re.compile(r'^PR57_323_COUNTER case=\S+ call=(\d+) PR57_239_CITIZEN name=start-journey calls=(\d+) ms=([\d.]+)'),
    'path': re.compile(r'^PR57_324_COUNTER case=\S+ call=(\d+) PR57_324_ROUTE name=walkCache calls=(\d+) misses=(\d+) missMs=([\d.]+)'),
}

def parse(raw):
    data = {}
    for line in raw.decode('utf-8').splitlines():
        for key, pattern in PATTERNS.items():
            match = pattern.match(line)
            if match:
                call = int(match.group(1))
                data.setdefault(call, {})[key] = tuple(float(x) for x in match.groups()[1:])
                break
    return data

def summarize(data, length):
    assert set(range(1, length + 1)) <= {i for i, row in data.items() if 'step' in row}
    wall = sum(data[i]['step'][1] for i in range(1, length + 1))
    citizens = sum(data[i]['step'][0] for i in range(1, length + 1))
    final = data[length]
    assert 'journey' in final and 'path' in final
    direct_calls, direct_ms = final['journey']
    path_calls, path_misses, path_ms = final['path']
    rows = []
    for call in (1, 2, 23, 32):
        now, before = data[call], data[call - 1]
        assert 'journey' in now and 'path' in now and 'journey' in before and 'path' in before
        j_calls = now['journey'][0] - before['journey'][0]
        j_ms = now['journey'][1] - before['journey'][1]
        p_calls = now['path'][0] - before['path'][0]
        p_misses = now['path'][1] - before['path'][1]
        p_ms = now['path'][2] - before['path'][2]
        assert min(j_calls, j_ms, p_calls, p_misses, p_ms) >= -1e-4
        rows.append((call, int(j_calls), round(j_ms, 3), int(p_calls), int(p_misses),
                     round(p_ms, 3), round(max(0, j_ms - p_ms), 3)))
    return (round(wall, 3), round(citizens, 3), int(direct_calls), round(direct_ms, 3),
            int(path_calls), int(path_misses), round(path_ms, 3), round(wall / (wall - direct_ms), 5)), rows

def main(filename):
    with open(filename, 'rb') as f:
        digest = hashlib.file_digest(f, 'sha256').hexdigest()
    assert digest == EXPECTED, (digest, EXPECTED)
    print('artifact SHA-256:', digest)
    pairs = 0
    with zipfile.ZipFile(filename) as z:
        for case, length, checkpoints in (
            ('real14hREAL', 100, (0, 20, 50, 100)),
            ('gridARTIFICIAL', 50, (0, 20, 25, 50)),
        ):
            for n in (1, 2):
                control = f'{case}-control{n}'
                probe = f'{case}-probe{n}'
                for at in checkpoints:
                    name = f'checkpoint-{at}.json'
                    assert z.read(f'{control}/{name}') == z.read(f'{probe}/{name}')
                    pairs += 1
                totals, rows = summarize(parse(z.read(probe + '.log')), length)
                print(case, 'probe', n,
                      'AdvanceWallMs,citizensMs,workHomeCalls,workHomeMs,pedPathCalls,misses,missMs,'
                      'impossibleWorkHomeFreeCeiling=', totals)
                for row in rows:
                    print('  call,workHomeCalls,workHomeMs,pedPathCalls,misses,missMs,'
                          'workHomeNotCoveredByPedMissTimerMs=', row)
    assert pairs == 16
    print('control/probe full JSON checkpoint pairs identical:', pairs, '/ 16')

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('Usage: python3 pr57-326-journey-census.py pr57-325.zip')
    main(sys.argv[1])
