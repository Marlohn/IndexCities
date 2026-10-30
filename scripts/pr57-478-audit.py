#!/usr/bin/env python3
"""Audit original PR57 #477 partial + #478 midnight ZIPs; no speedup claim."""
import hashlib,json,re,sys,zipfile

CASES=[
 ('worksREAL68',100,47,42,'b7c52ab87928e7eaa93cfc557b9f3dc935d5b17da44820b405990071940f95b5','686ed27e0cc458d3d8f411a1e308a2dc98e87bf588c12d5dcf1ea787bad1c883','47a4cf00bfa03fc05e15081b8b8d79a04e6dddde490c3488fd5be76570c2c1b7'),
 ('midnightREAL121',1800,121,98,'e31ad0aa321c8aa4272dd400e83575c5a600677ecb8a83879287c654eff1ce92','4d65cec7c3e2d77af757e08f0c30bea300dc3da4d7218a2807cdc3e8c4c7797c','7130344249d2958244dd08906eea5cfc74bb0585cd14a772a5e4c9ce9aff6902')]
ARMS=('A1','B1','B2','A2')
STABLE=('calls','buildings','wages','farmReady','farmFull','farmProduced','serviceVehicles')
TIMERS=('staffTicks','loopTicks','mechanicTicks')
def sha(b):return hashlib.sha256(b).hexdigest()

def audit(path,case):
 name,steps,alive,vehicles,inputsha,endsha,zipsha=case
 assert sha(open(path,'rb').read())==zipsha
 with zipfile.ZipFile(path) as z:
  assert z.testzip() is None
  expected={f'{name}/{arm}/{f}' for arm in ARMS for f in ('checkpoint-0.json','checkpoint-1.json','full.log')}
  if name=='midnightREAL121':
   expected.add('summary.json')
   summary=json.loads(z.read('summary.json'))
   assert summary['experiment']==478 and summary['allAdvance2'] and summary['observationOnly'] and not summary['gainAccepted']
   assert summary['cases'][name]['fullJsonParity'] and summary['cases'][name]['steps']==steps
  assert set(z.namelist())==expected
  starts=[];ends=[];rows=[];windows=[]
  for arm in ARMS:
   pre=f'{name}/{arm}/'
   before=z.read(pre+'checkpoint-0.json');after=z.read(pre+'checkpoint-1.json')
   assert sha(before)==inputsha and sha(after)==endsha
   state=json.loads(before)
   assert sum(bool(c['Alive']) for c in state['Citizens'])==alive and len(state['Vehicles'])==vehicles
   log=z.read(pre+'full.log').decode()
   assert 'INVARIANTS OK' in log and 'SAVE ' in log
   window=[s for s in log.splitlines() if s.startswith('WINDOW ')]
   assert len(window)==1 and f'seconds={steps*2} ' in window[0]
   census=[s for s in log.splitlines() if s.startswith('PR57_477_BUSINESS ')]
   assert len(census)==int(arm.startswith('B'))
   if census:
    row={k:int(v) for k,v in re.findall(r'([A-Za-z][A-Za-z0-9]*)=([0-9]+)',census[0])}
    assert row['calls']==steps and row['totalTicks']==sum(row[k] for k in TIMERS)
    rows.append(row)
   if name=='midnightREAL121':
    arm_summary=summary['cases'][name]['arms'][arm]
    assert arm_summary['sha256']==endsha and arm_summary['bytes']==len(after)
    assert arm_summary['census']==(census[0] if census else None) and arm_summary['window']==window[0]
   starts.append(before);ends.append(after);windows.append(window[0])
  assert len(rows)==2 and all(x==starts[0] for x in starts) and all(x==ends[0] for x in ends)
  assert all(rows[0][k]==rows[1][k] for k in STABLE)
  if name=='midnightREAL121':
   a=json.loads(starts[0]);b=json.loads(ends[0])
   assert (a['Now'],b['Now'])==('2026-01-07T23:59:58','2026-01-08T00:59:58')
   assert len(b['Ledger'])-len(a['Ledger'])==261
   assert sum(len(c['History']) for c in b['Citizens'])-sum(len(c['History']) for c in a['Citizens'])==81
   assert sum(x['WagesAccrued']!=y['WagesAccrued'] for x,y in zip(a['Citizens'],b['Citizens']))==63
  mean_ms={k:round((rows[0][k]+rows[1][k])*500/rows[0]['frequency'],6) for k in TIMERS+('totalTicks',)}
  return {'case':name,'steps':steps,'stable_counts':{k:rows[0][k] for k in STABLE},'mean_intrusive_ms':mean_ms,'final_sha256':endsha,'zip_sha256':zipsha,'full_json_parity':True,'windows':windows}

if __name__=='__main__':
 if len(sys.argv)!=3:raise SystemExit('usage: pr57-478-audit.py pr57-477-original.zip pr57-478-original.zip')
 print(json.dumps([audit(p,c) for p,c in zip(sys.argv[1:],CASES)],indent=2,sort_keys=True))
 print('PR57_478_INDEPENDENT_ABBA_AUDIT_PASS')
