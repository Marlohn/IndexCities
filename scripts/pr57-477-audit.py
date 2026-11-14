#!/usr/bin/env python3
"""Audit original PR57 #477 artifact; timing is observational, not speedup."""
import hashlib, json, re, sys, zipfile

CASES = {
    'worksREAL68': (100, 47, 42, 'b7c52ab87928e7eaa93cfc557b9f3dc935d5b17da44820b405990071940f95b5'),
    'midnightREAL121': (1800, 121, 98, 'e31ad0aa321c8aa4272dd400e83575c5a600677ecb8a83879287c654eff1ce92'),
}
ORDER=('A1','B1','B2','A2')
EXPECTED_ZIP='47a4cf00bfa03fc05e15081b8b8d79a04e6dddde490c3488fd5be76570c2c1b7'
EXPECTED_BASELINE='ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a'
EXPECTED_WORKS_FINAL='686ed27e0cc458d3d8f411a1e308a2dc98e87bf588c12d5dcf1ea787bad1c883'

def fields(line):
    return dict(re.findall(r'([A-Za-z][A-Za-z0-9]*)=([^ ]+)',line))

def main(path,baseline=None):
    raw=open(path,'rb').read()
    digest=hashlib.sha256(raw).hexdigest()
    assert digest==EXPECTED_ZIP,(digest,'artifact sha')
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        names=set(z.namelist())
        summary=json.loads(z.read('summary.json')) if 'summary.json' in names else None
        if summary:
            assert summary['experiment']==477 and summary['allAdvance2']
            assert summary['observationOnly'] and not summary['gainAccepted']
        results={}
        for name,(steps,alive,vehicles,source_sha) in CASES.items():
            if not any(n.startswith(name+'/') for n in names):
                results[name]={'status':'not_executed'}
                continue
            arms={}; end_bytes=[]; source_bytes=[]
            for arm in ORDER:
                prefix=f'{name}/{arm}/'
                if prefix+'full.log' not in names:
                    arms[arm]={'status':'not_executed'}
                    continue
                log=z.read(prefix+'full.log').decode()
                before=z.read(prefix+'checkpoint-0.json')
                after=z.read(prefix+'checkpoint-1.json')
                assert hashlib.sha256(before).hexdigest()==source_sha
                state=json.loads(before)
                assert sum(bool(c['Alive']) for c in state['Citizens'])==alive
                assert len(state['Vehicles'])==vehicles
                assert 'INVARIANTS OK' in log and 'SAVE ' in log
                assert len([x for x in log.splitlines() if x.startswith('WINDOW ')])==1
                census=[x for x in log.splitlines() if x.startswith('PR57_477_BUSINESS ')]
                assert len(census)==int(arm.startswith('B'))
                if census:
                    row=fields(census[0]); assert int(row['calls'])==steps
                    assert int(row['totalTicks'])==sum(int(row[k]) for k in ('staffTicks','loopTicks','mechanicTicks'))
                    assert int(row['buildings'])>=int(row['farmReady'])>=int(row['farmProduced'])
                    arms[arm]={'census':{k:int(v) for k,v in row.items()},'sha256':hashlib.sha256(after).hexdigest()}
                else:
                    arms[arm]={'sha256':hashlib.sha256(after).hexdigest()}
                end_bytes.append(after);source_bytes.append(before)
            assert all(x==source_bytes[0] for x in source_bytes)
            assert all(x==end_bytes[0] for x in end_bytes)
            if name=='worksREAL68':
                assert hashlib.sha256(end_bytes[0]).hexdigest()==EXPECTED_WORKS_FINAL
                if baseline:
                    original=open(baseline,'rb').read()
                    assert hashlib.sha256(original).hexdigest()==EXPECTED_BASELINE
                    with zipfile.ZipFile(baseline) as old:
                        assert old.testzip() is None
                        assert old.read('pr57-445/A/checkpoint-1.json')==end_bytes[0]
            if all(a in arms and 'census' in arms[a] for a in ('B1','B2')):
                for key in ('calls','buildings','wages','farmReady','farmFull','farmProduced','serviceVehicles'):
                    assert arms['B1']['census'][key]==arms['B2']['census'][key],(name,key)
            results[name]={'arms':arms,'parity':True,'steps':steps}
        print(json.dumps({'zip_sha256':digest,'summary_present':bool(summary),'results':results},indent=2,sort_keys=True))
        print('PR57_477_AUDIT_PASS')

if __name__=='__main__':
    if len(sys.argv) not in (2,3): raise SystemExit('usage: pr57-477-audit.py pr57-477-first.zip [pr57-445-original.zip]')
    main(sys.argv[1],sys.argv[2] if len(sys.argv)==3 else None)
