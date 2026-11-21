#!/usr/bin/env python3
"""PR57 #289: summarize authentic writer census, full checkpoint parity and phase costs.

Usage: python3 scripts/pr57-289-writer-summary.py pr57-289.zip
Only 2-second public Advance calls, not a speedup or a 10k/80y projection.
"""
import argparse
import hashlib
import re
from pathlib import Path
from zipfile import ZipFile

EXPECTED_SHA256 = "6782795ed643809f0de93bb1696abe5472a43dcf4a81ed8c1008fe9764a53b11"
CASES = {"worksREAL": 100, "queueREAL": 100, "real14hREAL": 100, "gridARTIFICIAL": 50}
KINDS = ("Personal", "Event", "Transfer")


def fields(line):
    return dict(re.findall(r"([A-Za-z0-9]+)=([^ ]+)", line))


def selected(lines, prefix):
    return [fields(line) for line in lines if line.startswith(prefix)]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("artifact", type=Path)
    args = parser.parse_args()
    digest = hashlib.sha256(args.artifact.read_bytes()).hexdigest()
    assert digest == EXPECTED_SHA256, (digest, EXPECTED_SHA256)
    with ZipFile(args.artifact) as archive:
        assert len(archive.namelist()) == 40, len(archive.namelist())
        for case, steps in CASES.items():
            marks = sorted({0, 20, steps // 2, steps})
            logs = {}
            for mode in ("control", "probe"):
                lines = archive.read(f"{case}-{mode}.log").decode("utf-8").splitlines()
                assert any(line.startswith(f"PR57_289_FINAL case={case} mode={mode} calls={steps} ") for line in lines)
                windows = selected(lines, "PR57_289_WINDOW ")
                phases = selected(lines, "PR57_289_PHASE ")
                checks = selected(lines, "PR57_289_CHECKPOINT ")
                writers = selected(lines, "PR57_289_WRITER_SUMMARY ")
                assert len(windows) == 3 and len(phases) == 39 and len(checks) == 4
                assert [int(x["calls"]) for x in checks] == marks
                assert bool(writers) == (mode == "probe")
                logs[mode] = (windows, phases, checks, writers)
            for at in marks:
                a = archive.read(f"{case}-control/checkpoint-{at}.json")
                b = archive.read(f"{case}-probe/checkpoint-{at}.json")
                assert a == b, (case, at, "full JSON parity")
            windows, phases, checks, writers = logs["probe"]
            for kind, key in (("Personal", "historyRecords"), ("Event", "cityEventRecords"),
                              ("Transfer", "ledgerRecords")):
                final = next(x for x in writers if x["calls"] == str(steps) and x["kind"] == kind)
                delta = int(checks[-1][key]) - int(checks[0][key])
                assert int(final["count"]) == delta, (case, kind, delta, final)
            warm_start = steps // 2
            warm = next(x for x in windows if x["from"] == str(warm_start) and x["to"] == str(steps))
            control_warm = next(x for x in logs["control"][0] if x["from"] == str(warm_start))
            mobility = next(x for x in phases if x["from"] == str(warm_start) and x["name"] == "citizen-mobility")
            print("PR57_289_CASE", case, f"calls={steps}", "fullCheckpoints=4", "exactParity=True",
                  "history,ledger,cityEvents=allAppendCountsMatched",
                  f"warmWallMsControl={control_warm['wallMs']}", f"warmWallMsProbe={warm['wallMs']}",
                  f"warmCitizenMobilityMs={mobility['ms']}")
            for kind in KINDS:
                old = next(x for x in writers if x["calls"] == str(warm_start) and x["kind"] == kind)
                new = next(x for x in writers if x["calls"] == str(steps) and x["kind"] == kind)
                print("PR57_289_WARM", case, kind,
                      f"count={int(new['count'])-int(old['count'])}",
                      f"stopwatchTicks={int(new['ticks'])-int(old['ticks'])}",
                      f"allocatedBytes={int(new['allocatedBytes'])-int(old['allocatedBytes'])}")
    print("PR57_289_LIMIT writerTicksIntrusive=True stopwatchFrequencyNotLogged=True "
          "noOtherWritersProven=True noIntegratedSpeedup=True noLongHorizonExtrapolation=True")


if __name__ == "__main__":
    main()
