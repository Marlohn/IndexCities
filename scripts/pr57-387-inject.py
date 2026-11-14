#!/usr/bin/env python3
"""PR57 #387: bounded, observation-only intra-slice live read-after-write probe.
Run only in a disposable checkout; never writes canonical source to the branch.
It measures a lower bound, NOT a causal closure certificate or scheduler.
"""
from pathlib import Path

path=Path("src/IndexCities.Simulation/Mobility.cs")
source=path.read_text()
def replace_once(old,new):
    global source
    count=source.count(old)
    if count!=1:
        raise SystemExit(f"#387 expected one anchor, found {count}: {old[:90]!r}")
    source=source.replace(old,new,1)

replace_once(
    "public sealed partial class CitySimulation\n{",
    """public sealed partial class CitySimulation
{
    // #387: opt-in observational lower bound on live intra-slice dependencies.
    static readonly bool Pr57Order387Enabled=Environment.GetEnvironmentVariable("PR57_387_ORDER_SHADOW")=="1";
    long _pr57Order387PhysicalCalls, _pr57Order387PoseWriters, _pr57Order387OtherWriters;
    long _pr57Order387Scans, _pr57Order387LiveReadsAfterPose, _pr57Order387CrossCohortReads;
    long _pr57Order387SelectedAfterPose, _pr57Order387CrossCohortSelected;
""")
replace_once(
    "        foreach(var v in ordered){",
    """        // Cohorts are PRE-slice WaitingSince==0 versus >0, not a proposal to reorder.
        // Keep the canonical WaitingSince/Id order and all effects unchanged.
        var pr57OrderSample=Pr57Order387Enabled&&dt>0&&_pr57Order387PhysicalCalls<600;
        if(pr57OrderSample)_pr57Order387PhysicalCalls++;
        var pr57OrderPoseChanged=pr57OrderSample?new HashSet<int>():null;
        var pr57OrderCohort=pr57OrderSample?ordered.ToDictionary(o=>o.Id,o=>o.WaitingSince>0):null;
        foreach(var v in ordered){
            var pr57OrderOldPosition=pr57OrderSample?v.Position:default;
            var pr57OrderOldHeading=pr57OrderSample?v.Heading:0;
            var pr57OrderOldStatus=pr57OrderSample?v.Status:default;
            var pr57OrderOldRouteIndex=pr57OrderSample?v.RouteIndex:0;
            var pr57OrderOldWaitingSince=pr57OrderSample?v.WaitingSince:0;
            var pr57OrderOldClaim=pr57OrderSample?v.IntersectionClaim:null;
            var pr57OrderOldSpaces=pr57OrderSample?v.ManoeuvreSpaces.Count:0;
""")
replace_once(
    "                foreach(var liveOther in candidates){",
    """                foreach(var liveOther in candidates){
                    if(pr57OrderSample){
                        _pr57Order387Scans++;
                        // Entry-frame clones are intentionally frozen; only live reads count.
                        if(!enteringLane&&liveOther.Id!=v.Id&&pr57OrderPoseChanged!.Contains(liveOther.Id)){
                            _pr57Order387LiveReadsAfterPose++;
                            if(pr57OrderCohort![v.Id]!=pr57OrderCohort[liveOther.Id])
                                _pr57Order387CrossCohortReads++;
                        }
                    }
""")
replace_once(
    "                if(blocker is not null)Pr57CausalWinner(v,blocker);",
    """                if(pr57OrderSample&&!enteringLane&&blocker is not null&&pr57OrderPoseChanged!.Contains(blocker.Id)){
                    _pr57Order387SelectedAfterPose++;
                    if(pr57OrderCohort![v.Id]!=pr57OrderCohort[blocker.Id])
                        _pr57Order387CrossCohortSelected++;
                }
                if(blocker is not null)Pr57CausalWinner(v,blocker);""")
replace_once(
    "                Pr57PhaseImpactAfterActor(v);",
    """                Pr57PhaseImpactAfterActor(v);
                if(pr57OrderSample){
                    // Net changes only: misses transient mutations and all unlisted fields.
                    var poseChanged=BitConverter.DoubleToInt64Bits(v.Position.X)!=BitConverter.DoubleToInt64Bits(pr57OrderOldPosition.X)
                        ||BitConverter.DoubleToInt64Bits(v.Position.Z)!=BitConverter.DoubleToInt64Bits(pr57OrderOldPosition.Z)
                        ||BitConverter.DoubleToInt64Bits(v.Heading)!=BitConverter.DoubleToInt64Bits(pr57OrderOldHeading)
                        ||v.Status!=pr57OrderOldStatus;
                    if(poseChanged){
                        pr57OrderPoseChanged!.Add(v.Id);
                        _pr57Order387PoseWriters++;
                    }
                    if(v.RouteIndex!=pr57OrderOldRouteIndex
                        ||BitConverter.DoubleToInt64Bits(v.WaitingSince)!=BitConverter.DoubleToInt64Bits(pr57OrderOldWaitingSince)
                        ||v.IntersectionClaim!=pr57OrderOldClaim||v.ManoeuvreSpaces.Count!=pr57OrderOldSpaces)
                        _pr57Order387OtherWriters++;
                }
""")
replace_once(
    "        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36",
    """        if(pr57OrderSample&&_pr57Order387PhysicalCalls==600)
            Console.WriteLine($"PR57_387_ORDER physicalCalls={_pr57Order387PhysicalCalls} poseWriters={_pr57Order387PoseWriters} otherWriters={_pr57Order387OtherWriters} laneCandidateReads={_pr57Order387Scans} liveReadsAfterPose={_pr57Order387LiveReadsAfterPose} crossCohortReads={_pr57Order387CrossCohortReads} selectedAfterPose={_pr57Order387SelectedAfterPose} crossCohortSelected={_pr57Order387CrossCohortSelected} scope=REAL_FIRST_600_PHYSICAL_CALLS_LOWER_BOUND noReorder=True noSkippedSlices=True");
        if(pr57PoseNoopSkip&&scanProfile is not null&&dt>0&&_pr57MovementAllocCalls>=36""")
path.write_text(source)
print("PR57_387_INJECTED observational=True changedCanonicalRules=False")
