#!/usr/bin/env python3
"""PR57 #290: reproduce warm per-sub-slice transition census from authentic CI ZIP.

Usage: python3 scripts/pr57-290-transition-summary.py pr57-290.zip
This is a net-state census, NOT a writer journal, causal closure or speedup.
"""
import argparse
import hashlib
import re
from pathlib import Path
from zipfile import ZipFile

EXPECTED_SHA256 = '3b97f652b83b2356a296d1f5d7ea392162b08e1a261f90403e3dadd570b63199'
CASES = {'worksREAL': 100, 'queueREAL': 100, 'real14hREAL': 100, 'gridARTIFICIAL': 50}
STAGES = ('operations', 'physical', 'citizens')
COUNTERS = ('intervals', 'citizenVisits', 'vehicleVisits', 'citizenVehicleNet',
            'citizenCrossingNet', 'citizenPositionNet', 'citizenBoardingNet',
            'citizenMovingNet', 'vehicleDriverNet', 'vehiclePositionNet',
            'vehicleClaimNet', 'vehicleStatusNet', 'intervalsChanged',
            'citizensChanged', 'vehiclesChanged', 'identityMismatch',
            'observerMs', 'observerAllocatedBytes')

def fields(line):
    return dict(re.findall(r'([A-Za-z0-9]+)=([^ ]+)', line))

def rows(log, prefix):
    return [fields(x) for x in log.splitlines() if x.startswith(prefix)]

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('artifact', type=Path)
    args = ap.parse_args()
    digest = hashlib.sha256(args.artifact.read_bytes()).hexdigest()
    if EXPECTED_SHA256:
        assert digest == EXPECTED_SHA256, (digest, EXPECTED_SHA256)
    print('PR57_290_ARTIFACT_SHA256', digest)
    with ZipFile(args.artifact) as z:
        print('PR57_290_ZIP_FILES', len(z.namelist()))
        for case, calls in CASES.items():
            checkpoints = sorted({0, 20, calls // 2, calls})
            logs = {mode: z.read(f'{case}-{mode}.log').decode('utf-8')
                    for mode in ('control', 'probe')}
            for at in checkpoints:
                a = z.read(f'{case}-control/checkpoint-{at}.json')
                b = z.read(f'{case}-probe/checkpoint-{at}.json')
                assert a == b, (case, at, 'full JSON parity')
            for mode, log in logs.items():
                assert f'PR57_290_FINAL case={case} mode={mode} calls={calls}' in log
                assert len(rows(log, 'PR57_290_WINDOW ')) == 3
                assert len(rows(log, 'PR57_290_PHASE ')) == 39
                assert len(rows(log, 'PR57_290_CHECKPOINT ')) == 4
                assert len(rows(log, 'PR57_290_STAGE ')) == (12 if mode == 'probe' else 0)
            warm_start = calls // 2
            warm_wall = {}
            warm_phase = {}
            for mode, log in logs.items():
                warm = next(x for x in rows(log, 'PR57_290_WINDOW ')
                            if int(x['from']) == warm_start and int(x['to']) == calls)
                warm_wall[mode] = float(warm['wallMs'])
                warm_phase[mode] = {x['name']: float(x['ms']) for x in rows(log, 'PR57_290_PHASE ')
                                    if int(x['from']) == warm_start and int(x['to']) == calls}
            stage_rows = rows(logs['probe'], 'PR57_290_STAGE ')
            print('PR57_290_CASE', case, f'calls={calls}', 'fullJsonParity=True',
                  f'warmControlWallMs={warm_wall["control"]:.4f}',
                  f'warmProbeWallMs={warm_wall["probe"]:.4f}',
                  f'warmControlVehiclesMs={warm_phase["control"]["vehicles"]:.4f}',
                  f'warmControlCitizensMs={warm_phase["control"]["citizen-mobility"]:.4f}')
            for stage in STAGES:
                first = next(x for x in stage_rows if x['stage'] == stage and int(x['calls']) == warm_start)
                last = next(x for x in stage_rows if x['stage'] == stage and int(x['calls']) == calls)
                delta = {k: float(last[k]) - float(first[k]) if k == 'observerMs'
                         else int(last[k]) - int(first[k]) for k in COUNTERS}
                assert delta['intervals'] >= 0
                assert 0 <= delta['intervalsChanged'] <= delta['intervals']
                print('PR57_290_WARM', case, stage, ' '.join(f'{k}={delta[k]}' for k in COUNTERS))
    print('PR57_290_LIMIT netChangesOnly=True intraIntervalWritesUnknown=True '
          'snapshotScanIsNotJournal=True observerIntrusive=True noIntegratedSpeedup=True')

if __name__ == '__main__':
    main()
