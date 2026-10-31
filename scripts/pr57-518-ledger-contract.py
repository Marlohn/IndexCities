#!/usr/bin/env python3
"""PR57 #518: offline ledger index parity on authentic REAL midnight ABBA saves.
Usage: python3 scripts/pr57-518-ledger-contract.py pr57-478-original.zip
No .NET/UI/Step performance claim; no engine or save changes.
"""
from collections import OrderedDict
from decimal import Decimal
from hashlib import sha256
import json
import sys
from pathlib import Path
from zipfile import ZipFile

ZIP_SHA = '7130344249d2958244dd08906eea5cfc74bb0585cd14a772a5e4c9ce9aff6902'
ARMS = ('A1', 'B1', 'B2', 'A2')


def municipal(e):
    return e['From'] == 'city' or e['To'] == 'city'


def add(totals, first, entries):
    for e in entries:
        if not municipal(e):
            continue
        first = e['At'] if first is None else min(first, e['At'])
        pair = totals.setdefault(e['Category'], [Decimal(0), Decimal(0)])
        if e['To'] == 'city':
            pair[0] += e['Amount']
        if e['From'] == 'city':
            pair[1] += e['Amount']
    return first


def full(ledger):
    totals = OrderedDict()
    first = add(totals, None, ledger)
    return first, totals, ledger[-20:][::-1]


def delta(old, new):
    assert new[:len(old)] == old, 'ledger prefix changed'
    tail = new[len(old):]
    first, totals, _ = full(old)
    first = add(totals, first, tail)
    return (first, totals, (old[-20:] + tail)[-20:][::-1]), len(tail)


def main(path):
    assert sha256(Path(path).read_bytes()).hexdigest() == ZIP_SHA
    with ZipFile(path) as z:
        assert z.testzip() is None and len(z.namelist()) == 13
        summary = json.loads(z.read('summary.json'))
        assert summary['experiment'] == 478 and summary['allAdvance2']
        assert summary['cases']['midnightREAL121']['steps'] == 1800
        assert summary['cases']['midnightREAL121']['fullJsonParity']
        shas = [set(), set()]
        rows = []
        for arm in ARMS:
            snapshots = []
            for i in (0, 1):
                raw = z.read(f'midnightREAL121/{arm}/checkpoint-{i}.json')
                shas[i].add(sha256(raw).hexdigest())
                snapshots.append(json.loads(raw, parse_float=Decimal))
            assert 'INVARIANTS OK' in z.read(f'midnightREAL121/{arm}/full.log').decode()
            before, after = snapshots
            assert len(after['Citizens']) == 121
            assert (before['Now'], after['Now']) == ('2026-01-07T23:59:58', '2026-01-08T00:59:58')
            expected = full(after['Ledger'])
            actual, tail_count = delta(before['Ledger'], after['Ledger'])
            assert expected == actual, 'finance UI grouping/order/amounts/tail diverged'
            assert (len(before['Ledger']), len(after['Ledger']), tail_count) == (4085, 4346, 261)
            rows.append((len(expected[1]), sum(map(municipal, before['Ledger'])),
                         sum(map(municipal, after['Ledger'][len(before['Ledger']):])), expected[0]))
        assert len(shas[0]) == len(shas[1]) == 1 and len(set(rows)) == 1
        print(json.dumps({'case': 'REAL midnight 121 living, 98 vehicles',
                          'ABBA_full_save_parity': True, 'Advance2_per_arm': 1800,
                          'ledger_entries_before_after': [4085, 4346],
                          'new_ledger_entries': 261, 'municipal_categories': rows[0][0],
                          'municipal_entries_before_new': list(rows[0][1:3]),
                          'first_municipal_at': rows[0][3],
                          'full_scan_entries': 4346, 'incremental_entries': 261,
                          'scan_work_ratio_not_speedup': round(4346 / 261, 3),
                          'group_order_decimal_sums_last20_exact': True,
                          'scope': 'offline only; not .NET, UI latency, Step, GC, save format or 80y'},
                         ensure_ascii=False, sort_keys=True))
    print('PR57_518_AUTHENTIC_LEDGER_QUERY_CONTRACT_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit('usage: pr57-518-ledger-contract.py pr57-478-original.zip')
    main(sys.argv[1])
