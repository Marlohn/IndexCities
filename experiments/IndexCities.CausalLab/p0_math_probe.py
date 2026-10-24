#!/usr/bin/env python3
"""Sonda matemática independente, não runtime C# ou prova de equivalência bit a bit.

Equações baseadas em FixedPoint.cs, Pollution.cs, Infrastructure.cs e Economy.cs.
Ver README: resultado serve para REJEITAR atalhos ingênuos, não validar motor novo.
"""
import json
import math
from decimal import Decimal, ROUND_HALF_UP, localcontext

RESOURCE = Decimal('1000000000')
POLLUTION_QUANTUM = Decimal('0.00000000000000000001')

def quantized_units(delta: Decimal, scale: Decimal) -> int:
    return int((delta * scale).to_integral_value(rounding=ROUND_HALF_UP))

def production(rate: Decimal, steps: int):
    # Quantidade corrente: capacity * dt / 43200, com dt=2s.
    delta = rate * Decimal(2) / Decimal(43200)
    per_step_units = quantized_units(delta, RESOURCE)
    stepwise_units = steps * per_step_units
    naive_bulk_units = quantized_units(delta * steps, RESOURCE)
    return {'capacity': str(rate), 'steps': steps, 'per_step_units': per_step_units,
            'stepwise_units': stepwise_units, 'naive_bulk_units': naive_bulk_units,
            'equivalent': stepwise_units == naive_bulk_units}

def pollution(steps: int):
    x = Decimal('0.31415926535897932384')
    f = Decimal(str(math.exp(-0.15 * 2 / 86400)))
    for _ in range(steps):
        x = (x * f).quantize(POLLUTION_QUANTUM, rounding=ROUND_HALF_UP)
    naive = (Decimal('0.31415926535897932384') *
             Decimal(str(math.exp(-0.15 * (steps * 2) / 86400)))).quantize(
                POLLUTION_QUANTUM, rounding=ROUND_HALF_UP)
    return {'steps': steps, 'stepwise': str(x), 'naive_interval': str(naive),
            'equivalent': x == naive}

def wind(t: float, seed: int = 42):
    return min(.95, max(.15, .55 + .25 * math.sin(t / 21600 + seed) +
                          .15 * math.sin(t / 172800 + seed * .3)))

def wind_energy(steps: int):
    stepwise_units = sum(
        quantized_units(Decimal(str(100 * wind(2 * i) * 2 / 86400)), RESOURCE)
        for i in range(steps))
    hourly_units = sum(
        quantized_units(Decimal(str(100 * wind(3600 * j) * 3600 / 86400)), RESOURCE)
        for j in range(math.ceil(2 * steps / 3600)))
    return {'stepwise_units': stepwise_units, 'hourly_units': hourly_units,
            'equivalent': stepwise_units == hourly_units}

def hunger_threshold():
    hunger_s = 11 * 3600 + 59 * 60
    pantry = 1
    reference_event = None
    for elapsed in range(2, 122, 2):
        hunger_s += 2
        if pantry >= 1 and hunger_s >= 12 * 3600:
            pantry -= 1
            hunger_s -= 12 * 3600
            reference_event = elapsed
            break
    naive_coarse_event = 120
    return {'reference_consumption_at_s': reference_event,
            'coarse_consumption_at_s': naive_coarse_event,
            'equivalent': reference_event == naive_coarse_event}

def main():
    with localcontext() as ctx:
        ctx.prec = 40
        result = {
            'production_below_half_unit': production(Decimal('0.000010584'), 10),
            'production_above_half_unit': production(Decimal('0.000011016'), 10),
            'pollution_10min': pollution(300),
            'wind_energy_1day': wind_energy(43200),
            'hunger_crossing': hunger_threshold(),
        }
    assert all(not r['equivalent'] for r in result.values()), (
        'Expected 5 demonstrative counterexamples')
    print(json.dumps({
        'purpose': 'Cinco contraexemplos matemáticos, NÃO benchmark integrado',
        'csharp_executed': False,
        'results': result,
    }, ensure_ascii=False, indent=2))

if __name__ == '__main__':
    main()
