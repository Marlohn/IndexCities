#!/usr/bin/env python3
"""PR57 #414: bounded per-actor nonlocal-write census on top of #412.

Run in disposable checkout AFTER #408, #410, #412. This is observational:
no canonical decision is replaced, and no global slice is skipped.
A hash of mutable integer lists is a witness filter, not causal proof.
"""
from pathlib import Path
p=Path("src/IndexCities.Simulation/Mobility.cs")
s=p.read_text()
def once(old,new):
    global s
    n=s.count(old)
    assert n==1,(old[:120],n)
    s=s.replace(old,new,1)

once('    static readonly bool Pr57412Enabled=Environment.GetEnvironmentVariable("PR57_412_SCALAR_PROJECTION")=="1";', '''    static readonly bool Pr57414Enabled=Environment.GetEnvironmentVariable("PR57_414_NONLOCAL_CENSUS")=="1";
    readonly record struct Pr57414Stamp(ulong Route,int RouteCount,int RouteIndex,
        ulong Spaces,int SpacesCount,int? Claim,bool HasAdmission);
    static ulong Pr57414Hash(IEnumerable<int> values){
        unchecked{
            ulong h=14695981039346656037UL;
            foreach(var value in values){h^=(uint)value;h*=1099511628211UL;}
            return h;
        }
    }
    static Pr57414Stamp Pr57414Capture(Vehicle v)=>new(
        Pr57414Hash(v.Route),v.Route.Count,v.RouteIndex,
        Pr57414Hash(v.ManoeuvreSpaces),v.ManoeuvreSpaces.Count,
        v.IntersectionClaim,v.MovementAdmission!=null);
    readonly Dictionary<int,Pr57414Stamp> _pr57414Current=new();
    readonly HashSet<int> _pr57414Seen=new();
    long _pr57414Actors,_pr57414Checks,_pr57414CrossScalar,_pr57414CrossExtra,
         _pr57414CrossEither,_pr57414Distinct,_pr57414PostSyncScalar,
         _pr57414PostSyncExtra,_pr57414Witnesses,_pr57414BuildTicks,
         _pr57414CheckTicks,_pr57414SyncTicks;
    static readonly bool Pr57412Enabled=Environment.GetEnvironmentVariable("PR57_412_SCALAR_PROJECTION")=="1";''')

once('            _pr57412BuildTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;\n        }', '''            _pr57412BuildTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
        }
        if(pr57412Sample&&Pr57414Enabled){
            var t=System.Diagnostics.Stopwatch.GetTimestamp();
            _pr57414Current.Clear();_pr57414Seen.Clear();
            foreach(var o in State.Vehicles)_pr57414Current[o.Id]=Pr57414Capture(o);
            _pr57414BuildTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
        }''')

once('                    _pr57412PublishTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;\n                }', '''                    _pr57412PublishTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
                }
                if(pr57412Sample&&Pr57414Enabled){
                    var t=System.Diagnostics.Stopwatch.GetTimestamp();
                    _pr57414Actors++;
                    _pr57414Current[v.Id]=Pr57414Capture(v);
                    foreach(var other in State.Vehicles){
                        if(other.Id==v.Id)continue;
                        _pr57414Checks++;
                        var scalarStale=!_pr57412Current.TryGetValue(other.Id,out var scalar)
                            ||!Pr57412Same(scalar,Pr57412Capture(other));
                        var extraStale=!_pr57414Current.TryGetValue(other.Id,out var extra)
                            ||!extra.Equals(Pr57414Capture(other));
                        if(scalarStale)_pr57414CrossScalar++;
                        if(extraStale)_pr57414CrossExtra++;
                        if(scalarStale||extraStale){
                            _pr57414CrossEither++;
                            if(_pr57414Seen.Add(other.Id))_pr57414Distinct++;
                            if(_pr57414Witnesses<12){
                                _pr57414Witnesses++;
                                Console.WriteLine($"PR57_414_WITNESS phase=actor physical={_pr57408Physical} actor={v.Id} target={other.Id} scalar={scalarStale} extra={extraStale} routeCount={other.Route.Count} claim={other.IntersectionClaim} admission={other.MovementAdmission!=null} scope=POST_ACTOR_NET");
                            }
                        }
                    }
                    _pr57414CheckTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
                }''')

once('        SyncTowedVehicles();\n        Pr57Transitions290End(dt<=0?0:1);', '''        SyncTowedVehicles();
        if(pr57412Sample&&Pr57414Enabled){
            var t=System.Diagnostics.Stopwatch.GetTimestamp();
            foreach(var other in State.Vehicles){
                var scalarStale=!_pr57412Current.TryGetValue(other.Id,out var scalar)
                    ||!Pr57412Same(scalar,Pr57412Capture(other));
                var extraStale=!_pr57414Current.TryGetValue(other.Id,out var extra)
                    ||!extra.Equals(Pr57414Capture(other));
                if(scalarStale)_pr57414PostSyncScalar++;
                if(extraStale)_pr57414PostSyncExtra++;
                if((scalarStale||extraStale)&&_pr57414Witnesses<12){
                    _pr57414Witnesses++;
                    Console.WriteLine($"PR57_414_WITNESS phase=postSync physical={_pr57408Physical} actor=-1 target={other.Id} scalar={scalarStale} extra={extraStale} routeCount={other.Route.Count} claim={other.IntersectionClaim} admission={other.MovementAdmission!=null} scope=POST_SYNC_NET");
                }
            }
            _pr57414SyncTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
        }
        Pr57Transitions290End(dt<=0?0:1);''')

once('        if(pr57412Sample&&_pr57408Physical==600){', '''        if(pr57412Sample&&Pr57414Enabled&&_pr57408Physical==600){
            double ms(long ticks)=>ticks*1000.0/System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine($"PR57_414_SUMMARY physical={_pr57408Physical} actors={_pr57414Actors} checks={_pr57414Checks} crossScalar={_pr57414CrossScalar} crossExtra={_pr57414CrossExtra} crossEither={_pr57414CrossEither} distinctPerSlice={_pr57414Distinct} postSyncScalar={_pr57414PostSyncScalar} postSyncExtra={_pr57414PostSyncExtra} witnesses={_pr57414Witnesses} buildMs={ms(_pr57414BuildTicks):F6} checksMs={ms(_pr57414CheckTicks):F6} syncMs={ms(_pr57414SyncTicks):F6} observational=True netBoundaryOnly=True causalClosure=False routeHashWitnessOnly=True");
        }
        if(pr57412Sample&&_pr57408Physical==600){''')

p.write_text(s)
print("PR57_414_INJECTED observational=True netBoundaryOnly=True noSkippedSlices=True")
