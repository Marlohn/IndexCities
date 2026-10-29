#!/usr/bin/env python3
"""Offline audit of PR57 #360 original Actions ZIP; no engine execution.

Usage: python3 scripts/pr57-360-audit.py pr57-360-original.zip
"""
from hashlib import sha256
from pathlib import Path
from zipfile import ZipFile
import sys

archive = Path(sys.argv[1]); raw = archive.read_bytes()
print('zip_sha256', sha256(raw).hexdigest(), 'bytes', len(raw))
with ZipFile(archive) as z:
    assert z.testzip() is None, 'ZIP CRC failure'
    assert len(z.namelist()) == 8, z.namelist()
    def read(path): return z.read(path)
    def rows(mode, prefix):
        return [dict(part.split('=', 1) for part in line.split()[1:] if '=' in part)
                for line in read(f'{mode}/full.log').decode().splitlines()
                if line.startswith(prefix + ' ')]
    control = read('control/checkpoint-10800.json')
    probe = read('probe/checkpoint-10800.json')
    assert control == probe and len(control) == 2254838
    print('checkpoint_sha256', sha256(control).hexdigest(), 'bytes', len(control))
    windows = {}
    for mode in ('control', 'probe'):
        assert len(rows(mode, 'PR57_337_HOUR')) == 6
        assert len(rows(mode, 'PR57_345_PHASE')) == 78
        assert rows(mode, 'PR57_337_FINAL')[0]['parity'] == 'IDENTICAL'
        assert len(rows(mode, 'PR57_206_RESIDUAL')) == 1200
        assert len(rows(mode, 'PR57_244_STATE')) == 6000
        assert len(rows(mode, 'PR57_359_GROUP')) == 10
        windows[mode] = rows(mode, 'PR57_359_WINDOW')
        assert [int(w['start']) for w in windows[mode]] == [40001, 50001]
        assert all(int(w['slices']) == 600 and int(w['movedActors']) == 0
                   for w in windows[mode])
    assert not rows('control', 'PR57_360_WINDOW')
    shadow = rows('probe', 'PR57_360_WINDOW')
    assert [int(s['start']) for s in shadow] == [40001, 50001]
    for i, s in enumerate(shadow):
        assert int(float(s['slices'])) == 600 and int(s['actors']) == 58800
        assert all(int(s[k]) == 0 for k in ('ownNetChanges', 'betweenSliceVehicleChanges',
                   'worldNetChanges', 'waitingDeadlineFlips', 'roadRevisionChanges',
                   'postActorSyncChanges'))
        assert int(s['selectedUniqueEdges']) == 19200
        assert float(s['largestSelectedComponentMean']) == 30
        assert int(s['largestSelectedComponentMax']) == 30
        assert float(s['selectedComponentMean']) == 68
        assert float(s['isolatedMean']) == 66
        base = float(windows['control'][i]['physicalSliceMs'])
        with_shadow = float(windows['probe'][i]['physicalSliceMs'])
        observer = float(s['observerMs'])
        print('PR57_360_AUDITED', 'start', s['start'], 'actors', s['actors'],
              'betweenCitizens', s['betweenSliceCitizenChanges'],
              'selectedEdgesPerSlice', int(s['selectedUniqueEdges']) // 600,
              'largestSelectedComponent', s['largestSelectedComponentMax'],
              'basePhysicalMs', base, 'shadowPhysicalMs', with_shadow,
              'physicalDeltaMs', round(with_shadow - base, 6),
              'directObserverMs', observer,
              'observerToBaseFraction', round(observer / base, 6))
    print('PR57_360_PARITY byteIdentical=True individualAdvance2=True '
          'REAL121=True GRID=NOT_TESTED selectedGraphLowerBound=True '
          'notCausalClosure=True acceptedIntegratedSpeedup=False')
