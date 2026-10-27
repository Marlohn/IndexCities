#!/usr/bin/env python3
"""PR57 #192: read-only signal/claim census of authentic GRID checkpoints.

Usage: python3 scripts/Pr57JunctionEndpointCensus.py pr57-causal-slice-191.zip
No simulation, mutation, fabricated entities or interpolation between checkpoints.
"""
import argparse
import json
import math
import zipfile


def census(data):
    signal = {r['Id']: r for r in data['Roads']
              if r['Control'] == 'Signal' and len(r['Neighbours']) >= 3}
    vehicles = data['Vehicles']
    approaches = []
    for vehicle in vehicles:
        if vehicle['Status'] != 'Travelling':
            continue
        route = vehicle['Route']
        index = vehicle['RouteIndex']
        if index >= len(route) or route[index] not in signal:
            continue
        node = signal[route[index]]
        distance = math.hypot(vehicle['Position']['X'] - node['Position']['X'],
                              vehicle['Position']['Z'] - node['Position']['Z'])
        if distance <= 15:
            approaches.append((vehicle['Id'], node['Id'], distance))
    claims = [v for v in vehicles if v.get('IntersectionClaim') in signal]
    manoeuvres = [v for v in vehicles if
                  any(node in signal for node in v.get('ManoeuvreSpaces', []))]
    return len(signal), len(approaches), len(claims), len(manoeuvres)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('artifact', help='original #191 or #192 workflow artifact ZIP')
    args = parser.parse_args()
    with zipfile.ZipFile(args.artifact) as archive:
        names = set(archive.namelist())
        for second in (0, 50, 100, 150, 200):
            path = f'control/checkpoint-{second}.json'
            if path not in names:
                raise SystemExit(f'missing {path}')
            state = json.loads(archive.read(path))
            signals, approaches, claims, manoeuvres = census(state)
            print(f'offset={second} signalNodes={signals} '
                  f'currentSignalApproachesWithin15m={approaches} '
                  f'signalClaims={claims} signalManoeuvreHolders={manoeuvres}')


if __name__ == '__main__':
    main()
