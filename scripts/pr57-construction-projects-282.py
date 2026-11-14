#!/usr/bin/env python3
"""PR57 #282: validate archived opt-in construction census, no .NET or simulation.

Usage: python3 scripts/pr57-construction-projects-282.py pr57-282-artifact.zip
The input is the original GitHub Actions ZIP, including full checkpoints and logs.
"""
import hashlib
import json
import re
import sys
import zipfile
from decimal import Decimal


def field(line: str, name: str) -> str:
    match = re.search(r"(?:^|\s)" + re.escape(name) + r"=([^ ]+)", line)
    if not match:
        raise ValueError(f"missing {name}: {line}")
    return match.group(1)


def inspect(path: str) -> None:
    with zipfile.ZipFile(path) as archive:
        names = set(archive.namelist())
        cases = sorted(n.removesuffix("-probe.log") for n in names if n.endswith("-probe.log"))
        assert cases, "no probe logs"
        for case in cases:
            probe = archive.read(f"{case}-probe.log").decode("utf-8")
            control = archive.read(f"{case}-control.log").decode("utf-8")
            for mode, log in (("probe", probe), ("control", control)):
                assert "INVARIANTS OK" in log and "SAVE " in log, (case, mode)
                assert len([x for x in log.splitlines() if x.startswith("WINDOW ")]) == 1
            for checkpoint in (0, 1):
                left = archive.read(f"{case}-control/checkpoint-{checkpoint}.json")
                right = archive.read(f"{case}-probe/checkpoint-{checkpoint}.json")
                assert left == right, (case, checkpoint, "FULL_JSON_MISMATCH")
                json.loads(left, parse_float=Decimal)
                print("PR57_282_PARITY", case, f"checkpoint={checkpoint}",
                      "fullJson=EXACT", "sha256=" + hashlib.sha256(left).hexdigest())
            census = next(x for x in probe.splitlines() if x.startswith("PR57_CONSTRUCTION_CENSUS "))
            sums = [x for x in probe.splitlines() if x.startswith("PR57_282_SUM ")]
            assert len(sums) == 2, (case, sums)
            for phase, census_key in (("order", "orderProjects"), ("progress", "progressProjects")):
                row = next(x for x in sums if field(x, "phase") == phase)
                calls = int(field(row, "calls"))
                assert calls == int(field(census, census_key)), (case, phase, "COUNT_MISMATCH")
                project_count = int(field(row, "projects"))
                top = [x for x in probe.splitlines()
                       if x.startswith(f"PR57_282_PROJECT phase={phase} ")]
                assert len(top) == min(10, project_count), (case, phase, "TOP_ROWS")
                ids = [int(field(x, "id")) for x in top]
                assert len(ids) == len(set(ids)), (case, phase, "DUPLICATE_ID")
                changed = int(field(row, "causeChanges"))
                unchanged = int(field(row, "noSelectedFieldChange"))
                assert 0 <= changed <= calls and 0 <= unchanged <= calls
                total_ms = float(field(row, "wallMs"))
                top_ms = float(field(top[0], "wallMs")) if top else 0.0
                print("PR57_282_RESULT", case, f"phase={phase}", f"projects={project_count}",
                      f"calls={calls}", f"causeChanges={changed}",
                      f"causeChangePct={100*changed/calls:.3f}" if calls else "causeChangePct=NA",
                      f"noSelectedFieldChange={unchanged}", f"wallMs={total_ms:.3f}",
                      f"topId={ids[0] if ids else 'none'}",
                      f"topSharePct={100*top_ms/total_ms:.2f}" if total_ms else "topSharePct=NA",
                      "note=INTRUSIVE_SELECTED_FIELDS_NOT_CAUSAL_INACTIVITY")
            for mode, log in (("control", control), ("probe", probe)):
                print("PR57_282_WINDOW", case, mode,
                      next(x for x in log.splitlines() if x.startswith("WINDOW ")))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: pr57-construction-projects-282.py <GitHub Actions ZIP>")
    inspect(sys.argv[1])
