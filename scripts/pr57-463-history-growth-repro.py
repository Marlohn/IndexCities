#!/usr/bin/env python3
"""PR57 #463: conditional, source-anchored 80-year history/save growth bound.

This is NOT a city simulation or throughput benchmark. It assumes sustained
positive accrued wages for N workers, one DailyFinance settlement per day.
It counts only the guaranteed PersonalEvent written by SettleWages; it does
not count other personal events, ledger transfers, city events, or generations.
"""
import argparse
import json
from pathlib import Path

ANCHORS = {
    'src/IndexCities.Simulation/CitySimulation.cs': [
        'while(State.Now>=State.NextDaily){DailyFinance();State.NextDaily=State.NextDaily.AddDays(1);}',
        'c.History.Add(new(State.Now,description));c.Decision=description;',
    ],
    'src/IndexCities.Simulation/Finance.cs': [
        'void DailyFinance()',
        'State.Citizens.Where(c=>c.Alive&&c.WagesAccrued>0)',
        'SettleWages(c,b);c.WorkSecondsToday=0;',
        'Personal(c,"Salário pago "+paid+"; devido "+due);',
    ],
    'src/IndexCities.Simulation/Model.cs': [
        'public List<PersonalEvent> History {get;set;}=[];',
        'public sealed record PersonalEvent(DateTime At,string Description);',
    ],
    'src/IndexCities.Simulation/Persistence.cs': [
        'return JsonSerializer.Serialize(State,SaveOptions);',
    ],
}

def verify_sources(root):
    for rel, anchors in ANCHORS.items():
        path = root / rel
        source = path.read_text(encoding='utf-8')
        for anchor in anchors:
            assert source.count(anchor) == 1, (rel, anchor, source.count(anchor))


def calculate(workers, years=80):
    assert workers >= 0 and years > 0
    days = 365 * years  # conservative: excludes leap days
    events = workers * days
    # Minimum serialized event with a 19-character ISO timestamp and empty
    # description; real salary descriptions are nonempty and generally longer.
    exemplar = json.dumps({'At':'2000-01-01T00:00:00', 'Description':''}, separators=(',',':'))
    min_json_bytes = events * (len(exemplar.encode('utf-8')) + 1) - (1 if events else 0)
    # Reference-slot bound only, conditional on 64-bit managed references.
    # Not an estimate of object/string overhead or list capacities.
    ref_slots = events * 8
    return dict(workers=workers, years=years, daysLowerBound=days,
                guaranteedSalaryPersonalEvents=events,
                minimumEventJsonBytes=min_json_bytes,
                minimumEventJsonGiB=round(min_json_bytes/1024**3,3),
                referenceSlotBytes64bit=ref_slots,
                referenceSlotGiB64bit=round(ref_slots/1024**3,3),
                eventsPerRealSecondAtSixHours=round(events/21600,1))

def main():
    p=argparse.ArgumentParser()
    p.add_argument('--repo-root',type=Path,help='Verify exact source anchors in a checkout')
    args=p.parse_args()
    if args.repo_root:
        verify_sources(args.repo_root)
    assert calculate(1,1)['guaranteedSalaryPersonalEvents']==365
    assert calculate(10000)['guaranteedSalaryPersonalEvents']==292_000_000
    print(json.dumps({'experiment':463,'sourceAnchorsChecked':bool(args.repo_root),
        'minimalEventJson':'{"At":"2000-01-01T00:00:00","Description":""}',
        'cases':[calculate(n) for n in (1000,5000,10000)],
        'limitations':'Conditional on sustained positive wages and continued existence; not a real city, no births, no CPU benchmark, no ledger or other history counted; byte bounds exclude JSON outer structure and nonempty text.'},sort_keys=True))
    print('PR57_463_HISTORY_GROWTH_REPRO_PASS')

if __name__=='__main__':main()
