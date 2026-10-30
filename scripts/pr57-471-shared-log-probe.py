#!/usr/bin/env python3
"""PR57 #471: append-only shared compressed log with sparse per-collection index.

Offline POC on original #465 artifact. No engine changes, no .NET throughput/heap claims.
python3 scripts/pr57-471-shared-log-probe.py pr57-465-original.zip
"""
import hashlib
import json
import statistics
import struct
import sys
import time
import zipfile
import zlib

EXPECTED_SHA = '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1'
SAVES = ('20h.json', 'A-pre/checkpoint-1.json', 'A-mid/checkpoint-1.json', 'A-post/checkpoint-1.json')
STRIDE = 16


def value_span(data, name, start=0):
    key = ('"' + name + '":').encode()
    at = data.find(key, start)
    assert at >= 0, (name, start)
    p = at + len(key)
    assert data[p] == 91
    depth, quoted, escaped = 0, False, False
    for i in range(p, len(data)):
        c = data[i]
        if quoted:
            if escaped: escaped = False
            elif c == 92: escaped = True
            elif c == 34: quoted = False
        elif c == 34: quoted = True
        elif c == 91: depth += 1
        elif c == 93:
            depth -= 1
            if depth == 0: return p, i+1
    raise AssertionError('unterminated array')


def array_elements(raw):
    assert raw[0] == 91 and raw[-1] == 93
    if raw == b'[]': return []
    out, depth, quoted, escaped, start = [], 0, False, False, 1
    for i in range(1, len(raw)-1):
        c = raw[i]
        if quoted:
            if escaped: escaped = False
            elif c == 92: escaped = True
            elif c == 34: quoted = False
        elif c == 34: quoted = True
        elif c in (91, 123): depth += 1
        elif c in (93, 125): depth -= 1
        elif c == 44 and depth == 0:
            out.append(raw[start:i]); start = i+1
    out.append(raw[start:-1])
    assert b'['+b','.join(out)+b']' == raw
    return out


def collections(data):
    parsed = json.loads(data)
    result, positions, pos = {}, {}, 0
    for citizen in parsed['Citizens']:
        start, end = value_span(data, 'History', pos)
        key = ('history', citizen['Id'])
        result[key] = array_elements(data[start:end])
        assert len(result[key]) == len(citizen['History'])
        positions[key] = (start, end)
        pos = end
    for name, key in [('Ledger', ('ledger', 0)), ('CityEvents', ('events', 0))]:
        start, end = value_span(data, name)
        result[key] = array_elements(data[start:end])
        assert len(result[key]) == len(parsed[name])
        positions[key] = (start, end)
    assert len(result) == len(positions)
    return result, positions


