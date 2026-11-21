#!/usr/bin/env python3
"""PR57 #526: conservative queue-allocation bounds from the authentic #522 ZIP.

No simulator modifications. Uses *disjoint* region and per-vehicle allocation
counters already emitted by the same observational run. It does not infer
individual visit attribution, eliminate physics, or estimate recoverable bytes.

Usage: python3 scripts/pr57-526-queue-region-bound.py pr57-522-original.zip
"""
import hashlib
import json
import re
import sys
import zipfile
from decimal import Decimal as D
from pathlib import Path

DIGEST = '2561e9a9fe2379ee1f60b28ae45eaac67ceee81f998e69b7d10059e649d8fbd9'
CASES = {'congestedREAL121': 100, 'worksREAL47': 300, 'GRID_ARTIFICIAL': 300}
GROUPS = ('operations', 'nontravelling', 'moved', 'queue', 'junction',
          'waiting', 'stationary-trip', 'other')
REGIONS = ('route', 'geometry', 'shared', 'entry', 'pose', 'blocked', 'junction', 'drive')
KV = re.compile(r'([A-Za-z][A-Za-z0-9]*)=([^\s]+)')


def entries(log, prefix):
    return [dict(KV.findall(line)) for line in log.splitlines() if line.startswith(prefix+' ')]


def at(log, prefix, field, boundary):
    hits = [r for r in entries(log, prefix) if int(r[field]) == boundary]
    return hits


def cost(log, boundary):
    rows = at(log, 'PR57_VEHICLE_COST', 'slices', boundary)
    assert len(rows) == len(GROUPS) and {r['group'] for r in rows} == set(GROUPS)
    return {r['group']: D(r['allocMB']) for r in rows}


def region(log, boundary):
    rows = at(log, 'PR57_ALLOC_REGIONS', 'calls', boundary)
    assert len(rows) == 1
    return {name: D(rows[0][name+'MB']) for name in REGIONS}


def junction_detail(log, boundary):
    rows = at(log, 'PR57_JUNCTION_DETAIL', 'calls', boundary)
    assert len(rows) == 1
    return {name: D(rows[0][name+'MB']) for name in ('manoeuvre','areaOwner',
        'bendHolder','bendPriority','preference','turnWay','bodyOnWay','other')}


def fmt(value):
    return f'{value:.3f}'


def check_zip(z):
    assert z.testzip() is None and len(z.namelist()) == 37
    summary = json.loads(z.read('summary.json'))
    assert summary['experiment'] == 522 and summary['observational'] is True
    assert summary['gainAccepted'] is False
    for case, steps in CASES.items():
        starts, ends = [], []
        for arm in ('A1', 'B1', 'B2', 'A2'):
            base = f'{case}/{arm}/'
            start = z.read(base+'checkpoint-0.json')
            end = z.read(base+'checkpoint-1.json')
            starts.append(start); ends.append(end)
            log = z.read(base+'full.log').decode('utf-8')
            assert 'INVARIANTS OK' in log
            profile = entries(log, 'STEP_PROFILE')
            assert len(profile) == 1 and int(profile[0]['steps']) == steps
            meta = summary['cases'][case]['arms'][arm]
            assert hashlib.sha256(start).hexdigest() == meta['initialSha256']
            assert hashlib.sha256(end).hexdigest() == meta['finalSha256']
            if arm.startswith('A'):
                assert not entries(log, 'PR57_VEHICLE_COST')
        assert len(set(starts)) == len(set(ends)) == 1


def bound(log, start, end):
    end_cost = cost(log, end)
    start_cost = cost(log, start) if start else {k:D(0) for k in GROUPS}
    end_region = region(log, end)
    start_region = region(log, start) if start else {k:D(0) for k in REGIONS}
    end_detail = junction_detail(log, end)
    start_detail = junction_detail(log, start) if start else {k:D(0) for k in end_detail}
    delta_cost = {k:end_cost[k]-start_cost[k] for k in GROUPS}
    delta_region = {k:end_region[k]-start_region[k] for k in REGIONS}
    delta_detail = {k:end_detail[k]-start_detail[k] for k in end_detail}
    queue = delta_cost['queue']+delta_cost['waiting']
    nonqueue = sum((v for k,v in delta_cost.items() if k not in ('queue','waiting')),D(0))
    # Each printed region MB has 2 decimal places, each group allocMB has 3.
    # Outward rounding: 2 endpoints for each delta, 6 nonqueue groups.
    region_error = D('0.010') if start else D('0.005')
    nonqueue_error = D('0.006') if start else D('0.003')
    queue_error = D('0.002') if start else D('0.001')
    def minimum(keys, detail=False):
        source = delta_detail if detail else delta_region
        err = (D('0.010') if start else D('0.005'))*len(keys)
        return max(D(0),sum((source[k] for k in keys),D(0))-err-nonqueue-nonqueue_error)
    junction_min = minimum(['junction'])
    entry_min = minimum(['entry'])
    combined_min = minimum(['entry','junction'])
    pref_body_min = minimum(['preference','bodyOnWay'],True)
    assert queue > 0 and all(x <= queue+queue_error for x in (junction_min,entry_min,combined_min,pref_body_min))
    # Source-code verified: entry region (before lane scan) and junction region
    # (after blocked branch) are sequential, non-overlapping scopes; details
    # preference and bodyOnWay are separate scopes within admission.
    return dict(queue=queue, nonqueue=nonqueue, junction=delta_region['junction'],
                entry=delta_region['entry'], preference=delta_detail['preference'],
                body=delta_detail['bodyOnWay'], junction_min=junction_min,
                entry_min=entry_min, combined_min=combined_min,
                pref_body_min=pref_body_min,
                combined_pct=D(100)*combined_min/(queue+queue_error),
                junction_pct=D(100)*junction_min/(queue+queue_error))


def main(path):
    data = Path(path).read_bytes()
    assert hashlib.sha256(data).hexdigest() == DIGEST
    with zipfile.ZipFile(path) as z:
        check_zip(z)
        for case, start, end in [('congestedREAL121',300,600),
                                 ('worksREAL47',0,300),('GRID_ARTIFICIAL',0,300)]:
            reports = []
            for arm in ('B1','B2'):
                log = z.read(f'{case}/{arm}/full.log').decode('utf-8')
                reports.append(bound(log,start,end))
            assert reports[0] == reports[1], (case,reports)
            r = reports[0]
            print(f'{case} B1=B2 slices={start}..{end} queueMB={fmt(r["queue"])} '
                  f'nonqueueMB={fmt(r["nonqueue"])} junctionRegionMB={fmt(r["junction"])} '
                  f'entryRegionMB={fmt(r["entry"])} '
                  f'queueJunctionLowerMB={fmt(r["junction_min"])} '
                  f'queueJunctionLowerPct={r["junction_pct"]:.2f} '
                  f'queueEntryPlusJunctionLowerMB={fmt(r["combined_min"])} '
                  f'queueEntryPlusJunctionLowerPct={r["combined_pct"]:.2f} '
                  f'queuePreferencePlusBodyLowerMB={fmt(r["pref_body_min"])}')
            if case == 'congestedREAL121':
                assert r['junction_min'] > D(46) and r['combined_min'] > D(62)
                assert r['combined_pct'] > D(62)
    print('PR57_526_AUTHENTIC_QUEUE_REGION_BOUND_PASS')


if __name__ == '__main__':
    assert len(sys.argv) == 2, __doc__
    main(sys.argv[1])
