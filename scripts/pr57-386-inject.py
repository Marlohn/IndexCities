#!/usr/bin/env python3
"""PR57 #386: reversible outer MoveVehicles cost census (after #384 and #385).

Observational only: no skipped slices, changed decisions, cache or persistent state.
Measures gate, entry-frame construction, movement forecast and Unsplit calls
inside the outer vehicle phase, plus the remaining outer orchestration.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
p=root/"src/IndexCities.Simulation/Mobility.LaneEntry.cs"
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch("    void MoveVehicles(double dt)\n    {\n        var profile=CostProfile;",
      "    void MoveVehicles(double dt)\n    {\n        var pr57386TotalStart=Pr57386Stamp();\n        var profile=CostProfile;")
patch("        var hasEntry=Pr57StrandedGraphEnabled?Pr57HasLiveLaneEntry():State.Vehicles.Any(LiveLaneEntry);",
      "        var pr57386GateStart=Pr57386Stamp();\n        var hasEntry=Pr57StrandedGraphEnabled?Pr57HasLiveLaneEntry():State.Vehicles.Any(LiveLaneEntry);\n        Pr57386Take(1,pr57386GateStart);")
patch("            MoveVehiclesUnsplit(dt);",
      "            var pr57386UnsplitStart=Pr57386Stamp();\n            MoveVehiclesUnsplit(dt);\n            Pr57386Take(4,pr57386UnsplitStart);\n            Pr57386CountUnsplit(2);")
patch("            _pr57300Step=null;\n            return;",
      "            _pr57300Step=null;\n            Pr57386End(pr57386TotalStart,false);\n            return;")
patch("            MoveVehiclesUnsplit(0,dt);",
      "            var pr57386OperationalStart=Pr57386Stamp();\n            MoveVehiclesUnsplit(0,dt);\n            Pr57386Take(4,pr57386OperationalStart);\n            Pr57386CountUnsplit(1);")
patch("                // Same entries as ToDictionary, in a dictionary reused across slices.\n                _entryFrameBuffer.Clear();",
      "                // Same entries as ToDictionary, in a dictionary reused across slices.\n                var pr57386SnapshotStart=Pr57386Stamp();\n                _entryFrameBuffer.Clear();")
patch("                _entryFrame=_entryFrameBuffer;\n                var pr57300ScheduleStart=",
      "                _entryFrame=_entryFrameBuffer;\n                Pr57386Take(2,pr57386SnapshotStart);\n                var pr57300ScheduleStart=")
patch("                var slice=Pr57StrandedGraphEnabled?Pr57NextMovementEvent(remaining):NextVehicleMovementEvent(remaining);",
      "                var pr57386ForecastStart=Pr57386Stamp();\n                var slice=Pr57StrandedGraphEnabled?Pr57NextMovementEvent(remaining):NextVehicleMovementEvent(remaining);\n                Pr57386Take(3,pr57386ForecastStart);")
patch("                MoveVehiclesUnsplit(slice);",
      "                var pr57386PhysicalStart=Pr57386Stamp();\n                MoveVehiclesUnsplit(slice);\n                Pr57386Take(4,pr57386PhysicalStart);\n                Pr57386CountUnsplit(0);")
patch("        if(profile is not null)PrintCompletedLaneSliceHours(State.ElapsedSeconds+dt);\n        Pr57JunctionCensusReportIfReady(State.ElapsedSeconds+dt);\n    }",
      "        if(profile is not null)PrintCompletedLaneSliceHours(State.ElapsedSeconds+dt);\n        Pr57JunctionCensusReportIfReady(State.ElapsedSeconds+dt);\n        Pr57386End(pr57386TotalStart,true);\n    }")
p.write_text(s,encoding="utf-8")

out=root/"src/IndexCities.Simulation/Mobility.Outer386.Pr57.cs"
assert not out.exists()
out.write_text(r'''using System.Diagnostics;

namespace IndexCities.Simulation;

// #386 observational timers: not consulted by physics or gameplay.
// Buckets: total, initial entry gate, entry-frame snapshots, forecast,
// Unsplit calls; remainder = total minus the four measured regions.
// Counts: physical Unsplit, operational Unsplit, direct Unsplit,
// public MoveVehicles calls, batched MoveVehicles calls.
public sealed partial class CitySimulation
{
    static readonly bool Pr57386Enabled=
        Environment.GetEnvironmentVariable("PR57_386_OUTER_PROBE")=="1";
    readonly long[] _pr57386Totals=new long[11];
    long Pr57386Stamp()=>Pr57386Enabled?Stopwatch.GetTimestamp():0L;
    void Pr57386Take(int bucket,long started)
    {
        if(Pr57386Enabled)_pr57386Totals[bucket]+=Stopwatch.GetTimestamp()-started;
    }
    void Pr57386CountUnsplit(int kind)
    {
        if(Pr57386Enabled)_pr57386Totals[6+kind]++;
    }
    void Pr57386End(long started,bool batched)
    {
        if(!Pr57386Enabled)return;
        _pr57386Totals[0]+=Stopwatch.GetTimestamp()-started;
        _pr57386Totals[9]++;
        if(batched)_pr57386Totals[10]++;
    }
    public long[] Pr57386Snapshot()=>(long[])_pr57386Totals.Clone();
    public void Pr57386Report(int hour,long[] before)
    {
        var now=Pr57386Snapshot();
        if(before.Length!=11)throw new InvalidOperationException("#386 snapshot length");
        var delta=new long[11];
        for(int i=0;i<11;i++)
        {
            delta[i]=now[i]-before[i];
            if(delta[i]<0)throw new InvalidOperationException("#386 negative counter");
        }
        long residual=delta[0]-delta[1]-delta[2]-delta[3]-delta[4];
        if(Pr57386Enabled&&(residual<0||delta[9]!=1800||
            delta[6]+delta[7]+delta[8]<delta[9]||
            delta[10]>delta[9]))
            throw new InvalidOperationException("#386 inconsistent outer accounting");
        static double Ms(long ticks)=>ticks*1000.0/Stopwatch.Frequency;
        Console.WriteLine($"PR57_386_OUTER hour={hour} enabled={Pr57386Enabled} "+
            $"steps={delta[9]} batchedSteps={delta[10]} physicalCalls={delta[6]} "+
            $"operationalCalls={delta[7]} directCalls={delta[8]} "+
            $"totalMs={Ms(delta[0]):F3} gateMs={Ms(delta[1]):F3} "+
            $"snapshotMs={Ms(delta[2]):F3} forecastMs={Ms(delta[3]):F3} "+
            $"unsplitMs={Ms(delta[4]):F3} remainderMs={Ms(residual):F3} "+
            "scope=REAL_6H_OUTER_OBSERVATIONAL noSkippedSlices=True noDecisionReuse=True");
    }
}
''',encoding="utf-8")

p=root/"scripts/pr57-actor-regime-337/Program.cs"
s=p.read_text(encoding="utf-8")
def runner_patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)
runner_patch("    var regionsBefore=sim.Pr57385Snapshot();",
             "    var outerBefore=sim.Pr57386Snapshot();\n    var regionsBefore=sim.Pr57385Snapshot();")
runner_patch("    sim.Pr57385Report(hour,regionsBefore);",
             "    sim.Pr57385Report(hour,regionsBefore);\n    sim.Pr57386Report(hour,outerBefore);")
p.write_text(s,encoding="utf-8")
print("PR57_386_INJECTED outer vehicle gate/snapshot/forecast/Unsplit/remainder")
