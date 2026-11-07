#!/usr/bin/env python3
"""PR57 #359: bounded physical-loop stage ceiling over 1,200 REAL sub-slices.

Checkout-only, observational. The canonical executor, effects and every
Advance(2) sub-slice are retained. This does NOT implement state scheduling.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
base=root/"src/IndexCities.Simulation"
p=base/"Mobility.ResidualExclusive.Pr57.cs"
s=p.read_text(encoding="utf-8")

def patch(old,new,label):
    global s
    assert s.count(old)==1,(label,s.count(old))
    s=s.replace(old,new,1)

patch("if (_pr57ExclusiveEligibleSlices < 37 || _pr57ExclusiveEligibleSlices > 39) return;",
      "if (!((_pr57ExclusiveEligibleSlices >= 40001 && _pr57ExclusiveEligibleSlices <= 40600) || (_pr57ExclusiveEligibleSlices >= 50001 && _pr57ExclusiveEligibleSlices <= 50600))) return;",
      "sample window")
patch("_pr57ExclusiveActive = true;\n        Array.Clear(_pr57ExclusiveTicks);",
      "_pr57ExclusiveActive = true;\n        Pr57359Begin();\n        Array.Clear(_pr57ExclusiveTicks);",
      "begin")
patch("Pr57DenseEndSlice();\n        _pr57ExclusiveActive = false;",
      "Pr57DenseEndSlice();\n        Pr57359End();\n        _pr57ExclusiveActive = false;",
      "end")
p.write_text(s,encoding="utf-8")
out=base/"Mobility.StateCeiling359.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// Intrusive observational aggregation of mutually exclusive #206 stages
// by #244 actor class. Wall is the observed physical-slice span, not Advance(2)
// or the whole integrated simulation. No result enters the engine.
public sealed partial class CitySimulation
{
    readonly long[,,] _pr57359StageTicks=new long[2,5,9];
    readonly long[,] _pr57359Actors=new long[2,5];
    readonly long[] _pr57359SliceTicks=new long[2];
    readonly long[] _pr57359Slices=new long[2];
    readonly long[] _pr57359Moves=new long[2];
    long _pr57359StartTick;

    void Pr57359Begin()
    {
        _pr57359StartTick=System.Diagnostics.Stopwatch.GetTimestamp();
    }

    void Pr57359End()
    {
        var ordinal=_pr57ExclusiveEligibleSlices;
        var window=ordinal<=40600?0:1;
        _pr57359SliceTicks[window]+=
            System.Diagnostics.Stopwatch.GetTimestamp()-_pr57359StartTick;
        _pr57359Slices[window]++;
        _pr57359Moves[window]+=_pr57ExclusiveMoved;
        for(var group=0;group<5;group++)
        {
            _pr57359Actors[window,group]+=_pr57State244Calls[group];
            for(var stage=0;stage<9;stage++)
                _pr57359StageTicks[window,group,stage]+=
                    _pr57State244Ticks[group][stage];
        }
        if(ordinal!=40600&&ordinal!=50600)return;
        var hz=(double)System.Diagnostics.Stopwatch.Frequency;
        var spanMs=_pr57359SliceTicks[window]*1000.0/hz;
        long actorTicks=0, selectedTicks=0;
        for(var group=0;group<5;group++)
            for(var stage=0;stage<9;stage++)
            {
                var ticks=_pr57359StageTicks[window,group,stage];
                actorTicks+=ticks;
                if(group==2&&stage>=1&&stage<=4)selectedTicks+=ticks;
            }
        var actorMs=actorTicks*1000.0/hz;
        var selectedMs=selectedTicks*1000.0/hz;
        var share=spanMs>0?selectedMs/spanMs:0;
        Console.WriteLine(
            $"PR57_359_WINDOW start={(window==0?40001:50001)} " +
            $"end={ordinal} slices={_pr57359Slices[window]} " +
            $"movedActors={_pr57359Moves[window]} " +
            $"physicalSliceMs={spanMs:F6} actorLoopMs={actorMs:F6} " +
            $"selectedStillS1to4Ms={selectedMs:F6} " +
            $"selectedShareOfPhysicalSlice={share:F8} " +
            $"idealPhysicalCeiling={(share<1?1/(1-share):double.PositiveInfinity):F6} " +
            "scope=INTRUSIVE_SAMPLED_PHYSICAL_SLICES notIntegratedAmdahl=True " +
            "noSkippedSlices=True noDecisionReuse=True");
        string[] names=["persistent-still","persistent-other",
            "other-travelling-still","other-travelling-moved","other"];
        for(var group=0;group<5;group++)
        {
            var parts=new string[9];
            long sum=0;
            for(var stage=0;stage<9;stage++)
            {
                var ticks=_pr57359StageTicks[window,group,stage];
                sum+=ticks;
                parts[stage]=$"s{stage}Ms={ticks*1000.0/hz:F6}";
            }
            Console.WriteLine(
                $"PR57_359_GROUP start={(window==0?40001:50001)} " +
                $"group={names[group]} actors={_pr57359Actors[window,group]} " +
                $"totalMs={sum*1000.0/hz:F6} "+string.Join(" ",parts)+
                " scope=EXCLUSIVE_ACTOR_STAGES_INTRUSIVE");
        }
    }
}
''',encoding="utf-8")
print("PR57_359_INJECTED bounded observational stage/physical-slice ceiling")
