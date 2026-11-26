#!/usr/bin/env python3
"""PR57 #318: compare authentic #312 actor-gate flips with #317 writer-site publication.

Usage: python3 pr57-318-sparse-gate-reconcile.py pr57-312-raw.zip pr57-317-raw.zip
Read-only. No engine changes. Different metrics are compared only on the observed
calls; equality of counts does NOT prove full causal coverage.
"""
from __future__ import annotations

import collections
import hashlib
import re
import sys
import zipfile
from pathlib import Path

EXPECTED = {
    "312": "df97505740901d41c9795756580957cf09d221d8907ad39679b278d71bdeedf1",
    "317": "69f2be65c99008246912c2757c0be2b1617a16863f3724c756e09e66ff73ab55",
}


def fields(line: str) -> dict[str, str]:
    return dict(re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^\s]+)", line))


def logs(z: zipfile.ZipFile, name: str, prefix: str) -> list[dict[str, str]]:
    return [fields(line) for line in z.read(name).decode().splitlines()
            if line.startswith(prefix)]


def run(old_path: Path, new_path: Path) -> None:
    for label, path in (("312", old_path), ("317", new_path)):
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        assert digest == EXPECTED[label], (label, digest)
        print(f"PR57_318_ZIP experiment={label} sha256={digest}")

    with zipfile.ZipFile(old_path) as old, zipfile.ZipFile(new_path) as new:
        pairs312 = pairs317 = cross_pairs = 0
        for case, calls, start in (("real14hREAL", 100, 76),
                                   ("gridARTIFICIAL", 50, 26)):
            for step in (0, 20, calls // 2, calls):
                for suffix in ("1", "2"):
                    old_base = old.read(f"{case}-control{suffix}/checkpoint-{step}.json")
                    assert old_base == old.read(
                        f"{case}-probe{suffix}/checkpoint-{step}.json")
                    pairs312 += 1
                    new_base = new.read(f"{case}-control{suffix}/checkpoint-{step}.json")
                    for arm in ("full", "sparse"):
                        assert new_base == new.read(
                            f"{case}-{arm}{suffix}/checkpoint-{step}.json")
                        pairs317 += 1
                for old_arm, new_arm in (("control1", "control1"),
                                         ("probe1", "sparse1"),
                                         ("probe2", "sparse2")):
                    assert old.read(f"{case}-{old_arm}/checkpoint-{step}.json") == \
                           new.read(f"{case}-{new_arm}/checkpoint-{step}.json")
                    cross_pairs += 1

            actor_flips = collections.defaultdict(list)
            for row in logs(old, f"{case}-probe1.log", "PR57_312_GATE "):
                actor_flips[int(row["call"])].append(row)
            sparse = {int(row["call"]): row for row in logs(
                new, f"{case}-sparse1.log", "PR57_317_WRITERS ")}
            assert sorted(sparse) == list(range(start, calls + 1))
            # Check deterministic duplicates independently, not just totals.
            assert logs(old, f"{case}-probe1.log", "PR57_312_GATE ") == \
                   logs(old, f"{case}-probe2.log", "PR57_312_GATE ")
            sparse1 = logs(new, f"{case}-sparse1.log", "PR57_317_WRITERS ")
            sparse2 = logs(new, f"{case}-sparse2.log", "PR57_317_WRITERS ")
            assert len(sparse1) == len(sparse2) == calls - start + 1
            assert all(fields_equal_except_timing(a, b)
                       for a, b in zip(sparse1, sparse2))

            differences = []
            for call in sorted(set(actor_flips) | set(sparse)):
                n_actor = len(actor_flips[call])
                n_site = int(sparse.get(call, {}).get("individualGateDeltas", 0))
                if n_actor != n_site:
                    differences.append((call, n_actor, n_site, actor_flips[call]))
            print(f"PR57_318_COUNTS case={case} actorFlips={sum(map(len, actor_flips.values()))} "
                  f"siteGateDeltas={sum(int(r['individualGateDeltas']) for r in sparse.values())} "
                  f"mismatchedCalls={len(differences)}")
            if case == "real14hREAL":
                assert len(differences) == 1, differences
                call, n_actor, n_site, events = differences[0]
                assert (call, n_actor, n_site) == (76, 1, 0)
                assert len(events) == 1
                e = events[0]
                assert (e["citizen"], e["beforeNode"], e["afterNode"],
                        e["beforeMoving"], e["afterMoving"]) == \
                       ("1490", "804", "null", "True", "False")
                assert int(sparse[76]["arrive"]) >= 1
                print("PR57_318_COUNTEREXAMPLE call=76 citizen=1490 node=804 "
                      "moving=True_to_False actorGateFlip=1 siteGateDelta=0 "
                      "cause=DERIVED_MOVING_CHANGES_BEFORE_CROSSING_CLEAR")
            else:
                assert not differences, differences
        assert (pairs312, pairs317, cross_pairs) == (16, 32, 24)
        print("PR57_318_VERIFIED oldPairs=16 newPairs=32 crossExperimentPairs=24 "
              "scope=SELECTED_WRITER_COVERAGE_COUNTEREXAMPLE_NOT_FULL_CAUSAL_JOURNAL")


def fields_equal_except_timing(a: dict[str, str], b: dict[str, str]) -> bool:
    return {k: v for k, v in a.items() if k != "writerScopeMs"} == \
           {k: v for k, v in b.items() if k != "writerScopeMs"}


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("Usage: pr57-318-sparse-gate-reconcile.py 312.zip 317.zip")
    run(Path(sys.argv[1]), Path(sys.argv[2]))
