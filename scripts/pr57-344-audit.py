#!/usr/bin/env python3
"""Verify PR57 #344 original Action ZIP, REAL 17h-20h; no engine execution.

Usage: python3 scripts/pr57-344-audit.py pr57-344-artifact.zip
The Amdahl ceiling eliminates ONLY the timed BodyOnWay calls at the two
MayEnterJunction sites, not TurnWay, other admission work, or other phases.
"""
import hashlib
import re
import sys
import zipfile

EXPECTED_ZIP = 'a726c32b34ef667d4032653755509444fb753ae463c98d44ebbea394ca18995e'
EXPECTED_CHECKPOINT = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'


def field(line, key):
    match = re.search(r'(?<!\\w)' + re.escape(key) + r'=([^\\s]+)', line)
    if not match:
        raise ValueError(f'Missing {key}: {line}')
    return match.group(1)


def read_mode(z, mode):
    lines = z.read(f'{mode}/census.log').decode('utf-8').splitlines()
    hours = [x for x in lines if x.startswith('PR57_337_HOUR ')]
    final = [x for x in lines if x.startswith('PR57_344_FINAL ')]
    groups = [x for x in lines if x.startswith('PR57_344_GROUP ')]
    assert len(hours) == 6 and len(final) == 1 and len(groups) == 4
    assert [int(field(x, 'hour')) for x in hours] == list(range(1, 7))
    assert [field(x, 'name') for x in groups] == [
        'onward-blocked', 'onward-clear', 'bend-blocked', 'bend-clear']
    assert field(final[0], 'enabled') == ('True' if mode == 'probe' else 'False')
    assert sum(int(field(x, 'calls')) for x in groups) == int(field(final[0], 'calls'))
    return {
        'wall_ms': sum(float(field(x, 'wallMs')) for x in hours[3:]),
        'cpu_ms': sum(float(field(x, 'cpuMs')) for x in hours[3:]),
        'alloc': sum(int(field(x, 'totalAllocatedBytes')) for x in hours[3:]),
        'calls': int(field(final[0], 'calls')),
        'body_ms': float(field(final[0], 'bodyMs')),
        'body_alloc': int(field(final[0], 'allocatedBytes')),
        'groups': [(field(x, 'name'), int(field(x, 'calls')),
                    float(field(x, 'bodyMs')), int(field(x, 'allocatedBytes')))
                   for x in groups],
    }


def main(path):
    raw = open(path, 'rb').read()
    digest = hashlib.sha256(raw).hexdigest()
    assert digest == EXPECTED_ZIP, (digest, EXPECTED_ZIP)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'ZIP CRC mismatch'
        a = z.read('control/checkpoint-10800.json')
        b = z.read('probe/checkpoint-10800.json')
        assert a == b, 'Control/probe checkpoints differ'
        assert hashlib.sha256(a).hexdigest() == EXPECTED_CHECKPOINT
        control, probe = read_mode(z, 'control'), read_mode(z, 'probe')
    assert control['calls'] == 0
    assert probe['calls'] == 159072
    assert probe['groups'][0][1] == 126672
    assert probe['groups'][2][1] == 32400
    share = probe['body_ms'] / probe['wall_ms']
    assert 0 < share < 1
    print('ZIP SHA-256:', digest)
    print('Canonical checkpoint SHA-256:', EXPECTED_CHECKPOINT, '(byte-identical)')
    for mode, values in [('control', control), ('probe', probe)]:
        print(f"{mode}: wall={values['wall_ms']:.3f}ms CPU={values['cpu_ms']:.3f}ms "
              f"allocated={values['alloc']}B")
    for name, calls, ms, allocated in probe['groups']:
        print(f'{name}: calls={calls} bodyMs={ms:.3f} allocatedBytes={allocated}')
    print(f"BodyOnWay total: {probe['calls']} calls, {probe['body_ms']:.3f}ms, "
          f"{probe['body_alloc']}B")
    print(f'Fraction of probe wall: {share:.6%}')
    print(f'Idealized maximum integrated speedup in this window: {1/(1-share):.6f}x')
    print('Scope: exclusive junction BodyOnWay only; observational and intrusive; '
          'not an achieved optimization, causal certificate, or 10k extrapolation')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('Usage: python3 pr57-344-audit.py <artifact.zip>')
    main(sys.argv[1])
