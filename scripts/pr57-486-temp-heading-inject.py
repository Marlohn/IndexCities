#!/usr/bin/env python3
"""PR57 #486: observe temporary heading and nested physical readers in REAL replay.

Injected only into a disposable CI checkout; all original decisions, sub-slices,
ordering, economy, history, and saves remain canonical. No cache or skip.
This does NOT establish a universal invalidation journal or an optimized overlay.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]

def once(path, old, new):
    text = path.read_text(encoding="utf-8")
    assert text.count(old) == 1, (str(path), old[:100], text.count(old))
    path.write_text(text.replace(old, new, 1), encoding="utf-8")

shared = root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs"
mobility = root / "src/IndexCities.Simulation/Mobility.cs"
headless = root / "src/IndexCities.Headless/Program.cs"

once(shared, "    bool CanTurnNow(Vehicle o)\n    {", """    // #486: observational per-invocation temporary heading witness.
    static readonly bool Pr57486Enabled =
        Environment.GetEnvironmentVariable("PR57_486_TEMP_HEADING") == "1";
    readonly Stack<int> _pr57486TempActors = new();
    long _pr57486Entries, _pr57486Changed, _pr57486Nested,
         _pr57486TempBodyNow, _pr57486TempBodyPose,
         _pr57486HookTicks, _pr57486PreferenceTicks;
    long Pr57486Enter(Vehicle actor, double original)
    {
        if (!Pr57486Enabled) return 0;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57486TempActors.Push(actor.Id);
        _pr57486Entries++;
        if (BitConverter.DoubleToInt64Bits(actor.Heading) !=
            BitConverter.DoubleToInt64Bits(original)) _pr57486Changed++;
        _pr57486HookTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        return System.Diagnostics.Stopwatch.GetTimestamp();
    }
    void Pr57486Leave(long start)
    {
        if (!Pr57486Enabled) return;
        _pr57486PreferenceTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        var hookStart = System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57486TempActors.Pop();
        _pr57486HookTicks += System.Diagnostics.Stopwatch.GetTimestamp() - hookStart;
    }
    bool Pr57486NestedCanTurn(Vehicle actor)
    {
        if (Pr57486Enabled && _pr57486TempActors.Count > 0)
            _pr57486Nested++;
        return CanTurnNow(actor);
    }
    bool Pr57486TemporaryBody(Vehicle candidate) =>
        Pr57486Enabled && _pr57486TempActors.Count > 0 &&
        _pr57486TempActors.Contains(candidate.Id);
    public void Pr57486Print()
    {
        if (!Pr57486Enabled) return;
        var hz = (double)System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine($"PR57_486_TEMP entries={_pr57486Entries} changed={_pr57486Changed} " +
            $"nested={_pr57486Nested} tempNow={_pr57486TempBodyNow} " +
            $"tempPose={_pr57486TempBodyPose} hookTicks={_pr57486HookTicks} " +
            $"preferenceTicks={_pr57486PreferenceTicks} " +
            $"hookMs={_pr57486HookTicks * 1000.0 / hz:F6} " +
            $"preferenceMs={_pr57486PreferenceTicks * 1000.0 / hz:F6} " +
            "observational=True closure=False");
    }
    bool CanTurnNow(Vehicle o)
    {""")

once(shared,
     "            var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);var limit=Math.Max(-.1,now+.001);",
     "            if(Pr57486TemporaryBody(o))_pr57486TempBodyNow++;\n"
     "            var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);var limit=Math.Max(-.1,now+.001);")
once(shared,
     "            foreach(var p in poses)if(p.Position.Distance(o.Position)<12){",
     "            foreach(var p in poses)if(p.Position.Distance(o.Position)<12){\n"
     "                if(Pr57486TemporaryBody(o))_pr57486TempBodyPose++;")
once(shared,
     "            var heading=v.Heading;v.Heading=Math.Atan2(entering.X,entering.Z);\n"
     "            var allowed=JunctionPreferenceAllows(v,node);v.Heading=heading;",
     "            var heading=v.Heading;v.Heading=Math.Atan2(entering.X,entering.Z);\n"
     "            var pr57486Start=Pr57486Enter(v,heading);\n"
     "            var allowed=JunctionPreferenceAllows(v,node);Pr57486Leave(pr57486Start);v.Heading=heading;")
once(mobility,
     "&&CanTurnNow(o)).ToList();",
     "&&Pr57486NestedCanTurn(o)).ToList();")
once(headless,
     '    if(Environment.GetEnvironmentVariable("PR57_AB_CHECKPOINT")=="1"){',
     '    sim.Pr57486Print();\n'
     '    if(Environment.GetEnvironmentVariable("PR57_AB_CHECKPOINT")=="1"){')

print("PR57_486_INJECTED observational=True temporaryHeading=True nestedReader=True noCache=True noSkip=True")
