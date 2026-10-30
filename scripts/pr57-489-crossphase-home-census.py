#!/usr/bin/env python3
"""PR57 #489: bound the usefulness of reusing infrastructure occupancy for needs.

Read-only authentic saves, no motor mutation. This is endpoint evidence only;
no assertion that an inter-phase writer cannot change membership inside Step.
"""
import hashlib
import json
import sys
import zipfile

SOURCES = [
    ('REAL_CONGESTED_MIDNIGHT', '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1',
     ('A-mid/checkpoint-0.json', 'A-mid/checkpoint-1.json', 'A-post/checkpoint-1.json'),
     ('B-mid/checkpoint-0.json', 'B-mid/checkpoint-1.json', 'B-post/checkpoint-1.json')),
    ('REAL_68_WORKS', 'ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a',
     ('pr57-445/A/checkpoint-0.json', 'pr57-445/A/checkpoint-1.json'),
     ('pr57-445/B/checkpoint-0.json', 'pr57-445/B/checkpoint-1.json')),
]


def sha256(blob):
    return hashlib.sha256(blob).hexdigest()


def census(state):
    citizens = state['Citizens']
    citizen_by_id = {c['Id']: c for c in citizens}
    families = {f['Id']: f for f in state['Families']}
    buildings = {b['Id']: b for b in state['Buildings']}
    # Mirrors Infrastructure.cs: occupied = count of alive, resident members
    # of b.FamilyId, irrespective of InsideMap; active means completed/not removed.
    occupied = []
    for b in state['Buildings']:
        if not b['Completed'] or b['Removed'] or b['Kind'] != 'House':
            continue
        family = families.get(b['FamilyId']) if b.get('FamilyId') is not None else None
        if family is not None and any(citizen_by_id.get(cid, {}).get('Alive', False)
                                      and citizen_by_id[cid]['Resident'] for cid in family['Members']):
            occupied.append((b['Position']['X'], b['Position']['Z']))
    # Mirrors Economy.cs: exact eligible citizens, family.HomeId, and BuildingById.
    needed = []
    eligible = 0
    for c in citizens:
        if not (c['Alive'] and c['Resident'] and c['InsideMap']):
            continue
        eligible += 1
        family = families.get(c['FamilyId'])
        home = buildings.get(family['HomeId']) if family is not None else None
        if home is None or home['Removed']:
            continue
        point = (home['Position']['X'], home['Position']['Z'])
        if point not in needed:
            needed.append(point)
    occ = set(occupied)
    need = set(needed)
    # Exact points are compared; no rounding or floating arithmetic.
    return dict(citizens=len(citizens), eligible=eligible,
                occupiedBuildings=len(occupied), occupiedPoints=len(occ),
                neededPoints=len(need), missedByOccupancy=len(need-occ),
                unnecessaryFromOccupancy=len(occ-need),
                sameOrder=occupied==needed,
                sameSet=occ==need)


def main(paths):
    assert len(paths) == 2
    all_rows = []
    for (label, expected, a_names, b_names), path in zip(SOURCES, paths):
        blob = open(path, 'rb').read()
        assert sha256(blob) == expected, (label, 'ZIP SHA mismatch')
        with zipfile.ZipFile(path) as z:
            assert z.testzip() is None, (label, 'CRC failed')
            for a, b in zip(a_names, b_names):
                assert z.read(a) == z.read(b), (label, 'A/B save mismatch', a, b)
                row = census(json.loads(z.read(a)))
                assert row['sameSet'] and row['missedByOccupancy'] == 0, (label, a, row)
                all_rows.append(row)
                print('PR57_489_ENDPOINT', 'regime=' + label, 'checkpoint=' + a,
                      'citizens=' + str(row['citizens']),
                      'eligible=' + str(row['eligible']),
                      'occupied=' + str(row['occupiedPoints']),
                      'needed=' + str(row['neededPoints']),
                      'missed=' + str(row['missedByOccupancy']),
                      'extra=' + str(row['unnecessaryFromOccupancy']),
                      'sameOrder=' + str(row['sameOrder']))
    assert len(all_rows) == 5
    # Historical #441: 85.575/84.942 ms for the *whole* home-list preparation
    # in 10,800 Steps. Eliminating it all for free is deliberately optimistic.
    prep_ms = (85.575 + 84.942) / 2
    profile_ms = 52559.524  # #439 13 phase control, 10,800 Advance(2)
    max_speedup = profile_ms / (profile_ms - prep_ms)
    print('PR57_489_OPTIMISTIC_AMDAHL', 'prepMsPer10800=' + f'{prep_ms:.4f}',
          'prepUsPerStep=' + f'{prep_ms * 1000 / 10800:.4f}',
          'maxSpeedup=' + f'{max_speedup:.7f}',
          'scope=historical_profile_not_new_benchmark')
    assert 1 < max_speedup < 1.002
    print('PR57_489_AUTHENTIC_CROSSPHASE_HOME_CENSUS_PASS '
          'scope=endpoints_only no_intraStep_equivalence=True acceptedGain=False')


if __name__ == '__main__':
    main(sys.argv[1:])
