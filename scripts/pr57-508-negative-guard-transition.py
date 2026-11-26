#!/usr/bin/env python3
"""PR57 #508: adversarial negative-guard transitions for four-reader graph #504/#505.

A bounded, synthetic guard-level falsification, NOT a .NET city replay, a
performance benchmark, or proof that any transition occurred in #505.
Optionally validate exact C# reader/writer anchors against a local checkout:
    python3 scripts/pr57-508-negative-guard-transition.py /path/to/checkout
"""
from dataclasses import dataclass, replace
from math import hypot
from pathlib import Path
import sys

@dataclass(frozen=True)
class Vehicle:
    id: int
    x: float
    z: float
    status: str = 'Travelling'
    waiting: float = 0.0
    towed_id: int = -1
    tow_truck_id: int = -1
    machine: str = 'None'
    leaving: bool = False
    gateway: bool = False
    spaces: tuple[int, ...] = ()


def distance(a, b):
    return hypot(a.x-b.x, a.z-b.z)


def body_guard(reader, other, reach):
    # Mirrors BodyOnWay's ordered guard; only the guard, NOT SAT/first blocker.
    return not (other.id == reader.id or other.id == reader.towed_id or
                other.tow_truck_id == reader.id or
                other.status in ('Outside', 'Towed') or
                distance(other, reader) > reach or
                (other.gateway and reader.gateway))


def traffic_guard(reader, other, seconds, highway_speed):
    # Mirrors TrafficOnWay's ordered guard; not the forward sweep.
    return not (other.id == reader.id or other.id == reader.towed_id or
                other.status != 'Travelling' or other.waiting > 0 or
                other.leaving or other.machine != 'None' or
                distance(other, reader) > seconds*highway_speed+20)


def space_holds(other, node):
    # A direct subset of SpaceFree's Holds/ManoeuvreSpaces admission predicate.
    return node in other.spaces


def assert_source(root):
    mobility = (root/'src/IndexCities.Simulation/Mobility.cs').read_text()
    shared = (root/'src/IndexCities.Simulation/Mobility.SharedSpace.cs').read_text()
    checks = {
        'BodyOnWay distance/status negative guard': (
            shared, 'o.Status is VehicleStatus.Outside or VehicleStatus.Towed||o.Position.Distance(v.Position)>reach'),
        'TrafficOnWay waiting negative guard': (
            shared, 'o.Status!=VehicleStatus.Travelling||o.WaitingSince>0||Leaving(o)'),
        'TrafficOnWay distance negative guard': (
            shared, 'o.Position.Distance(v.Position)>seconds*State.Tuning.HighwaySpeed+20'),
        'SpaceFree reservation read': (
            shared, 'if(Holds(o,node.Id)||area!=null&&'),
        'Holds implementation': (
            shared, 'static bool Holds(Vehicle o,int node)=>o.IntersectionClaim==node||o.ManoeuvreSpaces.Contains(node);'),
        'admission reservation writer': (
            shared, 'v.ManoeuvreSpaces=v.ManoeuvreSpaces.Concat(spaces.Select(s=>s.Id))'),
        'physical position and waiting writer': (
            mobility, 'v.Position=v.Position.MoveTowards(target,step);v.AddFuel('),
        'physical waiting reset': (
            mobility, 'remaining-=step/speed;v.WaitingSince=0;'),
        'actor sequential processing': (
            mobility, 'foreach(var v in ordered){'),
        'existing in-slice candidate maintenance': (
            mobility, 'If an initially parked car starts moving during this pass'),
    }
    for name, (source, anchor) in checks.items():
        assert source.count(anchor) == 1, (name, source.count(anchor))
    print(f'SOURCE_ANCHORS_PASS {len(checks)}')


def main():
    if len(sys.argv) > 2:
        raise SystemExit('usage: script.py [checkout-root]')
    if len(sys.argv) == 2:
        assert_source(Path(sys.argv[1]))
    else:
        print('SOURCE_ANCHORS_NOT_CHECKED (run with checkout-root; anchors separately audited on GitHub)')
    reader = Vehicle(1, 0, 0)
    # BodyOnWay: a short 2m path yields reach=2+12=14m. A candidate that
    # moved 0.5m can enter the guard without any reader-side mutation.
    reach = 2 + 12
    far = Vehicle(2, 0, 14.25)
    near = replace(far, z=13.75)
    assert not body_guard(reader, far, reach)
    assert body_guard(reader, near, reach)
    # An eligible-only certificate has no edge to ID=2 at t0 and misses it at t1.
    cached_eligible = {o.id for o in [far] if body_guard(reader, o, reach)}
    fresh_eligible = {o.id for o in [near] if body_guard(reader, o, reach)}
    assert cached_eligible == set() and fresh_eligible == {2}
    print('BODY_DISTANCE_NEGATIVE_TO_ELIGIBLE_PASS old=14.25m new=13.75m reach=14m')

    # TrafficOnWay: the same candidate changes guard result when motion clears
    # WaitingSince. The actual movement path also updates Position and fuel.
    waiting = Vehicle(3, 0, 10, waiting=12.0)
    moving = replace(waiting, waiting=0.0, z=9.5)
    seconds, highway_speed = 3.0, 20.0
    assert not traffic_guard(reader, waiting, seconds, highway_speed)
    assert traffic_guard(reader, moving, seconds, highway_speed)
    assert traffic_guard(reader, replace(waiting, waiting=0), seconds, highway_speed)
    print('TRAFFIC_WAITING_NEGATIVE_TO_ELIGIBLE_PASS waiting=12->0, position=10->9.5m')

    # A fresh reservation in an earlier actor becomes visible to a later reader
    # in the same slice; it need not alter Position or ElapsedSeconds.
    free = Vehicle(4, 1, 1)
    admitted = replace(free, spaces=(7,))
    assert not space_holds(free, 7) and space_holds(admitted, 7)
    assert (free.x, free.z, free.waiting) == (admitted.x, admitted.z, admitted.waiting)
    print('SPACE_RESERVATION_NEGATIVE_TO_HOLDER_PASS node=7 no pose/time change')

    # This is a *guard* counterexample, not a SAT collision or real-city event.
    # It falsifies reusing only the previously eligible edges. It does not
    # falsify writer-published versions with complete negative subscriptions.
    print('PR57_508_NEGATIVE_GUARD_COUNTEREXAMPLES_PASS')

if __name__ == '__main__':
    main()
