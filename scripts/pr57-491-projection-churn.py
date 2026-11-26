#!/usr/bin/env python3
"""PR57 #491: authentic, lossless boundary audit of joint cross-phase projections.

Not a phase-internal trace, causal proof, runtime benchmark, or proposed cache.
Inputs are original GitHub Actions ZIP artifacts #465, #445 and #476.
"""
import collections
import hashlib
import json
import sys
import zipfile

EXPECTED = {
    '465': '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1',
    '445': 'ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a',
    '476': '84bdf5c01fe95ad710589924e16414449ae37d7705c4bda170ce51abb4af359c',
}
# Candidate union of the three canonical readers: infrastructure, businesses, needs.
# Identity/layout and service values MUST NOT be confused; neither is a complete
# causal read set (time, roads, vehicles, tuning, writes and transient state omitted).
BUILDING_LAYOUT = ('Id', 'Kind', 'Position', 'Rotation', 'Completed', 'Removed',
                   'FamilyId', 'CompanyId', 'OperationClosed', 'MaintenanceRatioUnits')
BUILDING_SERVICE = ('WaterRatio', 'ElectricityRatio', 'WaterDemand',
                    'ElectricityDemand', 'SewageProduced', 'UntreatedSewage',
                    'OperationalCapacity', 'WaterOutageSecondsUnits')
CITIZEN_LAYOUT = ('Id', 'FamilyId', 'WorkplaceId', 'ConstructionProjectId',
                  'InsideMap', 'Resident', 'Alive', 'VehicleId', 'Shift', 'WeeklyDayOff')
CITIZEN_PHYSICAL = ('Position', 'PathIndex', 'BoardingVehicleId')
CITIZEN_NEEDS = ('HungerMicroseconds', 'HealthUnits', 'Satisfaction')
FAMILY_LAYOUT = ('Id', 'Members', 'HomeId')


def read_zip(path, kind):
    raw = open(path, 'rb').read()
    digest = hashlib.sha256(raw).hexdigest()
    assert digest == EXPECTED[kind], (kind, digest)
    z = zipfile.ZipFile(path)
    assert z.testzip() is None
    return z


def assert_parity(z, pairs):
    for a, b in pairs:
        assert z.read(a) == z.read(b), (a, b)


def rows(a, b, key, fields):
    aa = {x['Id']: x for x in a[key]}
    bb = {x['Id']: x for x in b[key]}
    assert aa.keys() == bb.keys(), (key, 'membership change: not comparable')
    counts = collections.Counter()
    changed = set()
    for ident in aa:
        for f in fields:
            assert f in aa[ident] and f in bb[ident], (key, f)
            if aa[ident][f] != bb[ident][f]:
                counts[f] += 1
                changed.add(ident)
    return {'rows': len(aa), 'changedRows': len(changed), 'fieldChanges': dict(counts)}


def compare(a, b):
    assert a['Now'] != b['Now']
    assert a['ElapsedSecondsUnits'] < b['ElapsedSecondsUnits']
    for field in ('Buildings', 'Citizens', 'Families'):
        assert len(a[field]) == len(b[field])
    ap = a['PollutionCells']; bp = b['PollutionCells']
    assert list(ap) == list(bp), 'pollution layout differs; cannot compare by slot'
    changes = sum(ap[k] != bp[k] for k in ap)
    home_a = {x['Id']: x for x in a['Buildings'] if x['Kind'] == 'House' and x['Completed'] and not x['Removed']}
    home_b = {x['Id']: x for x in b['Buildings'] if x['Kind'] == 'House' and x['Completed'] and not x['Removed']}
    assert home_a.keys() == home_b.keys()
    home_fields = ('WaterRatio', 'ElectricityRatio', 'WaterDemand', 'ElectricityDemand')
    return {
        'start': a['Now'], 'end': b['Now'],
        'steps2s': (b['ElapsedSecondsUnits'] - a['ElapsedSecondsUnits']) // 2_000_000,
        'buildingLayout': rows(a, b, 'Buildings', BUILDING_LAYOUT),
        'buildingService': rows(a, b, 'Buildings', BUILDING_SERVICE),
        'homeService': rows({'Buildings': list(home_a.values())},
                            {'Buildings': list(home_b.values())}, 'Buildings', home_fields),
        'citizenIdentity': rows(a, b, 'Citizens', CITIZEN_LAYOUT),
        'citizenPhysical': rows(a, b, 'Citizens', CITIZEN_PHYSICAL),
        'citizenNeeds': rows(a, b, 'Citizens', CITIZEN_NEEDS),
        'familyLayout': rows(a, b, 'Families', FAMILY_LAYOUT),
        'pollutionCells': {'rows': len(ap), 'changedRows': changes},
    }