class SharedLog:
    def __init__(self, block_size):
        self.block_size = block_size
        self.sealed = []
        self.tail = bytearray()
        self.offset = 0
        self.segments = {}
        self.counts = {}

    def append_record(self, event):
        assert 0 < len(event) < 65536
        at = self.offset
        payload = struct.pack('<H', len(event)) + event
        self.offset += len(payload)
        self.tail.extend(payload)
        while len(self.tail) >= self.block_size:
            raw = bytes(self.tail[:self.block_size])
            self.sealed.append(zlib.compress(raw, 6))
            del self.tail[:self.block_size]
        return at

    def append(self, previous, current):
        added, changed, sealed_before = 0, 0, tuple(self.sealed)
        assert set(previous) <= set(current)
        for key, events in current.items():
            self.counts.setdefault(key, 0)
            self.segments.setdefault(key, [])
            old = previous.get(key, [])
            assert events[:len(old)] == old, ('not append-only', key)
            assert self.counts.get(key, 0) == len(old)
            delta = events[len(old):]
            if not delta: continue
            changed += 1
            anchors = []
            for i, event in enumerate(delta):
                at = self.append_record(event)
                if i % STRIDE == 0: anchors.append(at)
            self.segments.setdefault(key, []).append((len(old), len(delta), anchors))
            self.counts[key] = len(events)
            added += len(delta)
        assert tuple(self.sealed[:len(sealed_before)]) == sealed_before
        return dict(appended=added, changed_collections=changed,
                    new_sealed_blocks=len(self.sealed)-len(sealed_before),
                    old_sealed_rewritten=0)

    def read(self, start, length):
        assert start >= 0 and start+length <= self.offset
        parts = []
        while length:
            block_id, within = divmod(start, self.block_size)
            raw = zlib.decompress(self.sealed[block_id]) if block_id < len(self.sealed) else self.tail
            take = min(length, len(raw)-within)
            assert take > 0
            parts.append(raw[within:within+take])
            start += take
            length -= take
        return b''.join(parts)

    def get(self, key, idx):
        assert 0 <= idx < self.counts[key]
        for begin, count, anchors in self.segments[key]:
            if begin <= idx < begin+count:
                rel = idx-begin
                pos = anchors[rel//STRIDE]
                for _ in range(rel % STRIDE + 1):
                    length = struct.unpack('<H', self.read(pos, 2))[0]
                    pos += 2
                    event = self.read(pos, length)
                    pos += length
                return event
        raise AssertionError((key, idx))

    def dump(self):
        return b''.join(zlib.decompress(b) for b in self.sealed) + bytes(self.tail)

    def reconstruct(self, key, raw):
        events = []
        for begin, count, anchors in self.segments.get(key, []):
            pos = anchors[0]
            for _ in range(count):
                length = struct.unpack_from('<H', raw, pos)[0]
                pos += 2
                events.append(raw[pos:pos+length])
                pos += length
        assert len(events) == self.counts.get(key, 0)
        return events

    def metrics(self):
        anchors = sum(len(anchors) for segments in self.segments.values() for _,_,anchors in segments)
        segments = sum(map(len, self.segments.values()))
        # Hypothetical packed index: 16 B/collection, 24 B/segment, 8 B/sparse anchor.
        index = 16*len(self.counts) + 24*segments + 8*anchors
        payload = sum(map(len, self.sealed))
        return dict(sealed_blocks=len(self.sealed), sealed_payload_bytes=payload,
                    raw_tail_bytes=len(self.tail), segment_count=segments, anchor_count=anchors,
                    hypothetical_index_bytes=index, hypothetical_total_bytes=payload+len(self.tail)+index,
                    max_block_inflate_bytes=self.block_size if self.sealed else len(self.tail))


def verify_full_save(data, store, positions):
    replacements = []
    raw = store.dump()
    for key, (start, end) in positions.items():
        reconstructed = b'[' + b','.join(store.reconstruct(key, raw)) + b']'
        assert reconstructed == data[start:end], key
        replacements.append((start,end,reconstructed))
    result = data
    for start,end,content in sorted(replacements, reverse=True):
        result = result[:start] + content + result[end:]
    assert result == data


def run(archive, block_size):
    store = SharedLog(block_size)
    old = {}
    summaries = []
    for step, name in enumerate(SAVES):
        data = archive.read(name)
        current, positions = collections(data)
        assert set(current) == set(collections(archive.read(SAVES[0]))[0])
        append = store.append(old, current)
        verify_full_save(data, store, positions)
        # Stable, deterministic 250 individual event reads from authentic collections.
        candidates = [(key, idx) for key, events in current.items() for idx in range(len(events))]
        sample = [candidates[(i*7919) % len(candidates)] for i in range(250)]
        times = []
        for _ in range(3):
            t0 = time.perf_counter_ns()
            for key, idx in sample:
                assert store.get(key, idx) == current[key][idx]
            times.append((time.perf_counter_ns()-t0)/1e6)
        summaries.append(dict(block_bytes=block_size, checkpoint=name, events=len(candidates),
                              **append, **store.metrics(), python_250_reads_median_ms=round(statistics.median(times),3)))
        old = current
    return summaries


def adversarial():
    # Pure isolated format tests, not synthetic city or throughput evidence.
    store = SharedLog(64)
    original = {('empty', 1): [], ('a', 2): [b'{"v":"escaped \\" quote"}', '{"v":"á"}'.encode()]}
    store.append({}, original)
    assert store.reconstruct(('empty', 1), store.dump()) == []
    assert store.reconstruct(('a', 2), store.dump()) == original[('a', 2)]
    next_state = {('empty', 1): [b'{"x":0}'], ('a', 2): original[('a', 2)] + [b'{"x":"' + b'Z'*128 + b'"}']}
    old_sealed = tuple(store.sealed)
    store.append(original, next_state)
    assert tuple(store.sealed[:len(old_sealed)]) == old_sealed
    for key, events in next_state.items():
        assert store.reconstruct(key, store.dump()) == events
        for i, event in enumerate(events): assert store.get(key, i) == event
    try:
        store.append(next_state, {**next_state, ('a', 2): next_state[('a', 2)] + [b'X'*65536]})
    except AssertionError:
        pass
    else:
        raise AssertionError('Oversize record must be rejected')


def main(path):
    adversarial()
    with open(path, 'rb') as f:
        assert hashlib.file_digest(f, 'sha256').hexdigest() == EXPECTED_SHA
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        for name in SAVES[1:]:
            assert z.read(name) == z.read(name.replace('A-', 'B-', 1))
        for block_size in (4096, 16384, 65536):
            for result in run(z, block_size):
                print('PR57_471 '+json.dumps(result,sort_keys=True))
    print('PR57_471_SHARED_APPEND_LOG_PASS')


if __name__ == '__main__':
    assert len(sys.argv) == 2, __doc__
    main(sys.argv[1])
