#!/usr/bin/env python3
"""PR57 #288: summarize authentic .NET ABBA/BAAB serializer logs.

Usage: python3 scripts/pr57-288-save-cost-summary.py pr57-288-results.zip
Only measured save/load cost, not Advance(2) throughput or an 80-year forecast.
"""
import argparse
import hashlib
import re
import statistics
from pathlib import Path
from zipfile import ZipFile

EXPECTED_SHA256 = '3a04abc1a54d09df9da24b1cc4224b64968a770632e038b3b54d42200ee8100f'
CASES = ('worksREAL', 'queueREAL', 'real14hREAL', 'gridARTIFICIAL')


def fields(line):
    return dict(re.findall(r'([A-Za-z0-9]+)=([^ ]+)', line))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('artifact', type=Path)
    args = parser.parse_args()
    actual = hashlib.sha256(args.artifact.read_bytes()).hexdigest()
    assert actual == EXPECTED_SHA256, (actual, EXPECTED_SHA256)
    with ZipFile(args.artifact) as z:
        assert len(z.namelist()) == 8, z.namelist()
        for case in CASES:
            records = {}
            sources = []
            for i, order in ((0, 'ABBA'), (1, 'BAAB')):
                log = z.read(f'{case}-checkpoint-{i}.log').decode('utf-8')
                lines = log.splitlines()
                assert lines[0].startswith('PR57_288_SOURCE ')
                assert lines[-1].startswith('PR57_288_PARITY ')
                assert 'originalSaveUnchanged=True' in lines[-1]
                src = fields(lines[0])
                assert src['order'] == order and src['fullByteRoundtrip'] == 'True'
                sources.append(src)
                measures = [fields(line) for line in lines if line.startswith('PR57_288_RESULT ')]
                assert len(measures) == 10, (case, i, len(measures))
                for m in measures:
                    assert m['order'] == order and m['reps'] == '6'
                    records.setdefault((m['op'], m['mode']), []).append(m)
            assert len(records[('serialize', 'full')]) == 4
            assert len(records[('serialize', 'without-history')]) == 4
            for s in sources:
                print('PR57_288_BYTES', case, s['path'].split('/')[-1],
                      f"full={s['fullBytes']} stripped={s['strippedBytes']}",
                      f"reductionPct={100 * (1 - int(s['strippedBytes']) / int(s['fullBytes'])):.2f}",
                      f"records={s['historyRecords']},{s['ledgerRecords']},{s['cityEventRecords']}")
            for op in ('serialize', 'deserialize'):
                full = records[(op, 'full')]
                trimmed = records[(op, 'without-history')]
                def median(rows, key):
                    return statistics.median(float(x[key]) for x in rows)
                full_wall = median(full, 'wallMsPerOp')
                trim_wall = median(trimmed, 'wallMsPerOp')
                full_alloc = median(full, 'allocBytesPerOp')
                trim_alloc = median(trimmed, 'allocBytesPerOp')
                print('PR57_288_COMPARISON', case, op,
                      f'fullMedianWallMs={full_wall:.3f}',
                      f'strippedMedianWallMs={trim_wall:.3f}',
                      f'fullMedianAllocMB={full_alloc / 1e6:.3f}',
                      f'strippedMedianAllocMB={trim_alloc / 1e6:.3f}',
                      f'allocReductionPct={100 * (1 - trim_alloc / full_alloc):.2f}',
                      'scope=serializer-only-no-causal-save')
            for op in ('public-SaveToJson', 'public-FromJson'):
                values = records[(op, 'full-only')]
                print('PR57_288_PUBLIC', case, op,
                      f"medianWallMs={statistics.median(float(x['wallMsPerOp']) for x in values):.3f}",
                      'fullSaveOnly=True')
    print('PR57_288_LIMIT notAdvance2Benchmark=True noHistoryDeletionInProduct=True '
          'not80YearProjection=True historicalArraysRequired=True')


if __name__ == '__main__':
    main()
