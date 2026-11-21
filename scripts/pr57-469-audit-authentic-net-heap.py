#!/usr/bin/env python3
"""Independent audit of the #469 original GitHub Actions ZIP; no engine changes.

Usage: python3 scripts/pr57-469-audit-authentic-net-heap.py pr57-469-original.zip
The checkpoint values are authentic #465, but removal is a destructive,
non-equivalent counterfactual used only to attribute memory/serialization.
"""
import hashlib
import json
import statistics
import sys
import zipfile

EXPECTED_SHA = 'faef32bac59285921d68132a2a05155b7e8dbba2ecbc99944f6072b4d50fd9f5'
EXPECTED = {
    '20h': {'historyCount': 7430, 'ledgerCount': 4085, 'eventsCount': 343,
            'fullBytes': 2254838, 'historyReclaimed': 1055480, 'ledgerReclaimed': 1298392},
    'midnight': {'historyCount': 7596, 'ledgerCount': 4346, 'eventsCount': 343,
                 'fullBytes': 2297998, 'historyReclaimed': 1078936, 'ledgerReclaimed': 1406032},
}


def audit(path):
    raw = open(path, 'rb').read()
    assert hashlib.sha256(raw).hexdigest() == EXPECTED_SHA, 'GitHub ZIP digest mismatch'
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None, 'CRC failure'
        names = set(archive.namelist())
        assert {'probe/Program.cs', 'probe/probe.csproj', 'probe-results.log'} <= names
        lines = archive.read('probe-results.log').decode('utf-8').splitlines()
        assert all(line.startswith('PR57_469_NET_HEAP ') for line in lines)
        rows = [json.loads(line.split(' ', 1)[1]) for line in lines]
    assert len(rows) == 8
    assert [(r['label'], r['repeat']) for r in rows] == [
        ('20h', 1), ('midnight', 1), ('midnight', 2), ('20h', 2),
        ('20h', 3), ('midnight', 3), ('midnight', 4), ('20h', 4)]
    for label, expected in EXPECTED.items():
        group = [r for r in rows if r['label'] == label]
        assert len(group) == 4
        for r in group:
            assert r['alive'] == r['citizenCount'] == 121
            for field in ('historyCount', 'ledgerCount', 'eventsCount'):
                assert r[field] == expected[field], (label, field)
            assert r['load']['exactRoundtrip'] is True
            assert r['load']['saveBytes'] == expected['fullBytes']
            assert r['fullSerialization']['jsonBytes'] == expected['fullBytes']
            assert r['historyReclaimedBytes'] == expected['historyReclaimed']
            assert r['ledgerReclaimedBytes'] == expected['ledgerReclaimed']
            assert r['eventsReclaimedBytes'] == 55888
            assert r['heapFull'] > r['heapWithoutHistory'] > r['heapWithoutLedger'] > r['heapWithoutEvents']
            a, b, c, d = (r[k]['jsonBytes'] for k in (
                'fullSerialization', 'noHistorySerialization',
                'noLedgerSerialization', 'noEventsSerialization'))
            assert a > b > c > d
            assert len(r['fullSerialization']['wallSamplesMs']) == 6
            assert len(r['fullSerialization']['allocSamplesBytes']) == 6
        med = lambda field: statistics.median(r[field] for r in group)
        med_stage = lambda stage, field: statistics.median(r[stage][field] for r in group)
        print(label, json.dumps({
            'liveHeapMedianBytes': med('heapFull'),
            'historyReclaimedBytes': med('historyReclaimedBytes'),
            'ledgerReclaimedBytes': med('ledgerReclaimedBytes'),
            'historyPlusLedgerRetainedFraction': round((med('historyReclaimedBytes') + med('ledgerReclaimedBytes')) / med('heapFull'), 6),
            'fullSerializeWallMedianMs': med_stage('fullSerialization', 'wallMedianMs'),
            'withoutHistoryAndLedgerWallMedianMs': med_stage('noLedgerSerialization', 'wallMedianMs'),
            'fullSerializeAllocatedBytes': med_stage('fullSerialization', 'allocMedianBytes'),
            'withoutHistoryAndLedgerAllocatedBytes': med_stage('noLedgerSerialization', 'allocMedianBytes'),
            'loadAllocatedBytes': med_stage('load', 'loadBytes'),
        }, sort_keys=True))
    print('PR57_469_INDEPENDENT_NET_HEAP_AUDIT_PASS')


if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: script original-469-zip'
    audit(sys.argv[1])
