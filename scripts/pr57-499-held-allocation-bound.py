#!/usr/bin/env python3
"""PR57 #499: independently reconcile REAL Manoeuvre poses with exclusive allocations.

Inputs are original, immutable GitHub Actions ZIPs #248 and #497. This is an
allocation *payload bound* for List<T> held arrays, not a speedup or a heap census.
"""
import hashlib
import re
import sys
import zipfile
from pathlib import Path

EXPECTED = (
    "7adffc6ddbd77184fb268815a5549089acc5640dcb44f216cd2083b545e2a740",
    "89eb18a37fc30d697562daa556c24d0742000473eeaf1b6d9a7940fde534c7fa",
)
START = "779c144f2d6b2b64ece079f5740bf8a0a473a47286ca40d8b5f6b9dd61ab616f"
END = "9ef479844aa324867c0c7d7733c61af7d0f5227e4ca4f1ba4a4d0365522cca23"


def sha(data):
    return hashlib.sha256(data).hexdigest()


def fields(line):
    return dict(re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^ ]+)", line))


def exactly(lines, prefix):
    matches = [line for line in lines if line.startswith(prefix)]
    assert len(matches) == 1, (prefix, len(matches))
    return fields(matches[0])


def main(a, b):
    assert sha(a.read_bytes()) == EXPECTED[0], "wrong #248 original ZIP"
    assert sha(b.read_bytes()) == EXPECTED[1], "wrong #497 original ZIP"
    with zipfile.ZipFile(a) as za, zipfile.ZipFile(b) as zb:
        assert za.testzip() is None and zb.testzip() is None, "ZIP CRC"
        assert len(za.namelist()) == 12 and len(zb.namelist()) == 12
        for z, arms in (
            (za, ["queueREAL-control", "queueREAL-observed"]),
            (zb, [f"congestedREAL121/{arm}" for arm in ("A1", "B1", "B2", "A2")]),
        ):
            for arm in arms:
                for i, expected in ((0, START), (1, END)):
                    assert sha(z.read(f"{arm}/checkpoint-{i}.json")) == expected, (arm, i)
        old = za.read("queueREAL-observed.log").decode().splitlines()
        new = zb.read("congestedREAL121/B1/full.log").decode().splitlines()
        m = exactly(old, "PR57_248_MANOEUVRE slices=600 ")
        n = exactly(new, "PR57_497_KIND kind=Manoeuvre ")
        t = exactly(new, "PR57_497_TOTAL ")
        p = exactly(new, "STEP_PROFILE steps=100 ")
        assert int(m["calls"]) == 14823 and int(m["totalPoses"]) == 264125
        assert int(n["calls"]) == 14825 and int(n["exclusiveBytes"]) == 55017648
        assert int(n["exclusiveBytes"]) == int(n["inclusiveBytes"])
        assert int(t["calls"]) == 600 and int(t["rootBytes"]) == int(t["exclusiveSum"])
        assert p["phases"] == "13"
        poses, calls, alloc = int(m["totalPoses"]), int(m["calls"]), int(n["exclusiveBytes"])
        # On x64: reference Vehicle (8) + Point(double,double) (16) + double (8).
        # The source uses \`new List<(Vehicle,Point,double)>()\` and Add, no capacity.
        # .NET 8 List<T> grows 0->4->8->16...; arrays are never returned to a pool.
        # For a call producing n poses, allocated element slots >= 2*n-4
        # and <= 4*n. Summing without individual lengths gives safe loose bounds.
        tuple_bytes = 32
        lower = max(0, 2 * poses - 4 * calls) * tuple_bytes
        upper = 4 * poses * tuple_bytes
        payload = poses * tuple_bytes
        assert 0 < payload <= lower <= upper < alloc
        old_mb = float(m["totalMB"]) * 1e6
        assert abs(old_mb - alloc) < 10000, "different windows/flags?"
        print("REAL_INITIAL_FINAL_CHECKPOINTS_IDENTICAL_BETWEEN_ACTIONS PASS")
        print(f"manoeuvre_calls_248={calls} manoeuvre_calls_497={n['calls']} poses_248={poses}")
        print(f"exclusive_497_bytes={alloc} total_248_rounded_bytes={old_mb:.0f}")
        print(f"held_payload_once_bytes={payload} held_backing_array_payload_lower_bytes={lower} upper_bytes={upper}")
        print(f"array_payload_fraction_of_exclusive_lower={lower/alloc:.4f} upper={upper/alloc:.4f}")
        print(f"poses_per_Advance2={poses/100:.2f} bytes_per_Advance2_lower={lower/100:.0f} upper={upper/100:.0f}")
        print("NOTE: cross-run bounds, not exact heap allocations, CPU savings, or 10k extrapolation")
        print("PR57_499_AUTHENTIC_HELD_ARRAY_BOUNDS_PASS")


if __name__ == "__main__":
    assert len(sys.argv) == 3, "usage: script.py pr57-248-original.zip pr57-497-partial.zip"
    main(Path(sys.argv[1]), Path(sys.argv[2]))
