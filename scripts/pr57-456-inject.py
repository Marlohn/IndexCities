#!/usr/bin/env python3
"""#456: strictly observational per-invocation pose/prefix census of CanTurnNow.

Injected only into disposable CI checkout. Every canonical reader, negative
candidate, pose, Penetration, first-blocker decision and Advance(2) is retained.
No cache, no reordered scan, no altered product, no changed public signatures.
"""
from pathlib import Path
root = Path(__file__).resolve().parents[1]

def once(path, old, new):
    s = path.read_text(encoding='utf-8')
    assert s.count(old) == 1, (str(path), old[:100], s.count(old))
    path.write_text(s.replace(old, new, 1), encoding='utf-8')

mob = root / 'src/IndexCities.Simulation/Mobility.cs'
once(mob, '&&CanTurnNow(o)).ToList();', '&&Pr57456PreferenceCanTurn(o)).ToList();')
once(mob, '        Pr57Stage249EndSlice();',
     '        Pr57Stage249EndSlice();\n        Pr57456EndSlice();')
shared = root / 'src/IndexCities.Simulation/Mobility.SharedSpace.cs'
once(shared, '    bool CanTurnNow(Vehicle o)\n    {', '''    // #456: only observational counters; no result or state is reused.
    static readonly bool Pr57456Enabled = Environment.GetEnvironmentVariable("PR57_456_POSE_PREFIX")=="1";
    bool _pr57456InPreference, _pr57456BodyProbe;
    long _pr57456Slices,_pr57456Calls,_pr57456TurnPoses,_pr57456BodyCalls,
         _pr57456BodyPoses,_pr57456Visits,_pr57456Eligible,_pr57456Near,
         _pr57456Hits,_pr57456Misses,_pr57456HitPrefixVisits,
         _pr57456HitPrefixEligible,_pr57456HitPrefixNear,_pr57456MaxPoses,
         _pr57456MaxVisits,_pr57456MaxNear;
    readonly Dictionary<int,long> _pr57456Blockers=new();
    bool Pr57456PreferenceCanTurn(Vehicle o)
    {
        if(!Pr57456Enabled)return CanTurnNow(o);
        var old=_pr57456InPreference;
        _pr57456InPreference=true;
        try{return CanTurnNow(o);}finally{_pr57456InPreference=old;}
    }
    Vehicle? Pr57456ObserveBodyOnWay(Vehicle o,List<(Vehicle Body,Point Position,double Heading)> poses)
    {
        if(!Pr57456Enabled||!_pr57456InPreference)return BodyOnWay(o,poses);
        var old=_pr57456BodyProbe;
        _pr57456BodyProbe=true;
        try{return BodyOnWay(o,poses);}finally{_pr57456BodyProbe=old;}
    }
    void Pr57456EndSlice()
    {
        if(!Pr57456Enabled)return;
        _pr57456Slices++;
        // One final cumulative row per physical slice; the audit reads the last.
        Console.WriteLine($"PR57_456_SUM slices={_pr57456Slices} calls={_pr57456Calls} turnPoses={_pr57456TurnPoses} bodyCalls={_pr57456BodyCalls} bodyPoses={_pr57456BodyPoses} visits={_pr57456Visits} eligible={_pr57456Eligible} near={_pr57456Near} hits={_pr57456Hits} misses={_pr57456Misses} hitPrefixVisits={_pr57456HitPrefixVisits} hitPrefixEligible={_pr57456HitPrefixEligible} hitPrefixNear={_pr57456HitPrefixNear} maxPoses={_pr57456MaxPoses} maxVisits={_pr57456MaxVisits} maxNear={_pr57456MaxNear} blockers={string.Join(";",_pr57456Blockers.OrderBy(x=>x.Key).Select(x=>$"{x.Key}:{x.Value}"))} observational=True");
    }
    bool CanTurnNow(Vehicle o)
    {''')
once(shared, '        var turn=TurnWay(o,out _);\n', '''        var turn=TurnWay(o,out _);
        if(Pr57456Enabled&&_pr57456InPreference){_pr57456Calls++;_pr57456TurnPoses+=turn.Count;}
''')
once(shared, '        var body=BodyOnWay(o,turn);\n',
     '        var body=Pr57456ObserveBodyOnWay(o,turn);\n')
once(shared, '        if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;\n', '''        var pr57456Probe=Pr57456Enabled&&_pr57456BodyProbe;
        var pr57456Visits=0;var pr57456Eligible=0;var pr57456Near=0;
        if(pr57456Probe){_pr57456BodyCalls++;_pr57456BodyPoses+=poses.Count;
            if(poses.Count>_pr57456MaxPoses)_pr57456MaxPoses=poses.Count;}
        if(poses.Count==0){if(pr57456Probe)_pr57456Misses++;return null;}
        var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;
''')
once(shared, '''        foreach(var o in State.Vehicles){
            if(census){visited++;Pr57306Read(v,o);}''', '''        foreach(var o in State.Vehicles){
            if(pr57456Probe){_pr57456Visits++;pr57456Visits++;}
            if(census){visited++;Pr57306Read(v,o);}''')
once(shared, '''            if(census)eligible++;
            var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);''', '''            if(pr57456Probe){_pr57456Eligible++;pr57456Eligible++;}
            if(census)eligible++;
            var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);''')
once(shared, '''            foreach(var p in poses)if(p.Position.Distance(o.Position)<12){
                if(census){geometry++;tested=true;}''', '''            foreach(var p in poses)if(p.Position.Distance(o.Position)<12){
                if(pr57456Probe){_pr57456Near++;pr57456Near++;}
                if(census){geometry++;tested=true;}''')
once(shared, '''                    if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);
                    return o;''', '''                    if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);
                    if(pr57456Probe){
                        _pr57456Hits++;_pr57456HitPrefixVisits+=pr57456Visits;
                        _pr57456HitPrefixEligible+=pr57456Eligible;
                        _pr57456HitPrefixNear+=pr57456Near;
                        if(pr57456Visits>_pr57456MaxVisits)_pr57456MaxVisits=pr57456Visits;
                        if(pr57456Near>_pr57456MaxNear)_pr57456MaxNear=pr57456Near;
                        _pr57456Blockers[o.Id]=_pr57456Blockers.GetValueOrDefault(o.Id)+1;
                    }
                    return o;''')
once(shared, '''        if(census)Pr57305Body(v,null,poses.Count,visited,eligible,nearNegative,geometry,started);
        return null;''', '''        if(census)Pr57305Body(v,null,poses.Count,visited,eligible,nearNegative,geometry,started);
        if(pr57456Probe){_pr57456Misses++;
            if(pr57456Visits>_pr57456MaxVisits)_pr57456MaxVisits=pr57456Visits;
            if(pr57456Near>_pr57456MaxNear)_pr57456MaxNear=pr57456Near;}
        return null;''')
print('PR57_456_INJECTED observational=True canonicalBodyOnWay=True')
