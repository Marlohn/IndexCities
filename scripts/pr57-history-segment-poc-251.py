#!/usr/bin/env python3
"""PR57 #251: offline, lossless append-only history POC; NEVER modifies simulation.

Usage: python scripts/pr57-history-segment-poc-251.py artifact.zip
Checkpoints are parsed with Decimal, so monetary/pollution precision is retained.
The archive is an experiment, not a replacement for the versioned .NET save.
"""
import argparse
import copy
import gzip
import hashlib
import json
import time
import zipfile
from decimal import Decimal


def encode(value):
    """Emit valid JSON without converting Decimal to binary float."""
    if value is None: return 'null'
    if value is True: return 'true'
    if value is False: return 'false'
    if isinstance(value, Decimal): return str(value)
    if isinstance(value, int): return str(value)
    if isinstance(value, float): raise TypeError('float would lose exact save precision')
    if isinstance(value, str): return json.dumps(value, ensure_ascii=True, separators=(',', ':'))
    if isinstance(value, list): return '['+','.join(map(encode, value))+']'
    if isinstance(value, dict): return '{'+','.join(encode(str(k))+':'+encode(v) for k,v in value.items())+'}'
    raise TypeError(type(value))


def load(raw): return json.loads(raw, parse_float=Decimal)


def history(state):
    return {str(c['Id']): c['History'] for c in state['Citizens']}


def core(state):
    out = copy.deepcopy(state)
    for citizen in out['Citizens']: citizen['History'] = []
    out['Ledger'] = []
    out['CityEvents'] = []
    return out


def append_delta(previous, current):
    old, new = history(previous), history(current)
    if not old.keys() <= new.keys(): raise ValueError('removed citizen: require full checkpoint/fallback')
    h = {}
    for citizen_id, events in new.items():
        prefix = old.get(citizen_id, [])
        if events[:len(prefix)] != prefix: raise ValueError('non-append citizen history: '+citizen_id)
        if len(events) > len(prefix): h[citizen_id] = events[len(prefix):]
    result = {'History': h}
    for name in ('Ledger','CityEvents'):
        before, after = previous[name], current[name]
        if after[:len(before)] != before: raise ValueError('non-append '+name)
        result[name] = after[len(before):]
    return result


def rebuild(core_state, deltas):
    result = copy.deepcopy(core_state)
    indexed = {str(c['Id']): c for c in result['Citizens']}
    for delta in deltas:
        for citizen_id, events in delta['History'].items():
            if citizen_id not in indexed: raise ValueError('missing citizen '+citizen_id)
            indexed[citizen_id]['History'].extend(events)
        result['Ledger'].extend(delta['Ledger'])
        result['CityEvents'].extend(delta['CityEvents'])
    return result


def sha(raw): return hashlib.sha256(raw).hexdigest()


def verified_blob(blobs, key):
    raw = blobs[key]
    if sha(raw) != key.split('/')[-1]: raise ValueError('segment checksum mismatch: '+key)
    return load(raw)