def main(paths):
    assert len(paths) == 3, 'usage: script 465.zip 445.zip 476.zip'
    z465, z445, z476 = [read_zip(p, k) for p, k in zip(paths, ('465', '445', '476'))]
    for prefix in ('A-pre', 'A-mid', 'A-post'):
        for checkpoint in (0, 1):
            assert_parity(z465, [(f'{prefix}/checkpoint-{checkpoint}.json',
                                  f'B-{prefix[2:]}/checkpoint-{checkpoint}.json')])
    for checkpoint in (0, 1):
        assert_parity(z445, [(f'pr57-445/A/checkpoint-{checkpoint}.json',
                              f'pr57-445/B/checkpoint-{checkpoint}.json')])
        for name in ('worksREAL68', 'midnightREAL121'):
            assert_parity(z476, [(f'{name}/A/checkpoint-{checkpoint}.json',
                                  f'{name}/B/checkpoint-{checkpoint}.json')])
    # Cross-artifact continuity and independent reproduction of the same saves.
    assert z465.read('A-mid/checkpoint-1.json') == z465.read('A-post/checkpoint-0.json')
    assert z465.read('A-mid/checkpoint-0.json') == z476.read('midnightREAL121/A/checkpoint-0.json')
    assert z465.read('A-post/checkpoint-1.json') == z476.read('midnightREAL121/A/checkpoint-1.json')
    assert z445.read('pr57-445/A/checkpoint-0.json') == z476.read('worksREAL68/A/checkpoint-0.json')
    read = lambda z, name: json.loads(z.read(name))
    intervals = [
        ('REAL121-midnight-2s', z465, 'A-mid/checkpoint-0.json', 'A-mid/checkpoint-1.json'),
        ('REAL121-after-midnight-2s', z465, 'A-post/checkpoint-0.json', 'A-post/checkpoint-1.json'),
        ('REAL68works-20steps', z476, 'worksREAL68/A/checkpoint-0.json', 'worksREAL68/A/checkpoint-1.json'),
        ('REAL68works-100steps', z445, 'pr57-445/A/checkpoint-0.json', 'pr57-445/A/checkpoint-1.json'),
    ]
    output = {}
    for label, z, a, b in intervals:
        output[label] = compare(read(z, a), read(z, b))
    # Stronger than endpoint-only: corroborate that the phase-internal probe
    # actually recorded building writes at infrastructure and citizen writes at
    # needs, and no net business writes in the 20-step worksite sample.
    for case in ('worksREAL68', 'midnightREAL121'):
        report = read(z476, f'{case}/B/phase-rows.json')
        lines = report['netFieldRows']
        assert len(lines) == 13
        for phase in ('infrastructure', 'businesses', 'needs'):
            assert sum(f'name={phase} ' in line for line in lines) == 1
        if case == 'worksREAL68':
            assert 'netEntityChanges=Vehicles:0,Citizens:0,Projects:0,Buildings:684,' in next(
                x for x in lines if 'name=infrastructure ' in x)
            assert 'changed=0 unchanged=20' in next(x for x in lines if 'name=businesses ' in x)
            assert 'Citizens:940' in next(x for x in lines if 'name=needs ' in x)
        else:
            assert 'Buildings:192' in next(x for x in lines if 'name=infrastructure ' in x)
            assert 'Citizens:18' in next(x for x in lines if 'name=businesses ' in x)
            assert 'Citizens:242' in next(x for x in lines if 'name=needs ' in x)
    # Explicitly forbid claiming an end-to-end optimization or intraphase parity.
    result = {'experiment': 491, 'observationOnly': True, 'gainAccepted': False,
              'intraphaseWriterEvidence': 'PR57 #476; only net field effects',
              'intervals': output}
    print(json.dumps(result, sort_keys=True, ensure_ascii=False, indent=2))
    print('PR57_491_AUTHENTIC_PROJECTION_CHURN_PASS')


if __name__ == '__main__':
    main(sys.argv[1:])
