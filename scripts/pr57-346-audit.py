#!/usr/bin/env python3
"""PR57 #346: independently audit the original six-sample late-jam ZIP.

The #346 CI ran both canonical replays and 511 tests; it failed only its
post-run assertion that a third sample window (eligible slice 65001) exists.
This script never turns observational timings into throughput improvements.
"""
from __future__ import annotations
import hashlib
import sys
from pathlib import Path
from zipfile import ZipFile

EXPECTED_ZIP = "cac843aff78493e94563757873ce0fc4faf2fdd1a1c009b647333e2a7590e086"
EXPECTED_SAVE = "e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa"
EXPECTED_SLICES = [40001, 40002, 40003, 50001, 50002, 50003]


def records(text: str, prefix: str) -> list[dict[str, str]]:
    return [dict(token.split("=", 1) for token in line.split()[1:] if "=" in token)
            for line in text.splitlines() if line.startswith(prefix + " ")]


def main(path: Path) -> None:
    data = path.read_bytes()
    digest = hashlib.sha256(data).hexdigest()
    assert digest == EXPECTED_ZIP, (digest, EXPECTED_ZIP)
    with ZipFile(path) as z:
        assert z.testzip() is None, "Corrupted ZIP member"
        control = z.read("control/census.log").decode()
        probe = z.read("probe/census.log").decode()
        a = z.read("control/checkpoint-10800.json")
        b = z.read("probe/checkpoint-10800.json")
        assert a == b and hashlib.sha256(a).hexdigest() == EXPECTED_SAVE
        assert len(records(control, "PR57_345_PHASE")) == 78
        assert len(records(probe, "PR57_345_PHASE")) == 78
        assert len(records(control, "PR57_337_HOUR")) == 6
        assert len(records(probe, "PR57_337_HOUR")) == 6
        assert not records(control, "PR57_206_RESIDUAL")
        assert not records(control, "PR57_244_STATE")
        assert len(records(control, "PR57_337_FINAL")) == 1
        assert len(records(probe, "PR57_337_FINAL")) == 1
        assert "parity=IDENTICAL" in control and "parity=IDENTICAL" in probe
        rows = records(probe, "PR57_206_RESIDUAL")
        states = records(probe, "PR57_244_STATE")
        assert [int(r["eligibleSlice"]) for r in rows] == EXPECTED_SLICES
        assert len(states) == 5 * len(rows)
        assert all(int(r["vehicles"]) == 98 for r in rows)
        assert all(int(r["cohortIds"]) == 4 for r in states)
        total = sum(float(r["totalMs"]) for r in rows)
        stage = [sum(float(r[f"s{i}Ms"]) for r in rows) for i in range(9)]
        assert abs(sum(stage) - total) < .00005
        assert abs(sum(float(r["totalMs"]) for r in states) - total) < .00005
        print(f"ZIP_SHA256={digest} SAVE_SHA256={EXPECTED_SAVE} checkpoints=IDENTICAL")
        print(f"SAMPLES={len(rows)} ACTOR_VISITS={sum(int(r['vehicles']) for r in rows)} "
              f"MOVED={sum(int(r['moved']) for r in rows)} TOTAL_ACTOR_MS={total:.6f}")
        for i, ms in enumerate(stage):
            print(f"STAGE={i} MS={ms:.6f} PCT_OF_SAMPLED_ACTOR={100 * ms / total:.3f}")
        for name, sample in (("early", rows[:3]), ("middle", rows[3:6])):
            t = sum(float(r["totalMs"]) for r in sample)
            print(f"WINDOW={name} TOTAL_MS={t:.6f} " + " ".join(
                f"S{i}_PCT={100 * sum(float(r[f's{i}Ms']) for r in sample) / t:.3f}"
                for i in (1, 2, 3, 4, 5, 6, 7)))
        for name in sorted({r["group"] for r in states}):
            group = [r for r in states if r["group"] == name]
            print(f"COHORT={name} ACTORS={sum(int(r['actors']) for r in group)} "
                  f"MS={sum(float(r['totalMs']) for r in group):.6f}")
        print("MISSING_WINDOW=65001..65003 (not eligible in this replay)")
        print("CI_STATUS=FAILED_POSTRUN_SAMPLE_ASSERTION; ENGINE_TESTS=511_PASS_IN_JOB_LOG")
        print("SCOPE=OBSERVATIONAL_6_SLICES_NO_THROUGHPUT_AB_NO_CAUSAL_CACHE_PROOF")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("usage: python3 pr57-346-audit.py <original-346-artifact.zip>")
    main(Path(sys.argv[1]))
