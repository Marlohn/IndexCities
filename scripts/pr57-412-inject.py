#!/usr/bin/env python3
"""#412 independent per-slice scalar projection, observational, disposable checkout.

Run AFTER #408 and #410. Publishes only the completed actor intentionally to
falsify actor-boundary sufficiency. No canonical decision or slice is replaced.
"""
from pathlib import Path
p=Path('src/IndexCities.Simulation/Mobility.cs')
s=p.read_text()
def once(old,new):
    global s
    n=s.count(old)
    assert n==1,(old[:110],n)
    s=s.replace(old,new,1)

once('    static readonly bool Pr57410Enabled=Environment.GetEnvironmentVariable("PR57_410_SCALAR_SHADOW")=="1";', '''    static readonly bool Pr57412Enabled=Environment.GetEnvironmentVariable("PR57_412_SCALAR_PROJECTION")=="1";
    readonly record struct Pr57412Scalar(int Id,VehicleStatus Status,Point Position,double Heading);
    static Pr57412Scalar Pr57412Capture(Vehicle o)=>new(o.Id,o.Status,o.Position,o.Heading);
    static bool Pr57412Same(Pr57412Scalar a,Pr57412Scalar b)=>a.Id==b.Id&&a.Status==b.Status&&
        BitConverter.DoubleToInt64Bits(a.Position.X)==BitConverter.DoubleToInt64Bits(b.Position.X)&&
        BitConverter.DoubleToInt64Bits(a.Position.Z)==BitConverter.DoubleToInt64Bits(b.Position.Z)&&
        BitConverter.DoubleToInt64Bits(a.Heading)==BitConverter.DoubleToInt64Bits(b.Heading);
    readonly Dictionary<int,Pr57412Scalar> _pr57412Current=new(),_pr57412Entry=new();
    long _pr57412Snapshots,_pr57412Published,_pr57412Scans,_pr57412CandidateReads,_pr57412StaleCross,_pr57412StaleSelf,_pr57412Missing;
    long _pr57412BuildTicks,_pr57412PublishTicks,_pr57412Ordinary,_pr57412OrdinaryExact,_pr57412OrdinarySameId;
    long _pr57412DirectMismatch,_pr57412CanonicalStepMismatch,_pr57412Witnesses;
    static readonly bool Pr57410Enabled=Environment.GetEnvironmentVariable("PR57_410_SCALAR_SHADOW")=="1";''')

once('        Array.Sort(ordered,static (a,b)=>a.WaitingSince.CompareTo(b.WaitingSince) is var c and not 0?c:a.Id.CompareTo(b.Id));', '''        Array.Sort(ordered,static (a,b)=>a.WaitingSince.CompareTo(b.WaitingSince) is var c and not 0?c:a.Id.CompareTo(b.Id));
        var pr57412Sample=Pr57412Enabled&&pr57408Sample;
        if(pr57412Sample){
            var t=System.Diagnostics.Stopwatch.GetTimestamp();
            _pr57412Current.Clear();_pr57412Entry.Clear();
            foreach(var o in State.Vehicles){
                _pr57412Current[o.Id]=Pr57412Capture(o);
                _pr57412Entry[o.Id]=Pr57412Capture(EntryFrameVehicle(o));
                _pr57412Snapshots++;
            }
            _pr57412BuildTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
        }''')

once('                var pr57410Distance=double.MaxValue;', '''                var pr57412Distance=double.MaxValue;
                var pr57412Id=-1;
                void Pr57412Consider(Pr57412Scalar other){
                    if(other.Id==v.Id||other.Status is not (VehicleStatus.Travelling or VehicleStatus.Waiting or VehicleStatus.Stranded))return;
                    var relative=other.Position-v.Position;
                    var sn=pr57FastScan?pr57Sin:Math.Sin(v.Heading);
                    var cs=pr57FastScan?pr57Cos:Math.Cos(v.Heading);
                    var forward=relative.X*sn+relative.Z*cs;
                    if(!(forward>0))return;
                    var side=Math.Abs(relative.X*cs-relative.Z*sn);
                    if(!(side<2.6))return;
                    if(!(Math.Cos(other.Heading-v.Heading)>.5))return;
                    var back=-relative.X*Math.Sin(other.Heading)-relative.Z*Math.Cos(other.Heading);
                    var backSide=Math.Abs(-relative.X*Math.Cos(other.Heading)+relative.Z*Math.Sin(other.Heading));
                    if(back>0&&backSide<2.6&&other.Id>v.Id&&other.Status!=VehicleStatus.Stranded)return;
                    if(forward<pr57412Distance){pr57412Distance=forward;pr57412Id=other.Id;}
                }
                var pr57410Distance=double.MaxValue;''')

