#!/usr/bin/env python3
"""PR57 #357: checkout-only SitePath dependency exposure and bounded writer probe.

The canonical engine still evaluates every actor, scan and Advance(2).
A reference setter observes assignments immediately; snapshots only observe net
content changes at slice boundaries. This is NOT causal closure or an optimization.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
base=root/"src/IndexCities.Simulation"

def patch(path,old,new):
    s=path.read_text()
    assert s.count(old)==1,(str(path),old,s.count(old))
    path.write_text(s.replace(old,new,1))

model=base/"Model.cs"
patch(model,"public List<Point> SitePath {get;set;}=[];",
      """public static Action<Vehicle>? Pr57357PathWrite; // injected observer only
    private List<Point> _pr57357SitePath=[];
    public List<Point> SitePath {get=>_pr57357SitePath;set{var old=_pr57357SitePath;_pr57357SitePath=value;if(!ReferenceEquals(old,value)){Pr57353Write?.Invoke(this,"SitePath");Pr57357PathWrite?.Invoke(this);}}}""")

mob=base/"Mobility.cs"
patch(mob,"Pr57353BeginSlice();","Pr57353BeginSlice();\n        Pr57357BeginSlice();")
patch(mob,"Pr57353EndSlice();","Pr57357EndSlice();\n        Pr57353EndSlice();")

shared=base/"Mobility.SharedSpace.cs"
patch(shared,"Pr57DockWriterRead(v,o); // #233: read-only dock dependency oracle after the canonical early guard",
      "Pr57DockWriterRead(v,o); // #233: read-only dock dependency oracle after the canonical early guard\n        Pr57357Read(v,o);")

out=base/"Mobility.SitePathExposure357.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// #357: real-window read exposure for canonical BodyAcrossLane, plus immediate
// SitePath reference assignments and net SitePath/OnSitePath boundary snapshots.
// No result is cached or reused; in-place transient edits and other readsets
// are NOT closed. The snapshots are NOT a writer journal.
public sealed partial class CitySimulation
{
    readonly record struct Pr57357Snapshot(string Key,bool OnSite);
    static readonly bool Pr57357Enabled =
        Environment.GetEnvironmentVariable("PR57_357_SITE_EXPOSURE")=="1";
    bool _pr57357Active;
    long _pr57357Window, _pr57357LastEndSlice;
    Dictionary<int,Pr57357Snapshot>? _pr57357Start;
    Dictionary<int,Pr57357Snapshot>? _pr57357PreviousEnd;
    readonly Dictionary<int,HashSet<int>> _pr57357ReadersByCandidate=new();
    readonly HashSet<int> _pr57357Readers=new();
    long _pr57357ReadCalls,_pr57357CanonicalReads,_pr57357TempReads;
    long _pr57357NonemptyReads,_pr57357OnSiteReads,_pr57357UniquePairs;
    long _pr57357Assignments,_pr57357CanonicalAssignments,_pr57357TempAssignments;
    long _pr57357PotentialFanout,_pr57357WithinChanges,_pr57357BetweenChanges;
    long _pr57357OnSiteFlips,_pr57357SnapshotPairs,_pr57357HookTicks;

    Pr57357Snapshot Pr57357Capture(Vehicle v)
    {
        var path=string.Join(";",v.SitePath.Select(p=>
            p.X.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+
            p.Z.ToString("R",System.Globalization.CultureInfo.InvariantCulture)));
        return new Pr57357Snapshot(
            path+"|"+v.SitePathIndex+"|"+v.SitePathBeforeRoad+"|"+
            v.Status+"|"+v.Machine+"|"+v.Route.Count+"|"+v.RouteIndex,
            OnSitePath(v));
    }
    Dictionary<int,Pr57357Snapshot> Pr57357CaptureFleet() =>
        State.Vehicles.ToDictionary(v=>v.Id,v=>Pr57357Capture(v));

    void Pr57357Compare(Dictionary<int,Pr57357Snapshot> a,
        Dictionary<int,Pr57357Snapshot> b,bool within)
    {
        foreach(var (id,old) in a)
        {
            if(!b.TryGetValue(id,out var now))continue;
            _pr57357SnapshotPairs++;
            if(old!=now)
            {
                if(within)_pr57357WithinChanges++;
                else _pr57357BetweenChanges++;
                if(old.OnSite!=now.OnSite)_pr57357OnSiteFlips++;
            }
        }
    }
    void Pr57357BeginSlice()
    {
        if(!Pr57357Enabled||!_pr57353Active)return;
        var slice=_pr57ExclusiveEligibleSlices;
        if(slice is 40001 or 50001)
        {
            if(_pr57357Active)throw new InvalidOperationException("#357 overlapping window");
            _pr57357Active=true;_pr57357Window=slice;
            _pr57357Start=null;_pr57357PreviousEnd=null;
            _pr57357ReadersByCandidate.Clear();_pr57357Readers.Clear();
            _pr57357ReadCalls=_pr57357CanonicalReads=_pr57357TempReads=0;
            _pr57357NonemptyReads=_pr57357OnSiteReads=_pr57357UniquePairs=0;
            _pr57357Assignments=_pr57357CanonicalAssignments=_pr57357TempAssignments=0;
            _pr57357PotentialFanout=_pr57357WithinChanges=_pr57357BetweenChanges=0;
            _pr57357OnSiteFlips=_pr57357SnapshotPairs=_pr57357HookTicks=0;
            _pr57357LastEndSlice=0;
            Vehicle.Pr57357PathWrite=Pr57357PathWrite;
        }
        if(!_pr57357Active)return;
        var current=Pr57357CaptureFleet();
        if(_pr57357PreviousEnd!=null&&_pr57357LastEndSlice+1==slice)
            Pr57357Compare(_pr57357PreviousEnd,current,false);
        _pr57357Start=current;
    }
    void Pr57357PathWrite(Vehicle v)
    {
        var begin=System.Diagnostics.Stopwatch.GetTimestamp();
        _pr57357Assignments++;
        if(_pr57353Canonical?.Contains(v)==true)
        {
            _pr57357CanonicalAssignments++;
            if(_pr57357ReadersByCandidate.TryGetValue(v.Id,out var readers))
                _pr57357PotentialFanout+=readers.Count;
        }
        else _pr57357TempAssignments++;
        _pr57357HookTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-begin;
    }
    void Pr57357Read(Vehicle reader,Vehicle candidate)
    {
        if(!_pr57357Active)return;
        _pr57357ReadCalls++;
        if(_pr57353Canonical?.Contains(candidate)!=true)
        {
            _pr57357TempReads++;return;
        }
        _pr57357CanonicalReads++;
        if(candidate.SitePath.Count>0)_pr57357NonemptyReads++;
        if(OnSitePath(candidate))_pr57357OnSiteReads++;
        _pr57357Readers.Add(reader.Id);
        if(!_pr57357ReadersByCandidate.TryGetValue(candidate.Id,out var readers))
        {
            readers=new HashSet<int>();
            _pr57357ReadersByCandidate.Add(candidate.Id,readers);
        }
        if(readers.Add(reader.Id))_pr57357UniquePairs++;
    }
    void Pr57357EndSlice()
    {
        if(!_pr57357Active)return;
        var slice=_pr57ExclusiveEligibleSlices;
        if(_pr57357Start!=null)
        {
            var end=Pr57357CaptureFleet();
            Pr57357Compare(_pr57357Start,end,true);
            _pr57357PreviousEnd=end;_pr57357LastEndSlice=slice;
        }
        if(slice!=_pr57357Window+2)return;
        Vehicle.Pr57357PathWrite=null;
        _pr57357Active=false;
        var maxFanout=_pr57357ReadersByCandidate.Count==0?0:
            _pr57357ReadersByCandidate.Values.Max(x=>x.Count);
        var hookMs=_pr57357HookTicks*1000.0/
            System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine("PR57_357_SITE window="+_pr57357Window+
            " readCalls="+_pr57357ReadCalls+
            " canonicalReads="+_pr57357CanonicalReads+
            " tempReads="+_pr57357TempReads+
            " nonemptyReads="+_pr57357NonemptyReads+
            " onSiteReads="+_pr57357OnSiteReads+
            " uniqueReaders="+_pr57357Readers.Count+
            " uniqueCandidates="+_pr57357ReadersByCandidate.Count+
            " uniquePairs="+_pr57357UniquePairs+
            " maxFanout="+maxFanout+
            " assignments="+_pr57357Assignments+
            " canonicalAssignments="+_pr57357CanonicalAssignments+
            " tempAssignments="+_pr57357TempAssignments+
            " potentialFanout="+_pr57357PotentialFanout+
            " snapshotPairs="+_pr57357SnapshotPairs+
            " withinChanges="+_pr57357WithinChanges+
            " betweenChanges="+_pr57357BetweenChanges+
            " onSiteFlips="+_pr57357OnSiteFlips+
            " hookMs="+hookMs.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+
            " scope=CANONICAL_BODY_ACROSS_LANE_EARLY_GUARD"+
            " inPlaceTransientWritesUnseen=True otherDependenciesUnclosed=True"+
            " noSkippedSlices=True observational=True intrusive=True");
        _pr57357Start=null;_pr57357PreviousEnd=null;
        _pr57357ReadersByCandidate.Clear();_pr57357Readers.Clear();
    }
}
''')
print("PR57_357_INJECTED bounded SitePath read exposure; no production changes")
