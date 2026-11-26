#!/usr/bin/env python3
"""PR57 #519: observational, offline chunk read/materialization cost on authentic saves.

Usage: python3 pr57-519-chunk-read-cost.py <pr57-478-original.zip> <pr57-259-original.zip>
Does not alter any save, product rule, engine, or public persistence format.
All timings are Python-only, not .NET/Advance(2) performance evidence.
"""
import hashlib
import json
import statistics
import sys
import time
import zipfile
import zlib

EXPECTED = {
    '478': '7130344249d2958244dd08906eea5cfc74bb0585cd14a772a5e4c9ce9aff6902',
    '259': '23472d3616d30a93addc97a4adb02d0e807c62ac1943fb814ded1528c550278b',
}
CASES = [
    ('478', 'REAL_congested_121', 'midnightREAL121/A1/checkpoint-1.json',
     ['midnightREAL121/A2/checkpoint-1.json', 'midnightREAL121/B1/checkpoint-1.json', 'midnightREAL121/B2/checkpoint-1.json']),
    ('259', 'GRID_ARTIFICIAL_1111', 'gridARTIFICIAL-control/checkpoint-1.json',
     ['gridARTIFICIAL-probe/checkpoint-1.json']),
]

def array_end(data, start):
    assert data[start] == 91
    depth = 0
    quoted = escaped = False
    for i in range(start, len(data)):
        c = data[i]
        if quoted:
            if escaped: escaped = False
            elif c == 92: escaped = True
            elif c == 34: quoted = False
        elif c == 34: quoted = True
        elif c == 91: depth += 1
        elif c == 93:
            depth -= 1
            if depth == 0: return i + 1
    raise AssertionError('unterminated array')

def elements(raw):
    assert raw[0] == 91 and raw[-1] == 93
    if raw == b'[]': return []
    parts, begin, braces, brackets = [], 1, 0, 0
    quoted = escaped = False
    for i in range(1, len(raw) - 1):
        c = raw[i]
        if quoted:
            if escaped: escaped = False
            elif c == 92: escaped = True
            elif c == 34: quoted = False
        elif c == 34: quoted = True
        elif c == 123: braces += 1
        elif c == 125: braces -= 1
        elif c == 91: brackets += 1
        elif c == 93: brackets -= 1
        elif c == 44 and braces == brackets == 0:
            parts.append(raw[begin:i]); begin = i + 1
    parts.append(raw[begin:-1])
    assert all(parts) and braces == brackets == 0
    return parts

def spans(data):
    out = []
    for key in (b'"Ledger":', b'"CityEvents":', b'"History":'):
        pos = 0
        while True:
            k = data.find(key, pos)
            if k < 0: break
            a = k + len(key)
            if data[a] == 91:
                b = array_end(data, a)
                out.append((key.decode().strip('":'), a, b, elements(data[a:b])))
                pos = b
            else: pos = a
    out.sort(key=lambda x: x[1])
    for prev, nxt in zip(out, out[1:]): assert prev[2] <= nxt[1]
    return out

def pack(parts, width=64):
    return [zlib.compress(b','.join(parts[i:i+width]), 6) for i in range(0, len(parts), width)]

def unpack(chunks):
    return b'[' + b','.join(zlib.decompress(c) for c in chunks) + b']'

def tail(chunks, count):
    if not chunks: return []
    # Decompress only as many final blocks as necessary; preserve exact bytes.
    last = []
    for c in reversed(chunks):
        last = elements(b'[' + zlib.decompress(c) + b']') + last
        if len(last) >= count: break
    return last[-count:]

def build(data, width=64):
    result = []
    for kind, a, b, parts in spans(data):
        chunks = pack(parts, width)
        assert unpack(chunks) == data[a:b]
        result.append((kind, a, b, parts, chunks))
    return result

def materialize(data, records):
    out = bytearray()
    prev = 0
    for _, a, b, _, chunks in records:
        out.extend(data[prev:a]); out.extend(unpack(chunks)); prev = b
    out.extend(data[prev:])
    return bytes(out)

