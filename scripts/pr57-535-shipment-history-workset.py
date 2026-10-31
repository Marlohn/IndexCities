#!/usr/bin/env python3
"""PR57 #535: authentic shipment-history pressure and causal index counterexample.

Offline, read-only ZIP audit; an in-memory mutation is *only* an adversarial
model of the C# ProjectId writer, not a replay of the simulation. No speedup,
.NET equivalence, or long-horizon sustainability claim is made.
"""
import collections
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path

SOURCES = (
    ("493", "f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89",
     (("worksREAL47", "worksREAL47", True),
      ("congestedREAL121", "congestedREAL121", True),
      ("GRID_ARTIFICIAL", "GRID_ARTIFICIAL", True))),
    ("478", "7130344249d2958244dd08906eea5cfc74bb0585cd14a772a5e4c9ce9aff6902",
     (("midnightREAL121", "midnightREAL121", True),)),
    ("259", "23472d36",  # check the complete digest from the downloaded ZIP below
     (("gridARTIFICIAL-control", "gridARTIFICIAL-control", False),
      ("gridARTIFICIAL-probe", "gridARTIFICIAL-probe", False),
      ("queueREAL-control", "queueREAL-control", False),
      ("queueREAL-probe", "queueREAL-probe", False))),
)


def canonical(shipments, project_id):
    # Exact order and predicates of Construction.PendingProjectShipments.
    return [s["Id"] for s in shipments
            if s["ProjectId"] == project_id and not s["Complete"]]


def membership(shipments):
    # Live references, including completed shipments; preserve insertion order.
    groups = collections.defaultdict(list)
    for s in shipments:
        if s["ProjectId"] is not None:
            groups[s["ProjectId"]].append(s)
    return groups


def indexed(groups, project_id):
    return [s["Id"] for s in groups.get(project_id, ()) if not s["Complete"]]


def verify_index(shipments, projects):
    groups = membership(shipments)
    for p in projects:
        assert canonical(shipments, p["Id"]) == indexed(groups, p["Id"])
    return groups


def check_zip(path, expected, fixtures):
    raw = path.read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    if len(expected) == 64:
        assert digest == expected, (path, digest)
    else:
        assert digest.startswith(expected), (path, digest)
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, f"CRC failure: {path}"
        for name, folder, abba in fixtures:
            def state(arm, i):
                prefix = f"{folder}/{arm}/" if abba else f"{folder}/"
                return z.read(prefix + f"checkpoint-{i}.json")
            if abba:
                for i in (0, 1):
                    reference = state("A1", i)
                    for arm in ("B1", "B2", "A2"):
                        assert state(arm, i) == reference, (name, arm, i)
                for arm in ("A1", "B1", "B2", "A2"):
                    log = z.read(f"{folder}/{arm}/full.log").decode()
                    assert "INVARIANTS OK" in log, (name, arm)
            initial = json.loads(state("B1", 0))
            final = json.loads(state("B1", 1))
            old, new = initial["Shipments"], final["Shipments"]
            assert len({s["Id"] for s in old}) == len(old)
            assert len({s["Id"] for s in new}) == len(new)
            old_by_id = {s["Id"]: s for s in old}
            new_by_id = {s["Id"]: s for s in new}
            added = set(new_by_id) - set(old_by_id)
            removed = set(old_by_id) - set(new_by_id)
            changed = {i for i in old_by_id.keys() & new_by_id.keys()
                       if old_by_id[i] != new_by_id[i]}
            verify_index(old, initial["Projects"])
            verify_index(new, final["Projects"])
            completed = sum(bool(s["Complete"]) for s in new)
            active = sum(s["ProjectId"] is not None and not s["Complete"] for s in new)
            print(f"PR57_535_AUTHENTIC case={name} initial={len(old)} final={len(new)} "
                  f"completed={completed} pendingProject={active} "
                  f"added={len(added)} removed={len(removed)} changed={len(changed)} "
                  f"prefix={new[:len(old)] == old}")
            if name == "worksREAL47":
                assert (len(old), len(new), completed, active, len(added),
                        len(removed), len(changed)) == (201, 214, 211, 3, 13, 0, 0)
                assert new[:len(old)] == old
                assert {s["Id"] for s in new if s["ProjectId"] is not None and not s["Complete"]} == {1050, 1051, 1052}
                # Existing instrumented full-list enumerations (progress and
                # reserve), distinct from the dispatch's own count-based build.
                log = z.read("worksREAL47/B1/full.log").decode()
                m = re.search(r"PR57_CONSTRUCTION_CENSUS steps=(\d+) .*?shipmentQueries=(\d+) shipmentVisits=(\d+)", log)
                assert m and tuple(map(int, m.groups())) == (100, 200, 41748)
                print("PR57_535_OBSERVED worksREAL47 steps=100 "
                      "fullListEnumerations=200 shipmentVisits=41748 "
                      "dispatchPotentialBuilds=100 "
                      "dispatchPotentialReadsLowerBound=20100 "
                      "dispatchPotentialReadsUpperBound=21400 "
                      "note=conditional-on-at-least-one-dispatch-index-query-per-Step")
                # Actual, persisted *input objects* as the adversarial fixture.
                # RedirectCancelled/TryRedirectMunicipalCargo can reassign a
                # Shipment.ProjectId without changing the list's Count.
                cloned = json.loads(json.dumps(new))
                groups = membership(cloned)
                victim = next(s for s in cloned if s["Id"] == 1050)
                old_project = victim["ProjectId"]
                new_project = next(s["ProjectId"] for s in cloned
                                   if s["Id"] == 1051)
                assert old_project != new_project
                victim["ProjectId"] = new_project
                assert len(cloned) == len(new)
                assert indexed(groups, old_project) != canonical(cloned, old_project)
                assert indexed(groups, new_project) != canonical(cloned, new_project)
                # Writer-published membership update: preserve the original
                # shipment list order, even after reassignment to another group.
                groups[old_project] = [s for s in groups[old_project] if s is not victim]
                groups[new_project].append(victim)
                position = {id(s): i for i, s in enumerate(cloned)}
                groups[new_project].sort(key=lambda s: position[id(s)])
                for p in final["Projects"]:
                    assert indexed(groups, p["Id"]) == canonical(cloned, p["Id"])
                # Completion is a live predicate, not a membership change.
                victim["Complete"] = True
                for p in final["Projects"]:
                    assert indexed(groups, p["Id"]) == canonical(cloned, p["Id"])
                print(f"PR57_535_ADVERSARIAL_PROJECT_REASSIGN_PASS "
                      f"shipment=1050 from={old_project} to={new_project} "
                      "listCountUnchanged=True liveCompletion=True "
                      "note=synthetic-mutation-not-engine-replay")
    return digest


def main():
    if len(sys.argv) != 4:
        raise SystemExit("usage: script.py <493.zip> <478.zip> <259.zip>")
    for (label, expected, fixtures), arg in zip(SOURCES, sys.argv[1:]):
        digest = check_zip(Path(arg), expected, fixtures)
        print(f"PR57_535_SOURCE experiment={label} sha256={digest}")
    print("PR57_535_AUTHENTIC_SHIPMENT_HISTORY_AND_WRITER_MODEL_PASS "
          "speedupAccepted=0 causalUniversalProof=False")


if __name__ == "__main__":
    main()
