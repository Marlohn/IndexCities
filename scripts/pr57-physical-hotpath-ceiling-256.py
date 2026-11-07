#!/usr/bin/env python3
"""PR57 #256: reproduce the exclusive s0..s8 time/allocation census.

Input: ORIGINAL Actions ZIPs #12731721437 (#206) and #12779716979 (#244).
No engine execution, speedup, causal proof, or cross-state A/B comparison.

python3 scripts/pr57-physical-hotpath-ceiling-256.py \
  --baseline pr57-206-evidence.zip --real14h pr57-244-evidence.zip
"""
import argparse
import hashlib
import re
import zipfile

STAGES = range(9)
KEYVAL = re.compile(r"([A-Za-z][A-Za-z0-9]*)=([^ ]+)")


def records(z, member, prefix):
    content = z.read(member).decode("utf-8")
    return [dict(KEYVAL.findall(line)) for line in content.splitlines()
            if line.startswith(prefix + " ")]


def verify_checkpoints(z, left, right):
    for index in (0, 1):
        a = z.read(f"{left}/checkpoint-{index}.json")
        b = z.read(f"{right}/checkpoint-{index}.json")
        assert a == b, f"checkpoint mismatch {left}/{right} index={index}"
    return hashlib.sha256(z.read(f"{left}/checkpoint-1.json")).hexdigest()[:16]


def summarize(z, name, log):
    rows = records(z, log, "PR57_206_RESIDUAL")
    assert [int(r["sample"]) for r in rows] == [1, 2, 3], (name, rows)
    assert all(r["exclusiveWithinLoop"] == "True" and
               r["observational"] == "True" and r["intrusive"] == "True"
               for r in rows), name
    times = [sum(float(r[f"s{s}Ms"]) for r in rows) for s in STAGES]
    allocs = [sum(float(r[f"s{s}MB"]) for r in rows) for s in STAGES]
    total_ms = sum(float(r["totalMs"]) for r in rows)
    total_mb = sum(float(r["totalMB"]) for r in rows)
    assert abs(sum(times) - total_ms) < 0.002, (name, times, total_ms)
    assert abs(sum(allocs) - total_mb) < 0.002, (name, allocs, total_mb)
    def fraction(values, stages):
        return 100 * sum(values[i] for i in stages) / sum(values)
    # This hypothetical Amdahl bound applies to the INTRUSIVE ORDERED LOOP only.
    loop_s34 = 1 / (1 - fraction(times, [3, 4]) / 100)
    print(f"{name:19s} loop={total_ms:9.3f}ms / {total_mb:8.3f}MB"
          f" s3={fraction(times,[3]):5.1f}%time/{fraction(allocs,[3]):5.2f}%bytes"
          f" s3+s4={fraction(times,[3,4]):5.1f}%time/{fraction(allocs,[3,4]):5.2f}%bytes"
          f" s1+s2+s7={fraction(times,[1,2,7]):5.1f}%time/{fraction(allocs,[1,2,7]):5.1f}%bytes"
          f" impossible_loop_s3s4_ceiling={loop_s34:.3f}x")
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", required=True, help="Action #40497553477 ZIP")
    parser.add_argument("--real14h", required=True, help="Action #40603986040 ZIP")
    args = parser.parse_args()
    with zipfile.ZipFile(args.baseline) as z:
        for name in ("worksREAL", "queueREAL", "gridARTIFICIAL"):
            print("checkpoint parity", name,
                  verify_checkpoints(z, f"{name}-plain", f"{name}-probe"))
            summarize(z, name + " #206", f"{name}-probe.log")
    with zipfile.ZipFile(args.real14h) as z:
        for arm in ("states", "writers"):
            print("checkpoint parity real14h", arm,
                  verify_checkpoints(z, "control", arm))
        rows = summarize(z, "queueREAL14h #244", "states.log")
        states = records(z, "states.log", "PR57_244_STATE")
        persistent = [r for r in states if r["group"] == "persistent-still"]
        assert [int(r["sample"]) for r in persistent] == [1, 2, 3]
        persistent_ms = sum(float(r["totalMs"]) for r in persistent)
        loop_ms = sum(float(r["totalMs"]) for r in rows)
        print(f"real14h persistent 24/98 actor time: {persistent_ms:.3f}/{loop_ms:.3f}ms"
              f" = {100*persistent_ms/loop_ms:.2f}% of intrusive loop")
    print("LIMIT: different states/commits; three intrusive slices each; stage bytes are NOT"
          " attributable uniquely to LINQ; no integrated speedup or 10k extrapolation.")


if __name__ == "__main__":
    main()
