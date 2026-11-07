#!/usr/bin/env python3
"""PR57 #503: descriptive graph census from authentic REAL checkpoints.

Not a causal independence proof, speedup estimate, or engine benchmark.
Usage: python3 pr57-503-partition-census.py 465.zip 445.zip
"""
import collections
import hashlib
import json
import math
import sys
import zipfile

SOURCES = [
    ('370943040748473e16cbeaaabc5c5048442c0fdc7806481c0638fe9759bc0ff1',
     ['20h.json', 'A-pre/checkpoint-1.json', 'A-mid/checkpoint-1.json', 'A-post/checkpoint-1.json'],
     [('A-pre/checkpoint-0.json', 'B-pre/checkpoint-0.json'),
      ('A-mid/checkpoint-1.json', 'B-mid/checkpoint-1.json'),
      ('A-post/checkpoint-1.json', 'B-post/checkpoint-1.json')]),
    ('ae6b36fca4965641b7aef93091476063317a953e422f5e4eb92f4b9d720d403a',
     ['pr57-445/A/checkpoint-0.json', 'pr57-445/A/checkpoint-1.json'],
     [('pr57-445/A/checkpoint-0.json','pr57-445/B/checkpoint-0.json'),
      ('pr57-445/A/checkpoint-1.json','pr57-445/B/checkpoint-1.json')]),
]

def components(nodes, edges):
    parent = {n:n for n in nodes}
    def root(x):
        while parent[x]!=x:
            parent[x]=parent[parent[x]]
            x=parent[x]
        return x
    for a,b in edges:
        if a not in parent or b not in parent: continue
        a,b=root(a),root(b)
        if a!=b:parent[b]=a
    groups=collections.Counter(root(n) for n in nodes)
    return sorted(groups.values(),reverse=True)

def route_graph(vehicles, n):
    ids=[v['Id'] for v in vehicles]
    by_node=collections.defaultdict(list)
    for v in vehicles:
        route=v['Route'][v['RouteIndex']:]
        for node in set(route[:n] if n else route):
            by_node[node].append(v['Id'])
    edges=set()
    for users in by_node.values():
        for i,a in enumerate(users):
            for b in users[i+1:]:edges.add((min(a,b),max(a,b)))
    return components(ids,edges),len(edges),len(by_node)

def proximity_graph(vehicles, threshold):
    edges=[]
    for i,a in enumerate(vehicles):
        for b in vehicles[i+1:]:
            if math.hypot(a['Position']['X']-b['Position']['X'],a['Position']['Z']-b['Position']['Z'])<threshold:
                edges.append((a['Id'],b['Id']))
    return components([v['Id'] for v in vehicles],edges),len(edges)

def census(name,d):
    roads=d['Roads']; nodes={r['Id'] for r in roads}; neighbours=[(r['Id'],n) for r in roads for n in r['Neighbours']]
    assert all(n in nodes for _,n in neighbours)
    road_components=components(nodes,neighbours)
    # Descriptive travelling cohort only; parked vehicles remain physically relevant.
    travellers=[v for v in d['Vehicles'] if v['Status']=='Travelling' and v['RouteIndex']<len(v['Route'])]
    physical=[v for v in d['Vehicles'] if v['Status'] not in ('Outside','Towed')]
    assert len({v['Id'] for v in d['Vehicles']})==len(d['Vehicles'])
    assert all(set(v['Route']).issubset(nodes) for v in travellers)
    print(f'{name}: roads={len(roads)} road_components={len(road_components)} largest_roads={road_components[:4]} vehicles={len(d["Vehicles"])} travelling_route={len(travellers)} physically_present={len(physical)}')
    for n in (1,3,8,0):
        sizes,edges,used=route_graph(travellers,n)
        print(f'  route_remaining_next={n or "ALL"}: components={len(sizes)} largest={sizes[:6]} edges={edges} distinct_nodes={used}')
    for metres in (12,24):
        sizes,edges=proximity_graph(physical,metres)
        print(f'  present_pose_distance_lt_{metres}m: components={len(sizes)} largest={sizes[:6]} edges={edges}')
    return road_components

def main(paths):
    assert len(paths)==2
    for path,(sha,names,pairs) in zip(paths,SOURCES):
        assert hashlib.sha256(open(path,'rb').read()).hexdigest()==sha
        with zipfile.ZipFile(path) as z:
            assert z.testzip() is None
            for a,b in pairs:assert z.read(a)==z.read(b),(a,b)
            for name in names:census(name,json.loads(z.read(name)))
    print('PR57_503_AUTHENTIC_PARTITION_CENSUS_PASS')

if __name__=='__main__':main(sys.argv[1:])
