#!/usr/bin/env python3
"""PR57 #537: authentic-data model of a same-phase shipment membership hazard.

No C# engine is executed here. The maintenance-retirement -> CancelWork ->
RedirectCancelled -> TryRedirectMunicipalCargo call path is verified against
HEAD source separately. Reassignment/append below are explicitly counterfactual
in-memory operations on copied AUTHENTIC shipments, not economic deliveries.
"""
import argparse
import copy
import hashlib
import json
import zipfile
from collections import defaultdict
from pathlib import Path

SHA536 = "45102fdf03c7e2f3fb594bf413c33efe95223223e6faf7398b3d88cc25266343"
ANCHORS = {
    "Construction.cs": (
        "var progressShipments=Pr57ProgressShipmentLookupEnabled",
        "State.Shipments.Count==indexedShipmentCount",
        "if(RetireMaintenanceWithoutTargets(p))continue;",
        "foreach(var s in State.Shipments.Where(s=>s.ProjectId==p.Id&&!s.Complete))",
    ),
    "Maintenance.cs": ("CancelWork(project);project.Cause=WorkCause.MaintenanceEndedRemoved;",),
    "Mobility.cs": ("if(TryRedirectMunicipalCargo(v,s))return;", "shipment.ProjectId=project.Id;"),
    "MunicipalRelocation.Execution.cs": (
        "void ProgressMunicipalRelocations(double dt)",
        "State.Shipments.Add(shipment);order.TransferShipmentIds.Add(shipment.Id);",
    ),
    "CitySimulation.cs": (
        "var completedConstruction=ProgressConstruction(dt);",
        "ProgressMunicipalRelocations(dt);",
        "OrderConstruction();",
    ),
}

def check_sources(root):
    src = root / "src/IndexCities.Simulation"
    for name, anchors in ANCHORS.items():
        content = (src / name).read_text(encoding="utf-8")
        for anchor in anchors:
            assert content.count(anchor) == 1, (name, anchor, content.count(anchor))
    step = (src / "CitySimulation.cs").read_text(encoding="utf-8")
    assert step.index("ProgressConstruction(dt)") < step.index("ProgressMunicipalRelocations(dt)") < step.index("OrderConstruction();")

def members(records, project):
    return [s["Id"] for s in records if s["ProjectId"] == project and not s["Complete"]]

def snapshot(records):
    by_project = defaultdict(list)
    for s in records:
        if s["ProjectId"] is not None:
            by_project[s["ProjectId"]].append(s)
    return by_project

def cached_members(index, project):
    # Exact current C# ToLookup semantics: membership frozen, predicate read live.
    return [s["Id"] for s in index[project] if s["ProjectId"] == project and not s["Complete"]]

class EpochWorkset:
    # Model of a writer-published invalidation contract; NOT an engine implementation.
    def __init__(self, records):
        self.records = records
        self.epoch = 0
        self.index_epoch = -1
        self.index = None
        self.rebuilds = 0
    def publish_reassignment(self, shipment, project):
        shipment["ProjectId"] = project
        self.epoch += 1
    def publish_append(self, shipment):
        self.records.append(shipment)
        self.epoch += 1
    def query(self, project):
        if self.index_epoch != self.epoch:
            self.index = snapshot(self.records)
            self.index_epoch = self.epoch
            self.rebuilds += 1
        return cached_members(self.index, project)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("zip", type=Path, help="Original #536 artifact ZIP")
    ap.add_argument("--checkout", type=Path, help="Optional local HEAD checkout for source-anchor verification")
    args = ap.parse_args()
    raw = args.zip.read_bytes()
    assert hashlib.sha256(raw).hexdigest() == SHA536, "Unexpected artifact ZIP"
    if args.checkout is not None:
        check_sources(args.checkout)
    with zipfile.ZipFile(args.zip) as z:
        assert z.testzip() is None and len(z.namelist()) == 37
        for case in ("worksREAL47", "congestedREAL121", "gridARTIFICIAL"):
            for checkpoint in (0, 1):
                first = z.read(f"{case}/A1/checkpoint-{checkpoint}.json")
                for arm in ("A2", "B1", "B2"):
                    assert first == z.read(f"{case}/{arm}/checkpoint-{checkpoint}.json"), (case, arm)
            for arm in ("A1", "B1", "B2", "A2"):
                assert b"INVARIANTS OK" in z.read(f"{case}/{arm}/full.log")
        before = json.loads(z.read("worksREAL47/A1/checkpoint-0.json"))
        after = json.loads(z.read("worksREAL47/A1/checkpoint-1.json"))
    old = before["Shipments"]
    current = after["Shipments"]
    assert len(old) == 201 and len(current) == 214
    assert old == current[:len(old)], "Authentic append-only checkpoint prefix changed"
    records = copy.deepcopy(current)
    shipment = next(s for s in records if s["Id"] == 1050)
    assert shipment["ProjectId"] == 916 and not shipment["Complete"] and shipment["GoodsOwnerAccount"] == "city"
    assert 918 in {p["Id"] for p in after["Projects"] if p["Status"] in ("Waiting", "InProgress")}
    frozen = snapshot(records)
    count = len(records)
    before_916 = cached_members(frozen, 916)
    before_918 = cached_members(frozen, 918)
    assert 1050 in before_916 and 1050 not in before_918
    # Counterfactual writer: this is a ProjectId reassignment, NOT a simulated trip.
    workset = EpochWorkset(records)
    assert workset.query(916) == members(records, 916)
    workset.publish_reassignment(shipment, 918)
    assert len(records) == count, "Project reassignment must preserve Count"
    assert cached_members(frozen, 918) != members(records, 918), "Expected missing new member"
    assert cached_members(frozen, 916) == members(records, 916), "Live predicate must drop old member"
    assert workset.query(918) == members(records, 918)
    for project in {s["ProjectId"] for s in records if s["ProjectId"] is not None}:
        assert workset.query(project) == members(records, project)
    # Independent interphase append witness: reuse an AUTHENTIC shipment from
    # the final checkpoint, inserted into the copied initial checkpoint only.
    # This does not claim the real append occurred in relocation.
    appended = copy.deepcopy(current[len(old)])
    base = copy.deepcopy(old)
    append_workset = EpochWorkset(base)
    assert append_workset.query(appended["ProjectId"]) == members(base, appended["ProjectId"])
    append_workset.publish_append(appended)
    assert append_workset.query(appended["ProjectId"]) == members(base, appended["ProjectId"])
    assert len(base) == len(old) + 1
    print("PR57_537_AUTHENTIC_INTRAPHASE_MEMBERSHIP_MODEL_PASS",
          "artifactSha256=" + SHA536,
          "realShipments=" + str(len(current)),
          "realAppends=" + str(len(current)-len(old)),
          "counterfactualReassignedShipment=1050",
          "oldProject=916", "newProject=918",
          "countUnchanged=" + str(count),
          "staleTargetMissing=True",
          "epochRebuilds=" + str(workset.rebuilds),
          "sourceAnchors=" + ("checked" if args.checkout else "separately_verified"))

if __name__ == "__main__":
    main()
