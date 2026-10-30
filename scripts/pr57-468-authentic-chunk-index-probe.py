#!/usr/bin/env python3
"""PR57 #468: offline bounded random-access/append compression tradeoff on authentic #465 saves.

Usage: python3 scripts/pr57-468-authentic-chunk-index-probe.py <original-465-artifact.zip>
No engine changes, no .NET claims, no new SIMs, no save-format adoption.
"""
import hashlib
import json
import statistics
import sys
import time
import zipfile
import zlib

EXPECTED_SHA = '370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1'
BEFORE = 'A-pre/checkpoint-1.json'  # 23:59:58
AFTER = 'A-mid/checkpoint-1.json'  # 00:00:00
SIZES = (16, 64, 256, 1024)
HEADER_BYTES = 20  # hypothetical fixed per-chunk: id,start,count,raw_length,compressed_length


def value_span(data, name, start=0):
    key = ('"' + name + '":').encode()
    at = data.find(key, start)
    assert at >= 0, (name, start)
    p = at + len(key)
    assert data[p] == 91, (name, p, data[p:p+30])
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
            if depth == 0: return data[p:i+1], i+1
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
        elif c in (91, 123): depth += 1
        elif c in (93, 125): depth -= 1
        elif c == 44 and depth == 0:
            out.append(raw[start:i]); start = i+1
    out.append(raw[start:-1])
    assert all(out) and b'['+b','.join(out)+b']' == raw
    return out


def collections(data):
    parsed = json.loads(data)
    result, pos = {}, 0
    for citizen in parsed['Citizens']:
        raw, pos = value_span(data, 'History', pos)
        elems = array_elements(raw)
        assert len(elems) == len(citizen['History'])
        result[('history', citizen['Id'])] = elems
    for name, key in [('Ledger', 'ledger'), ('CityEvents', 'events')]:
        raw, _ = value_span(data, name)
        elems = array_elements(raw)
        assert len(elems) == len(parsed[name])
        result[(key, 0)] = elems
    return result


def make_blocks(elems, size):
    return [zlib.compress(b','.join(elems[i:i+size]), 6) for i in range(0, len(elems), size)]


def decode_events(blocks):
    out=[]
    for block in blocks:
        payload=zlib.decompress(block)
        out.extend(array_elements(b'['+payload+b']'))
    return out


def compress_index(coll, size):
    blocks={k:make_blocks(v,size) for k,v in coll.items()}
    for k,v in coll.items(): assert decode_events(blocks[k]) == v
    return blocks


def update_index(old, new, blocks, size):
    changed, rewritten, created, payload_written = 0, 0, 0, 0
    updated={k:list(v) for k,v in blocks.items()}
    for k,old_events in old.items():
        next_events=new[k]
        assert next_events[:len(old_events)] == old_events, ('not append only', k)
        if len(next_events)==len(old_events): continue
        changed+=1
        old_count=len(old_events)
        first_chunk=old_count//size
        if old_count % size: first_chunk=(old_count-1)//size
        previous=len(updated[k])
        suffix_blocks=make_blocks(next_events[first_chunk*size:],size)
        updated[k][first_chunk:]=suffix_blocks
        rewritten+=int(first_chunk<previous)
        created+=max(0,len(updated[k])-previous)
        payload_written+=sum(map(len,suffix_blocks))
        assert decode_events(updated[k])==next_events, k
    return updated,dict(changed_collections=changed,rewritten_tail_blocks=rewritten,new_blocks=created,
                        recompressed_payload_bytes=payload_written)


def measure(coll, size, before=None):
    blocks=compress_index(coll,size)
    compressed=sum(len(b) for bs in blocks.values() for b in bs)
    block_count=sum(map(len,blocks.values()))
    raw=sum(len(b'['+b','.join(v)+b']') for v in coll.values())
    max_uncompressed=max((len(zlib.decompress(b)) for bs in blocks.values() for b in bs),default=0)
    max_compressed=max((len(b) for bs in blocks.values() for b in bs),default=0)
    # Deterministic sample of 1000 actual event positions, no synthetic events.
    candidates=[(k,idx) for k,v in coll.items() for idx in range(len(v))]
    assert candidates
    sample=[candidates[(i*7919)%len(candidates)] for i in range(1000)]
    times=[]
    for _ in range(5):
        start=time.perf_counter_ns()
        for key,idx in sample:
            block=zlib.decompress(blocks[key][idx//size])
            event=array_elements(b'['+block+b']')[idx%size]
            assert event==coll[key][idx]
        times.append((time.perf_counter_ns()-start)/1e6)
    result=dict(chunk_events=size,raw_collection_json_bytes=raw,compressed_payload_bytes=compressed,
                index_header_bytes=HEADER_BYTES*block_count,total_bytes=compressed+HEADER_BYTES*block_count,
                block_count=block_count,max_block_inflate_bytes=max_uncompressed,
                max_block_compressed_bytes=max_compressed,python_1000_reads_median_ms=round(statistics.median(times),3))
    if before is not None:
        initial=compress_index(before,size)
        final,append=update_index(before,coll,initial,size)
        assert final==blocks  # byte-identical deterministic zlib output, not merely event parity
        result.update(append)
    return result


def main(path):
    with open(path,'rb') as f: digest=hashlib.file_digest(f,'sha256').hexdigest()
    assert digest==EXPECTED_SHA,digest
    with zipfile.ZipFile(path) as z:
        assert z.testzip() is None
        a=z.read(BEFORE); b=z.read(AFTER)
        assert a==z.read(BEFORE.replace('A-','B-',1))
        assert b==z.read(AFTER.replace('A-','B-',1))
    old=collections(a); new=collections(b)
    assert old.keys()==new.keys()
    print('authentic',json.dumps(dict(zip_sha256=digest,old_bytes=len(a),new_bytes=len(b),
                                     citizens=sum(k[0]=='history' for k in new),
                                     history_entries=sum(len(v) for k,v in new.items() if k[0]=='history'),
                                     ledger_entries=len(new[('ledger',0)]),
                                     city_event_entries=len(new[('events',0)])),sort_keys=True))
    for size in SIZES:
        print('chunk',json.dumps(measure(new,size,old),sort_keys=True))
    print('PR57_468_AUTHENTIC_CHUNK_INDEX_PASS')


if __name__=='__main__':
    assert len(sys.argv)==2, __doc__
    main(sys.argv[1])
