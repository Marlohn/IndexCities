#!/usr/bin/env python3
"""PR57 #286: source-order guard and authentic mobility phase-coupling endpoint census.

Usage: python scripts/pr57-286-phase-coupling.py pr57-259.zip [--source src/IndexCities.Simulation]
Endpoints do not expose intermediate mutations or establish a causal skip certificate.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

CASES = ('worksREAL', 'queueREAL', 'real14hREAL', 'gridARTIFICIAL')
CITIZEN_FIELDS = ('Position', 'VehicleId', 'CrossingNodeId', 'Path', 'PathIndex',
                  'BoardingVehicleId', 'NextDecisionAt')
VEHICLE_FIELDS = ('Position', 'Status', 'DriverId', 'Route', 'RouteIndex',
                  'IntersectionClaim')


def changed(before, after, field):
    old = {e['Id']: e for e in before}
    new = {e['Id']: e for e in after}
    return sum(old[i].get(field) != new[i].get(field) for i in old.keys() & new.keys())


def source_guard(directory):
    def read(name):
        return (directory / name).read_text(encoding='utf-8')
    step, mobility, cars = read('CitySimulation.cs'), read('Mobility.cs'), read('Cars.cs')
    gate = read('Mobility.CrossingGate259.Pr57.cs')
    assert step.index('MoveVehicles(dt);') < step.index('MoveCitizens(dt);')
    assert step.index('MoveCitizens(dt);') < step.index('UpdateInfrastructure(dt);')
    assert 'if(v.PassengerCar&&v.Status==VehicleStatus.Waiting){ReplanCar(v);continue;}' in mobility
    assert 'LeaveCar(v,"Carro parado: rota ou vaga indisponível")' in cars
    assert 'c.VehicleId=null;c.BoardingVehicleId=null;c.Position=v.Position' in cars
    assert 'CitizenArrived(c);' in mobility
    assert 'vehicle.Status=VehicleStatus.Travelling;' in mobility
    assert 'c.CrossingNodeId=crossing.Id;' in mobility
    assert 'c.CrossingNodeId=null;' in mobility
    assert 'c.VehicleId == null &&' in gate
    assert 'c.Moving && c.CrossingNodeId == junction.Id' in gate
    print('PR57_286_SOURCE_GUARD order=vehicles_then_citizens two_way_state_writes=true crossing_writer=citizens gate_reader=vehicles')


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('artifact', type=Path)
    ap.add_argument('--source', type=Path)
    args = ap.parse_args()
    if args.source:
        source_guard(args.source)
    with zipfile.ZipFile(args.artifact) as z:
        for case in CASES:
            prefix = case + '-control/'
            raw = [z.read(prefix + f'checkpoint-{n}.json') for n in (0, 1)]
            before, after = [json.loads(data) for data in raw]
            for n in (0, 1):
                assert raw[n] == z.read(case + f'-probe/checkpoint-{n}.json'), (case, n)
            citizen = {f: changed(before['Citizens'], after['Citizens'], f) for f in CITIZEN_FIELDS}
            vehicle = {f: changed(before['Vehicles'], after['Vehicles'], f) for f in VEHICLE_FIELDS}
            print('PR57_286_CASE', case, 'aliveStart',
                  sum(c['Alive'] for c in before['Citizens']),
                  'vehiclesStart', len(before['Vehicles']),
                  'citizenEndpointDeltas', json.dumps(citizen, sort_keys=True, separators=(',', ':')),
                  'vehicleEndpointDeltas', json.dumps(vehicle, sort_keys=True, separators=(',', ':')),
                  'sha256', *(hashlib.sha256(data).hexdigest() for data in raw),
                  'controlProbeParity=true')
    print('PR57_286_LIMIT endpoint_only=true intermediate_writes_unknown=true '
          'no_phase_attribution=true no_throughput_ab=true no_causal_skip=true')


if __name__ == '__main__':
    main()
