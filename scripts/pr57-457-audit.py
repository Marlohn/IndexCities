#!/usr/bin/env python3
"""Independent audit of original PR57 #456 Action ZIP (read-only)."""
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP = '70afb0c0fe0bf13e1cb8b62a1b74f621eb2a1cc084742a8b9fb00d60c4e38064'
EXPECTED_FINAL = 'bf05b6ed3650bdc91fedf5a9c0e66c6c13b7e6de19f04a250a1b561a9c177c1f'
KEYS = ('slices','calls','turnPoses','bodyCalls','bodyPoses','visits','eligible',
        'near','hits','misses','hitPrefixVisits','hitPrefixEligible','hitPrefixNear',
        'maxPoses','maxVisits','maxNear')

def audit(path):
    raw=Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest()==EXPECTED_ZIP
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        assert len(z.namelist())==8
        summary=json.loads(z.read('pose-prefix-456/summary.json'))
        assert summary['experiment']==456 and summary['allAdvance2'] is True
        assert summary['callsAdvance2PerArm']==1800 and summary['living']==121
        assert summary['vehicles']==98 and summary['fullJsonParity'] is True
        starts=[z.read(f'pose-prefix-456/{arm}/checkpoint-0.json') for arm in 'AB']
        finals=[z.read(f'pose-prefix-456/{arm}/checkpoint-1.json') for arm in 'AB']
        assert starts[0]==starts[1] and finals[0]==finals[1]
        assert hashlib.sha256(starts[0]).hexdigest()==summary['inputSha256']
        assert hashlib.sha256(finals[0]).hexdigest()==summary['finalSha256']==EXPECTED_FINAL
        rows=[]
        for arm in 'AB':
            log=z.read(f'pose-prefix-456/{arm}/full.log').decode()
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            window=[x for x in log.splitlines() if x.startswith('WINDOW ')]
            assert len(window)==1 and window[0]==summary['arms'][arm]['window']
            assert re.search(r'\bseconds=3600\b',window[0])
            census=[x for x in log.splitlines() if x.startswith('PR57_456_SUM ')]
            if arm=='A':assert census==[]
            else:rows=census
        assert len(rows)==12618
        previous=None
        for i,row in enumerate(rows,1):
            d={k:int(v) for k,v in re.findall(r'([A-Za-z][A-Za-z0-9]*)=([0-9]+)',row)}
            assert d['slices']==i and 'observational=True' in row
            assert d['calls']==d['bodyCalls']==d['hits']+d['misses']
            assert d['turnPoses']==d['bodyPoses']
            assert d['visits']>=d['eligible']>=d['hits']
            assert d['near']>=d['hits']
            assert d['hitPrefixVisits']<=d['visits']
            assert d['hitPrefixEligible']<=d['eligible']
            assert d['hitPrefixNear']<=d['near']
            if previous:
                for k in KEYS:assert d[k]>=previous[k],(i,k)
            previous=d
        assert rows[-1]==summary['arms']['B']['lastRawCensus']
        for k in KEYS:assert previous[k]==summary['arms']['B']['census'][k],k
        blocker_text=re.search(r'\bblockers=([^ ]+)',rows[-1]).group(1)
        blockers={int(k):int(v) for k,v in (p.split(':') for p in blocker_text.split(';'))}
        assert blockers=={636:20380,1511:20600}
        assert {str(k):v for k,v in blockers.items()}==summary['arms']['B']['blockers']
        assert sum(blockers.values())==previous['hits']==40980
        assert previous['misses']==0 and previous['calls']==40980
        assert previous['turnPoses']==2335860 and previous['visits']==2384980
        assert previous['eligible']==256766 and previous['near']==1981940
        assert previous['maxPoses']==59 and previous['maxVisits']==95
        assert previous['maxNear']==78
        print('PR57_457_AUDIT_PASS zipSha256='+EXPECTED_ZIP,
              'censusRows=12618 allAdvance2=True fullJsonParity=True',
              'canonicalCalls=40980 blockers='+str(blockers),
              'poses=2335860 visited=2384980 eligible=256766 nearSAT=1981940')

if __name__=='__main__':
    if len(sys.argv)!=2:raise SystemExit('usage: pr57-457-audit.py original-pr57-456.zip')
    audit(sys.argv[1])
