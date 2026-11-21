#!/usr/bin/env python3
"""PR57 #473: independent audit of the original #472 GitHub Actions ZIP.

Usage: python3 scripts/pr57-473-audit-net-shared-log.py pr57-472-original.zip
Observational only. Does not benchmark CitySimulation or validate product equivalence.
"""
import hashlib
import json
import statistics
import sys
import zipfile
from pathlib import Path

EXPECTED_SHA256 = '4f82a4268e765acbeb95db8aaf865d66270bad52567efef4eb89d43206e6653f'
EXPECTED_ARTIFACT_ID = 13000659479
EXPECTED_RUN_ID = 41106863662
MODES = [('raw', 16384), ('packed', 4096), ('packed', 16384), ('packed', 65536)]


def main(path: Path) -> None:
    raw = path.read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    assert digest == EXPECTED_SHA256, (digest, EXPECTED_SHA256)
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None, 'ZIP CRC failure'
        names = archive.namelist()
        assert len(names) == 34, len(names)
        rows = json.loads(archive.read('results/summary.json'))
        assert len(rows) == 8
        assert {(r['mode'], r['block'], r['repeat']) for r in rows} == {
            (mode, block, repeat) for mode, block in MODES for repeat in (1, 2)
        }
        for r in rows:
            name = f"results/{r['mode']}-{r['block']}-{r['repeat']}.log"
            lines = [x for x in archive.read(name).decode().splitlines()
                     if x.startswith('PR57_472_NET ')]
            assert len(lines) == 1, name
            assert json.loads(lines[0].split(' ', 1)[1]) == r, name
            assert r['appended'] == 324
            assert r['checks']['events'] == 12285
            assert r['checks']['pointReads'] == 250
            assert r['checks']['exact'] is True
            assert r['checks']['exportedBytes'] > 1400000
            assert r['retainedAfterBytes'] > 0
            assert r['checks']['readMedianAllocatedBytes'] >= 0
            assert r['checks']['exportAllocatedBytes'] >= 0
            if r['mode'] == 'packed':
                s = r['storage']
                assert s['collections'] == 123 and s['entries'] == 12285
                assert s['segments'] == 187 and s['anchors'] == 887
                assert s['hypotheticalPackedIndexBytes'] == 13552
            else:
                assert r['storage']['rawBytes'] == 1442030
        grouped = {(m, b): sorted((r for r in rows if (r['mode'], r['block']) == (m, b)),
                                  key=lambda r: r['repeat']) for m, b in MODES}
        baseline = grouped[('raw', 16384)]
        def med(rs, *path):
            return statistics.median(next_value(r, path) for r in rs)
        base_read = med(baseline, 'checks', 'readMedianMs')
        base_export = med(baseline, 'checks', 'exportMs')
        base_heap = med(baseline, 'retainedAfterBytes')
        print('PR57_473_ZIP_SHA256', digest)
        print('PR57_473_RUN', EXPECTED_RUN_ID, 'ARTIFACT', EXPECTED_ARTIFACT_ID)
        print('mode block retained_after_B append_ms read_250_ms export_12285_ms '
              'export_alloc_B read_x export_x raw_minus_packed_heap_B')
        for (mode, block), group in grouped.items():
            read = med(group, 'checks', 'readMedianMs')
            export = med(group, 'checks', 'exportMs')
            heap = med(group, 'retainedAfterBytes')
            print(mode, block, int(heap), round(med(group, 'appendMs'), 4),
                  round(read, 4), round(export, 4),
                  int(med(group, 'checks', 'exportAllocatedBytes')),
                  round(read / base_read, 1), round(export / base_export, 1),
                  int(base_heap - heap))
        assert min(med(group, 'checks', 'exportMs') / base_export
                   for key, group in grouped.items() if key[0] == 'packed') > 50
        assert min(med(group, 'checks', 'readMedianMs') / base_read
                   for key, group in grouped.items() if key[0] == 'packed') > 1000
        print('PR57_473_NET_SHARED_LOG_AUDIT_PASS')


def next_value(row, path):
    for key in path:
        row = row[key]
    return row


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python3 pr57-473-audit-net-shared-log.py original-472.zip')
    main(Path(sys.argv[1]))
