#!/usr/bin/env python3
"""PR57 #445: observational direct WorkAccess across progress/dispatch.

Only modify the disposable Actions checkout. The canonical WorkAccess call
runs exactly once at each original site; no cached answer is used. The
observation is opt-in and is never saved into CityState.
"""
from pathlib import Path

def replace_once(path, old, new):
    source = path.read_text()
    count = source.count(old)
    assert count == 1, (str(path), old[:100], count)
    path.write_text(source.replace(old, new, 1))

construction = Path("src/IndexCities.Simulation/Construction.cs")
step = Path("src/IndexCities.Simulation/CitySimulation.cs")
replace_once(
    construction,
    "            var access=WorkAccess(p);\n            if(pr57DispatchProfile is not null)",
    """            var pr57445Start=Pr57445Enabled?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var access=WorkAccess(p);
            if(Pr57445Enabled)Pr57445RecordDispatch(p,access,System.Diagnostics.Stopwatch.GetTimestamp()-pr57445Start);
            if(pr57DispatchProfile is not null)""",
)
replace_once(
    construction,
    "            var access=WorkAccess(p);if(access==null){p.Cause=WorkCause.NoRoadAccess;continue;}",
    """            var pr57445Start=Pr57445Enabled?System.Diagnostics.Stopwatch.GetTimestamp():0L;
            var access=WorkAccess(p);
            if(Pr57445Enabled)Pr57445RecordProgress(p,access,System.Diagnostics.Stopwatch.GetTimestamp()-pr57445Start);
            if(access==null){p.Cause=WorkCause.NoRoadAccess;continue;}""",
)
replace_once(step, "        Pr57WorkAccessBeginStep();\n",
             "        Pr57445BeginStep();\n        Pr57WorkAccessBeginStep();\n")
replace_once(step, "        OrderConstruction();\n",
             "        OrderConstruction();\n        Pr57445EndStep();\n")
helper = Path("src/IndexCities.Simulation/Pr57ConstructionCrossPhase445.cs")
assert not helper.exists()
helper.write_text("""namespace IndexCities.Simulation;

// Opt-in observation, not a cache: no Step decisions read these counters.
public sealed partial class CitySimulation
{
    static readonly bool Pr57445Enabled =
        Environment.GetEnvironmentVariable("PR57_445_ACCESS_OBS")=="1";
    readonly Dictionary<int,(int? Access,int RoadRevision,int RoadEpoch,
        double Progress,WorkStatus Status,string? Cause,double Elapsed)> _pr57445Progress = new();
    long _pr57445Steps,_pr57445ProgressCalls,_pr57445DispatchCalls;
    long _pr57445ProgressTicks,_pr57445DispatchTicks;
    long _pr57445Matched,_pr57445NoProgress,_pr57445SameNonNull,_pr57445SameNull;
    long _pr57445DifferentAccess,_pr57445DifferentRevision,_pr57445DifferentEpoch;
    long _pr57445DifferentProgress,_pr57445DifferentStatus,_pr57445DifferentCause;
    long _pr57445ClockAdvanced;
    void Pr57445BeginStep()
    {
        if(!Pr57445Enabled)return;
        _pr57445Steps++;
        _pr57445Progress.Clear();
    }
    void Pr57445RecordProgress(WorkProject p,RoadNode? access,long ticks)
    {
        _pr57445ProgressCalls++;
        _pr57445ProgressTicks+=ticks;
        _pr57445Progress[p.Id]=(access?.Id,State.RoadRevision,_roadEpoch,
            p.Progress,p.Status,p.Cause,State.ElapsedSeconds);
    }
    void Pr57445RecordDispatch(WorkProject p,RoadNode? access,long ticks)
    {
        _pr57445DispatchCalls++;
        _pr57445DispatchTicks+=ticks;
        if(!_pr57445Progress.TryGetValue(p.Id,out var before))
        {
            _pr57445NoProgress++;
            return;
        }
        _pr57445Matched++;
        if(before.Access==access?.Id)
        {
            if(access is null)_pr57445SameNull++;
            else _pr57445SameNonNull++;
        }
        else _pr57445DifferentAccess++;
        if(before.RoadRevision!=State.RoadRevision)_pr57445DifferentRevision++;
        if(before.RoadEpoch!=_roadEpoch)_pr57445DifferentEpoch++;
        if(before.Progress!=p.Progress)_pr57445DifferentProgress++;
        if(before.Status!=p.Status)_pr57445DifferentStatus++;
        if(!string.Equals(before.Cause,p.Cause,StringComparison.Ordinal))_pr57445DifferentCause++;
        if(before.Elapsed!=State.ElapsedSeconds)_pr57445ClockAdvanced++;
    }
    void Pr57445EndStep()
    {
        if(!Pr57445Enabled || _pr57445Steps%100!=0)return;
        var ms=1000.0/System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_445_ACCESS steps={_pr57445Steps} progress={_pr57445ProgressCalls} dispatch={_pr57445DispatchCalls} matched={_pr57445Matched} noProgress={_pr57445NoProgress} sameNonNull={_pr57445SameNonNull} sameNull={_pr57445SameNull} differentAccess={_pr57445DifferentAccess} differentRevision={_pr57445DifferentRevision} differentEpoch={_pr57445DifferentEpoch} differentProgress={_pr57445DifferentProgress} differentStatus={_pr57445DifferentStatus} differentCause={_pr57445DifferentCause} clockAdvanced={_pr57445ClockAdvanced} progressMs={_pr57445ProgressTicks*ms:F6} dispatchMs={_pr57445DispatchTicks*ms:F6}");
    }
}
""")
print("PR57_445_INJECTED observationOnly=True canonicalWorkAccess=True")
