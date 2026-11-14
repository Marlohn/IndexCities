#!/usr/bin/env python3
"""PR57 #501: adversarial, source-anchored model of a *bounded* dual-stream plan.

This does NOT execute the .NET engine. It tests the semantics that an eventual
short-lived physical plan must preserve: transient contact headings, ordered
emitted poses, reader-relative reach, first-blocker order, live vehicle reads,
append invalidation, and reentrancy. The only cached scalar is immutable path
length; collision decisions and BodyOnWay reach are deliberately live.

Usage: python3 scripts/pr57-501-dual-stream-contract.py [Mobility.SharedSpace.cs]
"""
from __future__ import annotations

from dataclasses import dataclass, replace
import hashlib
import math
import random
import sys
from pathlib import Path

SOURCE_BLOB = '78c69c5525e2a3b4ff583f8fe486b4dbc82af7a9'


def source_contract(path: Path):
    raw = path.read_bytes()
    blob = hashlib.sha1(b'blob ' + str(len(raw)).encode() + b'\0' + raw).hexdigest()
    assert blob == SOURCE_BLOB, (blob, SOURCE_BLOB)
    text = raw.decode()
    anchors = (
        'for(var part=0;part<=8;part++)Note(at,facing+turn*part/8);held.Add((o,at,direction));',
        'foreach(var n in near)if(!found.Contains(n)&&EntersArea(o,p,h,n))found.Add(n);',
        'var index=way.FindIndex(p=>EntersArea(v,p.Position,p.Heading,space));var from=index>0?way[index-1].Position:v.Position;',
        'if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;',
        'if(poses.Count==0)return null;var length=0.0;for(var i=1;i<poses.Count;i++)length+=poses[i-1].Position.Distance(poses[i].Position);',
        'foreach(var o in State.Vehicles){',
        'if(yieldToTraffic&&ConflictsWithDockWay(v,way))',
        'if(yieldToTraffic&&TrafficOnWay(v,way) is {})',
        'if(BodyOnWay(v,way) is {} body)',
        '=>State.Vehicles.Any(o=>o.Id!=v.Id&&o.ManoeuvreDock!=null&&',
    )
    for anchor in anchors:
        assert anchor in text, anchor
    print('PR57_501_SOURCE_CONTRACT_PASS', blob)


@dataclass(frozen=True)
class Vehicle:
    id: int
    x: float
    z: float
    heading: float = 0.0
    length: float = 4.0  # half-length
    width: float = 1.0   # half-width
    status: str = 'Travelling'
    waiting: float = 0.0
    machine: bool = False
    leaving: bool = False
    tow_target: int = -1
    tow_truck: int = -1
    dock_owner: bool = False


@dataclass(frozen=True)
class Pose:
    x: float
    z: float
    heading: float


@dataclass(frozen=True)
class Node:
    id: int
    x: float
    z: float


def distance(ax, az, bx, bz):
    return math.hypot(ax-bx, az-bz)


def enters(v, p, n, clearance=3.3):
    dx, dz = abs(p.x-n.x), abs(p.z-n.z)
    s, c = abs(math.sin(p.heading)), abs(math.cos(p.heading))
    return dx < clearance+v.length*s+v.width*c and dz < clearance+v.length*c+v.width*s


def penetration(a, ap, b, bp):
    af = (math.sin(ap.heading), math.cos(ap.heading))
    ar = (af[1], -af[0])
    bf = (math.sin(bp.heading), math.cos(bp.heading))
    br = (bf[1], -bf[0])
    d = (bp.x-ap.x, bp.z-ap.z)
    dot = lambda x, y: x[0]*y[0]+x[1]*y[1]
    return min(a.length*abs(dot(af, axis))+a.width*abs(dot(ar, axis))+
               b.length*abs(dot(bf, axis))+b.width*abs(dot(br, axis))-
               abs(dot(d, axis)) for axis in (af, ar, bf, br))


def vpose(v):
    return Pose(v.x, v.z, v.heading)


