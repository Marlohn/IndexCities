#!/usr/bin/env python3
"""PR57 #422: observational first-divergence census for repeated physical decisions.

Composes #420 only in a disposable CI checkout. The canonical physics, actor
order, scans, Advance(2), economy and saves remain authoritative and unchanged.
The stage census is NOT proof of causal independence or a reusable cache.
"""
from pathlib import Path
import runpy

runpy.run_path("scripts/pr57-420-inject.py", run_name="__main__")
p = Path("src/IndexCities.Simulation/Mobility.cs")
s = p.read_text()

def once(old, new):
    global s
    count = s.count(old)
    assert count == 1, (old[:120], count)
    s = s.replace(old, new, 1)

once(
    "    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,\n"
    "        long X,long Z,long Heading,int Blocker,long Distance,long Step);",
    """    static readonly bool Pr57422Enabled=Environment.GetEnvironmentVariable("PR57_422_LIMITER_SHADOW")=="1";
    // First changed boundary, not a causal attribution: upstream state may vary.
    readonly long[] _pr57422First=new long[9],_pr57422Inputs=new long[4];
    long _pr57422ReaderChanged,_pr57422ReaderStepChanged,_pr57422SameLeaderStepChanged;
    readonly record struct Pr57420Decision(long Slice,int Node,bool Entry,int RouteIndex,bool Waiting,
        long X,long Z,long Heading,int Blocker,long Distance,long Step,
        long D,long Speed,long Remaining,long StopEarly,long Initial,long Stop,
        long Poses,long Bend,long Site,long Crossing,long Free);"""
)

once(
    "                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));",
    "                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));\n"
    "                var pr57422Initial=step;"
)
once(
    "                if(stopEarly>0)step=Math.Min(step,Math.Max(0,d-stopEarly));",
    "                if(stopEarly>0)step=Math.Min(step,Math.Max(0,d-stopEarly));\n"
    "                var pr57422Stop=step;"
)
once(
    "                if(crossingPoses!=null)step=ClearOfPoses(v,target,step,crossingPoses);",
    "                if(crossingPoses!=null)step=ClearOfPoses(v,target,step,crossingPoses);\n"
    "                var pr57422Poses=step;"
)
once(
    "                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=4;",
    "                var pr57422Bend=step;\n"
    "                if(pr57LimiterProbe&&pr57QueueFirstLimit==0&&step<.00001)pr57QueueFirstLimit=4;"
)
once(
    "                            blocker?.Id??-1,Pr57420Bits(leaderDistance),Pr57420Bits(step));",
    """                            blocker?.Id??-1,Pr57420Bits(leaderDistance),Pr57420Bits(step),
                            Pr57420Bits(d),Pr57420Bits(speed),Pr57420Bits(remaining),
                            Pr57420Bits(stopEarly),Pr57420Bits(pr57422Initial),
                            Pr57420Bits(pr57422Stop),Pr57420Bits(pr57422Poses),
                            Pr57420Bits(pr57422Bend),Pr57420Bits(afterSiteStep),
                            Pr57420Bits(afterCrossingStep),Pr57420Bits(free));"""
)
once(
    "                            if(reader&&blockerSame&&distanceSame&&stepSame)_pr57420SameReaderDecision++;",
    """                            if(reader&&blockerSame&&distanceSame&&stepSame)_pr57420SameReaderDecision++;
                            if(Pr57422Enabled&&reader&&!(blockerSame&&distanceSame&&stepSame)){
                                _pr57422ReaderChanged++;
                                if(!stepSame)_pr57422ReaderStepChanged++;
                                if(blockerSame&&distanceSame&&!stepSame)_pr57422SameLeaderStepChanged++;
                                if(now.D!=prior.D)_pr57422Inputs[0]++;
                                if(now.Speed!=prior.Speed)_pr57422Inputs[1]++;
                                if(now.Remaining!=prior.Remaining)_pr57422Inputs[2]++;
                                if(now.StopEarly!=prior.StopEarly)_pr57422Inputs[3]++;
                                // Mutually exclusive first differing stage in canonical order.
                                // Category 0 includes any leader identity/distance difference.
                                var category=!blockerSame||!distanceSame?0:
                                    now.Initial!=prior.Initial?1:
                                    now.Stop!=prior.Stop?2:
                                    now.Poses!=prior.Poses?3:
                                    now.Bend!=prior.Bend?4:
                                    now.Site!=prior.Site?5:
                                    now.Crossing!=prior.Crossing?6:
                                    !stepSame?7:8;
                                _pr57422First[category]++;
                            }"""
)
once(
    "            _pr57420Printed=true;",
    """            _pr57420Printed=true;
            if(Pr57422Enabled){
                Console.WriteLine("PR57_422_SUMMARY physical="+_pr57420Physical+
                    " readerChanged="+_pr57422ReaderChanged+
                    " readerStepChanged="+_pr57422ReaderStepChanged+
                    " sameLeaderStepChanged="+_pr57422SameLeaderStepChanged+
                    " leader="+_pr57422First[0]+
                    " initial="+_pr57422First[1]+
                    " stop="+_pr57422First[2]+
                    " poses="+_pr57422First[3]+
                    " bend="+_pr57422First[4]+
                    " site="+_pr57422First[5]+
                    " crossing="+_pr57422First[6]+
                    " steer="+_pr57422First[7]+
                    " unexplained="+_pr57422First[8]+
                    " dChanged="+_pr57422Inputs[0]+
                    " speedChanged="+_pr57422Inputs[1]+
                    " remainingChanged="+_pr57422Inputs[2]+
                    " stopEarlyChanged="+_pr57422Inputs[3]+
                    " observational=True causalClosure=False noDecisionReuse=True");
            }"""
)
p.write_text(s)
print("PR57_422_INJECTED observational=True noSkippedSlices=True noDecisionReuse=True")
