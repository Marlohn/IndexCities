#!/usr/bin/env python3
"""Independent PR57 #447 artifact audit. Input: original Actions ZIP; no .NET or mutation."""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP='89887c3b15105e8ba2860390b5349dd046a365e28629b61970a085b092d17efd'
EXPECTED_FINAL='e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
EXPECTED_FILES={'A/checkpoint-10800.json','B/checkpoint-10800.json','A/full.log',
                'B/full.log','checkpoints.sha256','summary-console.log','summary.json'}

p=Path(sys.argv[1]) if len(sys.argv)==2 else None
if p is None or not p.is_file():
    raise SystemExit('usage: python3 pr57-448-audit.py ORIGINAL_447.zip')
assert hashlib.sha256(p.read_bytes()).hexdigest()==EXPECTED_ZIP,'ZIP digest'
with zipfile.ZipFile(p) as z:
    assert z.testzip() is None,'CRC'
    assert set(z.namelist())==EXPECTED_FILES,'members'
    summary=json.loads(z.read('summary.json'))
    assert summary['experiment']==447 and summary['fullJsonParity'] and summary['observationOnly']
    final_a=z.read('A/checkpoint-10800.json')
    final_b=z.read('B/checkpoint-10800.json')
    assert final_a==final_b and len(final_a)==2254838,'full checkpoint parity'
    assert hashlib.sha256(final_a).hexdigest()==EXPECTED_FINAL,'canonical final hash'
    def rows(arm,prefix):
        return [dict(item.split('=',1) for item in line.split()[1:] if '=' in item)
                for line in z.read(f'{arm}/full.log').decode().splitlines()
                if line.startswith(prefix+' ')]
    for arm in ('A','B'):
        g=rows(arm,'PR57_337_GROUP')
        h=rows(arm,'PR57_337_HOUR')
        c=rows(arm,'PR57_447_COHORT')
        ch=rows(arm,'PR57_447_HOUR')
        f=rows(arm,'PR57_337_FINAL')
        assert (len(g),len(h),len(c),len(ch),len(f))==(48,6,30 if arm=='B' else 0,6 if arm=='B' else 0,1)
        assert f[0]['parity']=='IDENTICAL'
        assert sum(int(x['calls']) for x in g)==7410662
        assert sum(int(x['allocatedBytes']) for x in g)==summary['arms'][arm]['actorBytes']
        assert sum(int(x['totalAllocatedBytes']) for x in h)==summary['arms'][arm]['processBytes']
        assert all(int(x['living'])==121 for x in h)
        if arm=='B':
            for hour in range(1,7):
                gc=[x for x in g if int(x['hour'])==hour]
                cc=[x for x in c if int(x['hour'])==hour]
                hh=[x for x in ch if int(x['hour'])==hour]
                assert len(gc)==8 and len(cc)==5 and len(hh)==1
                for field in ('calls','allocatedBytes'):
                    assert sum(int(x[field]) for x in gc)==sum(int(x[field]) for x in cc)==int(hh[0][field])
            assert sum(int(x['ticks']) for x in c)==sum(int(x['ticks']) for x in ch)
            for kind,name in enumerate(('operations','nontravelling','travellingWaiting','travellingActive','special')):
                subset=[x for x in c if int(x['kind'])==kind]
                assert len(subset)==6
                for logfield,summaryfield in (('calls','calls'),('ticks','ticks'),('allocatedBytes','allocatedBytes')):
                    assert sum(int(x[logfield]) for x in subset)==summary['arms']['B']['preCohorts'][name][summaryfield]
    b=summary['arms']['B']
    wait=b['preCohorts']['travellingWaiting']
    assert wait['calls']==1992256 and wait['allocatedBytes']==30180992744
    assert b['actorBytes']==30215004968 and b['processBytes']==36743050120
    result={'zipSha256':EXPECTED_ZIP,'crc':'PASS','members':len(EXPECTED_FILES),
            'fullCheckpointBytes':len(final_a),'finalSha256':EXPECTED_FINAL,
            'advance2PerArm':10800,'actorCallsPerArm':7410662,
            'preWaitingCalls':wait['calls'],'preWaitingBytes':wait['allocatedBytes'],
            'preWaitingShareOfActorBytesPct':round(100*wait['allocatedBytes']/b['actorBytes'],4),
            'preWaitingShareOfProcessBytesPct':round(100*wait['allocatedBytes']/b['processBytes'],4),
            'observationalOnly':True,'acceptedSpeedup':False}
    print('PR57_448_AUDIT_PASS',json.dumps(result,sort_keys=True))
