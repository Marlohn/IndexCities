#!/usr/bin/env python3
"""PR57 #488: falsify naive cross-Step incremental home-pollution dot products.

Uses authentic #465 A/B saves; no engine mutation, synthetic citizens, or CI.
The C# source's ordered CellFactor/SampledPollution/Satisfaction expressions
are reproduced in Python double precision. This is a bounded, offline
counterexample, NOT a universal .NET equivalence proof or speed measurement.
"""
import hashlib
import json
import math
import struct
import sys
import zipfile
from decimal import Decimal

EXPECTED_ZIP = '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1'
PAIRS = (
    ('A-mid/checkpoint-0.json', 'A-mid/checkpoint-1.json',
     'B-mid/checkpoint-0.json', 'B-mid/checkpoint-1.json'),
    ('A-post/checkpoint-0.json', 'A-post/checkpoint-1.json',
     'B-post/checkpoint-0.json', 'B-post/checkpoint-1.json'),
)


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def bits(value):
    return struct.pack('>d', value)


def signed32(value):
    return value - (1 << 32) if value >= (1 << 31) else value


def factor(point, key):
    raw = int(key) & ((1 << 64) - 1)
    x = signed32(raw >> 32)
    z = signed32(raw & 0xffffffff)
    dx = float(point['X']) - (x + .5) * 32
    dz = float(point['Z']) - (z + .5) * 32
    return math.exp(-math.sqrt(dx * dx + dz * dz) / 64)


def ordered_dot(cells, weights):
    value = 0.0
    for key, cell in cells.items():
        value += float(cell) * weights[key]
    return value


def clamp(value, lower, upper):
    return min(upper, max(lower, value))


def satisfaction(citizen, family, home, pollution):
    unmet = (1 - float(home['WaterRatio'])) * .5 + (1 - float(home['ElectricityRatio'])) * .15 + clamp(pollution, 0, 2) * .1
    hunger_hours = citizen['HungerMicroseconds'] / (3600.0 * 1e6)
    return clamp(80 - hunger_hours * .7 - unmet * 15 - (8 if len(family['Members']) > 4 else 0), 0, 100)


def audit_pair(z, names):
    a0, a1, b0, b1 = names
    assert z.read(a0) == z.read(b0) and z.read(a1) == z.read(b1)
    previous = json.loads(z.read(a0), parse_float=Decimal)
    current = json.loads(z.read(a1), parse_float=Decimal)
    assert len(previous['PollutionCells']) == len(current['PollutionCells']) == 90
    assert list(previous['PollutionCells']) == list(current['PollutionCells'])
    changed_cells = sum(previous['PollutionCells'][k] != current['PollutionCells'][k] for k in previous['PollutionCells'])
    assert changed_cells == 90
    homes = [building for building in current['Buildings'] if building['Kind'] == 'House']
    assert len(homes) == 80
    home_results = {}
    for home in homes:
        weights = {key: factor(home['Position'], key) for key in previous['PollutionCells']}
        before = ordered_dot(previous['PollutionCells'], weights)
        canonical = ordered_dot(current['PollutionCells'], weights)
        # Tempting O(changed-cells) shortcut: old dot plus changes, in cell order.
        # It preserves all cell values/factors but NOT the original floating
        # accumulation order of the current complete dot product.
        delta = before
        for key, old in previous['PollutionCells'].items():
            delta += (float(current['PollutionCells'][key]) - float(old)) * weights[key]
        home_results[home['Id']] = (canonical, delta)
    raw_mismatch = sum(bits(a) != bits(b) for a, b in home_results.values())
    clamped_mismatch = sum(bits(clamp(a, 0, 2)) != bits(clamp(b, 0, 2)) for a, b in home_results.values())
    families = {family['Id']: family for family in current['Families']}
    buildings = {building['Id']: building for building in current['Buildings']}
    reconstructed = 0
    hypothetical_changed = []
    for citizen in current['Citizens']:
        if not (citizen['Alive'] and citizen['Resident'] and citizen['InsideMap']):
            continue
        family = families[citizen['FamilyId']]
        home = buildings.get(family['HomeId'])
        if home is None:
            continue
        canonical, delta = home_results[home['Id']]
        actual = satisfaction(citizen, family, home, canonical)
        assert bits(actual) == bits(float(citizen['Satisfaction'])), (
            'canonical satisfaction reconstruction failed', citizen['Id'], actual, citizen['Satisfaction'])
        reconstructed += 1
        candidate = satisfaction(citizen, family, home, delta)
        if bits(candidate) != bits(actual):
            hypothetical_changed.append((citizen['Id'], home['Id'], actual, candidate))
    assert reconstructed == 121
    print('PR57_488_PAIR', 'from=' + previous['Now'], 'to=' + current['Now'],
          'cellsChanged=' + str(changed_cells), 'homes=' + str(len(homes)),
          'rawDotMismatches=' + str(raw_mismatch),
          'clampedPollutionMismatches=' + str(clamped_mismatch),
          'canonicalSatisfactionMatched=' + str(reconstructed),
          'deltaSatisfactionMismatches=' + str(len(hypothetical_changed)),
          'exampleIds=' + ','.join(str(row[0]) for row in hypothetical_changed[:6]))
    return hypothetical_changed


def main(path):
    assert sha256(open(path, 'rb').read()) == EXPECTED_ZIP
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        differences = [audit_pair(z, pair) for pair in PAIRS]
    assert differences[0], 'Expected real midnight counterexample absent'
    print('PR57_488_AUTHENTIC_POLLUTION_DELTA_COUNTEREXAMPLE_PASS '
          'source=Action465 scope=REAL_121_living_80_homes '
          'engineUnmodified=True acceptedGain=False')


if __name__ == '__main__':
    assert len(sys.argv) == 2, 'usage: pr57-488-authentic-pollution-delta.py pr57-465-original.zip'
    main(sys.argv[1])
