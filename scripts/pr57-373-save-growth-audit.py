#!/usr/bin/env python3
"""PR57 #373: byte-exact JSON persistence growth, original #259 and #372 ZIPs.

Usage: python3 pr57-373-save-growth-audit.py <artifact-259.zip> <artifact-372.zip>
Read-only offline audit. A six-hour window is not an 80-year extrapolation.
"""
import hashlib
import json
import sys
import zipfile


def load(z, name):
    raw = z.read(name)
    text = raw.decode('utf-8')
    state = json.loads(text)
    decoder = json.JSONDecoder()

    def field_bytes(field):
        token = '"' + field + '":'
        start = text.index(token) + len(token)
        _, end = decoder.raw_decode(text, start)
        return len(text[start:end].encode('utf-8'))

    histories, pos = [], 0
    while True:
        found = text.find('"History":', pos)
        if found < 0:
            break
        start = found + len('"History":')
        _, end = decoder.raw_decode(text, start)
        histories.append(len(text[start:end].encode('utf-8')))
        pos = end
    assert len(histories) == len(state['Citizens'])
    return state, dict(save=len(raw), history=sum(histories), ledger=field_bytes('Ledger'),
                       roads=field_bytes('Roads'), citizens=field_bytes('Citizens'),
                       vehicles=field_bytes('Vehicles'), personal_events=sum(len(c['History']) for c in state['Citizens']),
                       transfers=len(state['Ledger']), city_events=len(state['CityEvents']),
                       live=sum(c['Alive'] for c in state['Citizens']))


def compare(label, a, b):
    before, x = a
    after, y = b
    old = {c['Id']: c for c in before['Citizens']}
    new = {c['Id']: c for c in after['Citizens']}
    assert old.keys() == new.keys(), 'citizen identity changed; report requires separate interpretation'
    assert all(new[k]['History'][:len(c['History'])] == c['History'] for k, c in old.items())
    assert after['Ledger'][:len(before['Ledger'])] == before['Ledger']
    assert after['CityEvents'][:len(before['CityEvents'])] == before['CityEvents']
    print(label, 'from', before['Now'], 'to', after['Now'])
    for key in ('save','history','ledger','roads','citizens','vehicles','personal_events','transfers','city_events','live'):
        print(' ', key, x[key], y[key], 'delta', y[key]-x[key])
    print(' history+ledger_share_final_pct', round(100*(y['history']+y['ledger'])/y['save'], 3))
    print(' history+ledger_share_save_growth_pct', round(100*((y['history']-x['history'])+(y['ledger']-x['ledger']))/(y['save']-x['save']), 3))


def main(initial_zip, final_zip):
    with zipfile.ZipFile(initial_zip) as z0, zipfile.ZipFile(final_zip) as z1:
        assert z0.testzip() is None and z1.testzip() is None, 'CRC mismatch'
        saves = [z1.read(f'{arm}/checkpoint-10800.json') for arm in ('baseA','publishA','publishB','baseB')]
        assert all(s == saves[0] for s in saves), 'ABBA checkpoint divergence'
        print('final_checkpoint_sha256', hashlib.sha256(saves[0]).hexdigest())
        compare('REAL_6H', load(z0, 'real14hREAL-control/checkpoint-0.json'),
                load(z1, 'baseA/checkpoint-10800.json'))
        compare('GRID_ARTIFICIAL_100S', load(z0, 'gridARTIFICIAL-control/checkpoint-0.json'),
                load(z0, 'gridARTIFICIAL-control/checkpoint-1.json'))


if __name__ == '__main__':
    if len(sys.argv) != 3:
        raise SystemExit(__doc__)
    main(*sys.argv[1:])
