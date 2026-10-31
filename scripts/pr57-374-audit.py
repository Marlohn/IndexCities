#!/usr/bin/env python3
"""Independent read-only audit of original PR57 #374 Actions ZIP.

Usage: python3 scripts/pr57-374-audit.py /path/to/original-374.zip
No 10k extrapolation, no causal closure or speedup claim.
"""
import hashlib
import json
import sys
import zipfile
from pathlib import Path

ARTIFACT_SHA = '048d230107d8a605bf339432dcfae9368fd336e8c6239765f7c00d1edd0d7a17'
SAVE_SHA = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'

def records(log, prefix):
    return [dict(part.split('=', 1) for part in line.split()[1:] if '=' in part)
            for line in log.splitlines() if line.startswith(prefix + ' ')]

def one(log, prefix):
    rows = records(log, prefix)
    assert len(rows) == 1, (prefix, len(rows))
    return rows[0]

def audit(path):
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == ARTIFACT_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        expected = {'summary.txt', 'checkpoints.sha256'}
        expected |= {f'{arm}/{name}' for arm in ('control', 'shadow')
                     for name in ('full.log', 'census.log', 'checkpoint-10800.json')}
        assert set(z.namelist()) == expected
        saves = [z.read(f'{arm}/checkpoint-10800.json') for arm in ('control', 'shadow')]
        assert saves[0] == saves[1]
        assert len(saves[0]) == 2254838
        assert hashlib.sha256(saves[0]).hexdigest() == SAVE_SHA
        assert z.read('checkpoints.sha256').decode().lower().count(SAVE_SHA) == 2
        logs = {arm: z.read(f'{arm}/census.log').decode() for arm in ('control', 'shadow')}
        results = {}
        for arm, log in logs.items():
            hours = records(log, 'PR57_337_HOUR')
            assert len(hours) == 6
            assert [int(x['hour']) for x in hours] == list(range(1, 7))
            assert all(int(x['living']) == 121 for x in hours)
            assert one(log, 'PR57_337_FINAL')['parity'] == 'IDENTICAL'
            assert int(one(log, 'PR57_367_SUMMARY')['writes']) == 1852
            assert int(one(log, 'PR57_369_SUMMARY')['countChanges']) == 17
            assert int(one(log, 'PR57_370_SUMMARY')['refContentDifferent']) == 51
            assert int(one(log, 'PR57_372_SUMMARY')['events']) == 1920
            assert int(one(log, 'PR57_372_SUMMARY')['published']) == 0
            results[arm] = dict(wall_ms=round(sum(float(x['wallMs']) for x in hours), 3),
                                cpu_ms=sum(float(x['cpuMs']) for x in hours),
                                allocated_bytes=sum(int(x['totalAllocatedBytes']) for x in hours))
        assert not records(logs['control'], 'PR57_374_SUMMARY')
        s = one(logs['shadow'], 'PR57_374_SUMMARY')
        expected_counts = dict(events=1920, physicalSlices=12618, dirtySlices=286,
                               dirtySliceIdPairs=881, dirtyAdvances=50,
                               dirtyAdvanceIdPairs=161, maxDirtyIdsPerAdvance=7,
                               maxEventsPerAdvance=88, externalEvents=30,
                               nextSliceDirtyWindows=291,
                               nextSliceDirtyCandidatePairs=899,
                               nextSliceReadCandidatePairs=751,
                               nextSliceReadPairs=20141, nextSliceReadCalls=20915,
                               nextSliceTrue=20699, nextSliceFalse=216,
                               maxReadersPerDirtyCandidate=29)
        assert {k: int(s[k]) for k in expected_counts} == expected_counts
        assert expected_counts['nextSliceTrue'] + expected_counts['nextSliceFalse'] == expected_counts['nextSliceReadCalls']
        assert expected_counts['nextSliceReadCandidatePairs'] <= expected_counts['nextSliceDirtyCandidatePairs']
        assert expected_counts['nextSliceReadPairs'] <= expected_counts['nextSliceReadCalls']
        assert s['noCausalClosure'] == 'True' and s['noSkippedSlices'] == 'True'
        results['shadow']['counts'] = expected_counts
        results['shadow']['coalescing_by_slice_pct'] = round(100*(1-881/1920), 3)
        results['shadow']['coalescing_by_advance_pct'] = round(100*(1-161/1920), 3)
        results['shadow']['dirty_slice_pct'] = round(100*286/12618, 3)
        results['shadow']['dirty_candidate_next_slice_read_pct'] = round(100*751/899, 3)
        results['shadow']['mean_unique_readers_per_read_candidate'] = round(20141/751, 3)
        print(json.dumps(dict(artifact_sha256=ARTIFACT_SHA, crc='ok', members=8,
                              save_sha256=SAVE_SHA, save_parity='byte-identical',
                              results=results), indent=2, sort_keys=True))

if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)
    audit(sys.argv[1])
