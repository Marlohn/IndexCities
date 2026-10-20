from decimal import Decimal
import json
from pathlib import Path
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

root = Path(__file__).resolve().parent


def read(path):
    raw = path.read_bytes()
    return raw.decode('utf-16' if raw.startswith((b'\xff\xfe', b'\xfe\xff')) else 'utf-8-sig')


def final(version, scenario, seed):
    text = read(root / f'resume-{version}-{scenario}-{seed}.log')
    lines = text.splitlines()
    assert 'INVARIANTS OK' in lines
    line = [line for line in lines if line.startswith('DAY ')][-1]
    result = dict(re.findall(r'(\w+)=([^ ]+)', line))
    result['sha256'] = re.search(r'sha256=(\w+)', text)[1]
    result['stuck'] = [line for line in lines if line.startswith('STUCK SUMMARY ')]
    return result


def delta(a, b):
    return Decimal(b.replace(',', '.')) - Decimal(a.replace(',', '.'))


pairs = []
for scenario in ('cycle', 'grid'):
    for seed in (42, 7, 43):
        a, b = (final(version, scenario, seed) for version in ('before', 'after'))
        pairs.append(dict(scenario=scenario, seed=seed, before=a, after=b))

(root / 'resume-comparison.json').write_text(json.dumps(pairs, ensure_ascii=False, indent=2), encoding='utf-8')
rows = [
    '| Cenário | Seed | Mortes antes → depois | Refeições antes → depois | Salários antes → depois | Saúde média antes → depois | Satisfação média antes → depois | Circulação antes → depois |',
    '|---|---|---|---|---|---|---|---|',
]
for pair in pairs:
    a, b = pair['before'], pair['after']
    scenario, seed = pair['scenario'], pair['seed']
    cells = [f"{a[key]} → {b[key]}" for key in ('dead', 'meals', 'wages', 'health', 'satisfaction')]
    stopped = 'parados ≥30min (máx): 0 → 0' if scenario == 'grid' else 'em viagem no fim: 0 → 0'
    rows.append(f"| {scenario}, {'11' if scenario == 'grid' else '7'} dias | {seed} | " + ' | '.join(cells) + f' | {stopped} |')
rows += ['', '| Cenário | Seed | Eventos pessoais antes → depois | Variação | Lançamentos antes → depois | Δ salários | Δ satisfação |', '|---|---|---|---|---|---|---|']
for pair in pairs:
    a, b = pair['before'], pair['after']
    n = int(b['history']) - int(a['history'])
    pct = Decimal(n) * 100 / Decimal(a['history'])
    rows.append(f"| {pair['scenario']} | {pair['seed']} | {a['history']} → {b['history']} | {n:+d} ({pct:+.3f}%) | {a['ledger']} → {b['ledger']} | {delta(a['wages'], b['wages']):+.2f} | {delta(a['satisfaction'], b['satisfaction']):+.6f} |")
(root / 'resume-tables.md').write_text('\n'.join(rows) + '\n', encoding='utf-8')
print('\n'.join(rows))
