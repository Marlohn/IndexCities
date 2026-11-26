#!/usr/bin/env python3
"""Independent offline audit of the original PR57 #359 Actions ZIP.

Usage: python3 scripts/pr57-359-audit.py pr57-359-original.zip
No GitHub access, motor mutation, or benchmark rerun is required.
"""
from hashlib import sha256
from pathlib import Path
from zipfile import ZipFile
import sys

archive=Path(sys.argv[1])
raw=archive.read_bytes()
print("zip_sha256",sha256(raw).hexdigest(),"bytes",len(raw))
with ZipFile(archive) as z:
    assert z.testzip() is None,"ZIP CRC failed"
    names=z.namelist()
    def read(suffix):
        matches=[x for x in names if x.endswith(suffix)]
        assert len(matches)==1,(suffix,matches)
        return z.read(matches[0])
    control=read("control/checkpoint-10800.json")
    probe=read("probe/checkpoint-10800.json")
    assert control==probe,"Full checkpoint mismatch"
    print("checkpoint_sha256",sha256(control).hexdigest(),"bytes",len(control))
    def rows(mode,prefix):
        return [
            {k:v for k,v in (part.split("=",1) for part in line.split()[1:] if "=" in part)}
            for line in read(mode+"/full.log").decode().splitlines()
            if line.startswith(prefix+" ")
        ]
    windows=rows("probe","PR57_359_WINDOW")
    groups=rows("probe","PR57_359_GROUP")
    raw_slices=rows("probe","PR57_206_RESIDUAL")
    raw_states=rows("probe","PR57_244_STATE")
    assert len(windows)==2 and len(groups)==10
    assert len(raw_slices)==1200 and len(raw_states)==6000
    assert not rows("control","PR57_359_WINDOW")
    for mode in ("control","probe"):
        assert len(rows(mode,"PR57_337_HOUR"))==6
        assert len(rows(mode,"PR57_345_PHASE"))==78
        assert rows(mode,"PR57_337_FINAL")[0]["parity"]=="IDENTICAL"
    for window in windows:
        start,end=int(window["start"]),int(window["end"])
        assert (start,end) in ((40001,40600),(50001,50600))
        assert int(window["slices"])==600
        sample_first=1 if start==40001 else 601
        sample_last=sample_first+599
        sampled=[r for r in raw_slices if start<=int(r["eligibleSlice"])<=end]
        assert len(sampled)==600
        assert [int(r["sample"]) for r in sampled]==list(range(sample_first,sample_last+1))
        grouped=[r for r in groups if int(r["start"])==start]
        assert len(grouped)==5
        assert sum(int(r["actors"]) for r in grouped)==600*98
        for group in grouped:
            original=[r for r in raw_states if
                      sample_first<=int(r["sample"])<=sample_last and
                      r["group"]==group["group"]]
            assert len(original)==600,(start,group["group"])
            assert sum(int(r["actors"]) for r in original)==int(group["actors"])
            for stage in range(9):
                from_raw=sum(float(r[f"s{stage}Ms"]) for r in original)
                assert abs(from_raw-float(group[f"s{stage}Ms"]))<.03,(
                    start,group["group"],stage,from_raw,group[f"s{stage}Ms"])
        total=sum(float(r["totalMs"]) for r in grouped)
        selected=sum(float(r[f"s{i}Ms"]) for r in grouped
                     if r["group"]=="other-travelling-still"
                     for i in range(1,5))
        assert abs(total-float(window["actorLoopMs"]))<.01
        assert abs(selected-float(window["selectedStillS1to4Ms"]))<.01
        share=selected/float(window["physicalSliceMs"])
        assert abs(share-float(window["selectedShareOfPhysicalSlice"]))<1e-6
        assert 0<=share<1
        assert abs(1/(1-share)-float(window["idealPhysicalCeiling"]))<1e-5
        print("PR57_359_VERIFIED","window",start,"slices",600,
              "physicalMs",window["physicalSliceMs"],
              "actorMs",window["actorLoopMs"],
              "stillS1to4Ms",window["selectedStillS1to4Ms"],
              "share",window["selectedShareOfPhysicalSlice"],
              "idealPhysicalCeiling",window["idealPhysicalCeiling"])
    print("PR57_359_PARITY fullCheckpointByteIdentical=True "
          "individualAdvance2=True observational=True "
          "notIntegratedAmdahl=True noSkippedSlices=True acceptedSpeedup=False")
