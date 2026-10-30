#!/usr/bin/env python3
"""PR57 #464: audit authentic 6h REAL save/history growth from original Actions ZIPs.

Usage: python3 pr57-464-authentic-history-delta.py pr57-259-original.zip pr57-439-original.zip
No simulation, extrapolation, or changes to engine. Exact JSON byte spans are measured.
"""
import collections
import datetime as dt
import hashlib
import json
import pathlib
import sys
import zipfile

SOURCES = (
    ('23472d3616d30a93addc97a4adb02d0e807c62ac1943fb814ded1528c550278b',
     'real14hREAL-control/checkpoint-0.json'),
    ('0e16ec8569d9ecc203c9a3ba02fc594f4a54097f0cde0f2264e021152dc4f90e',
     'base/checkpoint-10800.json'),
)

def read_original(path, expected_digest, member):
    payload = pathlib.Path(path).read_bytes()
    assert hashlib.sha256(payload).hexdigest() == expected_digest, 'ZIP digest differs from GitHub'
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None, 'ZIP CRC failure'
        return archive.read(member).decode('utf-8')

def raw_arrays(source, name):
    """Return decoded values and exact raw JSON spans without reserializing .NET JSON."""
    needle = '"' + name + '":'
    decoder = json.JSONDecoder()
    cursor = 0
    found = []
    while (pos := source.find(needle, cursor)) >= 0:
        start = pos + len(needle)
        value, end = decoder.raw_decode(source, start)
        assert isinstance(value, list)
        found.append((value, source[start:end]))
        cursor = end
    return found

def main(start_path, end_path):
    before = read_original(start_path, *SOURCES[0])
    after = read_original(end_path, *SOURCES[1])
    initial, final = json.loads(before), json.loads(after)
    assert initial['Now'] == '2026-01-07T14:00:00'
    assert final['Now'] == '2026-01-07T20:00:00'
    assert (dt.datetime.fromisoformat(final['Now']) - dt.datetime.fromisoformat(initial['Now'])).total_seconds() == 21600
    assert initial['NextDaily'] == final['NextDaily'] == '2026-01-08T00:00:00', 'daily settlement crossed'
    assert len(initial['Citizens']) == len(final['Citizens']) == 121
    assert sum(c['Alive'] for c in final['Citizens']) == 121
    assert len(initial['Vehicles']) == len(final['Vehicles']) == 98
    assert [c['Id'] for c in initial['Citizens']] == [c['Id'] for c in final['Citizens']]
    start_hist, end_hist = raw_arrays(before, 'History'), raw_arrays(after, 'History')
    assert len(start_hist) == len(end_hist) == 121
    new_events = []
    history_byte_growth = 0
    changed_citizens = 0
    for (old, old_raw), (new, new_raw) in zip(start_hist, end_hist):
        assert new[:len(old)] == old, 'History not append-only in this window'
        assert new_raw.startswith(old_raw[:-1]), 'raw History prefix changed'
        if len(new) > len(old):
            changed_citizens += 1
        new_events.extend(new[len(old):])
        history_byte_growth += len(new_raw) - len(old_raw)
    category_counts = {}
    for key in ('Ledger', 'CityEvents'):
        x, y = raw_arrays(before, key), raw_arrays(after, key)
        assert len(x) == len(y) == 1
        assert y[0][0][:len(x[0][0])] == x[0][0], f'{key} not append-only'
        assert y[0][1].startswith(x[0][1][:-1]), f'{key} raw prefix changed'
        category_counts[key] = {
            'before': len(x[0][0]), 'after': len(y[0][0]),
            'new': len(y[0][0]) - len(x[0][0]),
            'delta_raw_bytes': len(y[0][1]) - len(x[0][1]),
            'end_raw_bytes': len(y[0][1]),
        }
    save_growth = len(after.encode()) - len(before.encode())
    assert save_growth == 39406
    assert history_byte_growth == 29075
    assert len(new_events) == 356
    assert category_counts['Ledger']['new'] == 20
    assert category_counts['CityEvents']['new'] == 0
    assert hashlib.sha256(after.encode()).hexdigest() == 'e92293faf61a6ea2aab7dd0fdac83c14bd4668f611a126c701dc6705587bb9fa'
    result = {
        'scope': 'AUTHENTIC_REAL_121_LIVING_98_VEHICLES_6H_NO_DAILY_SETTLEMENT',
        'advance2': 10800,
        'from': initial['Now'], 'to': final['Now'],
        'save_bytes_before': len(before.encode()), 'save_bytes_after': len(after.encode()),
        'save_growth_bytes': save_growth,
        'history_entries_before': sum(len(x[0]) for x in start_hist),
        'history_entries_after': sum(len(x[0]) for x in end_hist),
        'history_new_entries': len(new_events),
        'history_changed_citizens': changed_citizens,
        'history_delta_raw_bytes': history_byte_growth,
        'history_end_raw_bytes': sum(len(x[1]) for x in end_hist),
        'history_fraction_of_net_save_growth_pct': round(100 * history_byte_growth / save_growth, 3),
        'history_plus_ledger_fraction_of_final_save_pct': round(100 * (sum(len(x[1]) for x in end_hist) + category_counts['Ledger']['end_raw_bytes']) / len(after.encode()), 3),
        'top_new_history_descriptions': collections.Counter(x['Description'] for x in new_events).most_common(8),
        'other_state_net_growth_bytes': save_growth - history_byte_growth - category_counts['Ledger']['delta_raw_bytes'] - category_counts['CityEvents']['delta_raw_bytes'],
        'collections': category_counts,
        'final_save_sha256': hashlib.sha256(after.encode()).hexdigest(),
        'parity_claim': 'original #439 base/shadow parity separately verified by its workflow; this script reads base',
        'extrapolation_to_80_years': False,
    }
    print(json.dumps(result, ensure_ascii=False, sort_keys=True, indent=2))
    print('PR57_464_AUTHENTIC_HISTORY_AUDIT_PASS')

if __name__ == '__main__':
    assert len(sys.argv) == 3, 'pass original ZIP #259 and ZIP #439'
    main(*sys.argv[1:])
