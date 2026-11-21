#!/usr/bin/env python3
"""PR57 #344: checkout-only exclusive junction BodyOnWay timing, REAL 17-20h.

Observational. All canonical calls, physical sub-slices and return decisions stay
unchanged. No timing result is an accepted optimization or exact causal proof.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
sim = root / "src/IndexCities.Simulation"


def once(path, old, new):
    text = path.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{path}: expected one anchor, got {count}: {old!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8")


once(sim / "Mobility.cs",
     "        var bendBody=BodyOnWay(v,way);",
     """        var pr57344Bend=Pr57344Begin();
        var bendBody=BodyOnWay(v,way);
        Pr57344End(v,bendBody,pr57344Bend,true);""")
once(sim / "Mobility.cs",
     "        var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);",
     """        var pr57344Onward=Pr57344Begin();
        var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);
        Pr57344End(v,onwardBody,pr57344Onward,false);""")
once(root / "scripts/pr57-actor-regime-337/Program.cs",
     "sim.CostProfile=null;",
     """sim.Pr57344Final();
sim.CostProfile=null;""")

target = sim / "Mobility.ExclusiveBody344.Pr57.cs"
if target.exists():
    raise RuntimeError("Refusing to overwrite #344 observer")
target.write_text(r'''namespace IndexCities.Simulation;

// PR57 #344: exclusive stopwatch around the ACTUAL BodyOnWay invocation at
// MayEnterJunction bend/onward sites. This is NOT the whole admission cost;
// it includes stopwatch overhead and cannot prove that a call can be skipped.
public sealed partial class CitySimulation
{
    static readonly bool Pr57344Enabled =
        Environment.GetEnvironmentVariable("PR57_344_EXCLUSIVE") == "1";
    static readonly DateTime Pr57344From = new(2026,1,7,17,0,0);
    static readonly DateTime Pr57344Until = new(2026,1,7,20,0,0);
    readonly long[] _pr57344Calls = new long[4];
    readonly long[] _pr57344Ticks = new long[4];
    readonly long[] _pr57344Bytes = new long[4];
    readonly long[] _pr57344SelectedCalls = new long[4];
    readonly long[] _pr57344SelectedTicks = new long[4];
    readonly HashSet<int>[] _pr57344Readers = [new(),new(),new(),new()];
    readonly HashSet<int>[] _pr57344Blockers = [new(),new(),new(),new()];

    (long Ticks,long Bytes) Pr57344Begin()
    {
        if(!Pr57344Enabled||State.Now<Pr57344From||State.Now>=Pr57344Until)
            return default;
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        return (System.Diagnostics.Stopwatch.GetTimestamp(),bytes);
    }
    void Pr57344End(Vehicle reader,Vehicle? blocker,(long Ticks,long Bytes) stamp,bool bend)
    {
        if(stamp.Ticks==0)return;
        var end=System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        var group=(bend?2:0)+(blocker is null?1:0);
        var elapsed=Math.Max(0,end-stamp.Ticks);
        _pr57344Calls[group]++;
        _pr57344Ticks[group]+=elapsed;
        _pr57344Bytes[group]+=Math.Max(0,bytes-stamp.Bytes);
        _pr57344Readers[group].Add(reader.Id);
        if(blocker is not null)_pr57344Blockers[group].Add(blocker.Id);
        if(reader.Id is 1071 or 1243 or 1282 or 1380)
        {
            _pr57344SelectedCalls[group]++;
            _pr57344SelectedTicks[group]+=elapsed;
        }
    }
    public void Pr57344Final()
    {
        Console.WriteLine($"PR57_344_FINAL enabled={Pr57344Enabled} window=REAL_17h_20h " +
            $"calls={_pr57344Calls.Sum()} bodyMs={_pr57344Ticks.Sum()*1000.0/System.Diagnostics.Stopwatch.Frequency:F3} " +
            $"allocatedBytes={_pr57344Bytes.Sum()} selectedCalls={_pr57344SelectedCalls.Sum()} " +
            "scope=ACTUAL_BODY_ON_WAY_EXCLUSIVE_NOT_FULL_ADMISSION_NO_SKIP");
        string[] names=["onward-blocked","onward-clear","bend-blocked","bend-clear"];
        for(var i=0;i<4;i++)
            Console.WriteLine($"PR57_344_GROUP name={names[i]} calls={_pr57344Calls[i]} " +
                $"bodyMs={_pr57344Ticks[i]*1000.0/System.Diagnostics.Stopwatch.Frequency:F3} " +
                $"allocatedBytes={_pr57344Bytes[i]} distinctReaders={_pr57344Readers[i].Count} " +
                $"distinctBlockers={_pr57344Blockers[i].Count} " +
                $"selectedCalls={_pr57344SelectedCalls[i]} " +
                $"selectedMs={_pr57344SelectedTicks[i]*1000.0/System.Diagnostics.Stopwatch.Frequency:F3}");
    }
}
''', encoding="utf-8")
print("PR57_344_INJECTED: timed actual junction BodyOnWay sites, no behavior change")
