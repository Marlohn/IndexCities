#!/usr/bin/env python3
"""PR57 #406: conservative route-writer publication vs dock-body reader fanout.

Disposable checkout only, AFTER #399/#400/#401/#404. All original Route
mutations and physical scans remain sequential. No decision reuse, no skips.
Reader registry covers ONLY BodyAcrossLane after its early proximity gate;
it is NOT the complete negative-dependency graph or a causal certificate.
"""
from pathlib import Path

root = Path("src/IndexCities.Simulation")
p = root / "CitySimulation.cs"
s = p.read_text(encoding="utf-8")

def once(old, new):
    global s
    assert s.count(old) == 1, (old[:120], s.count(old))
    s = s.replace(old, new)

anchor = '    static readonly bool Pr57404Enabled=Environment.GetEnvironmentVariable("PR57_404_ROUTE_WRITERS")=="1";'
helper = """    static readonly bool Pr57406Enabled=Environment.GetEnvironmentVariable("PR57_406_DOCK_FANOUT")=="1";
    bool _pr57406Physical;
    int _pr57406Slices,_pr57406Reads,_pr57406Unique,_pr57406Writes,_pr57406WithReaders;
    int _pr57406MaxFanout,_pr57406MaxPerSlice,_pr57406OutsideRange;
    long _pr57406ReadTicks,_pr57406PublishTicks,_pr57406Notices;
    readonly Dictionary<int,int> _pr57406Ordinals=new();
    Dictionary<int,(ulong Low,ulong High)> _pr57406Current=new();
    Dictionary<int,(ulong Low,ulong High)> _pr57406Previous=new();
    void Pr57406BeginSlice(double dt,double? operationsDt)
    {
        _pr57406Physical=Pr57406Enabled&&_pr57399Step>0&&_pr57399Step<=1820
            &&dt>0&&operationsDt is null;
        if(!_pr57406Physical)return;
        _pr57406Slices++;
        if(_pr57406Ordinals.Count==0)
            foreach(var vehicle in State.Vehicles)
                _pr57406Ordinals.TryAdd(vehicle.Id,_pr57406Ordinals.Count);
        var old=_pr57406Previous;
        _pr57406Previous=_pr57406Current;
        _pr57406Current=old;
        _pr57406Current.Clear();
    }
    void Pr57406DockRead(Vehicle reader,Vehicle candidate)
    {
        if(!_pr57406Physical)return;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57406Reads++;
        if(!_pr57406Ordinals.TryGetValue(reader.Id,out var ordinal)||ordinal>=128)
        {
            _pr57406OutsideRange++;
            return;
        }
        _pr57406Current.TryGetValue(candidate.Id,out var mask);
        var bit=1UL<<(ordinal&63);
        var before=ordinal<64?mask.Low:mask.High;
        if((before&bit)==0)_pr57406Unique++;
        if(ordinal<64)mask.Low|=bit;else mask.High|=bit;
        _pr57406Current[candidate.Id]=mask;
        var fanout=System.Numerics.BitOperations.PopCount(mask.Low)+
            System.Numerics.BitOperations.PopCount(mask.High);
        _pr57406MaxPerSlice=Math.Max(_pr57406MaxPerSlice,fanout);
        _pr57406ReadTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
    }
    void Pr57406RouteWrite(Vehicle candidate,string site)
    {
        if(!Pr57406Enabled||_pr57399Step<=0||_pr57399Step>1820)return;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57406Writes++;
        _pr57406Current.TryGetValue(candidate.Id,out var current);
        _pr57406Previous.TryGetValue(candidate.Id,out var previous);
        var low=current.Low|previous.Low;
        var high=current.High|previous.High;
        var fanout=System.Numerics.BitOperations.PopCount(low)+
            System.Numerics.BitOperations.PopCount(high);
        if(fanout>0)_pr57406WithReaders++;
        _pr57406Notices+=fanout;
        _pr57406MaxFanout=Math.Max(_pr57406MaxFanout,fanout);
        _pr57406PublishTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        if(_pr57406Writes<=60)
            Console.WriteLine($"PR57_406_WRITE step={_pr57399Step} phase={_pr57399Phase} physical={_pr57399Physical} vehicle={candidate.Id} site={site} readersLastTwoPhysical={fanout} routeChanged=True");
    }
"""
once(anchor, helper + anchor)
# #404 observed only 11 selected IDs; #406 tests every vehicle in this
# 98-vehicle REAL fixture. Exact SequenceEqual is still used for changes.
target = '||!Pr57399Targets.Contains(v.Id)'
assert s.count(target)==3, s.count(target)
s=s.replace(target,'')
once('        if(!changed)return;\n        _pr57404Changes++;',
     '        if(!changed)return;\n        Pr57406RouteWrite(v,site);\n        _pr57404Changes++;')
once('        v.Route=route;\n        _pr57401Ticks+=',
     '        v.Route=route;\n        if(changed)Pr57406RouteWrite(v,site);\n        _pr57401Ticks+=')
summary = '        if(Pr57404Enabled&&_pr57399Step==1820)'
once(summary,
     '        if(Pr57406Enabled&&_pr57399Step==1820)\n'
     '            Console.WriteLine($"PR57_406_SUMMARY steps={_pr57399Step} physical={_pr57399Physical} slices={_pr57406Slices} dockReads={_pr57406Reads} uniqueReaderCandidatePerSlice={_pr57406Unique} routeChanges={_pr57406Writes} changesWithDockReaders={_pr57406WithReaders} potentialNotices={_pr57406Notices} maxWriterFanout={_pr57406MaxFanout} maxCandidateReadersPerSlice={_pr57406MaxPerSlice} readerOrdinalOverflow={_pr57406OutsideRange} readTicks={_pr57406ReadTicks} publishTicks={_pr57406PublishTicks} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True causalClosure=False noSkippedSlices=True");\n'
     + summary)
p.write_text(s,encoding="utf-8")

p = root / "Mobility.cs"
s=p.read_text(encoding="utf-8")
anchor='        Pr57SetupGapStart(dt,operationsDt);'
assert s.count(anchor)==1
s=s.replace(anchor,anchor+'\n        Pr57406BeginSlice(dt,operationsDt);')
anchor='        _pr57SiteOwnerRoster=null; // no index may survive this movement slice'
assert s.count(anchor)==1
s=s.replace(anchor,'        _pr57406Physical=false;\n'+anchor)
p.write_text(s,encoding="utf-8")

p=root/"Mobility.SharedSpace.cs"
s=p.read_text(encoding="utf-8")
anchor='        Pr57DockWriterRead(v,o); // #233: read-only dock dependency oracle after the canonical early guard'
assert s.count(anchor)==1
s=s.replace(anchor,anchor+'\n        Pr57406DockRead(v,o); // #406: conservative route-sensitive dock reader, observation only')
p.write_text(s,encoding="utf-8")
print("PR57_406_INJECTED fullFleetRouteWriters=True dockReaderAfterProximityGate=True observational=True")
