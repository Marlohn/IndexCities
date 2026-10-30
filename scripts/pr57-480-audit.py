#!/usr/bin/env python3
"""Independent PR57 #480 artifact audit. Input is the ORIGINAL Actions ZIP."""
import hashlib, json, re, sys, zipfile

EXPECTED_ZIP = 'ad5093fd3e605733cf12a203c7be2471d5e55e1a8c797958f66d0ac50c5bdbe4'
EXPECTED_INPUT = '779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f'
EXPECTED_FINAL = '9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4a4d0365522cca23'
COUNTERS = ('slices','calls','pairs','sameActor','samePoses','sameCandidates',
            'sameObserved','sameBlocker','sameTime','sameVisitCount','allVisits','matchedVisits')

def digest(blob): return hashlib.sha256(blob).hexdigest()

def main(path):
    assert digest(open(path,'rb').read()) == EXPECTED_ZIP
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        names=set(z.namelist())
        assert names == {'summary.json'} | {
            f'{arm}/{file}' for arm in ('A1','B1','B2','A2')
            for file in ('checkpoint-0.json','checkpoint-1.json','full.log')}
        summary=json.loads(z.read('summary.json'))
        assert summary['experiment']==480 and summary['allAdvance2']
        assert summary['callsAdvance2PerArm']==100
        assert summary['inputSha256']==EXPECTED_INPUT
        assert summary['finalSha256']==EXPECTED_FINAL
        assert summary['fullSaveByteParity'] and not summary['acceptedGain']
        rows_by_arm={}
        for arm in ('A1','B1','B2','A2'):
            before=z.read(f'{arm}/checkpoint-0.json')
            after=z.read(f'{arm}/checkpoint-1.json')
            assert digest(before)==EXPECTED_INPUT
            assert digest(after)==EXPECTED_FINAL
            state=json.loads(before)
            assert len(state['Vehicles'])==98
            assert sum(bool(c['Alive']) for c in state['Citizens'])==121
            log=z.read(f'{arm}/full.log').decode()
            assert 'INVARIANTS OK' in log and 'SAVE ' in log
            windows=[x for x in log.splitlines() if x.startswith('WINDOW ')]
            assert len(windows)==1 and re.search(r'\bseconds=200\b',windows[0])
            assert windows[0]==summary['arms'][arm]['window']
            rows=[x for x in log.splitlines() if x.startswith('PR57_480_SUM ')]
            if arm.startswith('A'):
                assert not rows
                continue
            assert len(rows)==718
            parsed=[{k:int(v) for k,v in re.findall(r'([A-Za-z][A-Za-z0-9]*)=(\d+)',row)} for row in rows]
            assert all(set(COUNTERS)<=set(row) for row in parsed)
            assert [row['slices'] for row in parsed]==list(range(1,719))
            for key in COUNTERS:
                assert all(a[key]<=b[key] for a,b in zip(parsed,parsed[1:])),key
            final=parsed[-1]
            assert final==summary['arms'][arm]['census']
            assert rows[-1]==summary['arms'][arm]['lastRow']
            assert final['calls']==1596 and final['pairs']==1336
            assert final['sameObserved']==902 and final['sameCandidates']==905
            assert final['sameActor']==1330 and final['samePoses']==1336
            assert final['sameBlocker']==1336 and final['sameTime']==0
            assert final['allVisits']==97156 and final['matchedVisits']==49948
            assert final['sameObserved']<=min(final['sameActor'],final['samePoses'],final['sameCandidates'])
            rows_by_arm[arm]=parsed
        assert rows_by_arm['B1']==rows_by_arm['B2']
    print(json.dumps({'zip_sha256':EXPECTED_ZIP,'input_sha256':EXPECTED_INPUT,
          'final_sha256':EXPECTED_FINAL,'pairs':1336,'same_observed':902,
          'observed_repeat_percent':round(100*902/1336,3),
          'changed_candidate_snapshots':1336-905,
          'candidate_changed_percent':round(100*(1336-905)/1336,3),
          'time_changed_pairs':1336,'accepted_gain':False},sort_keys=True))
    print('PR57_480_INDEPENDENT_ABBA_AUDIT_PASS')

if __name__=='__main__':
    if len(sys.argv)!=2:raise SystemExit('usage: pr57-480-audit.py original.zip')
    main(sys.argv[1])
