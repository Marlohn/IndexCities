#!/usr/bin/env python3
"""PR57 #496: authentic allocation topology + bounded scratch-lifetime falsification.

Usage: python3 scripts/pr57-496-workset-lifetime.py pr57-493-original.zip [checkout-root]
The ZIP is the original GitHub Actions #41150158318 artifact #13022328951.
No engine, .NET timing, or causal-equivalence claim is made by this probe.
"""
import hashlib
import math
import pathlib
import re
import sys
import zipfile

EXPECTED_SHA256 = "f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89"


def row(log, prefix, calls=600):
    hits = [line for line in log.splitlines() if line.startswith(prefix + " ") and
            re.search(r"\bcalls=" + str(calls) + r"\b", line)]
    assert len(hits) == 1, (prefix, len(hits))
    return {k: float(v) if "." in v else int(v)
            for k, v in re.findall(r"([A-Za-z][A-Za-z0-9]*)=(-?\d+(?:\.\d+)?)", hits[0])}


def same(a, b, key):
    assert a[key] == b[key], (key, a[key], b[key])


class DepthPool:
    """Minimal model: lease one independent list per live invocation, no sharing until return."""
    def __init__(self):
        self.free = []
        self.live = []

    def acquire(self, values):
        value = self.free.pop() if self.free else []
        value.clear()
        value.extend(values)
        self.live.append(value)
        return value

    def release(self, value):
        assert self.live and self.live[-1] is value, "LIFO lifetime violation"
        self.live.pop()
        value.clear()
        self.free.append(value)


def alias_counterexamples():
    # ClearOfDockWay captures the corridor list. A subsequent Manoeuvre is allowed
    # to call extraClear while the first list is still live.
    global_scratch = []
    def unsafe(values):
        global_scratch.clear()
        global_scratch.extend(values)
        return global_scratch
    corridor = unsafe(["exit-a", "exit-b"])
    expected = tuple(corridor)
    outer = unsafe(["approach"])
    assert tuple(corridor) != expected and outer is corridor

    pool = DepthPool()
    corridor = pool.acquire(["exit-a", "exit-b"])
    expected = tuple(corridor)
    outer = pool.acquire(["approach"])
    assert tuple(corridor) == expected and outer is not corridor
    pool.release(outer)
    assert tuple(corridor) == expected
    pool.release(corridor)

    # MayEnterJunction retains 'way' across SpaceFree(false), which can call
    # JunctionPreferenceAllows -> CanTurnNow -> TurnWay (nested). A singleton
    # scratch would replace the outer way before its next shared-space check.
    way = unsafe(["first-space", "second-space"])
    nested = unsafe(["competitor-space"])
    assert way is nested and "second-space" not in way
    way = pool.acquire(["first-space", "second-space"])
    nested = pool.acquire(["competitor-space"])
    assert "second-space" in way and nested is not way
    pool.release(nested)
    assert "second-space" in way
    pool.release(way)
    assert not pool.live


def source_contract(root):
    files = {
        "mobility": root / "src/IndexCities.Simulation/Mobility.cs",
        "shared": root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs",
    }
    texts = {k: v.read_text(encoding="utf-8") for k, v in files.items()}
    anchors = {
        "shared": [
            "var corridor=Manoeuvre(o,route,0,at,heading,site,false,int.MaxValue",
            "return (p,h)=>!corridor.Any(",
            "extraClear?.Invoke(at,facing)",
            "limit==null?ClearOfDockWay(",
            "var way=EntryPlan(o,dock,out _,out _,out var needed);",
            "var turn=TurnWay(o,out _);",
            "var body=BodyOnWay(o,turn);",
            "var heading=v.Heading;v.Heading=Math.Atan2(entering.X,entering.Z);",
            "var allowed=JunctionPreferenceAllows(v,node);v.Heading=heading;",
            "v.MovementAdmission=new(){Route=v.Route.ToList(),SitePath=v.SitePath.ToList()",
            "v.DockCorridor=PhysicalDockWay(",
            "v.ManoeuvreSpaces=v.ManoeuvreSpaces.Concat(",
        ],
        "mobility": [
            "var way=TurnWay(v,out var spaces);",
            "if(!SpaceFree(v,space,space.Position-from,false))",
            "var competitors=State.Vehicles.Where(",
            "CanTurnNow(o)).ToList();",
            "var onward=TurnWay(v,out var crossed);",
            "var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);",
        ],
    }
    for key, parts in anchors.items():
        for part in parts:
            assert part in texts[key], (key, part)
    return sum(map(len, anchors.values()))


def main():
    assert len(sys.argv) in (2, 3), __doc__
    path = pathlib.Path(sys.argv[1])
    assert hashlib.sha256(path.read_bytes()).hexdigest() == EXPECTED_SHA256
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
        b = [archive.read(f"congestedREAL121/{arm}/full.log").decode("utf-8")
             for arm in ("B1", "B2")]
    keys = ["PR57_ALLOC_REGIONS", "PR57_JUNCTION_DETAIL",
            "PR57_JUNCTION_ALLOC", "PR57_ALLOC_PROFILE"]
    data = [{k: row(log, k) for k in keys} for log in b]
    for k in keys:
        assert data[0][k] == data[1][k], k
    d = data[0]
    region, detail, junction, total = (d[k] for k in keys)
    assert total["calls"] == region["calls"] == detail["calls"] == junction["calls"] == 600
    assert total["totalMB"] == 207.41
    assert region["entryMB"] == 32.65 and region["junctionMB"] == 88.20
    assert junction["admissionMB"] == 87.96 and junction["admissionCalls"] == 2478
    assert detail["preferenceCalls"] == 1406 and detail["turnWayCalls"] == 1925
    assert detail["bodyOnWayCalls"] == 1925
    categories = ("manoeuvreMB", "areaOwnerMB", "bendHolderMB", "bendPriorityMB",
                  "preferenceMB", "turnWayMB", "bodyOnWayMB", "otherMB")
    assert math.isclose(sum(detail[k] for k in categories), junction["admissionMB"], abs_tol=.03)
    assert math.isclose(junction["admissionMB"] + junction["deniedMB"], region["junctionMB"], abs_tol=.02)
    alias_counterexamples()
    anchors = source_contract(pathlib.Path(sys.argv[2])) if len(sys.argv) == 3 else None
    print("Authentic 600 unsplit calls / 100 public Advance(2), REAL 121 alive / 98 vehicles")
    print(f"Entry: {region['entryMB']:.2f} MB; junction: {region['junctionMB']:.2f} MB; "
          f"together: {(region['entryMB']+region['junctionMB'])/total['totalMB']:.2%} of "
          f"{total['totalMB']:.2f} MB sampled movement allocations")
    print(f"Junction admission: {junction['admissionMB']:.2f} MB / "
          f"{junction['admissionCalls']} calls; exclusive buckets sum within rounding")
    for k in categories:
        print(f"  {k}: {detail[k]:.2f} MB, "
              f"{detail[k]/junction['admissionMB']:.2%} of admission")
    print(f"Direct turnWay region: {detail['turnWayMB']/total['totalMB']:.2%} "
          "of sampled movement allocation (impossible zero-cost upper bound, not savings)")
    print("A single shared mutable pose list FAILS both retained-corridor and nested-way cases; "
          "independent live leases PASS. Durable admissions require owning copies.")
    if anchors is not None:
        print(f"Checked {anchors} exact source anchors in checkout")
    print("PR57_496_AUTHENTIC_WORKSET_LIFETIME_PASS")


if __name__ == "__main__":
    main()
