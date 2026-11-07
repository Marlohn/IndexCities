#!/usr/bin/env python3
"""PR57 #394: exact path-content shadow at sequential physical actor boundaries.

Sample 600 physical calls distributed across first 12000 dt>0 calls of
the authentic REAL 14h replay. Capture Route/SitePath and admission contents,
not just lengths/references. Compare canonical live candidates without using
the shadow for decisions. No skip, reorder, save or product changes.
"""
from pathlib import Path

p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    assert s.count(old)==1,(old,s.count(old))
    s=s.replace(old,new,1)

patch("public sealed partial class CitySimulation\n{", """public sealed partial class CitySimulation
{
    static readonly bool Pr57394Enabled=Environment.GetEnvironmentVariable("PR57_394_PATH_SHADOW")=="1";
    sealed class Pr57394Path
    {
        readonly int[] route,admissionRoute;
        readonly Point[] sitePath,admissionSite;
        readonly bool hasAdmission, reachesDock;
        readonly int endIndex;
        public Pr57394Path(Vehicle v)
        {
            route=v.Route.ToArray(); sitePath=v.SitePath.ToArray();
            var a=v.MovementAdmission;
            hasAdmission=a!=null;
            admissionRoute=a?.Route.ToArray()??Array.Empty<int>();
            admissionSite=a?.SitePath.ToArray()??Array.Empty<Point>();
            endIndex=a?.EndRouteIndex??-1;
            reachesDock=a?.ReachesDock??false;
        }
        public bool RouteSame(Vehicle v)=>route.SequenceEqual(v.Route);
        public bool SiteSame(Vehicle v)=>sitePath.SequenceEqual(v.SitePath);
        public bool AdmissionSame(Vehicle v)
        {
            var a=v.MovementAdmission;
            if(hasAdmission!=(a!=null))return false;
            return a==null||endIndex==a.EndRouteIndex&&
                reachesDock==a.ReachesDock&&
                admissionRoute.SequenceEqual(a.Route)&&admissionSite.SequenceEqual(a.SitePath);
        }
        public bool Same(Vehicle v)=>RouteSame(v)&&SiteSame(v)&&AdmissionSame(v);
    }
    readonly Dictionary<int,Pr57394Path> _pr57394Current=new(),_pr57394Previous=new();
    long _pr57394Physical,_pr57394Samples,_pr57394Initial,_pr57394Publications,
        _pr57394ChangedActors,_pr57394Between,_pr57394PostSync,
        _pr57394Reads,_pr57394Missing,_pr57394Stale,_pr57394Self,_pr57394Cross,
        _pr57394CrossRoute,_pr57394CrossSite,_pr57394CrossAdmission,
        _pr57394InitialTicks,_pr57394PublishTicks,_pr57394EndTicks;
""")
patch("        Pr57SetupGapEnd(ordered.Length,pr57ScanCandidates.Count);", """        Pr57SetupGapEnd(ordered.Length,pr57ScanCandidates.Count);
        if(Pr57394Enabled&&dt>0)_pr57394Physical++;
        var pr57394Sample=Pr57394Enabled&&dt>0&&_pr57394Physical<=12000&&_pr57394Physical%20==0;
        if(pr57394Sample){
            _pr57394Samples++;
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            _pr57394Current.Clear();
            foreach(var actor in State.Vehicles){
                if(_pr57394Previous.TryGetValue(actor.Id,out var prior)&&!prior.Same(actor))
                    _pr57394Between++;
                _pr57394Current.Add(actor.Id,new Pr57394Path(actor));
                _pr57394Initial++;
            }
            _pr57394InitialTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        }""")
patch("                foreach(var liveOther in candidates){", """                foreach(var liveOther in candidates){
                    if(pr57394Sample){
                        _pr57394Reads++;
                        if(!_pr57394Current.TryGetValue(liveOther.Id,out var published))
                            _pr57394Missing++;
                        else if(!published.Same(liveOther)){
                            _pr57394Stale++;
                            if(liveOther.Id==v.Id)_pr57394Self++;
                            else{
                                _pr57394Cross++;
                                if(!published.RouteSame(liveOther))_pr57394CrossRoute++;
                                if(!published.SiteSame(liveOther))_pr57394CrossSite++;
                                if(!published.AdmissionSame(liveOther))_pr57394CrossAdmission++;
                                if(_pr57394Cross<=6)
                                    Console.WriteLine($"PR57_394_CROSS physical={_pr57394Physical} reader={v.Id} candidate={liveOther.Id} route={!published.RouteSame(liveOther)} site={!published.SiteSame(liveOther)} admission={!published.AdmissionSame(liveOther)}");
                            }
                        }
                    }""")
patch("            }finally{\n                Pr57Effect253AfterActor(v);", """            }finally{
                if(pr57394Sample){
                    var start=System.Diagnostics.Stopwatch.GetTimestamp();
                    if(_pr57394Current.TryGetValue(v.Id,out var prior)&&!prior.Same(v))
                        _pr57394ChangedActors++;
                    _pr57394Current[v.Id]=new Pr57394Path(v);
                    _pr57394Publications++;
                    _pr57394PublishTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
                }
                Pr57Effect253AfterActor(v);""")
patch("        SyncTowedVehicles();", """        SyncTowedVehicles();
        if(pr57394Sample){
            var start=System.Diagnostics.Stopwatch.GetTimestamp();
            _pr57394Previous.Clear();
            foreach(var actor in State.Vehicles){
                if(_pr57394Current.TryGetValue(actor.Id,out var prior)&&!prior.Same(actor))
                    _pr57394PostSync++;
                _pr57394Previous[actor.Id]=new Pr57394Path(actor);
            }
            _pr57394EndTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
            if(_pr57394Samples==600)
                Console.WriteLine($"PR57_394_PATH physical={_pr57394Physical} samples={_pr57394Samples} initial={_pr57394Initial} published={_pr57394Publications} changedActors={_pr57394ChangedActors} betweenSamples={_pr57394Between} postSync={_pr57394PostSync} reads={_pr57394Reads} missing={_pr57394Missing} stale={_pr57394Stale} self={_pr57394Self} cross={_pr57394Cross} crossRoute={_pr57394CrossRoute} crossSite={_pr57394CrossSite} crossAdmission={_pr57394CrossAdmission} initialTicks={_pr57394InitialTicks} publishTicks={_pr57394PublishTicks} endTicks={_pr57394EndTicks} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True noDecisionReuse=True noSkippedSlices=True");
        }""")
p.write_text(s,encoding="utf-8")
print("PR57_394_INJECTED exactRouteAndAdmissionShadow=True observational=True")
