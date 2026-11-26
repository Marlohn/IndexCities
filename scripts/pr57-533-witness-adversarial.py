#!/usr/bin/env python3
"""#533 bounded model of #532's positive-witness short circuit.

Replays the canonical BodyOnWay guards/contact rule on adversarial snapshots.
This is a model, not a C# integration test or proof of universal equivalence.
"""
from dataclasses import dataclass, replace
from math import sin, cos, hypot
from random import Random

@dataclass(frozen=True)
class Car:
    id: int
    x: float
    z: float
    h: float = 0.0
    half_length: float = 4.0
    half_width: float = 1.0
    status: str = "Travelling"
    tow_target: int = -1
    tow_truck: int = -1


def penetration(a, ap, ah, b, bp, bh):
    af = (sin(ah), cos(ah)); ar = (af[1], -af[0])
    bf = (sin(bh), cos(bh)); br = (bf[1], -bf[0])
    d = (bp[0] - ap[0], bp[1] - ap[1])
    dot = lambda u, v: u[0]*v[0] + u[1]*v[1]
    return min(a.half_length*abs(dot(af, axis)) + a.half_width*abs(dot(ar, axis))
               + b.half_length*abs(dot(bf, axis)) + b.half_width*abs(dot(br, axis))
               - abs(dot(d, axis)) for axis in (af, ar, bf, br))


def blocks(v, poses, o):
    reach = max(hypot(x-v.x, z-v.z) for x, z, _ in poses) + 12
    if (o.id == v.id or o.id == v.tow_target or o.tow_truck == v.id
        or o.status in ("Outside", "Towed")
        or hypot(o.x-v.x, o.z-v.z) > reach):
        return False
    # Gateway early guard is excluded: these fixtures are away from Entry.
    now = penetration(v, (v.x,v.z), v.h, o, (o.x,o.z), o.h)
    limit = max(-.1, now + .001)
    return any(hypot(x-o.x, z-o.z) < 12 and
               penetration(v, (x,z), h, o, (o.x,o.z), o.h) > limit
               for x,z,h in poses)


def canonical(v, poses, fleet):
    return next((o for o in fleet if blocks(v, poses, o)), None)


def hinted(v, poses, fleet, witness):
    if witness is not None and any(o is witness for o in fleet) and blocks(v, poses, witness):
        return witness, True
    return canonical(v, poses, fleet), False


def main():
    v = Car(1, 0, 0)
    poses = [(0.0, 0.0, 0.0), (0.0, 3.0, 0.0), (0.0, 6.0, 0.0)]
    first = Car(2, 20, 0)
    last = Car(3, 0, 7)
    assert canonical(v, poses, [first, last]) is last
    assert hinted(v, poses, [first, last], last) == (last, True)
    # A new earlier blocker changes the *first identity* but not the boolean.
    earlier = replace(first, x=0, z=9)
    assert canonical(v, poses, [earlier, last]) is earlier
    assert hinted(v, poses, [earlier, last], last) == (last, True)
    # A removed, moved or ineligible witness must fall back to full scanning.
    for changed in ([], [replace(last, x=100)], [replace(last, status="Outside")],
                    [replace(last, status="Towed")]):
        result, hit = hinted(v, poses, [first] + changed, last)
        assert result is None and not hit
    rng = Random(532)
    trials = 5000
    hits = 0
    for _ in range(trials):
        reader = Car(1, rng.uniform(-5,5), rng.uniform(-5,5), rng.uniform(-3,3))
        route = [(reader.x + rng.uniform(-5,5), reader.z + i*2, rng.uniform(-3,3))
                 for i in range(6)]
        fleet = [Car(i+2, rng.uniform(-20,20), rng.uniform(-20,20), rng.uniform(-3,3),
                     status=rng.choice(("Travelling", "Parked", "Outside", "Towed")))
                 for i in range(6)]
        old_witness = rng.choice(fleet)
        if rng.random() < .3:
            fleet = [o for o in fleet if o is not old_witness]
        else:
            fleet = [replace(o, x=o.x+rng.uniform(-8,8), z=o.z+rng.uniform(-8,8))
                     if o is old_witness and rng.random() < .5 else o for o in fleet]
        full = canonical(reader, route, fleet)
        got, hit = hinted(reader, route, fleet, old_witness)
        assert (got is None) == (full is None)
        if hit: hits += 1
    print(f"PR57_533_ADVERSARIAL_BOOLEAN_WITNESS_MODEL_PASS trials={trials} hits={hits} "
          "firstBlockerIdentityNotPreserved=True noCSharpParityClaim=True")

if __name__ == "__main__":
    main()
