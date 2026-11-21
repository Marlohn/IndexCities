#!/usr/bin/env python3
"""PR57 #475 companion: authentic 68-worksite 100xAdvance(2) endpoint diff.

Endpoint differences are NOT per-Step mutation rates or phase-level write sets.
Usage: python3 pr57-475-worksite-boundary.py pr57-445-original.zip
"""
import collections
import hashlib
import json
import sys
import zipfile

EXPECTED = 'ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a'
PREFIX = 'pr57-445/'


def main(path):
    sha = hashlib.sha256(open(path, 'rb').read()).hexdigest()
    assert sha == EXPECTED, sha
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        a0, a1 = [z.read(PREFIX + f'A/checkpoint-{i}.json') for i in (0, 1)]
        assert a0 == z.read(PREFIX + 'B/checkpoint-0.json')
        assert a1 == z.read(PREFIX + 'B/checkpoint-1.json')
        before, after = json.loads(a0), json.loads(a1)
        report = {}
        for kind in ('Citizens', 'Buildings', 'Families', 'Roads', 'Vehicles', 'Projects'):
            left = {row['Id']: row for row in before[kind]}
            right = {row['Id']: row for row in after[kind]}
            assert left.keys() == right.keys()
            fields = collections.Counter()
            changed = 0
            for key in left:
                differences = [field for field in set(left[key]) | set(right[key])
                               if left[key].get(field) != right[key].get(field)]
                changed += bool(differences)
                fields.update(differences)
            report[kind] = {'total': len(left), 'changed': changed,
                            'fields': dict(sorted(fields.items(), key=lambda kv: (-kv[1], kv[0])))}
        assert before['PollutionCells'].keys() == after['PollutionCells'].keys()
        pollution = sum(before['PollutionCells'][k] != after['PollutionCells'][k]
                        for k in before['PollutionCells'])
        print(json.dumps({'experiment': 475, 'fixture': 'AUTHENTIC_REAL_68_WORKS_100_ADVANCE2',
                          'source_sha256': sha, 'from': before['Now'], 'to': after['Now'],
                          'pollution_changed': pollution, 'pollution_total': len(before['PollutionCells']),
                          'entities': report,
                          'scope': 'ENDPOINT_DIFF_NOT_PHASE_WRITERS_OR_PER_STEP'},
                         indent=2, sort_keys=True))
        print('PR57_475_WORKSITE_BOUNDARY_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        raise SystemExit('usage: python3 pr57-475-worksite-boundary.py pr57-445-original.zip')
    main(sys.argv[1])