def measure(fn, repeats=11):
    wall, cpu = [], []
    for _ in range(repeats):
        t = time.perf_counter_ns(); c = time.process_time_ns()
        fn()
        wall.append((time.perf_counter_ns() - t) / 1e6)
        cpu.append((time.process_time_ns() - c) / 1e6)
    return {'wall_median_ms': round(statistics.median(wall), 4),
            'cpu_median_ms': round(statistics.median(cpu), 4),
            'wall_min_ms': round(min(wall), 4), 'wall_max_ms': round(max(wall), 4)}

def run_case(name, data):
    state = json.loads(data)
    records = build(data)
    assert len(records) == len(state['Citizens']) + 2
    assert materialize(data, records) == data
    assert sum(k == 'History' for k, *_ in records) == len(state['Citizens'])
    total_parts = sum(len(parts) for _, _, _, parts, _ in records)
    chunks = sum(len(c) for _, _, _, _, c in records)
    compressed = sum(sum(len(v) for v in c) for _, _, _, _, c in records)
    original = sum(b-a for _, a, b, _, _ in records)
    # Reconstruct the exact last 8 entries of every personal history and last
    # 20 ledger/city events, including tails that cross a chunk boundary.
    tail_checks = 0
    for kind, _, _, parts, packed in records:
        n = 8 if kind == 'History' else 20
        assert tail(packed, n) == parts[-n:]
        tail_checks += 1
    histories = [(parts, c) for kind, _, _, parts, c in records if kind == 'History' and c]
    def read_tails():
        return sum(len(tail(c, 8)) for _, c in histories)
    def decompress_last():
        return sum(len(zlib.decompress(c[-1])) for _, c in histories)
    def tail_json(c):
        result = json.loads(b'[' + zlib.decompress(c[-1]) + b']')
        if len(result) < 8 and len(c) > 1:
            result = json.loads(b'[' + zlib.decompress(c[-2]) + b']') + result
        return result[-8:]
    def read_tails_json():
        return sum(len(tail_json(c)) for _, c in histories)
    def read_tails_uncompressed():
        return sum(len(json.loads(b'[' + b','.join(parts[-8:]) + b']')) for parts, _ in histories)
    for parts, c in histories:
        assert tail_json(c) == [json.loads(p) for p in parts[-8:]]
    def materialize_all():
        return materialize(data, records)
    def compress_all():
        return [pack(parts) for _, _, _, parts, _ in records]
    def parse_full():
        return json.loads(data)
    out = dict(case=name, citizens=len(state['Citizens']), ledger=len(state['Ledger']),
               city_events=len(state['CityEvents']), arrays=len(records), records=total_parts,
               chunks=chunks, original_save_bytes=len(data), historical_raw_bytes=original,
               compressed_chunk_bytes=compressed, metadata_bytes=4*len(records)+4*chunks,
               tail_parity_checks=tail_checks, exact_save_reconstruction=True,
               bench={'compress_all':measure(compress_all), 'materialize_all':measure(materialize_all),
                      'read_8_all_nonempty_citizens_python_scanner':measure(read_tails), 'decompress_last_chunk_all_citizens':measure(decompress_last), 'read_last_chunk_json_all_citizens':measure(read_tails_json), 'read_last_8_uncompressed_all_citizens':measure(read_tails_uncompressed), 'parse_original_json':measure(parse_full)})
    return out

def main(argv):
    if len(argv) != 2: raise SystemExit(__doc__)
    paths = dict(zip(('478', '259'), argv))
    for k, name, member, mirrors in CASES:
        with open(paths[k], 'rb') as f: blob = f.read()
        assert hashlib.sha256(blob).hexdigest() == EXPECTED[k], 'artifact SHA mismatch'
        with zipfile.ZipFile(paths[k]) as z:
            assert z.testzip() is None
            data = z.read(member)
            for mirror in mirrors: assert z.read(mirror) == data, 'A/B mismatch'
        print(json.dumps(run_case(name, data), sort_keys=True))
    print('PR57_519_AUTHENTIC_CHUNK_READ_COST_PASS')

if __name__ == '__main__': main(sys.argv[1:])
