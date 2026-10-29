#!/usr/bin/env python3
"""PR57 #389: observational lane-limit relevance of post-writer winners.

Apply after #387 and #388 in a disposable checkout only. The same-winner
pre-writer projection is NOT a counterfactual rescan, decision or causal proof.
"""
from pathlib import Path

path=Path("src/IndexCities.Simulation/Mobility.cs")
s=path.read_text()
def patch(old,new):
    global s
    if s.count(old)!=1:
        raise SystemExit(f"#389 expected one anchor, got {s.count(old)}: {old[:100]!r}")
    s=s.replace(old,new,1)

patch(
    "    long _pr57Order387SelectedAfterPose, _pr57Order387CrossCohortSelected;",
    """    long _pr57Order387SelectedAfterPose, _pr57Order387CrossCohortSelected;
    long _pr57Order389Selected, _pr57Order389Binding, _pr57Order389Stopped;
    long _pr57Order389Cross, _pr57Order389CrossBinding;""")

patch(
    "                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));",
    """                var step=Math.Min(d,Math.Min(speed*remaining,Math.Max(0,leaderDistance-8)));
                // #389: count whether a live, already-mutated selected blocker
                // actually bounds the lane step, versus a distant winner whose
                // pose changed but cannot constrain this quantum's displacement.
                if(pr57OrderSample&&!enteringLane&&blocker is not null
                    &&pr57OrderPoseChanged!.Contains(blocker.Id)){
                    _pr57Order389Selected++;
                    var rawCap=Math.Min(d,speed*remaining);
                    var leaderCap=Math.Max(0,leaderDistance-8);
                    var binding=leaderCap<rawCap;
                    if(binding)_pr57Order389Binding++;
                    if(leaderCap<=0)_pr57Order389Stopped++;
                    if(pr57OrderCohort![v.Id]!=pr57OrderCohort[blocker.Id]){
                        _pr57Order389Cross++;
                        if(binding)_pr57Order389CrossBinding++;
                        var before=pr57Order388Before![blocker.Id];
                        // Same selected winner and current reader heading only;
                        // does not re-run eligibility, tie-breaks, or other candidates.
                        var preForward=(before.X-v.Position.X)*Math.Sin(v.Heading)
                            +(before.Z-v.Position.Z)*Math.Cos(v.Heading);
                        var preSameWinnerCap=Math.Max(0,preForward-8);
                        var preSameWinnerStep=Math.Min(rawCap,preSameWinnerCap);
                        Console.WriteLine(FormattableString.Invariant(
                            $"PR57_389_CROSS call={_pr57Order387PhysicalCalls} reader={v.Id} blocker={blocker.Id} rawCap={rawCap:R} leaderDistance={leaderDistance:R} postLeaderCap={leaderCap:R} postLaneStep={step:R} preForwardProjection={preForward:R} preSameWinnerCap={preSameWinnerCap:R} preSameWinnerStep={preSameWinnerStep:R} binding={binding} observationOnly=True noCounterfactualDecision=True"));
                    }
                }""")

patch(
    "        if(pr57OrderSample&&_pr57Order387PhysicalCalls==600)\n            Console.WriteLine($\"PR57_387_ORDER",
    """        if(pr57OrderSample&&_pr57Order387PhysicalCalls==600)
            Console.WriteLine($"PR57_389_BIND selectedAfterPose={_pr57Order389Selected} binding={_pr57Order389Binding} zeroLeaderCap={_pr57Order389Stopped} crossCohort={_pr57Order389Cross} crossBinding={_pr57Order389CrossBinding} samplePhysicalCalls={_pr57Order387PhysicalCalls} observationOnly=True noReorder=True noSkippedSlices=True");
        if(pr57OrderSample&&_pr57Order387PhysicalCalls==600)
            Console.WriteLine($"PR57_387_ORDER""")

path.write_text(s)
print("PR57_389_INJECTED observational=True noCounterfactualDecision=True")