@dataclass(frozen=True)
class Plan:
    # No mutable List alias; a nested plan cannot overwrite the outer plan.
    poses: tuple[Pose, ...] = ()
    spaces: tuple[Node, ...] = ()
    length: float = 0.0

    def append(self, pose, notes=()):
        length = self.length
        if self.poses:
            prev = self.poses[-1]
            length += distance(prev.x, prev.z, pose.x, pose.z)
        spaces = list(self.spaces)
        for node in notes:
            if node not in spaces:
                spaces.append(node)
        return Plan(self.poses+(pose,), tuple(spaces), length)

    def note_only(self, notes):
        spaces = list(self.spaces)
        for node in notes:
            if node not in spaces:
                spaces.append(node)
        return Plan(self.poses, tuple(spaces), self.length)


def note_nodes(v, pose, nodes):
    return [n for n in nodes if enters(v, pose, n)]


def make_plan(v, poses, nodes, transient=()):
    plan = Plan()
    # The actual Manoeuvre invokes Note on transient headings before Add.
    for pose in transient:
        plan = plan.note_only(note_nodes(v, pose, nodes))
    for pose in poses:
        plan = plan.append(pose, note_nodes(v, pose, nodes))
    return plan


def raw_length(poses):
    length = 0.0
    for i in range(1, len(poses)):
        a, b = poses[i-1], poses[i]
        length += distance(a.x, a.z, b.x, b.z)
    return length


def body_on_way(v, poses, vehicles, trace):
    if not poses:
        return None
    reach = max(distance(p.x, p.z, v.x, v.z) for p in poses)+12
    for o in vehicles:
        trace.append(('body-visit', o.id))
        if (o.id in (v.id, v.tow_target) or o.tow_truck == v.id or
            o.status in ('Outside', 'Towed') or
            distance(o.x, o.z, v.x, v.z)>reach):
            continue
        now = penetration(v, vpose(v), o, vpose(o))
        limit = max(-.1, now+.001)
        for i, p in enumerate(poses):
            if distance(p.x, p.z, o.x, o.z)<12:
                trace.append(('body-sat', o.id, i))
                if penetration(v, p, o, vpose(o))>limit:
                    return o.id
    return None


def traffic_on_way(v, poses, vehicles, trace, length=None):
    if not poses:
        return None
    if length is None:
        length = raw_length(poses)
    seconds = length/5.0+1.0  # fixed synthetic tuning; same in both arms
    for o in vehicles:
        trace.append(('traffic-visit', o.id))
        if (o.id in (v.id, v.tow_target) or o.status!='Travelling' or
            o.waiting>0 or o.leaving or o.machine or
            distance(o.x, o.z, v.x, v.z)>seconds*10.0+20):
            continue
        forward = (math.sin(o.heading), math.cos(o.heading))
        reach = 5.0*seconds
        run = 0.0
        while run<=reach:
            at = Pose(o.x+forward[0]*run, o.z+forward[1]*run, o.heading)
            for i, p in enumerate(poses):
                if distance(p.x, p.z, at.x, at.z)<12:
                    trace.append(('traffic-sat', o.id, run, i))
                    if penetration(v, p, o, at)>-.1:
                        return o.id
            run += 1.0
    return None


def dock_conflict(v, poses, owners, trace):
    for owner, owner_way in owners:
        trace.append(('dock-owner', owner.id))
        if owner.id==v.id or not owner.dock_owner:
            continue
        for j, q in enumerate(owner_way):
            for i, p in enumerate(poses):
                if distance(p.x, p.z, q.x, q.z)<12:
                    trace.append(('dock-sat', owner.id, j, i))
                    if penetration(v, p, owner, q)>0:
                        return True
    return False


def admission(v, poses, spaces, vehicles, owners, blocked, trace, cached_length=None):
    # A bounded model of the *ordered* pre-admission gates, not a replacement
    # for the full C# admission/reservation/mutation logic.
    if dock_conflict(v, poses, owners, trace):
        return 'dock'
    for node in spaces:
        first = next((i for i, p in enumerate(poses) if enters(v, p, node)), -1)
        prior = poses[first-1] if first>0 else vpose(v)
        trace.append(('space', node.id, first, prior.x, prior.z))
        if node.id in blocked:
            return 'space'
    if traffic_on_way(v, poses, vehicles, trace, cached_length) is not None:
        return 'traffic'
    if body_on_way(v, poses, vehicles, trace) is not None:
        return 'body'
    return 'accepted'


