#!/usr/bin/env python3
"""PR57 #513: authentic project/fleet dependency, NOT an integrated replay.

Usage: python3 scripts/pr57-513-authentic-fleet-guard.py pr57-493-original.zip
No state is written to the save; the one-vehicle perturbation is a synthetic
predicate counterexample and is never presented as an executed game transition.
"""
import collections
import hashlib
import json
import sys
import zipfile
from pathlib import Path

EXPECTED_ZIP_SHA256 = 'f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89'
RESOURCE_BODY = {'Aggregate': 'Dump', 'Asphalt': 'Dump', 'Concrete': 'Mixer',
                 'Steel': 'Flatbed', 'Wood': 'Flatbed'}
POINT_SCALE = 1_000_000_000


def eligible_external(v):
    # Exactly the six non-specialist trucks in this authentic fixture.  The
    # canonical general-purpose IsTow predicate is not reimplemented here.
    return (v['External'] and not v['PassengerCar'] and v['Machine'] == 'None'
            and v['HomeBuildingId'] is None and v['Id'] in (18, 19, 20, 21, 22, 23))


def available_truck(state, resource):
    # Matches Construction.Fleet.cs AvailableTruck for the fixture's six trucks.
    trucks = [v for v in state['Vehicles'] if eligible_external(v)]
    if sum(v['Status'] != 'Outside' for v in trucks) >= state['Tuning']['ExternalTruckCount']:
        return None
    return next((v['Id'] for v in trucks if v['Status'] == 'Outside'
                 and v['ServicePhase'] == 'None' and not v['BrokenDown']
                 and v['Body'] == RESOURCE_BODY[resource]), None)


def missing_resources(state, project):
    pending = collections.Counter()
    for shipment in state['Shipments']:
        if shipment['ProjectId'] == project['Id'] and not shipment['Complete']:
            for resource, quantity in shipment['Cargo'].items():
                pending[resource] += quantity / POINT_SCALE
    return [resource for resource, required in project['Required'].items()
            if required - project['Delivered'].get(resource, 0) / POINT_SCALE
            - pending[resource] > 0.001]


def main(path):
    raw = Path(path).read_bytes()
    digest = hashlib.sha256(raw).hexdigest()
    assert digest == EXPECTED_ZIP_SHA256, f'Unexpected artifact SHA-256: {digest}'
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None, 'ZIP CRC failure'
        # Full saves, not just summary fields, agree in independent ABBA arms.
        for checkpoint in (0, 1):
            originals = [z.read(f'worksREAL47/{arm}/checkpoint-{checkpoint}.json')
                         for arm in ('A1', 'B1', 'B2', 'A2')]
            assert all(data == originals[0] for data in originals[1:]), 'ABBA mismatch'
        a, b = (json.loads(z.read(f'worksREAL47/A1/checkpoint-{i}.json'))
                for i in (0, 1))
        logs = [z.read(f'worksREAL47/{arm}/full.log').decode()
                for arm in ('A1', 'B1', 'B2', 'A2')]
        assert all('STEP_PROFILE steps=100' in log for log in logs)
        assert all('PR57_AB_CHECKPOINT window=1' in log for log in logs)
        assert 'orderProjects=6800 progressProjects=6800 reserveProjects=34000' in logs[1]

    assert a['Tuning']['ExternalTruckCount'] == b['Tuning']['ExternalTruckCount'] == 6
    assert {v['Id'] for v in a['Vehicles'] if eligible_external(v)} == set(range(18, 24))
    assert {v['Id'] for v in b['Vehicles'] if eligible_external(v)} == set(range(18, 24))
    before = {p['Id']: p for p in a['Projects']}
    after = {p['Id']: p for p in b['Projects']}
    active = [p for p in before.values() if p['Status'] in ('Waiting', 'InProgress')]
    assert len(active) == 68
    only_cause = [pid for pid, p in before.items() if p['Status'] == 'Waiting'
                  and {key for key in p if p[key] != after[pid][key]} == {'Cause'}]
    assert len(only_cause) == 60
    assert all(after[pid]['Cause'] == 'Aguardando caminhão externo disponível'
               for pid in only_cause)
    assert all(set(missing_resources(b, after[pid])) == set(RESOURCE_BODY) - {'Asphalt'}
               for pid in only_cause)

    statuses = {}
    for label, state in (('initial', a), ('final', b)):
        statuses[label] = {
            'busy': sum(v['Status'] != 'Outside' for v in state['Vehicles']
                        if eligible_external(v)),
            'available': {resource: available_truck(state, resource)
                          for resource in ('Aggregate', 'Concrete', 'Steel', 'Wood')},
        }
    assert statuses['initial'] == {
        'busy': 0, 'available': {'Aggregate': 18, 'Concrete': 21, 'Steel': 22, 'Wood': 22}}
    assert statuses['final'] == {
        'busy': 5, 'available': {'Aggregate': None, 'Concrete': None,
                                'Steel': None, 'Wood': None}}

    # One counterfactual truck-status write; every project-local field stays
    # untouched.  This checks only AvailableTruck's guard, not dispatch success.
    flatbed = next(v for v in b['Vehicles'] if v['Id'] == 22)
    assert flatbed['Status'] == 'Parked'
    flatbed['Status'] = 'Outside'
    assert available_truck(b, 'Steel') == available_truck(b, 'Wood') == 22
    assert available_truck(b, 'Aggregate') is None
    flatbed['Status'] = 'Parked'
    assert available_truck(b, 'Steel') is None

    print(json.dumps({'artifact_sha256': digest, 'steps': 100,
                      'active_projects': len(active),
                      'projects_with_only_cause_changed': len(only_cause),
                      'shared_deficit_projects_by_body': {'Dump': 60, 'Mixer': 60, 'Flatbed': 60},
                      'truck_guard': statuses,
                      'synthetic_one_truck_flip': 'Flatbed 22 Parked -> Outside enables Steel/Wood guard only',
                      'scope': 'authentic endpoints + synthetic guard perturbation; no per-Step attribution or throughput'},
                     ensure_ascii=False, sort_keys=True))
    print('PR57_513_AUTHENTIC_FLEET_GUARD_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit('Usage: pr57-513-authentic-fleet-guard.py pr57-493-original.zip')
    main(sys.argv[1])
