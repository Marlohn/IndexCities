#!/usr/bin/env python3
"""PR57 #460: falsify naive coalescing of public Advance(2) quanta.

This is a source-level arithmetic counterexample, NOT a benchmark or a
simulation replay. C# double multiplication, Math.Round(AwayFromZero), and
FixedPoint.ResourceScale are reproduced for positive values away from ties.
Run: python3 scripts/pr57-460-quantum-repro.py [repo-root]
"""
import json
import math
import sys
from pathlib import Path

SCALE = 1_000_000_000
QUANTUM = 2.0
DAY = 86400.0


def quantize_positive(value):
    scaled = value * SCALE
    assert math.isfinite(scaled) and abs(scaled - (math.floor(scaled) + 0.5)) > 1e-7
    return math.floor(scaled + 0.5)


def source_contract(root):
    checks = {
        'src/IndexCities.Simulation/FixedPoint.cs': [
            'public const long ResourceScale=1_000_000_000;',
            'Math.Round(value*scale,MidpointRounding.AwayFromZero)',
            'checked(units+Quantize(delta,scale))',
        ],
        'src/IndexCities.Simulation/Model.cs': [
            'AddElectricityAvailableToday(double delta)=>ElectricityAvailableTodayUnits=FixedPoint.Add(ElectricityAvailableTodayUnits,delta,FixedPoint.ResourceScale)',
        ],
        'src/IndexCities.Simulation/Infrastructure.cs': [
            'State.AddElectricityAvailableToday(State.ElectricityCapacity*dt/86400);',
        ],
        'src/IndexCities.Simulation/CitySimulation.cs': [
            'public double Quantum=>2;',
            'var dt=Math.Min(Quantum,remaining);Step(dt);remaining-=dt;',
        ],
    }
    for filename, tokens in checks.items():
        data = (root / filename).read_text()
        for token in tokens:
            assert token in data, (filename, token)


def main():
    if len(sys.argv) > 2:
        raise SystemExit('usage: script.py [repo-root]')
    if len(sys.argv) == 2:
        source_contract(Path(sys.argv[1]))
    capacity = 10.0  # Hypothetical constant capacity, not a real fixture.
    per_step = quantize_positive(capacity * QUANTUM / DAY)
    examples = []
    for steps in (2, 1800, 10800):
        serial = steps * per_step
        collapsed = quantize_positive(capacity * (QUANTUM * steps) / DAY)
        assert serial != collapsed
        examples.append({'advance2_calls': steps, 'serial_units': serial,
                         'single_collapsed_units': collapsed,
                         'difference_units': collapsed - serial})
    result = {
        'id': 'PR57_460_QUANTUM_REPRO_PASS',
        'scope': 'HYPOTHETICAL_CONSTANT_CAPACITY_ARITHMETIC_ONLY',
        'capacity': capacity, 'resource_scale': SCALE,
        'per_advance2_delta_units': per_step,
        'examples': examples,
        'conclusion': 'One larger dt is not byte-equivalent to serial quantized 2s updates.',
        'limitation': 'Does not forbid an exact repeated-quantum executor with full causal event handling; not a throughput measurement.'
    }
    print(json.dumps(result, sort_keys=True, indent=2))


if __name__ == '__main__':
    main()
