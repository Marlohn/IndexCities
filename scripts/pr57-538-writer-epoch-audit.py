#!/usr/bin/env python3
"""PR57 #538: fail-closed audit and disposable epoch-guard patch (no motor edits).

Run on a local checkout of PR #57: python3 scripts/pr57-538-writer-epoch-audit.py .
Optional --diff prints a candidate C# patch; it is NOT a validated optimization.
"""
import argparse
import difflib
from pathlib import Path
import re

EXPECTED = {('Mobility.cs', 's.ProjectId=null;'),
            ('Mobility.cs', 'shipment.ProjectId=project.Id;')}


def replace_once(text, old, new):
    assert text.count(old) == 1, f'anchor drift: {old!r} count={text.count(old)}'
    return text.replace(old, new, 1)


def candidate(construction, mobility):
    edits = [
        ('    bool _pr57ProgressShipmentLookupAnnounced;',
         '    bool _pr57ProgressShipmentLookupAnnounced;\n    long _pr57ShipmentProjectEpoch;'),
        ('        var indexedShipmentCount=State.Shipments.Count;\n'
         '        IEnumerable<Shipment> PendingProgressShipments(int projectId)\n'
         '            =>progressShipments!=null&&State.Shipments.Count==indexedShipmentCount',
         '        var indexedShipmentCount=State.Shipments.Count;\n'
         '        var indexedShipmentProjectEpoch=_pr57ShipmentProjectEpoch;\n'
         '        IEnumerable<Shipment> PendingProgressShipments(int projectId)\n'
         '            =>progressShipments!=null&&State.Shipments.Count==indexedShipmentCount\n'
         '                &&_pr57ShipmentProjectEpoch==indexedShipmentProjectEpoch'),
        ('        var indexedShipmentCount=0;\n'
         '        IEnumerable<Shipment> PendingProjectShipments(int projectId)',
         '        var indexedShipmentCount=0;\n'
         '        var indexedShipmentProjectEpoch=_pr57ShipmentProjectEpoch;\n'
         '        IEnumerable<Shipment> PendingProjectShipments(int projectId)'),
        ('            while(indexedShipmentCount<State.Shipments.Count)',
         '            if(indexedShipmentProjectEpoch!=_pr57ShipmentProjectEpoch)\n'
         '            {\n'
         '                foreach(var group in _pr57OrderShipments.Values)group.Clear();\n'
         '                indexedShipmentCount=0;\n'
         '                indexedShipmentProjectEpoch=_pr57ShipmentProjectEpoch;\n'
         '            }\n'
         '            while(indexedShipmentCount<State.Shipments.Count)'),
        ('?shipments.Where(s=>!s.Complete):Enumerable.Empty<Shipment>();',
         '?shipments.Where(s=>s.ProjectId==projectId&&!s.Complete):Enumerable.Empty<Shipment>();'),
    ]
    for old, new in edits:
        construction = replace_once(construction, old, new)
    for old, new in [
        ('s.ProjectId=null;s.Purpose=ShipmentPurpose.Maintenance;',
         's.ProjectId=null;_pr57ShipmentProjectEpoch++;s.Purpose=ShipmentPurpose.Maintenance;'),
        ('shipment.ProjectId=project.Id;shipment.Cancelled=false;',
         'shipment.ProjectId=project.Id;_pr57ShipmentProjectEpoch++;shipment.Cancelled=false;'),
    ]:
        mobility = replace_once(mobility, old, new)
    return construction, mobility


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('checkout', type=Path)
    parser.add_argument('--diff', action='store_true')
    args = parser.parse_args()
    base = args.checkout / 'src/IndexCities.Simulation'
    files = {p.name: p.read_text(encoding='utf-8-sig') for p in base.glob('*.cs')}
    assert 'Construction.cs' in files and 'Mobility.cs' in files, 'checkout incomplete'
    writes, appends, removals = [], [], []
    for name, content in files.items():
        for number, line in enumerate(content.splitlines(), 1):
            for match in re.finditer(r'\b(?:s|shipment)\.ProjectId\s*=(?!=)', line):
                value = re.match(r'(?:s|shipment)\.ProjectId\s*=\s*[^;]+;', line[match.start():])
                if value:
                    writes.append((name, re.sub(r'\s+', '', value.group())))
            if re.search(r'\bState\.Shipments\.Add\s*\(', line):
                appends.append((name, number))
            if re.search(r'\bState\.Shipments\.(?:Remove|RemoveAt|RemoveAll|Clear|Insert|AddRange|Sort|Reverse)\s*\(', line):
                removals.append((name, number))
    assert set(writes) == EXPECTED and len(writes) == 2, f'unknown ProjectId writers: {writes}'
    assert len(appends) == 7 and not removals, f'list mutation coverage failed: {appends}, {removals}'
    patched = candidate(files['Construction.cs'], files['Mobility.cs'])
    if args.diff:
        for name, after in zip(('Construction.cs', 'Mobility.cs'), patched):
            print(''.join(difflib.unified_diff(files[name].splitlines(True), after.splitlines(True),
                 fromfile='a/'+name, tofile='b/'+name)), end='')
    print(f'PR57_538_STATIC_WRITER_EPOCH_CANDIDATE_PASS files={len(files)} '
          f'projectWriters={len(writes)} appendSites={len(appends)} '
          'dotnetParity=NOT_TESTED')


if __name__ == '__main__':
    main()
