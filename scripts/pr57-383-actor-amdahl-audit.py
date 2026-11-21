#!/usr/bin/env python3
"""PR57 #383: read-only Amdahl oracle from the original #382 REAL ZIP.
Usage: python3 scripts/pr57-383-actor-amdahl-audit.py artifact.zip
No simulator run, no state skipping, no accepted throughput gain.
"""
import hashlib
import json
import sys
import zipfile

GROUPS = ('operations','nontravelling','moved','queue','junction','waiting','stationary-trip','other')

def rows(z, arm, prefix):
    return [dict(t.split('=',1) for t in line.split()[1:] if '=' in t)
            for line in z.read(f'{arm}/full.log').decode().splitlines()
            if line.startswith(prefix+' ')]

def census(z, arm):
    r=rows(z,arm,'PR57_VEHICLE_COST')
    assert len(r)==10080
    by={}
    for x in r:
        s=int(x['slices']);g=x['group']
        assert s%60==0 and g in GROUPS and g not in by.setdefault(s,{})
        by[s][g]=(int(x['calls']),float(x['ms']),int(x['claimChanges']),int(x['spaceCountChanges']))
    assert sorted(by)==list(range(60,75601,60))
    assert all(set(v)==set(GROUPS) and sum(x[0] for x in v.values())==98*s for s,v in by.items())
    return by

def delta(data, start, end):
    assert start%60==end%60==0 and 0<=start<end<=75600
    return {g:tuple(data[end][g][i]-(data[start][g][i] if start else 0) for i in range(4)) for g in GROUPS}

def main(path):
    with open(path,'rb') as f: digest=hashlib.sha256(f.read()).hexdigest()
    assert digest=='a1fcbf649db20d58759c065192325346040deb546bfbcfe0a377df8d43b1e5ac'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist())==8
        a,b=census(z,'control'),census(z,'regime')
        assert all(a[s][g][i]==b[s][g][i] for s in a for g in GROUPS for i in (0,2,3))
        save=z.read('control/checkpoint-10800.json')
        assert save==z.read('regime/checkpoint-10800.json') and len(save)==2254838
        h=rows(z,'regime','PR57_337_HOUR')
        assert len(h)==6 and sum(int(x['slices']) for x in h)==75619
        assert all(int(x['living'])==121 for x in h)
        wall=sum(float(x['wallMs']) for x in h)
        total=delta(b,0,75600)
        actor=sum(x[1] for x in total.values())
        queue_wait=total['queue'][1]+total['waiting'][1]
        assert 0<queue_wait<actor<wall
        budget=10800*17.1116/1000
        windows={}
        for name,start,end in [('early',0,600),('late',75000,75600)]:
            w=delta(b,start,end);t=sum(x[1] for x in w.values());q=w['queue'][1]+w['waiting'][1]
            windows[name]={'actorMs':round(t,3),'queueWaitingMs':round(q,3),
                           'queueWaitingPct':round(100*q/t,3),'movedCalls':w['moved'][0]}
        print(json.dumps({'sourceZipSha256':digest,'saveSha256':hashlib.sha256(save).hexdigest(),
                          'Advance2Calls':10800,'observedSlices':75600,'totalSlices':75619,'observedVehicleVisits':75600*98,
                          'wallMs':round(wall,3),'actorMs':round(actor,3),
                          'queueWaitingMs':round(queue_wait,3),
                          'queueWaitingShareOfWallPct':round(100*queue_wait/wall,3),
                          'queueWaitingShareOfActorPct':round(100*queue_wait/actor,3),
                          'oracleQueueWaitingFree':{'ceilingX':round(wall/(wall-queue_wait),3),
                                                    'remainingVsTargetX':round((wall-queue_wait)/budget,2)},
                          'oracleAllMeasuredActorFree':{'ceilingX':round(wall/(wall-actor),3),
                                                'remainingVsTargetX':round((wall-actor)/budget,2)},
                          'windows600':windows,'acceptedSpeedup':False,
                          'scope':'INTRUSIVE_POST_HOC_CAUSE_GROUPS_NOT_CAUSAL_CERTIFICATES'},indent=2))

if __name__=='__main__':
    main(sys.argv[1])
