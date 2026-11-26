#!/usr/bin/env python3
"""PR57 #299: offline per-physical-pass queue amplification audit of #298 CI.

Observational only: a pass cannot be removed without a causal equivalence proof.
Class 'queue' means final actor Cause/Status, not a specific expensive predicate.
"""
import argparse
import hashlib
import json
import re
import zipfile

CASES = {'gridARTIFICIAL': 50, 'real14hREAL': 100}

def fields(line):
    return dict(re.findall(r'(\w+)=([^\s]+)', line))

def analyze(z, case, arm, calls):
    lines = z.read(f'{case}-{arm}.log').decode().splitlines()
    phase = {int(v['call']): v for v in map(fields, (s for s in lines if s.startswith('PR57_294_STEP ')))}
    steps = {int(v['call']): v for v in map(fields, (s for s in lines if s.startswith('PR57_298_STEP ')))}
    groups = {(int(v['call']), int(v['group'])): v for v in map(fields, (s for s in lines if s.startswith('PR57_298_CLASS ')))}
    assert sorted(phase) == sorted(steps) == list(range(1, calls+1))
    assert len(groups) == 8*calls
    rows = []
    for call in range(1, calls+1):
        s, p = steps[call], phase[call]
        g = [groups[call, i] for i in range(8)]
        assert len({int(v['slices']) for v in g}) == 1
        passes = int(s['slices'])
        batched = int(g[0]['evaluations']) > 0
        assert passes >= 1 + int(batched)
        hz = int(s['stopwatchFrequency'])
        assert hz > 0
        queue = g[3]
        assert abs(sum(int(v['ticks']) for v in g)*1000/hz - float(s['classifiedMs'])) < .001
        assert abs(float(s['vehiclesMs'])-float(p['vehiclesMs'])) < .001
        rows.append({'call':call, 'batched':batched, 'passes':passes,
                     'queueEval':int(queue['evaluations']),
                     'queueMs':int(queue['ticks'])*1000/hz,
                     'vehicleMs':float(p['vehiclesMs']), 'wallMs':float(p['advanceWallMs'])})
    for label, subset in [('batched', [r for r in rows if r['batched']]),
                          ('unsplit', [r for r in rows if not r['batched']]),
                          ('unsplitAfter4', [r for r in rows if not r['batched'] and r['call']>4])]:
        if not subset: continue
        passes = sum(r['passes'] for r in subset)
        evaluations = sum(r['queueEval'] for r in subset)
        queue_ms = sum(r['queueMs'] for r in subset)
        vehicle_ms = sum(r['vehicleMs'] for r in subset)
        print(json.dumps({'case':case,'arm':arm,'cohort':label,
            'calls':len(subset),'physicalPasses':passes,'queueEvaluations':evaluations,
            'queueEvaluationsPerPass':round(evaluations/passes,3),
            'queueMsPerPass':round(queue_ms/passes,3),
            'vehicleMsPerPass':round(vehicle_ms/passes,3),
            'queueUsPerEvaluation':round(queue_ms*1000/evaluations,3),
            'wallMs':round(sum(r['wallMs'] for r in subset),3)},sort_keys=True))
    return rows

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('zip_path')
    parser.add_argument('--sha256', required=True)
    args = parser.parse_args()
    with open(args.zip_path,'rb') as f:
        digest = hashlib.file_digest(f,'sha256').hexdigest()
    assert digest == args.sha256, (digest,args.sha256)
    parity = 0
    with zipfile.ZipFile(args.zip_path) as z:
        for case, calls in CASES.items():
            for control, probe in [('control1','probe1'),('control2','probe2')]:
                for at in sorted({0,20,calls//2,calls}):
                    a=z.read(f'{case}-{control}/checkpoint-{at}.json')
                    b=z.read(f'{case}-{probe}/checkpoint-{at}.json')
                    assert a==b, (case,control,probe,at)
                    json.loads(a)
                    parity+=1
                rows=analyze(z,case,probe,calls)
                if case=='gridARTIFICIAL':
                    batched=[r['call'] for r in rows if r['batched']]
                    assert batched==[5,6,7,8,9,11,14,15,16,19,20,22]
                    assert sum(r['passes'] for r in rows if r['batched'])==222
                else:
                    assert all(r['batched'] for r in rows)
                    assert sum(r['passes'] for r in rows)==718
    assert parity==16
    print(f'PR57_299_VERIFIED sha256={digest} parity={parity}/16 processes=8 allAdvance2=True offline=True noGainClaim=True')

if __name__=='__main__': main()
