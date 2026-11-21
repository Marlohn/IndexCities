#!/usr/bin/env python3
"""PR57 #283: endpoint-only causal-surface census for authentic saved checkpoints.

Does not claim intra-Step independence: every public Advance(2) still executes.
Usage: python scripts/pr57-283-causal-surface.py checkpoint-0.json checkpoint-1.json
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path


def indexed(state, collection):
    return {item['Id']: item for item in state[collection]}


def changed_ids(before, after):
    return {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('before', type=Path)
    parser.add_argument('after', type=Path)
    args = parser.parse_args()
    start = json.loads(args.before.read_bytes())
    end = json.loads(args.after.read_bytes())
    projects_before = indexed(start, 'Projects')
    projects_after = indexed(end, 'Projects')
    active = {id for id, p in projects_before.items() if p['Status'] in ('Waiting', 'InProgress')}
    assert active <= projects_after.keys(), 'active project removed from end checkpoint'
    changed_fields = {id: sorted(k for k in projects_before[id].keys() | projects_after[id].keys()
                                 if projects_before[id].get(k) != projects_after[id].get(k))
                      for id in active}
    only_cause = {id for id, fields in changed_fields.items() if fields == ['Cause']}
    substantive = {id for id, fields in changed_fields.items() if any(k != 'Cause' for k in fields)}
    field_counts = collections.Counter(field for fields in changed_fields.values() for field in fields)
    transitions = collections.Counter((projects_before[id]['Cause'], projects_after[id]['Cause']) for id in active)

    shipments_before = indexed(start, 'Shipments')
    shipments_after = indexed(end, 'Shipments')
    shipment_changed = changed_ids(shipments_before, shipments_after)
    shipment_projects = set()
    for shipment_id in shipment_changed:
        for snapshot in (shipments_before, shipments_after):
            shipment = snapshot.get(shipment_id)
            if shipment is not None and shipment.get('ProjectId') is not None:
                shipment_projects.add(shipment['ProjectId'])

    vehicles_before = indexed(start, 'Vehicles')
    vehicles_after = indexed(end, 'Vehicles')
    vehicle_changed = changed_ids(vehicles_before, vehicles_after)
    vehicle_projects = set()
    for vehicle_id in vehicle_changed:
        for vehicle_snapshot in (vehicles_before, vehicles_after):
            vehicle = vehicle_snapshot.get(vehicle_id)
            if vehicle is None:
                continue
            for field in ('MachineProjectId', 'SiteProjectId'):
                if vehicle.get(field) is not None:
                    vehicle_projects.add(vehicle[field])
            shipment_id = vehicle.get('ShipmentId')
            if shipment_id is not None:
                for shipment_snapshot in (shipments_before, shipments_after):
                    shipment = shipment_snapshot.get(shipment_id)
                    if shipment is not None and shipment.get('ProjectId') is not None:
                        vehicle_projects.add(shipment['ProjectId'])
    for id in active:
        crew_id = projects_before[id].get('CrewVehicleId')
        if crew_id in vehicle_changed:
            vehicle_projects.add(id)

    citizens_before = indexed(start, 'Citizens')
    citizens_after = indexed(end, 'Citizens')
    citizen_changed = changed_ids(citizens_before, citizens_after)
    citizen_projects = set()
    for citizen_id in citizen_changed:
        for snapshot in (citizens_before, citizens_after):
            citizen = snapshot.get(citizen_id)
            if citizen is not None and citizen.get('ConstructionProjectId') is not None:
                citizen_projects.add(citizen['ConstructionProjectId'])

    buildings_before = indexed(start, 'Buildings')
    buildings_after = indexed(end, 'Buildings')
    building_changed = changed_ids(buildings_before, buildings_after)
    own_building_changed = {id for id in active if projects_before[id].get('BuildingId') in building_changed}

    print('PR57_283_INPUT', args.before, 'sha256', hashlib.sha256(args.before.read_bytes()).hexdigest())
    print('PR57_283_INPUT', args.after, 'sha256', hashlib.sha256(args.after.read_bytes()).hexdigest())
    print('PR57_283_PROJECTS', 'active', len(active), 'causeOnlyEndpoint', len(only_cause),
          'otherFieldEndpoint', len(substantive), 'fullyUnchangedEndpoint',
          sum(not fields for fields in changed_fields.values()))
    print('PR57_283_PROJECT_FIELDS', json.dumps(dict(sorted(field_counts.items())), ensure_ascii=False, sort_keys=True))
    print('PR57_283_CAUSE_TRANSITIONS', json.dumps([{'before': b, 'after': a, 'count': n}
          for (b, a), n in transitions.most_common()], ensure_ascii=False))
    print('PR57_283_LINKED_ENDPOINTS', 'shipmentProjectIds', len(shipment_projects & active),
          'vehicleProjectIds', len(vehicle_projects & active),
          'citizenProjectIds', len(citizen_projects & active),
          'ownBuildingProjectIds', len(own_building_changed))
    touched = shipment_projects | vehicle_projects | citizen_projects | own_building_changed
    print('PR57_283_CAUSE_ONLY_WITHOUT_DIRECT_LINKED_ENDPOINT', len(only_cause - touched),
          'of', len(only_cause), 'ids', ','.join(map(str, sorted(only_cause - touched))))
    print('PR57_283_GLOBAL_ENDPOINT', 'changedRoads', len(changed_ids(indexed(start, 'Roads'), indexed(end, 'Roads'))),
          'changedVehicles', len(vehicle_changed), 'changedCitizens', len(citizen_changed),
          'changedBuildings', len(building_changed), 'changedShipments', len(shipment_changed),
          'ledgerCount', len(start['Ledger']), len(end['Ledger']))
    print('PR57_283_LIMIT', 'Endpoint-only; no intermediate mutations, external edits, temporal predicates, or full causal closure; not a skip/caching certificate')


if __name__ == '__main__':
    main()
