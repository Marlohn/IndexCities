#!/usr/bin/env python3
"""PR57 #352: source-guarded logical falsifiers for #351 candidate witness.

Run from an IndexCities checkout: python3 scripts/pr57-352-closure-falsifiers.py
No engine state changes. This is NOT a C# runtime replay or an A/B benchmark.
"""
from pathlib import Path


def source_checks(root: Path):
    witness = (root / "scripts/pr57-351-inject.py").read_text()
    shared = (root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs").read_text()
    towing = (root / "src/IndexCities.Simulation/Towing.cs").read_text()
    mobility = (root / "src/IndexCities.Simulation/Mobility.cs").read_text()
    lane = (root / "src/IndexCities.Simulation/Mobility.LaneEntry.cs").read_text()
    key = witness.split("static string Pr57351Key(Vehicle o)=>", 1)[1].split("void Pr57351Observe(", 1)[0]
    assert "State.Entry" not in key and "VehicleMovementTime" not in key
    assert all(x in key for x in ("o.Position.X", "o.Heading", "o.DockPosition"))
    assert "AtGateway(o)&&AtGateway(v)" in shared
    assert "bool AtGateway(Vehicle v)=>v.Position.Distance(State.Entry)<8;" in shared
    assert "o.DockPosition is {} at&&at.Distance(o.Position)<.1" in shared
    assert "bool TowHeldTooLong(Vehicle v,Vehicle? blocker)" in towing
    assert "VehicleMovementTime-v.WaitingSince>=State.Tuning.UTurnWaitSeconds" in towing
    assert "TowHeldTooLong(v,blocker)&&TryUTurn(v,blocker,TowClosedRoads(v))" in mobility
    assert "Vehicle EntryFrameVehicle(Vehicle current)" in lane
    assert "Pr57ScanLane(pr57LaneList);" in mobility
    print("source_guards=PASS")


def gateway_case(entry):
    # Canonical BodyAcrossLane early guard. Actor=(0,0), other=(0,4),
    # headings=0; other is a truck unloading at its own DockPosition.
    # All subsequent dock/body tests admit the 4m forward candidate.
    actor, other = (0.0, 0.0), (0.0, 4.0)
    def at_gateway(p):
        return ((p[0]-entry[0])**2+(p[1]-entry[1])**2)**0.5 < 8
    return None if at_gateway(actor) and at_gateway(other) else 4.0


def tow_gate(time, since=100.0, threshold=300.0):
    # The canonical TowHeldTooLong time clause, with its other conditions
    # fixed true. This branch is NOT claimed to occur in #351's 20 readers.
    return since > 0 and time-since >= threshold


def main():
    source_checks(Path(__file__).resolve().parents[1])
    assert gateway_case((0.0, 0.0)) is None
    assert gateway_case((100.0, 100.0)) == 4.0
    print("gateway_counterexample=PASS witness same; BodyAcrossLane None -> 4m")
    assert not tow_gate(399.999)
    assert tow_gate(400.0)
    print("clock_counterexample=PASS witness same; TowHeldTooLong false -> true")
    readers, candidates, slices = 20, 98, 6
    assert readers*candidates*slices == 11760
    print(f"snapshot_revalidation_visits={readers*candidates*slices} per_slice={readers*candidates}")
    print("result=full-snapshot-on-read cannot eliminate candidate visits; exact writer journal untested")


if __name__ == "__main__":
    main()
