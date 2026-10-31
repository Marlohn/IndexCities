#!/usr/bin/env python3
"""PR57 #388: identify the single cross-cohort winner observed by #387.

Apply ONLY after scripts/pr57-387-inject.py in a disposable checkout.
Observational: no reordered actor, no replaced predicate, no skipped slice.
The before pose is the prior actor's PRE-turn snapshot, not a counterfactual.
"""
from pathlib import Path

path=Path("src/IndexCities.Simulation/Mobility.cs")
s=path.read_text()
def patch(old,new):
    global s
    if s.count(old)!=1:
        raise SystemExit(f"#388 expected one #387 anchor; got {s.count(old)}: {old[:100]!r}")
    s=s.replace(old,new,1)

patch(
    "        var pr57OrderPoseChanged=pr57OrderSample?new HashSet<int>():null;",
    """        var pr57OrderPoseChanged=pr57OrderSample?new HashSet<int>():null;
        // #388: retain only PRE-turn scalar pose/status of changed canonical actors.
        var pr57Order388Before=pr57OrderSample
            ?new Dictionary<int,(double X,double Z,double Heading,VehicleStatus Status)>():null;""")

patch(
    """                    if(poseChanged){
                        pr57OrderPoseChanged!.Add(v.Id);
                        _pr57Order387PoseWriters++;
                    }""",
    """                    if(poseChanged){
                        pr57OrderPoseChanged!.Add(v.Id);
                        pr57Order388Before![v.Id]=(pr57OrderOldPosition.X,pr57OrderOldPosition.Z,
                            pr57OrderOldHeading,pr57OrderOldStatus);
                        _pr57Order387PoseWriters++;
                    }""")

patch(
    """                    if(pr57OrderCohort![v.Id]!=pr57OrderCohort[blocker.Id])
                        _pr57Order387CrossCohortSelected++;""",
    """                    if(pr57OrderCohort![v.Id]!=pr57OrderCohort[blocker.Id]){
                        _pr57Order387CrossCohortSelected++;
                        var before=pr57Order388Before![blocker.Id];
                        // No re-evaluation of HoldFor: describe the canonical winner only.
                        Console.WriteLine(FormattableString.Invariant(
                            $"PR57_388_CROSS call={_pr57Order387PhysicalCalls} time={State.ElapsedSeconds+_vehicleMovementOffset:R} reader={v.Id} blocker={blocker.Id} readerCohortWait={pr57OrderCohort[v.Id]} blockerCohortWait={pr57OrderCohort[blocker.Id]} readerX={v.Position.X:R} readerZ={v.Position.Z:R} readerHeading={v.Heading:R} leaderDistance={leaderDistance:R} blockerPreX={before.X:R} blockerPreZ={before.Z:R} blockerPreHeading={before.Heading:R} blockerPreStatus={before.Status} blockerPostX={blocker.Position.X:R} blockerPostZ={blocker.Position.Z:R} blockerPostHeading={blocker.Heading:R} blockerPostStatus={blocker.Status} blockerRouteIndex={blocker.RouteIndex} blockerWaitingSince={blocker.WaitingSince:R} scope=REAL_FIRST_600_PHYSICAL_CALLS observationOnly=True"));
                    }""")

path.write_text(s)
print("PR57_388_INJECTED cross_cohort_selected_trace=True noReorder=True")
