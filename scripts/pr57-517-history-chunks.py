#!/usr/bin/env python3
"""PR57 #517: lossless, independently compressed history chunks on authentic saves.

Usage: python3 pr57-517-history-chunks.py 478.zip 493.zip 259.zip
Pure offline representation experiment: no engine change, no synthetic citizens,
no product save format, no .NET throughput or retained-heap claim.
"""
import hashlib
import json
import re
import struct
import sys
import zipfile
import zlib
from pathlib import Path

SOURCES = (
    ('7130344249d2958244dd08906eea5cfc74bb0585cd14a772a5e4c9ce9aff6902',
     'midnightREAL121', ('A1', 'B1', 'B2', 'A2'), 'REAL midnight 121'),
    ('f5eae23074f49c3426a814e24e8bee8f20bee5713340b3e25dabafc128564c89',
     'worksREAL47', ('A1', 'B1', 'B2', 'A2'), 'REAL works 47'),
    ('23472d3616d30a93addc97a4adb02d0e807c62ac1943fb814ded1528c550278b',
     'gridARTIFICIAL', ('control', 'probe'), 'GRID ARTIFICIAL 1111'),
)
PATTERN = re.compile(rb'"(Ledger|CityEvents|History)":(?=\[)')
DECODER = json.JSONDecoder()


def split_save(raw):
    assert raw.isascii()
    arrays, parts, previous = [], [], 0
    for m in PATTERN.finditer(raw):
        start = m.end()
        value, chars = DECODER.raw_decode(raw[start:].decode('ascii'))
        assert isinstance(value, list)
        arr = raw[start:start + chars]
        assert arr.startswith(b'[') and arr.endswith(b']')
        arrays.append((m.group(1).decode('ascii'), arr, len(value)))
        parts.extend((raw[previous:start], b'[]'))
        previous = start + chars
    parts.append(raw[previous:])
    return b''.join(parts), arrays


def events(arr):
    """Split compact original JSON bytes at top-level commas, without reserializing."""
    assert arr[0] == 91 and arr[-1] == 93
    body = arr[1:-1]
    if not body:
        return []
    result = []
    depth = 0
    quoted = escaped = False
    start = 0
    for i, c in enumerate(body):
        if quoted:
            if escaped:
                escaped = False
            elif c == 92:
                escaped = True
            elif c == 34:
                quoted = False
        elif c == 34:
            quoted = True
        elif c in (91, 123):
            depth += 1
        elif c in (93, 125):
            depth -= 1
            assert depth >= 0
        elif c == 44 and depth == 0:
            result.append(body[start:i])
            start = i + 1
    result.append(body[start:])
    assert depth == 0 and not quoted and all(result)
    assert b'[' + b','.join(result) + b']' == arr
    return result


def archive(core, arrays, width):
    """Model: core with [] placeholders, per-array u32 chunk count,
    per-chunk u32 byte length + independent zlib level-6 compressed payload.
    No indexes for citizen IDs, versioning, crash recovery or other metadata.
    """
    packed = bytearray()
    recovered = []
    chunk_count = 0
    by_kind = {}
    for kind, arr, count in arrays:
        records = events(arr)
        assert len(records) == count
        blocks = [b','.join(records[i:i + width]) for i in range(0, len(records), width)]
        packed.extend(struct.pack('<I', len(blocks)))
        restored = []
        for block in blocks:
            compressed = zlib.compress(block, 6)
            packed.extend(struct.pack('<I', len(compressed)))
            packed.extend(compressed)
            restored.append(zlib.decompress(compressed))
            chunk_count += 1
        recovered.append((kind, b'[' + b','.join(restored) + b']'))
        s = by_kind.setdefault(kind, [0, 0, 0])
        s[0] += len(arr)
        s[1] += 4 + sum(4 + len(zlib.compress(block, 6)) for block in blocks)
        s[2] += len(blocks)
    chunks = bytes(packed)
    assert len(chunks) == sum(v[1] for v in by_kind.values())
    pieces, last = [], 0
    matches = list(PATTERN.finditer(core))
    assert len(matches) == len(recovered)
    for match, (kind, raw_arr) in zip(matches, recovered):
        assert match.group(1).decode('ascii') == kind
        start = match.end()
        assert core[start:start + 2] == b'[]'
        pieces.extend((core[last:start], raw_arr))
        last = start + 2
    full = b''.join(pieces + [core[last:]])
    return full, len(core) + len(chunks), chunk_count, by_kind