def main():
    ap=argparse.ArgumentParser(); ap.add_argument('artifact'); args=ap.parse_args()
    started=time.perf_counter()
    with zipfile.ZipFile(args.artifact) as z:
        names=sorted((n for n in z.namelist() if n.startswith('overnight-') and n.endswith('/checkpoint-1.json')))
        if len(names)<2: raise ValueError('need consecutive real checkpoints')
        raw=[z.read(n) for n in names]
    states=[load(x) for x in raw]
    previous=None; deltas=[]; cores=[]; committed=[]; blobs={}
    total_original=sum(map(len,raw)); total_core=total_delta=total_manifest=0
    for name, source, state in zip(names,raw,states):
        base=core(state)
        # The initial checkpoint stores the full historical prefix once.
        delta={'History':history(state),'Ledger':state['Ledger'],'CityEvents':state['CityEvents']} if previous is None else append_delta(previous,state)
        core_bytes=encode(base).encode(); delta_bytes=encode(delta).encode()
        assert load(core_bytes)==base and load(delta_bytes)==delta
        cores.append(base); deltas.append(delta)
        total_core+=len(core_bytes);total_delta+=len(delta_bytes)
        core_id='core/'+sha(core_bytes); delta_id='delta/'+sha(delta_bytes)
        blobs[core_id]=core_bytes;blobs[delta_id]=delta_bytes
        # A manifest is published only after all referenced content-addressed blobs exist.
        generation={'checkpoint':name,'core':core_id,'deltas':['delta/'+sha(encode(d).encode()) for d in deltas]}
        manifest=encode(generation).encode(); total_manifest+=len(manifest)
        assert all(key in blobs for key in [generation['core'],*generation['deltas']])
        committed.append(generation)
        recovered=rebuild(verified_blob(blobs,generation['core']),[verified_blob(blobs,k) for k in generation['deltas']])
        if recovered != state: raise AssertionError('integral value mismatch at '+name)
        previous=state
    # Crash before commit: orphaned/truncated data is ignored by last published manifest.
    latest=committed[-1]; blobs['delta/orphan']=b'{"History":'
    assert rebuild(verified_blob(blobs,latest['core']),[verified_blob(blobs,k) for k in latest['deltas']])==states[-1]
    # Corrupting a referenced segment MUST be detected rather than silently repaired.
    damaged=latest['deltas'][-1]; bad=bytearray(blobs[damaged]);bad[-1]^=1
    original=blobs[damaged]; blobs[damaged]=bytes(bad)
    try: verified_blob(blobs,damaged)
    except ValueError: pass
    else: raise AssertionError('corrupt committed segment accepted')
    blobs[damaged]=original
    # Non-append mutations and deletions must fail closed.
    modified=copy.deepcopy(states[-1]); modified['Ledger'][0]['Reason']='adversarial edit'
    try: append_delta(states[-2],modified)
    except ValueError: pass
    else: raise AssertionError('missed in-place ledger mutation')
    modified=copy.deepcopy(states[-1]); modified['Citizens'][0]['History'].pop(0)
    try: append_delta(states[-2],modified)
    except ValueError: pass
    else: raise AssertionError('missed personal history deletion')
    # Indexed per-SIM chronological retrieval (all events preserved, no global scan).
    last=states[-1]; citizen_id=str(last['Citizens'][0]['Id']); events=history(last)[citizen_id]
    low,high=events[0]['At'],events[-1]['At']
    indexed=[e for d in deltas for e in d['History'].get(citizen_id,[]) if low<=e['At']<=high]
    assert indexed==[e for e in events if low<=e['At']<=high]
    # Checkpoint bytes and compressed bytes are estimates for THIS Python layout, not .NET IO.
    archive_bytes=total_core+total_delta+total_manifest
    compressed_original=sum(len(gzip.compress(x,compresslevel=6,mtime=0)) for x in raw)
    compressed_core=sum(len(gzip.compress(encode(x).encode(),compresslevel=6,mtime=0)) for x in cores)
    compressed_delta=sum(len(gzip.compress(encode(x).encode(),compresslevel=6,mtime=0)) for x in deltas)
    print('PR57_251 checkpoints=%d citizens=%d first=%s last=%s' % (len(names),len(last['Citizens']),names[0],names[-1]))
    print('PR57_251 original_bytes=%d core_bytes=%d append_bytes=%d manifest_bytes=%d archive_bytes=%d ratio=%.4f' % (total_original,total_core,total_delta,total_manifest,archive_bytes,archive_bytes/total_original))
    print('PR57_251 gzip_original=%d gzip_core=%d gzip_append=%d gzip_ratio_without_manifest=%.4f' % (compressed_original,compressed_core,compressed_delta,(compressed_core+compressed_delta)/compressed_original))
    print('PR57_251 final_history=%d final_ledger=%d final_events=%d query_sim=%s query_count=%d' % (sum(len(x) for x in history(last).values()),len(last['Ledger']),len(last['CityEvents']),citizen_id,len(indexed)))
    print('PR57_251 full_decimal_parity=%d/%d append_prefix=PASS mutation_delete=REJECT torn_uncommitted=IGNORE committed_corruption=DETECT wall_s=%.3f' % (len(states),len(states),time.perf_counter()-started))
    print('PR57_251 NOTE: all checkpoints retain full mutable core; not a .NET save benchmark, not an Advance(2) speedup')

if __name__=='__main__':main()
