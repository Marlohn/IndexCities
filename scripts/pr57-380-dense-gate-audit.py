#!/usr/bin/env python3
"""PR57 #380: independently prove or falsify complete partial-gate read graph.

Input: original ZIP of PR57 #379 Action #40940050736, artifact #12926297882.
This is an offline audit, NOT a benchmark, scheduler or causal certificate.
"""
import argparse
import hashlib
import json
import zipfile

EXPECTED_ZIP = '38b80ea19cc0da3166387d6f221a4ad66af8dcb253cae6fd7d9220b1f8ede9d0'
EXPECTED_SAVE = 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'


def summary_line(content, prefix):
    matches = [line for line in content.splitlines() if line.startswith(prefix + ' ')]
    assert len(matches) == 1, (prefix, len(matches))
    return dict(part.split('=', 1) for part in matches[0].split()[1:] if '=' in part)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('zip', help='Original PR57 #379 artifact ZIP')
    parser.add_argument('--vehicles', type=int, default=98,
                        help='Authentic roster size; verify against save and canonical loop')
    args = parser.parse_args()
    raw = open(args.zip, 'rb').read()
    zip_hash = hashlib.sha256(raw).hexdigest()
    assert zip_hash == EXPECTED_ZIP, ('unexpected ZIP, do not mix experiment arms', zip_hash)
    with zipfile.ZipFile(args.zip) as archive:
        assert archive.testzip() is None, 'ZIP CRC failure'
        summary = json.loads(archive.read('summary.txt'))
        assert summary['gate'] == 'PARTIAL_OCCUPIES_ONLY'
        assert summary['causalClosure'] is False and summary['acceptedSpeedup'] is False
        logs = {arm: archive.read(f'{arm}/census.log').decode()
                for arm in ('control', 'presence')}
        final = {arm: archive.read(f'{arm}/checkpoint-10800.json')
                 for arm in ('control', 'presence')}
        assert final['control'] == final['presence'], 'control and shadow saves differ'
        save_hash = hashlib.sha256(final['control']).hexdigest()
        assert save_hash == EXPECTED_SAVE, ('unexpected REAL checkpoint', save_hash)
        vehicles = json.loads(final['control'])['Vehicles']
        assert len(vehicles) == args.vehicles
        assert len({v['Id'] for v in vehicles}) == args.vehicles, 'duplicate vehicle ID'
        reads = [summary_line(logs[arm], 'PR57_377_SUMMARY')
                 for arm in ('control', 'presence')]
        counters = ('physicalSlices', 'readCalls', 'uniqueReadPairs',
                    'writerEvents', 'uniqueWriters', 'externalEvents',
                    'writersWithReaders', 'invalidatedReaderPairs',
                    'maxReadersPerWriter')
        assert all(reads[0][key] == reads[1][key] for key in counters), \
            'partial gate graph differs between arms'
        for row in reads:
            assert int(row['physicalSlices']) == 600
            assert row['noDecisionReuse'] == row['noSkippedSlices'] == 'True'
        r = reads[0]
        total_edges = int(r['uniqueReadPairs'])
        total_calls = int(r['readCalls'])
        churn = summary['churn']
        presence = summary['presence']
        assert int(churn['physicalSlices']) == 600
        assert int(churn['transitions']) == int(presence['transitions']) == 599
        stable = int(churn['stableEdges'])
        added = int(churn['addedEdges'])
        removed = int(churn['removedEdges'])
        active_prev = int(presence['prevActiveSum'])
        active_now = int(presence['nowActiveSum'])
        assert stable + removed == total_edges, 'slice 600 is not empty'
        assert stable + added == total_edges, 'slice 1 is not empty'
        assert active_prev == active_now, 'unexpected active reader boundary'
        # The source's self-ID guard excludes exactly one of 98 vehicles from
        # Pr57StageOccupies. No reader can form more than N-1 UNIQUE edges/slice.
        max_possible_edges = (args.vehicles - 1) * active_prev
        assert total_edges <= max_possible_edges, 'invalid source/roster assumption'
        missing_edges = max_possible_edges - total_edges
        toggles = int(presence['entries']) + int(presence['exits'])
        assert int(presence['persistentChurn']) == 0
        assert int(presence['toggledChurn']) == int(presence['edgeChurn'])
        assert int(presence['edgeChurn']) == added + removed
        assert int(presence['edgeChurn']) <= (args.vehicles - 1) * toggles
        assert total_calls >= total_edges
        result = {
            'experiment': 380,
            'sourceArtifact': 12926297882,
            'zipSha256': zip_hash,
            'saveSha256': save_hash,
            'physicalSlices': 600,
            'vehicles': args.vehicles,
            'activeReaderSliceOccurrences': active_prev,
            'uniqueCandidateReaderEdges': total_edges,
            'maxPossibleUniqueEdges': max_possible_edges,
            'missingEdges': missing_edges,
            'allActiveReadersReadEveryOtherVehicle': missing_edges == 0,
            'canonicalGateCalls': total_calls,
            'repeatCallsBeyondUniqueEdges': total_calls - total_edges,
            'presenceEntriesAndExits': toggles,
            'presenceAttributedChurn': int(presence['toggledChurn']),
            'firstAndLastSliceHaveZeroEdges': True,
            'scope': 'FIRST_600_REAL_PHYSICAL_SLICES_PARTIAL_OCCUPIES_ONLY',
            'causalClosure': False,
            'decisionReuse': False,
            'acceptedIntegratedSpeedup': False,
        }
        print(json.dumps(result, indent=2, ensure_ascii=False))
        if missing_edges:
            print('NOT a complete graph; investigate missing candidate reads.')
        else:
            print('Complete directed reader-to-other-vehicle graph for this bounded gate only.')
            print('A candidate-specific reverse graph provides no selectivity in this window.')
            print('Reader presence is observed after the scan; this does not authorize skipping it.')


if __name__ == '__main__':
    main()