def main(paths):
    assert len(paths) == len(SOURCES)
    for path, (expected, case, arms, label) in zip(paths, SOURCES):
        data = Path(path).read_bytes()
        assert hashlib.sha256(data).hexdigest() == expected
        with zipfile.ZipFile(path) as z:
            assert z.testzip() is None
            initial, final = set(), set()
            for arm in arms:
                prefix = f'{case}/{arm}' if arm in ('A1', 'B1', 'B2', 'A2') else f'{case}-{arm}'
                if arm in ('A1', 'B1', 'B2', 'A2'):
                    prefix += '/'
                else:
                    prefix += '/'
                before = z.read(prefix + 'checkpoint-0.json')
                after = z.read(prefix + 'checkpoint-1.json')
                initial.add(hashlib.sha256(before).hexdigest())
                final.add(hashlib.sha256(after).hexdigest())
            assert len(initial) == len(final) == 1, 'control/probe checkpoints diverged'
            prefix0 = f'{case}/{arms[0]}/' if arms[0] == 'A1' else f'{case}-{arms[0]}/'
            previous_raw = z.read(prefix0 + 'checkpoint-0.json')
            raw = z.read(prefix0 + 'checkpoint-1.json')
            core, arrays = split_save(raw)
            _, previous_arrays = split_save(previous_raw)
            assert len(previous_arrays) == len(arrays)
            assert len(arrays) == len(json.loads(raw)['Citizens']) + 2
            for width in (16, 64, 256):
                rebuilt, stored, chunks, kinds = archive(core, arrays, width)
                assert rebuilt == raw, 'byte-exact roundtrip failed'
                reused_chunks = replaced_chunks = reused_bytes = replaced_bytes = 0
                changed_arrays = 0
                for (old_kind, old_arr, old_count), (new_kind, new_arr, new_count) in zip(previous_arrays, arrays):
                    assert old_kind == new_kind
                    old_records, new_records = events(old_arr), events(new_arr)
                    assert old_count == len(old_records) and new_count == len(new_records)
                    assert new_records[:old_count] == old_records, 'history not append-only'
                    changed_arrays += (new_count != old_count)
                    old_blocks = [zlib.compress(b','.join(old_records[i:i + width]), 6) for i in range(0, old_count, width)]
                    new_blocks = [zlib.compress(b','.join(new_records[i:i + width]), 6) for i in range(0, new_count, width)]
                    for i, blob in enumerate(new_blocks):
                        if i < len(old_blocks) and blob == old_blocks[i]:
                            reused_chunks += 1
                            reused_bytes += len(blob)
                        else:
                            replaced_chunks += 1
                            replaced_bytes += len(blob)
                assert reused_chunks + replaced_chunks == chunks
                print(json.dumps({
                    'case': label, 'chunk_events': width,
                    'original_save_bytes': len(raw),
                    'core_bytes': len(core),
                    'history_arrays': len(arrays),
                    'history_raw_bytes': sum(len(a) for _, a, _ in arrays),
                    'chunk_count': chunks,
                    'changed_arrays_since_prior_checkpoint': changed_arrays,
                    'unchanged_compressed_chunks': reused_chunks,
                    'new_or_recompressed_chunks': replaced_chunks,
                    'reused_compressed_bytes': reused_bytes,
                    'new_or_recompressed_bytes': replaced_bytes,
                    'chunk_payload_plus_index_bytes': stored - len(core),
                    'core_plus_chunks_bytes': stored,
                    'history_reduction_pct': round(100 * (1 - (stored - len(core)) / sum(len(a) for _, a, _ in arrays)), 2),
                    'total_save_reduction_pct': round(100 * (1 - stored / len(raw)), 2),
                    'kinds': kinds,
                    'byte_exact_reconstruction': True,
                    'ABBA_or_control_probe_parity': True,
                }, ensure_ascii=False, sort_keys=True))
    print('PR57_517_AUTHENTIC_HISTORY_CHUNK_COMPRESSION_PASS')


if __name__ == '__main__':
    if len(sys.argv) != 4:
        sys.exit('usage: pr57-517-history-chunks.py 478.zip 493.zip 259.zip')
    main(sys.argv[1:])