once('                    if(pr57410Sample)Pr57410Consider(other);', '''                    if(pr57410Sample)Pr57410Consider(other);
                    if(pr57412Sample){
                        _pr57412CandidateReads++;
                        var table=enteringLane?_pr57412Entry:_pr57412Current;
                        if(!table.TryGetValue(liveOther.Id,out var projected)){
                            _pr57412Missing++;projected=Pr57412Capture(other);
                        }
                        if(!Pr57412Same(projected,Pr57412Capture(other))){
                            if(liveOther.Id==v.Id)_pr57412StaleSelf++;
                            else {
                                _pr57412StaleCross++;
                                if(_pr57412Witnesses++<12)
                                    Console.WriteLine($"PR57_412_STALE scan={_pr57412Scans+1} reader={v.Id} candidate={liveOther.Id} entering={enteringLane} projectedStatus={projected.Status} liveStatus={other.Status} projectedX={projected.Position.X:R} liveX={other.Position.X:R} projectedZ={projected.Position.Z:R} liveZ={other.Position.Z:R} projectedHeading={projected.Heading:R} liveHeading={other.Heading:R} scope=ACTOR_ONLY_PUBLICATION");
                            }
                        }
                        Pr57412Consider(projected);
                    }''')

once('                if(pr57410Sample){\n                    _pr57410Scans++;', '''                if(pr57412Sample){
                    _pr57412Scans++;
                    var nominal=Math.Min(d,speed*remaining);
                    var projectionStep=Math.Min(nominal,Math.Max(0,pr57412Distance-8));
                    var canonicalStep=Math.Min(nominal,Math.Max(0,leaderDistance-8));
                    if(pr57412Id!=pr57410Id||BitConverter.DoubleToInt64Bits(pr57412Distance)!=BitConverter.DoubleToInt64Bits(pr57410Distance)){
                        _pr57412DirectMismatch++;
                        if(_pr57412Witnesses++<12)
                            Console.WriteLine($"PR57_412_MISMATCH scan={_pr57412Scans} reader={v.Id} entering={enteringLane} directId={pr57410Id} projectedId={pr57412Id} directDistance={pr57410Distance:R} projectedDistance={pr57412Distance:R} cause={pr57408Cause} scope=SCALAR_ONLY");
                    }
                    if(BitConverter.DoubleToInt64Bits(projectionStep)!=BitConverter.DoubleToInt64Bits(canonicalStep))_pr57412CanonicalStepMismatch++;
                    if(blocker is not null&&pr57408Cause==6&&Math.Max(0,leaderDistance-8)<nominal){
                        _pr57412Ordinary++;
                        if(pr57412Id==blocker.Id){
                            _pr57412OrdinarySameId++;
                            if(BitConverter.DoubleToInt64Bits(pr57412Distance)==BitConverter.DoubleToInt64Bits(leaderDistance))_pr57412OrdinaryExact++;
                        }
                    }
                }
                if(pr57410Sample){
                    _pr57410Scans++;''')

once('                Pr57ActorShadowAfterActor(v);', '''                Pr57ActorShadowAfterActor(v);
                if(pr57412Sample){
                    var t=System.Diagnostics.Stopwatch.GetTimestamp();
                    _pr57412Current[v.Id]=Pr57412Capture(v);
                    if(_entryFrame==null||!_entryFrame.ContainsKey(v.Id))_pr57412Entry[v.Id]=Pr57412Capture(v);
                    _pr57412Published++;
                    _pr57412PublishTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-t;
                }''')

once('        if(pr57408Sample&&_pr57408Physical==600&&Pr57410Enabled)', '''        if(pr57412Sample&&_pr57408Physical==600){
            double ms(long ticks)=>ticks*1000.0/System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine($"PR57_412_SUMMARY physical={_pr57408Physical} scans={_pr57412Scans} snapshots={_pr57412Snapshots} publications={_pr57412Published} candidateReads={_pr57412CandidateReads} staleCross={_pr57412StaleCross} staleSelf={_pr57412StaleSelf} missing={_pr57412Missing} ordinary={_pr57412Ordinary} ordinarySameId={_pr57412OrdinarySameId} ordinaryExact={_pr57412OrdinaryExact} directMismatch={_pr57412DirectMismatch} canonicalStepMismatch={_pr57412CanonicalStepMismatch} buildMs={ms(_pr57412BuildTicks):F6} publishMs={ms(_pr57412PublishTicks):F6} witnesses={Math.Min(12,_pr57412Witnesses)} observational=True independentScalarProjection=True actorOnlyPublication=True causalClosure=False");
        }
        if(pr57408Sample&&_pr57408Physical==600&&Pr57410Enabled)''')

p.write_text(s)
print('PR57_412_INJECTED observational=True independentScalarProjection=True actorOnlyPublication=True')
