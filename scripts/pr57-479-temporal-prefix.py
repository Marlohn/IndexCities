#!/usr/bin/env python3
"""PR57 #479: offline temporal signature census of authentic #456 observations.

A repeated count signature is NOT evidence of identical geometry, decisions, or
safe memoization. No engine changes, no new benchmark, no fake actors.
"""
import hashlib, json, re, sys, zipfile
from collections import Counter

EXPECTED_ZIP_SHA256='70afb0c0fe0bf13e1cb8b62a1b74f621eb2a1cc084742a8b9fb00d60c4e38064'
FINAL_SHA256='bf05b6ed3650bdc91fedf5a9c0e66c6c13b7e6de19f04a250a1b561a9c177c1f'
FIELDS=('calls','turnPoses','bodyCalls','bodyPoses','visits','eligible','near','hits','misses','hitPrefixVisits','hitPrefixEligible','hitPrefixNear')
SIGNATURE=('calls','turnPoses','visits','eligible','near','hits','misses','blocker636','blocker1511')

def parse(line):
    vals={k:int(v) for k,v in re.findall(r'([A-Za-z][A-Za-z0-9]*)=(\d+)',line)}
    assert line.startswith('PR57_456_SUM ') and line.endswith('observational=True')
    raw=line.split('blockers=',1)[1].split(' observational=',1)[0]
    blockers={int(k):int(v) for k,v in re.findall(r'(\d+):(\d+)',raw)}
    assert set(blockers).issubset({636,1511}),blockers
    vals['blocker636']=blockers.get(636,0)
    vals['blocker1511']=blockers.get(1511,0)
    return vals

def runs(values):
    if not values:return []
    out=[];last=values[0];n=1
    for x in values[1:]:
        if x==last:n+=1
        else:out.append((last,n));last=x;n=1
    out.append((last,n))
    return out

def main(path):
    raw=open(path,'rb').read();assert hashlib.sha256(raw).hexdigest()==EXPECTED_ZIP_SHA256
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert len(z.namelist())==8
        assert z.read('pose-prefix-456/A/checkpoint-0.json')==z.read('pose-prefix-456/B/checkpoint-0.json')
        start=z.read('pose-prefix-456/B/checkpoint-0.json')
        end=z.read('pose-prefix-456/B/checkpoint-1.json')
        assert end==z.read('pose-prefix-456/A/checkpoint-1.json')
        assert hashlib.sha256(end).hexdigest()==FINAL_SHA256
        lines=z.read('pose-prefix-456/B/full.log').decode().splitlines()
    samples=[parse(x) for x in lines if x.startswith('PR57_456_SUM ')]
    assert len(samples)==12618
    assert [s['slices'] for s in samples]==list(range(1,12619))
    deltas=[];prev={k:0 for k in FIELDS+('blocker636','blocker1511')}
    for s in samples:
        d={k:s[k]-prev[k] for k in prev}
        assert all(v>=0 for v in d.values())
        assert d['calls']==d['hits']+d['misses']
        assert d['hits']==d['blocker636']+d['blocker1511']
        assert d['turnPoses']==d['bodyPoses'] and d['calls']==d['bodyCalls']
        deltas.append(d);prev={k:s[k] for k in prev}
    assert samples[-1]['calls']==40980 and samples[-1]['hits']==40980
    assert samples[-1]['turnPoses']==2335860 and samples[-1]['near']==1981940
    nonzero=[d for d in deltas if d['calls']>0]
    sigs=[tuple(d[k] for k in SIGNATURE) for d in nonzero]
    # Runs are consecutive PHYSICAL slices; don't bridge idle slices.
    full_sigs=[tuple(d[k] for k in SIGNATURE) if d['calls'] else None for d in deltas]
    active_runs=[(sig,n) for sig,n in runs(full_sigs) if sig is not None]
    uninterrupted_active_runs=runs(sigs) # Ignores idle slices; NOT a causal interval.
    a=json.loads(start);b=json.loads(end)
    va={v['Id']:v for v in a['Vehicles']};vb={v['Id']:v for v in b['Vehicles']}
    assert va.keys()==vb.keys()
    changed_vehicle_ids={i for i in va if va[i]!=vb[i]}
    longest=max((n for _,n in active_runs),default=0)
    stable_adjacent=sum(1 for a,b in zip(full_sigs,full_sigs[1:]) if a is not None and a==b)
    pairs=sum(1 for a,b in zip(full_sigs,full_sigs[1:]) if a is not None and b is not None)
    frequencies=Counter(sigs)
    top_sig,top_n=frequencies.most_common(1)[0]
    run_counts=Counter(n for _,n in active_runs)
    result={
        'source_zip_sha256':EXPECTED_ZIP_SHA256,'source_final_save_sha256':FINAL_SHA256,
        'slices':len(deltas),'active_slices':len(nonzero),'total_calls':samples[-1]['calls'],
        'blockers':{'636':samples[-1]['blocker636'],'1511':samples[-1]['blocker1511']},
        'unique_count_signatures':len(frequencies),
        'most_common_signature':dict(zip(SIGNATURE,top_sig)),
        'most_common_signature_slices':top_n,
        'most_common_signature_share_of_active_slices':round(top_n/len(nonzero),8),
        'most_common_signature_share_of_near_tests':round(top_n*top_sig[SIGNATURE.index('near')]/samples[-1]['near'],8),
        'consecutive_active_pairs':pairs,
        'consecutive_equal_count_signature_pairs':stable_adjacent,
        'consecutive_equal_count_signature_pair_fraction':round(stable_adjacent/pairs,8) if pairs else None,
        'longest_identical_count_signature_run_slices':longest,
        'equal_consecutive_active_signatures_ignoring_idle_slices':sum(x==y for x,y in zip(sigs,sigs[1:])),
        'longest_active_signature_run_ignoring_idle_slices':max(n for _,n in uninterrupted_active_runs),
        'changed_vehicle_records_between_endpoints':len(changed_vehicle_ids),
        'changed_blocker_records_between_endpoints':sum(va[i]!=vb[i] for i in (636,1511)),
        'active_runs':len(active_runs),
        'active_runs_ge_10':sum(v for n,v in run_counts.items() if n>=10),
        'active_slices_in_runs_ge_10':sum(n*v for n,v in run_counts.items() if n>=10),
        'sample_limit':'Aggregate counts only; cannot identify candidate IDs, geometry, route/heading, first-blocker order, or read/write invalidators.',
        'speedup_accepted':False,
    }
    print(json.dumps(result,indent=2,sort_keys=True))
    print('PR57_479_TEMPORAL_PREFIX_CENSUS_PASS')

if __name__=='__main__':
    if len(sys.argv)!=2:raise SystemExit('usage: pr57-479-temporal-prefix.py pr57-456-original.zip')
    main(sys.argv[1])
