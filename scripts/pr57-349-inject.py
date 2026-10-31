#!/usr/bin/env python3
"""PR57 #349: checkout-only observation of bypass gates, inner calls and Cause writes.

The canonical TryBypassStopped/TryOvertake/TryUTurn/FindRoadRoute still execute
in the original order. No decision, state mutation, route, or slice is skipped.
Only the six late-jam physical samples selected by #346 are counted.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
mobility=root/"src/IndexCities.Simulation/Mobility.cs"
bypass=root/"src/IndexCities.Simulation/Mobility.Bypass.cs"
residual=root/"src/IndexCities.Simulation/Mobility.ResidualExclusive.Pr57.cs"

def change(path,old,new,label):
    s=path.read_text(encoding="utf-8")
    assert s.count(old)==1, f"#349 {label}: expected one anchor, got {s.count(old)}"
    path.write_text(s.replace(old,new,1),encoding="utf-8")

change(mobility,"var bypassed=TryBypassStopped(v,blocker);",
       "var bypassed=Pr57349BypassCall(v,blocker);","caller")
change(bypass,"&&TryOvertake(v,blocker))return true;",
       "&&Pr57349Overtake(v,blocker))return true;","overtake")
change(bypass,"&&TryUTurn(v,blocker))return true;",
       "&&Pr57349UTurn(v,blocker))return true;","uturn")
change(bypass,"var alternative=FindRoadRoute(behind,v.Route[^1],false,avoid);",
       "Pr57349RouteCall();\n        var alternative=FindRoadRoute(behind,v.Route[^1],false,avoid);","route")
for cause in (
    "Rua bloqueada por veículo parado; sem espaço para meia-volta",
    "Rua bloqueada por veículo parado; sem rota alternativa",
    "Meia-volta: rua bloqueada por veículo parado",
):
    change(bypass,'v.Cause="'+cause+'";',
           'Pr57349CauseWrite(v,"'+cause+'");v.Cause="'+cause+'";',"cause "+cause)
change(residual,"        _pr57ExclusiveActive = true;",
       "        _pr57ExclusiveActive = true;\n        Pr57349Reset();","reset")
change(residual,"        Pr57348Report();",
       "        Pr57348Report();\n        Pr57349Report();","report")

cs=r'''namespace IndexCities.Simulation;

// #349 is an intrusive observer, not a bypass fast path or a cache.
public sealed partial class CitySimulation
{
    static readonly bool Pr57349Enabled =
        Environment.GetEnvironmentVariable("PR57_349_BYPASS_TRACE")=="1";
    long _pr57349Attempts, _pr57349BaseGate, _pr57349Stranded;
    long _pr57349OverThreshold, _pr57349TurnThreshold;
    long _pr57349OverCalls, _pr57349TurnCalls, _pr57349RouteCalls;
    long _pr57349OverSuccess, _pr57349TurnSuccess, _pr57349Success;
    long _pr57349CauseWrites, _pr57349CauseChanged, _pr57349CauseSame;
    long _pr57349CauseNet, _pr57349Ticks, _pr57349Bytes;
    long _pr57349OverTicks, _pr57349TurnTicks;
    bool _pr57349InBypass;

    void Pr57349Reset()
    {
        if (!Pr57349Enabled) return;
        _pr57349Attempts=_pr57349BaseGate=_pr57349Stranded=0;
        _pr57349OverThreshold=_pr57349TurnThreshold=0;
        _pr57349OverCalls=_pr57349TurnCalls=_pr57349RouteCalls=0;
        _pr57349OverSuccess=_pr57349TurnSuccess=_pr57349Success=0;
        _pr57349CauseWrites=_pr57349CauseChanged=_pr57349CauseSame=0;
        _pr57349CauseNet=_pr57349Ticks=_pr57349Bytes=0;
        _pr57349OverTicks=_pr57349TurnTicks=0;
        _pr57349InBypass=false;
    }

    bool Pr57349BypassCall(Vehicle v,Vehicle blocker)
    {
        if (!Pr57349Enabled || !_pr57ExclusiveActive)
            return TryBypassStopped(v,blocker);
        _pr57349Attempts++;
        var baseGate=v.Detour.Count==0&&v.WaitingSince>0
            &&v.RouteIndex>=1&&v.RouteIndex<v.Route.Count;
        if (baseGate) _pr57349BaseGate++;
        if (baseGate&&blocker.Status==VehicleStatus.Stranded)
        {
            _pr57349Stranded++;
            var waited=VehicleMovementTime-v.WaitingSince;
            if(waited>=State.Tuning.BypassWaitSeconds) _pr57349OverThreshold++;
            if(waited>=State.Tuning.UTurnWaitSeconds) _pr57349TurnThreshold++;
        }
        var beforeCause=v.Cause;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var alloc=GC.GetAllocatedBytesForCurrentThread();
        _pr57349InBypass=true;
        try
        {
            var result=TryBypassStopped(v,blocker);
            if(result) _pr57349Success++;
            return result;
        }
        finally
        {
            _pr57349InBypass=false;
            _pr57349Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
            _pr57349Bytes+=GC.GetAllocatedBytesForCurrentThread()-alloc;
            if(v.Cause!=beforeCause) _pr57349CauseNet++;
        }
    }

    bool Pr57349Overtake(Vehicle v,Vehicle blocker)
    {
        if(!Pr57349Enabled||!_pr57349InBypass)return TryOvertake(v,blocker);
        _pr57349OverCalls++;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=TryOvertake(v,blocker);
        _pr57349OverTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        if(result)_pr57349OverSuccess++;
        return result;
    }

    bool Pr57349UTurn(Vehicle v,Vehicle blocker)
    {
        if(!Pr57349Enabled||!_pr57349InBypass)return TryUTurn(v,blocker);
        _pr57349TurnCalls++;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var result=TryUTurn(v,blocker);
        _pr57349TurnTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        if(result)_pr57349TurnSuccess++;
        return result;
    }

    void Pr57349RouteCall()
    {
        if(Pr57349Enabled&&_pr57349InBypass)_pr57349RouteCalls++;
    }

    void Pr57349CauseWrite(Vehicle v,string next)
    {
        if(!Pr57349Enabled||!_pr57349InBypass)return;
        _pr57349CauseWrites++;
        if(v.Cause==next)_pr57349CauseSame++;
        else _pr57349CauseChanged++;
    }

    void Pr57349Report()
    {
        if(!Pr57349Enabled||!_pr57ExclusiveActive)return;
        var freq=System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_349_TRACE sample={_pr57ExclusiveSamples} " +
            $"attempts={_pr57349Attempts} baseGate={_pr57349BaseGate} " +
            $"stranded={_pr57349Stranded} overThreshold={_pr57349OverThreshold} " +
            $"turnThreshold={_pr57349TurnThreshold} overCalls={_pr57349OverCalls} " +
            $"turnCalls={_pr57349TurnCalls} routeCalls={_pr57349RouteCalls} " +
            $"overSuccess={_pr57349OverSuccess} turnSuccess={_pr57349TurnSuccess} " +
            $"success={_pr57349Success} causeWrites={_pr57349CauseWrites} " +
            $"causeChanged={_pr57349CauseChanged} causeSame={_pr57349CauseSame} " +
            $"causeNet={_pr57349CauseNet} " +
            $"bypassMs={_pr57349Ticks*1000.0/freq:F6} " +
            $"overMs={_pr57349OverTicks*1000.0/freq:F6} " +
            $"turnMs={_pr57349TurnTicks*1000.0/freq:F6} " +
            $"bytes={_pr57349Bytes} scope=CANONICAL_BYPASS_CALLS_INTRUSIVE");
    }
}
'''
(root/"src/IndexCities.Simulation/Mobility.Bypass349.Pr57.cs").write_text(cs,encoding="utf-8")
print("PR57_349_INJECTED observational wrappers and exact Cause writer hooks")
