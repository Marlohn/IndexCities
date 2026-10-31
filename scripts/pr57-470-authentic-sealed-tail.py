#!/usr/bin/env python3
"""PR57 #470: sealed zlib chunks + mutable raw tail on authentic #465 events.

python3 scripts/pr57-470-authentic-sealed-tail.py pr57-465-original.zip
Offline POC; no engine changes, .NET heap/CPU or integrated gain claims.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import zipfile
import zlib

spec = importlib.util.spec_from_file_location('pr468', Path(__file__).with_name('pr57-468-authentic-chunk-index-probe.py'))
prior = importlib.util.module_from_spec(spec)
spec.loader.exec_module(prior)
NAMES = ('20h.json', 'A-pre/checkpoint-1.json', 'A-mid/checkpoint-1.json', 'A-post/checkpoint-1.json')


def encode(events):
    return b'[' + b','.join(events) + b']'


class SealedTail:
    def __init__(self, size, initial):
        self.size = size
        self.sealed = {}
        self.tail = {}
        self.count = {}
        for key, values in initial.items():
            full = len(values)//size
            self.sealed[key] = [zlib.compress(b','.join(values[i*size:(i+1)*size]), 6)
                                for i in range(full)]
            self.tail[key] = list(values[full*size:])
            self.count[key] = len(values)

    def append(self, next_values):
        prior_blocks = {key: tuple(blocks) for key, blocks in self.sealed.items()}
        written = new_blocks = appended = 0
        for key, values in next_values.items():
            old = self.count[key]
            assert len(values) >= old
            for event in values[old:]:
                appended += 1
                self.tail[key].append(event)
                self.count[key] += 1
                if len(self.tail[key]) == self.size:
                    compressed = zlib.compress(b','.join(self.tail[key]), 6)
                    self.sealed[key].append(compressed)
                    written += len(compressed)
                    new_blocks += 1
                    self.tail[key] = []
            assert tuple(self.sealed[key][:len(prior_blocks[key])]) == prior_blocks[key]
        return dict(appended=appended, new_sealed=new_blocks, sealed_written=written, old_sealed_rewritten=0)

    def materialize(self, key):
        values = []
        for compressed in self.sealed[key]:
            values.extend(prior.array_elements(b'[' + zlib.decompress(compressed) + b']'))
        return values + self.tail[key]

    def measure(self):
        blocks = sum(map(len, self.sealed.values()))
        payload = sum(len(b) for v in self.sealed.values() for b in v)
        raw_tail = sum(len(b','.join(v)) for v in self.tail.values())
        # Hypothetical 20 B/sealed block + 16 B/collection, not .NET heap.
        return dict(sealed_blocks=blocks, raw_tail_bytes=raw_tail,
                    hypothetical_total_bytes=payload+raw_tail+20*blocks+16*len(self.sealed),
                    max_tail_events=max(map(len,self.tail.values())))


def fixed_bytes(collections, size):
    chunks = prior.compress_index(collections, size)
    return sum(len(b)+20 for blocks in chunks.values() for b in blocks)


def main(path):
    with open(path,'rb') as f:
        assert hashlib.file_digest(f,'sha256').hexdigest() == prior.EXPECTED_SHA
    with zipfile.ZipFile(path) as archive:
        assert archive.testzip() is None
        saves = [archive.read(name) for name in NAMES]
        for step, phase in ((1,'pre'),(2,'mid'),(3,'post')):
            assert saves[step] == archive.read(f'B-{phase}/checkpoint-1.json')
    states = [prior.collections(save) for save in saves]
    assert all(state.keys()==states[0].keys() for state in states)
    for size in (16,64,256):
        store = SealedTail(size, states[0])
        for step, state in enumerate(states):
            delta = store.append(state) if step else dict(appended=0,new_sealed=0,sealed_written=0,old_sealed_rewritten=0)
            for key, original in state.items():
                assert encode(store.materialize(key)) == encode(original), (size,step,key)
            result = dict(size=size, checkpoint=NAMES[step], events=sum(map(len,state.values())),
                          fixed_hypothetical_bytes=fixed_bytes(state,size), **store.measure(), **delta)
            print('PR57_470 '+json.dumps(result,sort_keys=True))
    print('PR57_470_AUTHENTIC_SEALED_TAIL_PASS')


if __name__=='__main__':
    assert len(sys.argv)==2, __doc__
    main(sys.argv[1])
