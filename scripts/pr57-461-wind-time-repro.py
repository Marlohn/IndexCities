#!/usr/bin/env python3
"""PR57 #461: time is an input to infrastructure even with no external edits.

Synthetic arithmetic counterexample, NOT a city fixture, C# replay or benchmark.
The optional repository path checks exact source anchors from PR57 HEAD.
Usage: python3 pr57-461-wind-time-repro.py [repo-root]
"""
import json
import math
import sys
from pathlib import Path

SCALE = 1_000_000_000
STEPS = 10_800
DT = 2.0
START = 14 * 3600
SEED = 42
CAPACITY = 1200.0  # One hypothetical staffed, maintained, accessible wind plant.


def check_source(root):
    required = {
        'src/IndexCities.Simulation/CitySimulation.cs': (
            'public double Quantum=>2;',
            'var dt=Math.Min(Quantum,remaining);Step(dt);remaining-=dt;',
            'UpdateInfrastructure(dt);',
            'State.AddElapsedSeconds(dt);State.Now=State.Now.AddSeconds(dt);',
        ),
        'src/IndexCities.Simulation/Infrastructure.cs': (
            'State.WindFactor=Math.Clamp(.55+.25*Math.Sin(State.ElapsedSeconds/21600+State.Seed)+.15*Math.Sin(State.ElapsedSeconds/172800+State.Seed*.3),.15,.95);',
            'b.OperationalCapacity=b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*State.WindFactor;',
            'State.ElectricityCapacity=wind.Sum(b=>b.OperationalCapacity);State.AddElectricityAvailableToday(State.ElectricityCapacity*dt/86400);',
        ),
        'src/IndexCities.Simulation/FixedPoint.cs': (
            'public const long ResourceScale=1_000_000_000;',
            'Math.Round(value*scale,MidpointRounding.AwayFromZero)',
            'checked(units+Quantize(delta,scale))',
        ),
        'src/IndexCities.Simulation/Model.cs': (
            'AddElectricityAvailableToday(double delta)=>ElectricityAvailableTodayUnits=FixedPoint.Add(ElectricityAvailableTodayUnits,delta,FixedPoint.ResourceScale)',
        ),
    }
    for name, tokens in required.items():
        source = (root / name).read_text(encoding='utf-8')
        for token in tokens:
            assert source.count(token) == 1, (name, token, source.count(token))


def wind_factor(t):
    return max(.15, min(.95, .55 + .25 * math.sin(t / 21600 + SEED)
                          + .15 * math.sin(t / 172800 + SEED * .3)))


def quantize_positive(value):
    scaled = value * SCALE
    assert math.isfinite(scaled) and scaled >= 0
    # Values in this example are far from a midpoint; Python/C# ties differ.
    assert abs(scaled - (math.floor(scaled) + .5)) > 1e-6
    return math.floor(scaled + .5)


def main():
    if len(sys.argv) > 2:
        raise SystemExit('usage: pr57-461-wind-time-repro.py [repo-root]')
    if len(sys.argv) == 2:
        check_source(Path(sys.argv[1]))
    deltas = [quantize_positive(CAPACITY * wind_factor(START + DT * i) * DT / 86400)
              for i in range(STEPS)]
    adjacent_equal = sum(a == b for a, b in zip(deltas, deltas[1:]))
    gaps = [abs(b - a) for a, b in zip(deltas, deltas[1:])]
    assert adjacent_equal == 0 and len(set(deltas)) == STEPS
    assert min(gaps) >= 1
    serial = sum(deltas)
    stale = STEPS * deltas[0]
    assert serial != stale
    print(json.dumps({
        'id': 'PR57_461_WIND_TIME_REPRO_PASS',
        'classification': 'SYNTHETIC_SINGLE_WIND_SOURCE_ARITHMETIC_NOT_CITY',
        'seed': SEED, 'elapsed_start_seconds': START, 'advance2_calls': STEPS,
        'capacity_nominal': CAPACITY, 'staff_factor': 1, 'maintenance_ratio': 1,
        'adjacent_equal_quantized_deltas': adjacent_equal,
        'distinct_quantized_deltas': len(set(deltas)),
        'minimum_adjacent_gap_nanos': min(gaps),
        'first_delta_nanos': deltas[0], 'last_delta_nanos': deltas[-1],
        'serial_electricity_available_nanos': serial,
        'incorrect_frozen_first_delta_nanos': stale,
        'difference_nanos': serial - stale,
        'scope': 'Time-driven invalidation is mandatory even with fixed building, staff and road inputs.',
        'limitations': 'Python libm not a .NET bitwise parity test; no economy, buildings, pollution, saves or throughput measured.'
    }, sort_keys=True, indent=2))


if __name__ == '__main__':
    main()