def check(v, plan, vehicles, owners=(), blocked=()):
    a, b = [], []
    reference = admission(v, list(plan.poses), list(plan.spaces), vehicles, owners, blocked, a)
    candidate = admission(v, plan.poses, plan.spaces, vehicles, owners, blocked, b, plan.length)
    assert (reference, a)==(candidate, b), (reference, candidate, a, b)
    return reference


def main():
    assert len(sys.argv)<=2
    if len(sys.argv)==2:
        source_contract(Path(sys.argv[1]))
    actor = Vehicle(1, 6.0, 6.0)
    node = Node(10, 0.0, 0.0)
    headings = [math.pi*i/16 for i in range(9)]
    transients = [Pose(6, 6, h) for h in headings]
    plan = make_plan(actor, [transients[-1]], [node], transients)
    assert tuple(n.id for n in plan.spaces)==(10,)
    assert all(not enters(actor, p, node) for p in plan.poses)
    t=[]
    assert admission(actor, plan.poses, plan.spaces, [], [], {10}, t, plan.length)=='space'
    assert ('space', 10, -1, 6.0, 6.0) in t
    assert check(actor, plan, [], blocked={10})=='space'

    # Reader-relative reach: a producer-relative cached radius can omit a
    # vehicle which the actual BodyOnWay(reader, way) must visit.
    far = Vehicle(2, 120, 0, status='Parked')
    other_reader = Vehicle(3, 0, 0)
    remote = Plan().append(Pose(120, 0, 0))
    producer_reach = max(distance(p.x, p.z, 120, 0) for p in remote.poses)+12
    reader_reach = max(distance(p.x, p.z, other_reader.x, other_reader.z) for p in remote.poses)+12
    assert producer_reach != reader_reach
    assert producer_reach==12 and reader_reach==132
    assert check(other_reader, remote, [far])=='body'
    # Live state must not be frozen in the plan; moving a candidate can flip it.
    near = Vehicle(4, 6, 6, heading=math.pi/2)
    live_plan = make_plan(actor, [Pose(6, 7, 0), Pose(6, 9, 0)], [])
    first = check(actor, live_plan, [near])
    second = check(actor, live_plan, [replace(near, x=200, z=200)])
    assert first != second, (first, second)

    # A plan append changes length; nested plans never mutate a held outer view.
    outer = Plan().append(Pose(0, 0, 0)).append(Pose(0, 10, 0))
    nested = outer.append(Pose(0, 30, 0))
    assert outer.length==10 and nested.length==30
    assert len(outer.poses)==2 and len(nested.poses)==3

    rng = random.Random(501)
    counts = {'dock':0,'space':0,'traffic':0,'body':0,'accepted':0}
    for _ in range(1200):
        v = Vehicle(1, rng.uniform(-15,15), rng.uniform(-15,15), rng.uniform(-3,3))
        nodes = [Node(i+10,rng.uniform(-15,15),rng.uniform(-15,15)) for i in range(rng.randrange(4))]
        poses = [Pose(rng.uniform(-20,20),rng.uniform(-20,20),rng.uniform(-3,3)) for _ in range(rng.randrange(1,7))]
        transient = [Pose(rng.uniform(-20,20),rng.uniform(-20,20),rng.uniform(-3,3)) for _ in range(rng.randrange(3))]
        p = make_plan(v, poses, nodes, transient)
        vs = [Vehicle(i+2,rng.uniform(-20,20),rng.uniform(-20,20),rng.uniform(-3,3),
                      status=rng.choice(('Travelling','Travelling','Parked','Outside')),
                      waiting=rng.choice((0.0,0.0,1.0))) for i in range(rng.randrange(5))]
        owners = [(replace(o, dock_owner=True),tuple(poses[:rng.randrange(1,len(poses)+1)]))
                  for o in vs[:rng.randrange(len(vs)+1)]]
        blocked = {n.id for n in nodes if rng.random()<.2}
        outcome = check(v, p, vs, owners, blocked)
        counts[outcome]+=1
        assert p.length==raw_length(p.poses)
    assert sum(counts.values())==1200 and counts['accepted']>0 and counts['body']>0
    print('PR57_501_DUAL_STREAM_MODEL_PARITY_PASS', counts)
    print('adversarial=transient_heading_contact,reader_relative_reach,live_state,append_version,nested_ownership')
    print('LIMIT=synthetic_model_not_engine_parity_not_wall_speedup_not_causal_invalidation_proof')


if __name__=='__main__':
    main()
