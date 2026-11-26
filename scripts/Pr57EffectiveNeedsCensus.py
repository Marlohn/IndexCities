#!/usr/bin/env python3
"""PR57 #189: observational endpoint census of effective needs inputs.

Usage: python3 scripts/Pr57EffectiveNeedsCensus.py pr57-retained-184.zip

Reads the authentic plain checkpoints from Actions #40451480020. Reconstructs
LocalPollution's positive, insertion-ordered cell sum and clamp for home points,
then compares the effective water/electricity/pollution terms of UpdateNeeds.
The Python floating-point result is diagnostic, NOT a C# bit-exact oracle.
Saturation uses a strict 1e-8 margin to avoid classifying roundoff at 2.0.
Endpoints never prove stability between checkpoints or per-Step eligibility.
"""
import argparse
import json
import math
import zipfile
from decimal import Decimal

SCENARIOS = ('real12h', 'real72h', 'queueREAL', 'gridARTIFICIAL')
MARGIN = 1e-8


def centre(key):
    key = int(key)
    x = key >> 32
    z = key & 0xffffffff
    if z >= 1 << 31:
        z -= 1 << 32
    return ((x + .5) * 32, (z + .5) * 32)


def home_inputs(state):
    families = {f['Id']: f for f in state['Families']}
    buildings = {b['Id']: b for b in state['Buildings']}
    eligible = [c for c in state['Citizens'] if c['Alive'] and c['Resident']
                and c['InsideMap'] and c['FamilyId'] in families]
    home_ids = {families[c['FamilyId']]['HomeId'] for c in eligible}
    home_ids &= buildings.keys()
    cells = [(centre(key), float(value)) for key, value in state['PollutionCells'].items()]
    nonnegative = all(value >= 0 for _, value in cells)
    rows = {}
    for hid in sorted(home_ids):
        b = buildings[hid]
        x, z = float(b['Position']['X']), float(b['Position']['Z'])
        load = 0.0
        first_saturation = None
        for index, ((cx, cz), value) in enumerate(cells, 1):
            load += value * math.exp(-math.sqrt((x-cx)**2 + (z-cz)**2) / 64)
            if first_saturation is None and load > 2 + MARGIN:
                first_saturation = index
        saturated = load > 2 + MARGIN
        rows[hid] = {
            'water': b['WaterRatio'], 'electricity': b['ElectricityRatio'],
            'saturated': saturated,
            'pollution': min(2., max(0., load)),
            'raw_load': load,
            'first_saturation': first_saturation if nonnegative else None,
        }
    return rows, len(cells), nonnegative, len(eligible)


def analyze(archive, scenario):
    previous = None
    records = []
    for i in range(5):
        state = json.loads(archive.read(f'{scenario}/plain/checkpoint-{i}.json'), parse_float=Decimal)
        homes, cells, nonnegative, citizens = home_inputs(state)
        saturated = sum(row['saturated'] for row in homes.values())
        actual_terms = sum((row['first_saturation'] or cells) for row in homes.values())
        possible_terms = len(homes) * cells
        raw_sat = [row['raw_load'] for row in homes.values() if row['saturated']]
        rec = dict(scenario=scenario, checkpoint=i, eligible_citizens=citizens,
                   homes=len(homes), cells=cells, nonnegative=nonnegative,
                   saturated=saturated, full_terms=possible_terms,
                   early_terms=actual_terms,
                   early_reduction_percent=round(100 * (1 - actual_terms / possible_terms), 2) if possible_terms else 0,
                   min_saturated_margin=round(min(raw_sat)-2, 6) if raw_sat else None)
        if previous is not None:
            common = set(previous) & set(homes)
            rec['common_homes'] = len(common)
            rec['both_saturated'] = sum(previous[h]['saturated'] and homes[h]['saturated'] for h in common)
            rec['unchanged_water_electricity'] = sum(previous[h]['water'] == homes[h]['water'] and previous[h]['electricity'] == homes[h]['electricity'] for h in common)
            rec['endpoint_effective_input_stable_saturation'] = sum(
                previous[h]['saturated'] and homes[h]['saturated']
                and previous[h]['water'] == homes[h]['water']
                and previous[h]['electricity'] == homes[h]['electricity']
                for h in common)
        records.append(rec)
        previous = homes
    return records


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('artifact_zip')
    args = p.parse_args()
    with zipfile.ZipFile(args.artifact_zip) as archive:
        for scenario in SCENARIOS:
            for rec in analyze(archive, scenario):
                print(json.dumps(rec, sort_keys=True, separators=(',', ':')))


if __name__ == '__main__':
    main()
