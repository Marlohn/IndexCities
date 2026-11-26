#!/usr/bin/env python3
"""PR57 #375: offline lossless JSON span compression (not a Step benchmark).
Usage: python3 pr57-375-lossless-history-codec.py <artifact-259.zip> <artifact-372.zip>
"""
import hashlib, json, statistics, struct, sys, time, zipfile, zlib

DIGESTS=('23472d3616d30a93addc97a4adb02d0e807c62ac1943fb814ded1528c550278b',
         '9a8fb9ed2f86762cb07ddbc543859490876aae2fcfe2f31d08f1c15071d0e1be')

def sha(x): return hashlib.sha256(x).hexdigest()
def fields(raw):
    s=raw.decode(); decoder=json.JSONDecoder(); spans=[]
    for key in ('"Ledger":','"History":'):
        pos=0
        while (i:=s.find(key,pos))!=-1:
            a=i+len(key); _,b=decoder.raw_decode(s,a)
            spans.append((len(s[:a].encode()),len(s[:b].encode()),key));pos=b
    spans.sort();state=json.loads(raw)
    assert len(spans)==len(state['Citizens'])+1 and spans[0][2]=='"Ledger":'
    assert all(a[1]<=b[0] for a,b in zip(spans,spans[1:]))
    return spans,state

def grouped(parts,k):
    # Ledger separately, then consecutive groups of k citizen histories.
    return [parts[:1]]+[parts[i:i+k] for i in range(1,len(parts),k)]

def encode(groups):
    out=bytearray()
    for group in groups:
        plain=b''.join(struct.pack('<I',len(x))+x for x in group)
        chunk=zlib.compress(plain,1)
        out+=struct.pack('<I',len(chunk))+chunk
    return bytes(out)

def decode(blob,groups):
    pos=0;out=[]
    for group in groups:
        n,=struct.unpack_from('<I',blob,pos);pos+=4
        plain=zlib.decompress(blob[pos:pos+n]);pos+=n;off=0
        for _ in group:
            length,=struct.unpack_from('<I',plain,off);off+=4
            out.append(plain[off:off+length]);off+=length
        assert off==len(plain)
    assert pos==len(blob)
    return out

def measure(fn):
    wall=[];cpu=[];value=None
    for _ in range(13):
        a=time.perf_counter_ns();b=time.process_time_ns();value=fn()
        wall.append((time.perf_counter_ns()-a)/1e6)
        cpu.append((time.process_time_ns()-b)/1e6)
    return value,statistics.median(wall),statistics.median(cpu)

def test(label,raw):
    spans,state=fields(raw);parts=[raw[a:b] for a,b,_ in spans]
    target=sum(map(len,parts));print('CASE',label,'save',len(raw),'target',target,
        'citizens',len(state['Citizens']),'historyEvents',sum(len(c['History']) for c in state['Citizens']),
        'ledger',len(state['Ledger']),'sha256',sha(raw))
    for k in (1,8,32,128,len(parts)):
        groups=grouped(parts,k)
        packed,ew,ec=measure(lambda:encode(groups))
        recovered,dw,dc=measure(lambda:decode(packed,groups))
        assert recovered==parts
        restored=bytearray(raw)
        for (a,b,_),p in reversed(list(zip(spans,recovered))):restored[a:b]=p
        assert bytes(restored)==raw
        print('CODEC',label,'group',k,'bytes',len(packed),'hypotheticalSave',len(raw)-target+len(packed),
              'encodeWallMs',round(ew,3),'encodeCpuMs',round(ec,3),
              'decodeWallMs',round(dw,3),'decodeCpuMs',round(dc,3),'exact',True)
    packed,ew,ec=measure(lambda:zlib.compress(raw,1))
    recovered,dw,dc=measure(lambda:zlib.decompress(packed));assert recovered==raw
    print('BASELINE',label,'wholeSaveZlib1',len(packed),'encodeWallMs',round(ew,3),
          'encodeCpuMs',round(ec,3),'decodeWallMs',round(dw,3),'decodeCpuMs',round(dc,3),'exact',True)

def main(a,b):
    for path,digest in zip((a,b),DIGESTS):
        assert sha(open(path,'rb').read())==digest
    with zipfile.ZipFile(a) as x,zipfile.ZipFile(b) as y:
        assert x.testzip() is None and y.testzip() is None
        final=y.read('baseA/checkpoint-10800.json')
        assert all(y.read(f'{arm}/checkpoint-10800.json')==final for arm in ('publishA','publishB','baseB'))
        for name,raw in (('REAL_14H',x.read('real14hREAL-control/checkpoint-0.json')),
                         ('REAL_20H',final),
                         ('GRID_0_ARTIFICIAL',x.read('gridARTIFICIAL-control/checkpoint-0.json')),
                         ('GRID_100S_ARTIFICIAL',x.read('gridARTIFICIAL-control/checkpoint-1.json'))):test(name,raw)
if __name__=='__main__':
    if len(sys.argv)!=3:raise SystemExit(__doc__)
    main(*sys.argv[1:])
