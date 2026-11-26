#!/usr/bin/env python3
"""#454: observe repeated canonical CanTurnNow calls within one physical slice.
Never cache, omit a predicate, mutate state, or treat repeated results as causal proof.
Injected only into disposable CI checkout; no engine source committed.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
def once(path,old,new):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==1,(str(path),old[:90],s.count(old))
    path.write_text(s.replace(old,new,1),encoding="utf-8")
m=root/"src/IndexCities.Simulation/Mobility.cs"
once(m,"        Pr57JunctionCensusBeginSlice(dt,operationsDt);",
        "        Pr57454BeginSlice(dt);\n        Pr57JunctionCensusBeginSlice(dt,operationsDt);")
once(m,"        Pr57Stage249EndSlice();",
        "        Pr57Stage249EndSlice();\n        Pr57454EndSlice();")
once(m,"&&CanTurnNow(o)).ToList();","&&Pr57454CountCanTurn(o)).ToList();")
s=root/"src/IndexCities.Simulation/Mobility.SharedSpace.cs"
once(s,"    bool CanTurnNow(Vehicle o)\n    {", """    // #454: read-only fanout census; the original decision is always executed.
    static readonly bool Pr57454Enabled=Environment.GetEnvironmentVariable("PR57_454_FANOUT")=="1";
    readonly Dictionary<int,bool> _pr57454Previous=new();
    readonly Dictionary<int,int> _pr57454Fanout=new();
    readonly long[] _pr57454Calls=new long[2],_pr57454Unique=new long[2],
        _pr57454Repeats=new long[2],_pr57454Flips=new long[2],
        _pr57454Allowed=new long[2],_pr57454RepeatSlices=new long[2];
    long _pr57454Slices,_pr57454MaxCalls,_pr57454MaxFanout;
    int _pr57454CurrentCalls,_pr57454CurrentRepeats;
    bool _pr57454Positive;
    void Pr57454BeginSlice(double dt)
    {
        if(!Pr57454Enabled)return;
        _pr57454Positive=dt>0;
        _pr57454Previous.Clear();_pr57454Fanout.Clear();
        _pr57454CurrentCalls=0;_pr57454CurrentRepeats=0;
    }
    bool Pr57454CountCanTurn(Vehicle o)
    {
        if(!Pr57454Enabled)return CanTurnNow(o);
        var result=CanTurnNow(o); // exactly once, canonical order
        var k=_pr57454Positive?1:0;
        _pr57454Calls[k]++;_pr57454CurrentCalls++;
        if(result)_pr57454Allowed[k]++;
        if(_pr57454Previous.TryGetValue(o.Id,out var previous))
        {
            _pr57454Repeats[k]++;_pr57454CurrentRepeats++;
            if(result!=previous)_pr57454Flips[k]++;
            _pr57454Previous[o.Id]=result;
            var count=++_pr57454Fanout[o.Id];
            if(count>_pr57454MaxFanout)_pr57454MaxFanout=count;
        }
        else
        {
            _pr57454Unique[k]++;_pr57454Previous.Add(o.Id,result);
            _pr57454Fanout.Add(o.Id,1);
            if(_pr57454MaxFanout<1)_pr57454MaxFanout=1;
        }
        return result;
    }
    void Pr57454EndSlice()
    {
        if(!Pr57454Enabled)return;
        _pr57454Slices++;
        var k=_pr57454Positive?1:0;
        if(_pr57454CurrentRepeats>0)_pr57454RepeatSlices[k]++;
        if(_pr57454CurrentCalls>_pr57454MaxCalls)_pr57454MaxCalls=_pr57454CurrentCalls;
        // Every slice is emitted so the final line covers the entire replay.
        Console.WriteLine($"PR57_454_SUM slices={_pr57454Slices} zeroCalls={_pr57454Calls[0]} positiveCalls={_pr57454Calls[1]} zeroUnique={_pr57454Unique[0]} positiveUnique={_pr57454Unique[1]} zeroRepeats={_pr57454Repeats[0]} positiveRepeats={_pr57454Repeats[1]} zeroFlips={_pr57454Flips[0]} positiveFlips={_pr57454Flips[1]} zeroAllowed={_pr57454Allowed[0]} positiveAllowed={_pr57454Allowed[1]} zeroRepeatSlices={_pr57454RepeatSlices[0]} positiveRepeatSlices={_pr57454RepeatSlices[1]} maxCallsPerSlice={_pr57454MaxCalls} maxFanout={_pr57454MaxFanout} observational=True");
    }
    bool CanTurnNow(Vehicle o)
    {""")
print("PR57_454_INJECTED observational=True canonicalCanTurnNow=True")
