#!/usr/bin/env python3
"""PR57 #462: count every reached CanTurnNow, including SpaceFree and preference.
Observation only in a disposable CI checkout. Never memoize a result or skip
an invocation. Results alone do not establish causal equivalence.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
def once(path,old,new):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==1,(str(path),old[:110],s.count(old))
    path.write_text(s.replace(old,new,1),encoding="utf-8")

mob=root/"src/IndexCities.Simulation/Mobility.cs"
once(mob,"        Pr57JunctionCensusBeginSlice(dt,operationsDt);",
     "        Pr57462BeginSlice(dt);\n        Pr57JunctionCensusBeginSlice(dt,operationsDt);")
once(mob,"        Pr57Stage249EndSlice();",
     "        Pr57Stage249EndSlice();\n        Pr57462EndSlice();")
once(mob,"&&CanTurnNow(o)).ToList();","&&Pr57462PreferenceCanTurn(o)).ToList();")

shared=root/"src/IndexCities.Simulation/Mobility.SharedSpace.cs"
once(shared,"&&CanTurnNow(o)&&","&&Pr57462SpaceFreeCanTurn(o)&&")
once(shared,"    bool CanTurnNow(Vehicle o)\n    {",'''    // #462: all canonical calls still run, even repeated negative decisions.
    static readonly bool Pr57462Enabled=Environment.GetEnvironmentVariable("PR57_462_ALL_FANOUT")=="1";
    readonly Dictionary<int,(bool Result,int Context,int Count)> _pr57462Seen=new();
    long _pr57462Slices,_pr57462PositiveSlices,_pr57462Calls,_pr57462Unique,
         _pr57462Repeats,_pr57462Stable,_pr57462Flips,_pr57462CrossContext,
         _pr57462SpaceFreeCalls,_pr57462PreferenceCalls,_pr57462OtherCalls,
         _pr57462SpaceFreeRepeats,_pr57462RepeatSlices,_pr57462MaxCallsPerSlice;
    int _pr57462Context,_pr57462ThisSlice,_pr57462ThisSliceRepeats,_pr57462MaxFanout;
    bool _pr57462InSlice;
    void Pr57462BeginSlice(double dt)
    {
        if(!Pr57462Enabled)return;
        _pr57462InSlice=true;
        _pr57462Seen.Clear();
        _pr57462ThisSlice=0;_pr57462ThisSliceRepeats=0;
        if(dt>0)_pr57462PositiveSlices++;
    }
    bool Pr57462PreferenceCanTurn(Vehicle o)=>Pr57462Call(o,2);
    bool Pr57462SpaceFreeCanTurn(Vehicle o)=>Pr57462Call(o,1);
    bool Pr57462Call(Vehicle o,int context)
    {
        if(!Pr57462Enabled)return CanTurnNow(o);
        var old=_pr57462Context;_pr57462Context=context;
        try{return CanTurnNow(o);}finally{_pr57462Context=old;}
    }
    void Pr57462EndSlice()
    {
        if(!Pr57462Enabled)return;
        _pr57462InSlice=false;_pr57462Slices++;
        if(_pr57462ThisSliceRepeats>0)_pr57462RepeatSlices++;
        if(_pr57462ThisSlice>_pr57462MaxCallsPerSlice)_pr57462MaxCallsPerSlice=_pr57462ThisSlice;
        Console.WriteLine($"PR57_462_SUM slices={_pr57462Slices} positiveSlices={_pr57462PositiveSlices} calls={_pr57462Calls} unique={_pr57462Unique} repeats={_pr57462Repeats} stableRepeats={_pr57462Stable} flips={_pr57462Flips} crossContextRepeats={_pr57462CrossContext} spaceFreeCalls={_pr57462SpaceFreeCalls} preferenceCalls={_pr57462PreferenceCalls} otherCalls={_pr57462OtherCalls} spaceFreeRepeats={_pr57462SpaceFreeRepeats} repeatSlices={_pr57462RepeatSlices} maxFanout={_pr57462MaxFanout} maxCallsPerSlice={_pr57462MaxCallsPerSlice} observational=True");
    }
    bool CanTurnNow(Vehicle o)
    {
        var result=Pr57462CanonicalCanTurnNow(o); // one canonical evaluation
        if(!Pr57462Enabled||!_pr57462InSlice)return result;
        _pr57462Calls++;_pr57462ThisSlice++;
        var context=_pr57462Context;
        if(context==1)_pr57462SpaceFreeCalls++;
        else if(context==2)_pr57462PreferenceCalls++;
        else _pr57462OtherCalls++;
        if(_pr57462Seen.TryGetValue(o.Id,out var prev))
        {
            _pr57462Repeats++;_pr57462ThisSliceRepeats++;
            if(prev.Result==result)_pr57462Stable++;else _pr57462Flips++;
            if(prev.Context!=context)_pr57462CrossContext++;
            if(context==1)_pr57462SpaceFreeRepeats++;
            var count=prev.Count+1;
            if(count>_pr57462MaxFanout)_pr57462MaxFanout=count;
            _pr57462Seen[o.Id]=(result,context,count);
        }
        else
        {
            _pr57462Unique++;
            _pr57462Seen.Add(o.Id,(result,context,1));
            if(_pr57462MaxFanout<1)_pr57462MaxFanout=1;
        }
        return result;
    }
    bool Pr57462CanonicalCanTurnNow(Vehicle o)
    {''')
print("PR57_462_INJECTED observational=True allCanonicalCalls=True")
