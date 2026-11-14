#!/usr/bin/env python3
"""PR57 #510: independent audit of authentic #509 ABBA ZIP; no optimization claim."""
import hashlib
import re
import statistics
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP_SHA='fbfac28dc3a11f2e2d321424976ce00f83703e578035bd292a10a95b29366a81'
ARMS=('A1','B1','B2','A2')
PREFIX='PR57_509_WRITER_GUARD '
INPUT_SHA='779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
FINAL_SHA='9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23'
FIELDS=('writerChanges','subscriptions','guardChecks','stableChecks','bodyNegativeToEligible','trafficNegativeToEligible')

def main(path):
    data=Path(path).read_bytes();digest=hashlib.sha256(data).hexdigest()
    assert digest==EXPECTED_ZIP_SHA,digest
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert len(z.namelist())==13
        starts=[z.read(f'{arm}/checkpoint-0.json') for arm in ARMS]
        ends=[z.read(f'{arm}/checkpoint-1.json') for arm in ARMS]
        assert all(x==starts[0] for x in starts)
        assert all(x==ends[0] for x in ends)
        assert hashlib.sha256(starts[0]).hexdigest()==INPUT_SHA
        assert hashlib.sha256(ends[0]).hexdigest()==FINAL_SHA
        logs={arm:z.read(f'{arm}/full.log').decode() for arm in ARMS}
        rows={arm:[line for line in logs[arm].splitlines() if line.startswith(PREFIX)] for arm in ARMS}
        assert rows['A1']==rows['A2']==[]
        assert rows['B1']==rows['B2'] and len(rows['B1'])>1
        parsed=[{k:int(v) for k,v in re.findall(r'\b(\w+)=(\d+)',line)} for line in rows['B1']]
        assert all(1<=r['slice']<=600 and r['stableChecks']<=r['guardChecks'] for r in parsed)
        assert any(r['writerChanges']>0 for r in parsed)
        for arm in ARMS:
            log=logs[arm]
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            assert 'STEP_PROFILE steps=100 ' in log
            profile=next(x for x in log.splitlines() if x.startswith('VEHICLE_PROFILE '))
            assert re.search(r'\bslices=618\b',profile),profile
        measurements={}
        for arm in ARMS:
            window=next(x for x in logs[arm].splitlines() if x.startswith('WINDOW 1 '))
            measurements[arm]={k:float(re.search(r'\b'+k+r'=([0-9.]+)',window).group(1)) for k in ('wall','cpu','allocMB','gc0')}
        print('PR57_510_INDEPENDENT_REAL_WRITER_GUARD_AUDIT_PASS')
        print('artifact_sha256',digest,'arms',ARMS,'rows',len(parsed),'distinct_slices',len({r['slice'] for r in parsed}))
        print('checkpoint_start_sha256',INPUT_SHA,'checkpoint_end_sha256',FINAL_SHA)
        for k in FIELDS:
            xs=[r[k] for r in parsed]
            print(k,'sum',sum(xs),'median',statistics.median(xs),'max',max(xs),'positive_rows',sum(x>0 for x in xs))
        for k in ('wall','cpu','allocMB','gc0'):
            a=statistics.mean(measurements[x][k] for x in ('A1','A2'))
            b=statistics.mean(measurements[x][k] for x in ('B1','B2'))
            print('cost',k,'control',a,'probe',b,'ratio',round(b/a,5))
        print('LIMIT: only one position/waiting writer; only previously subscribed same-slice readers; no transient writer coverage, no reuse, no performance gain.')

if __name__=='__main__':
    assert len(sys.argv)==2,'usage: python3 pr57-510-audit-writer-guard.py artifact.zip'
    main(sys.argv[1])
