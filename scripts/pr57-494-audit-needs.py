#!/usr/bin/env python3
"""PR57 #494 independent audit of #493 artifact: no optimization is claimed."""
import hashlib, json, re, statistics, sys, zipfile
from pathlib import Path

EXPECTED_ZIP='f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
CASES={
    'congestedREAL121':(100,121,98,'779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'),
    'worksREAL47':(100,47,42,'b7c52ab87928e7eaa93cfc557b9f3dc935d5b17da44820b405990071940f95b5'),
    'GRID_ARTIFICIAL':(50,None,None,None),
}
ARMS=('A1','B1','B2','A2')

def fields(line):
    return dict(re.findall(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)',line))

def audit(path):
    raw=Path(path).read_bytes()
    assert hashlib.sha256(raw).hexdigest()==EXPECTED_ZIP,'ZIP digest'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None,'CRC failure'
        names=set(z.namelist())
        assert len(names)==37,'unexpected member count'
        assert names=={'summary.json'}|{
            f'{case}/{arm}/{file}' for case in CASES for arm in ARMS
            for file in ('checkpoint-0.json','checkpoint-1.json','full.log')},'unexpected/missing members'
        summary=json.loads(z.read('summary.json'))
        assert summary['experiment']==493 and summary['observational'] and not summary['gainAccepted']
        assert summary['cadence']=='one public Advance(2) per quantum'
        assert set(summary['cases'])==set(CASES)
        report={}
        for case,(steps,alive,vehicles,expected_input) in CASES.items():
            initials=[];finals=[];brows=[];measure=[]
            meta=summary['cases'][case]
            assert meta['stepsPerArm']==steps and meta['fullJsonParity']
            assert meta['artificial']==(case=='GRID_ARTIFICIAL')
            for arm in ARMS:
                root=f'{case}/{arm}/'
                initial=z.read(root+'checkpoint-0.json')
                final=z.read(root+'checkpoint-1.json')
                log=z.read(root+'full.log').decode()
                initials.append(initial);finals.append(final)
                assert f'STEP_PROFILE steps={steps} ' in log
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                assert len([x for x in log.splitlines() if x.startswith('WINDOW ')])==1
                assert len([x for x in log.splitlines() if x.startswith('PR57_AB_CHECKPOINT ')])==1
                state=json.loads(initial)
                if alive is not None:
                    assert sum(bool(c['Alive']) for c in state['Citizens'])==alive
                    assert len(state['Vehicles'])==vehicles
                if expected_input:
                    assert hashlib.sha256(initial).hexdigest()==expected_input
                saved=meta['arms'][arm]
                assert hashlib.sha256(initial).hexdigest()==saved['initialSha256']
                assert hashlib.sha256(final).hexdigest()==saved['finalSha256']
                assert len(final)==saved['finalBytes']
                rows=[x for x in log.splitlines() if x.startswith('PR57_493_NEEDS ')]
                assert len(rows)==int(arm.startswith('B'))
                if rows:
                    r={k:int(v) for k,v in fields(rows[0]).items() if v.isdecimal()}
                    assert r==saved['census']
                    assert r['sampledSteps']==steps//8 and r['sampledCitizens']>0
                    assert r['frequency']>0
                    assert all(r[k]>=0 for k in ('resolveTicks','serviceTicks','mathTicks','deathTicks'))
                    brows.append(r)
                details={}
                for line in log.splitlines():
                    if line.strip().startswith('STEP_DETAIL name=needs-'):
                        f=fields(line);details[f['name']]=float(f['wall'].rstrip('s'))
                assert set(details)=={'needs-pollution','needs-citizens'}
                measure.append(details)
            assert all(x==initials[0] for x in initials),'initial parity'
            assert all(x==finals[0] for x in finals),'final parity'
            assert len(brows)==2 and all(brows[0][k]==brows[1][k] for k in ('sampledSteps','sampledCitizens','frequency'))
            medians={k:round(statistics.median(1e6*b[k]/b['frequency']/b['sampledCitizens'] for b in brows),4)
                     for k in ('resolveTicks','serviceTicks','mathTicks','deathTicks')}
            report[case]={'steps':steps,'sampledCitizens':brows[0]['sampledCitizens'],
                'medianUsPerSampledCitizen':medians,
                'sampledTotalUsPerCitizen':round(sum(medians.values()),4),
                'needsCitizenDetailMsB':[x['needs-citizens']*1000 for x in measure[1:3]],
                'initialSha256':hashlib.sha256(initials[0]).hexdigest(),
                'finalSha256':hashlib.sha256(finals[0]).hexdigest()}
        print(json.dumps(report,sort_keys=True,indent=2))
        print('PR57_494_INDEPENDENT_NEEDS_ZIP_AUDIT_PASS')

if __name__=='__main__':
    audit(sys.argv[1] if len(sys.argv)>1 else '/mnt/data/pr57-493-original.zip')
