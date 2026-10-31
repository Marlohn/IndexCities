#!/usr/bin/env python3
"""PR57 #534: bounded, executable BayEntryHold plan-ownership counterexample.

Synthetic model, NOT the .NET engine. Demonstrates that a workset keyed only by
vehicle/route is stale after a same-visit dock relocation or external owner
write; a reentrant, versioned lease preserves the modeled first-blocker order.
The C# source-anchor check is optional: pass a checkout root as the argument.
No cache, rule, or optimization is installed in IndexCities by this script.
"""
from __future__ import annotations
from dataclasses import dataclass
from pathlib import Path
from random import Random
import sys


@dataclass(frozen=True)
class World:
    dock: int
    route: tuple[int, ...]
    pose: int
    heading: int
    owner_epoch: int
    owners: tuple[tuple[int, int], ...]  # original iteration order (id, dock)


@dataclass(frozen=True)
class Plan:
    dock: int
    poses: tuple[int, ...]
    spaces: tuple[int, ...]
    owner_exits: tuple[tuple[int, int], ...]


def plan(w: World) -> Plan:
    # Synthetic two-stream plan: the spaces stream contains a pivot contact
    # absent from the emitted poses, as required by #500. This is NOT C# geometry.
    poses = (w.pose, w.dock, w.dock + (w.heading % 3))
    spaces = (w.route[0], w.dock // 4) if w.route else (w.dock // 4,)
    return Plan(w.dock, poses, spaces, w.owners)


def consume(w: World, p: Plan) -> tuple[bool, int | None, tuple[int, ...]]:
    # A deliberately bounded analogue of first-blocker-order semantics.
    # The owner order, not ID sort order, determines the first blocker.
    first = next((oid for oid, odock in p.owner_exits
                  if odock == p.dock or odock in p.poses), None)
    return first is None, first, p.spaces


class Workset:
    def __init__(self) -> None:
        self._cache: dict[tuple, Plan] = {}
        self._live: list[Plan] = []
        self.builds = 0
        self.hits = 0

    def lease(self, w: World, *, exact: bool) -> Plan:
        key = ((w.dock, w.route, w.pose, w.heading, w.owner_epoch, w.owners)
               if exact else (w.route,))
        p = self._cache.get(key)
        if p is None:
            p = plan(w)
            self._cache[key] = p
            self.builds += 1
        else:
            self.hits += 1
        # A nested producer may run before the outer plan's last reader.
        self._live.append(p)
        return p

    def release(self, p: Plan) -> None:
        assert self._live and self._live[-1] is p, 'non-LIFO plan lease'
        self._live.pop()


def evaluate(w: World, ws: Workset | None = None, *, exact: bool = True):
    p = plan(w) if ws is None else ws.lease(w, exact=exact)
    # Nested plan must not overwrite the outer plan or change first-blocker order.
    if ws is not None:
        nested = World(w.dock + 37, w.route, w.pose + 9, w.heading,
                       w.owner_epoch, w.owners)
        q = ws.lease(nested, exact=exact)
        assert q.poses[0] == nested.pose or not exact
        ws.release(q)
    answer = consume(w, p)
    if ws is not None:
        ws.release(p)
    return answer


def source_contract(root: Path) -> None:
    path = root / 'src/IndexCities.Simulation/Mobility.SharedSpace.cs'
    s = path.read_text(encoding='utf-8')
    anchors = (
        'var way=EntryPlan(v,dock,out var spaces,out var limit,out var needed);',
        'v.DockPosition=dock=revised;way=EntryPlan(v,dock,out spaces,out limit,out needed);',
        'if(!occupied&&AdmitManoeuvre(v,way,spaces,dock,false,limit))',
        'var exits=OwnersWayOut(v,dock);',
        'if(!safe)way=WayIn(v,dock,out spaces);',
        'var way=EntryPlan(o,dock,out _,out _,out var needed);',
        'if(BodyOnWay(v,way) is {} body)',
    )
    for anchor in anchors:
        assert s.count(anchor) == 1, f'changed source contract: {anchor}'
    a = s.index(anchors[0]); b = s.index(anchors[1]); c = s.index(anchors[2]);
    assert a < b < c, 'BayEntryHold ordering changed'
    print('PR57_534_GITHUB_SOURCE_ANCHORS_PASS')


def main() -> None:
    if len(sys.argv) > 2:
        raise SystemExit('usage: script.py [checkout-root]')
    if len(sys.argv) == 2:
        source_contract(Path(sys.argv[1]))
    initial = World(8, (1, 2, 3), 0, 1, 0, ((41, 8), (17, 12)))
    relocated = World(12, initial.route, initial.pose, initial.heading,
                      initial.owner_epoch, initial.owners)
    # Old owner 41 obstructs dock 8; new dock 12 is obstructed by owner 17.
    stale = Workset()
    before = evaluate(initial, stale, exact=False)
    after_stale = evaluate(relocated, stale, exact=False)
    after_ref = evaluate(relocated)
    assert before[1] == 41 and after_ref[1] == 17
    assert after_stale != after_ref, 'stale per-vehicle/route cache unexpectedly safe'
    exact = Workset()
    assert evaluate(initial, exact) == before
    assert evaluate(relocated, exact) == after_ref

    # External owner moves without a change to the reader, route or dock.
    moved = World(8, initial.route, initial.pose, initial.heading, 1,
                  ((41, 100), (17, 8)))
    assert evaluate(moved)[1] == 17
    assert evaluate(moved, exact) == evaluate(moved)

    rng = Random(534)
    ws = Workset()
    trials = 5000
    for _ in range(trials):
        route = tuple(rng.randrange(1, 12) for _ in range(rng.randrange(1, 5)))
        dock = rng.randrange(4, 32)
        owners = tuple((i, rng.randrange(4, 32)) for i in rng.sample(range(100, 120), 5))
        w = World(dock, route, rng.randrange(0, 30), rng.randrange(0, 8),
                  rng.randrange(0, 40), owners)
        assert evaluate(w, ws) == evaluate(w), 'versioned lease mismatch'
        assert evaluate(w, ws) == evaluate(w), 'stable-input reuse mismatch'
        # A subsequent writer changes an owner, keeping reader fields identical.
        owners2 = list(owners)
        owners2[0] = (owners2[0][0], dock)
        changed = World(dock, route, w.pose, w.heading, w.owner_epoch + 1,
                        tuple(owners2))
        assert evaluate(changed, ws) == evaluate(changed), 'owner invalidation missed'
    assert ws.hits >= trials and not ws._live and not exact._live and not stale._live
    print(f'PR57_534_DOCK_REPLAN_WORKSET_MODEL_PASS trials={trials} '
          f'cacheHits={ws.hits} staleWitness={after_stale[1]} '
          f'correctWitness={after_ref[1]}')


if __name__ == '__main__':
    main()
