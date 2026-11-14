#!/usr/bin/env python3
"""PR57 #502: audit authentic repeated Manoeuvre work against later reader scopes.

Usage: python3 scripts/pr57-502-authentic-plan-fanout.py 248.zip 497.zip
No engine changes, no claim that equal explicit arguments imply causal reuse.
"""
import hashlib
import re
import sys
import zipfile

EXPECTED = (
    '7adffc6ddbd77184fb268815a5549089acc5640dcb44f216cd2083b545e2a740',
    '89eb18a37fc30d697562daa556c24d0742000473eeaf1b6d9a7940fde534c7fa',
)

def archive(path, expected):
    assert hashlib.sha256(open(path, 'rb').read()).hexdigest() == expected, path
    z = zipfile.ZipFile(path)
    assert z.testzip() is None, path
    return z

def row(log, prefix, predicate=lambda _: True):
    matches = [s for s in log.splitlines() if s.startswith(prefix) and predicate(s)]
    assert len(matches) == 1, (prefix, len(matches))
    return dict(re.findall(r'(\w+)=([^ ]+)', matches[0]))

def num(d, key):
    return float(d[key])

def main(p248, p497):
    with archive(p248, EXPECTED[0]) as a, archive(p497, EXPECTED[1]) as b:
        for checkpoint in (0, 1):
            data = [a.read(f'queueREAL-{arm}/checkpoint-{checkpoint}.json') for arm in ('control', 'observed')]
            data += [b.read(f'congestedREAL121/{arm}/checkpoint-{checkpoint}.json') for arm in ('A1', 'B1', 'B2', 'A2')]
            assert all(item == data[0] for item in data), f'checkpoint-{checkpoint} differs'
            print(f'checkpoint-{checkpoint}: {len(data[0])} bytes, sha256={hashlib.sha256(data[0]).hexdigest()}, 6/6 exact')
        log248 = a.read('queueREAL-observed.log').decode()
        man = row(log248, 'PR57_248_MANOEUVRE ', lambda s: 'slices=600 ' in s)
        win248 = row(log248, 'WINDOW 1 ')
        assert int(man['calls']) == 14823 and int(man['repeatedActorCalls']) == 8831
        assert man['noReuse'] == 'True' and man['callbacksExcludedFromRepeats'] == 'True'
        wall248_ms = float(win248['wall'].removesuffix('s')) * 1000
        repeated_ms = num(man, 'repeatedMs')
        optimistic_fraction = repeated_ms / wall248_ms
        print(f'#248: manoeuvre calls={man["calls"]}, same-actor explicit-argument repeats={man["repeatedActorCalls"]}, '
              f'repeat work={repeated_ms:.4f} ms, all manoeuvre={man["totalMs"]} ms')
        print(f'#248: repeat poses={man["repeatedPoses"]}/{man["totalPoses"]}; '
              f'repeat alloc={man["repeatedMB"]} MB; observed window wall~{wall248_ms:.0f} ms')
        print(f'#248: optimistic repeat-only Amdahl~{1/(1-optimistic_fraction):.4f}x '
              '(intrusive, rounded wall, not a causal cache or universal bound)')
        results = []
        for arm in ('B1', 'B2'):
            log = b.read(f'congestedREAL121/{arm}/full.log').decode()
            total = row(log, 'PR57_497_TOTAL ')
            kinds = {name: row(log, 'PR57_497_KIND ', lambda s, name=name: f'kind={name} ' in s)
                     for name in ('Manoeuvre', 'BodyOnWay', 'TrafficOnWay')}
            assert int(total['calls']) == 600 and int(total['rootBytes']) == int(total['exclusiveSum'])
            results.append((arm, total, kinds))
            print(f'#497 {arm}: Manoeuvre={kinds["Manoeuvre"]["calls"]} calls / '
                  f'{int(kinds["Manoeuvre"]["exclusiveBytes"])/1e6:.3f} MB exclusive; '
                  f'BodyOnWay={kinds["BodyOnWay"]["calls"]} calls / '
                  f'{int(kinds["BodyOnWay"]["exclusiveBytes"])/1e6:.3f} MB; '
                  f'TrafficOnWay={kinds["TrafficOnWay"]["calls"]} calls / '
                  f'{int(kinds["TrafficOnWay"]["exclusiveBytes"])/1e6:.3f} MB')
        for name in ('Manoeuvre', 'BodyOnWay', 'TrafficOnWay'):
            assert results[0][2][name]['calls'] == results[1][2][name]['calls'], name
        print('INTERPRETATION: repeated explicit arguments do NOT cover callbacks, writers, '
              'external edits or exact physical input equality; counts from different instrumentations '
              'are not a producer-to-consumer pairing. No new accepted integrated gain.')
        print('PR57_502_AUTHENTIC_PLAN_FANOUT_AUDIT_PASS')

if __name__ == '__main__':
    assert len(sys.argv) == 3, __doc__
    main(sys.argv[1], sys.argv[2])
