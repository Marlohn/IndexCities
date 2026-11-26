#!/usr/bin/env python3
"""PR57 #509: disposable observation of two negative-guard transitions at one real writer.

This is NOT a causal certificate, optimization, complete writer journal, or proof
of parallel safety. Captures actual BodyOnWay/TrafficOnWay reader radii within
one physical sub-slice; at the canonical position+WaitingSince writer, tests
whether a previously excluded candidate becomes eligible for a still-stable
reader. No global slice is skipped and no cache crosses Advance(2).
"""
from pathlib import Path
root = Path('src/IndexCities.Simulation')
shared = root / 'Mobility.SharedSpace.cs'
mobility = root / 'Mobility.cs'

def within(s, start, end, old, new):
    a = s.index(start); b = s.index(end, a + len(start)); section = s[a:b]
    assert section.count(old) == 1, (start, old, section.count(old))
    return s[:a] + section.replace(old, new, 1) + s[b:]

s = shared.read_text()
s = within(s, '    Vehicle? BodyOnWay(', '    Vehicle? TrafficOnWay(',
    'var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;',
    'var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;Pr57509Subscribe(v,0,reach);')
s = within(s, '    Vehicle? TrafficOnWay(', '    List<(Vehicle Body,Point Position,double Heading)> TurnWay(',
    'var seconds=length/State.Tuning.VehicleSpeed+1;',
    'var seconds=length/State.Tuning.VehicleSpeed+1;Pr57509Subscribe(v,1,seconds*State.Tuning.HighwaySpeed+20);')
shared.write_text(s)
s = mobility.read_text()
anchor = '        Pr57Man248BeginSlice(dt,operationsDt,scanProfile is not null);'
assert s.count(anchor) == 1
s = s.replace(anchor, anchor+'\n        Pr57509BeginSlice(dt,operationsDt);', 1)
anchor = '        // A towed vehicle moves only with its tow (platform or trailing a heavy tow).'
assert s.count(anchor) == 1
s = s.replace(anchor, '        Pr57509EndSlice();\n'+anchor, 1)
anchor = 'step=Math.Min(step,v.Fuel/FuelRate(v));v.Position=v.Position.MoveTowards(target,step);v.AddFuel(-(step*FuelRate(v)));v.FuelUnits=Math.Max(0,v.FuelUnits);v.AddFuelBurnedLastStep(step*FuelRate(v));remaining-=step/speed;v.WaitingSince=0;v.Cause="Em viagem";'
assert s.count(anchor) == 1
s = s.replace(anchor,
    'var pr57509OldPosition=v.Position;var pr57509OldWaiting=v.WaitingSince;var pr57509OldLeaving=Pr57509BeforeLeaving(v);\n'
    '                '+anchor+'Pr57509Writer(v,pr57509OldPosition,pr57509OldWaiting,pr57509OldLeaving);',1)
mobility.write_text(s)

probe = r'''namespace IndexCities.Simulation;
// PR57 #509: CI-only, writer-site bounded observational probe.
// Only the movement writer in Mobility.cs is tracked; other writers, transient
// edits, and readers not yet subscribed are outside the observation.
public sealed partial class CitySimulation
{
    static readonly bool Pr57509Enabled=Environment.GetEnvironmentVariable("PR57_509_WRITER_GUARD")=="1";
    readonly record struct Pr57509Subscription(Vehicle Reader,Point Position,int? TowTargetId,int Kind,double Reach);
    readonly List<Pr57509Subscription> _pr57509Subscriptions=new();
    readonly HashSet<(int Reader,int Candidate,int Kind)> _pr57509NewEligible=new();
    bool _pr57509Active;
    int _pr57509Slice;
    long _pr57509WriterChanges,_pr57509Checks,_pr57509StableChecks,_pr57509BodyFlips,_pr57509TrafficFlips;
    void Pr57509BeginSlice(double dt,double? operationsDt)
    {
        _pr57509Active=Pr57509Enabled&&_pr57509Slice<600&&dt>0&&
            operationsDt is null&&_vehicleMovementBatch is not null;
        if(!_pr57509Active)return;
        _pr57509Slice++;
        _pr57509Subscriptions.Clear();_pr57509NewEligible.Clear();
        _pr57509WriterChanges=0;_pr57509Checks=0;_pr57509StableChecks=0;
        _pr57509BodyFlips=0;_pr57509TrafficFlips=0;
    }
    void Pr57509Subscribe(Vehicle reader,int kind,double reach)
    {
        if(!_pr57509Active)return;
        _pr57509Subscriptions.Add(new(reader,reader.Position,reader.TowTargetId,kind,reach));
    }
    bool Pr57509BeforeLeaving(Vehicle candidate)=>_pr57509Active&&Leaving(candidate);
    void Pr57509Writer(Vehicle candidate,Point oldPosition,double oldWaiting,bool oldLeaving)
    {
        if(!_pr57509Active)return;
        if(candidate.Position.Distance(oldPosition)==0&&candidate.WaitingSince==oldWaiting)return;
        _pr57509WriterChanges++;
        var newLeaving=Leaving(candidate);
        var oldGateway=oldPosition.Distance(State.Entry)<8;
        var newGateway=AtGateway(candidate);
        foreach(var sub in _pr57509Subscriptions)
        {
            _pr57509Checks++;
            var reader=sub.Reader;
            if(reader.Position.Distance(sub.Position)!=0||reader.TowTargetId!=sub.TowTargetId)continue;
            _pr57509StableChecks++;
            bool oldEligible,newEligible;
            if(sub.Kind==0)
            {
                var fixedExcluded=candidate.Id==reader.Id||candidate.Id==reader.TowTargetId||
                    candidate.TowTruckId==reader.Id||candidate.Status is VehicleStatus.Outside or VehicleStatus.Towed;
                var readerGateway=AtGateway(reader);
                oldEligible=!fixedExcluded&&oldPosition.Distance(reader.Position)<=sub.Reach&&!(oldGateway&&readerGateway);
                newEligible=!fixedExcluded&&candidate.Position.Distance(reader.Position)<=sub.Reach&&!(newGateway&&readerGateway);
            }
            else
            {
                var fixedExcluded=candidate.Id==reader.Id||candidate.Id==reader.TowTargetId||
                    candidate.Status!=VehicleStatus.Travelling||candidate.Machine!=PavingMachine.None;
                oldEligible=!fixedExcluded&&oldWaiting<=0&&!oldLeaving&&oldPosition.Distance(reader.Position)<=sub.Reach;
                newEligible=!fixedExcluded&&candidate.WaitingSince<=0&&!newLeaving&&candidate.Position.Distance(reader.Position)<=sub.Reach;
            }
            if(!oldEligible&&newEligible&&_pr57509NewEligible.Add((reader.Id,candidate.Id,sub.Kind)))
            {
                if(sub.Kind==0)_pr57509BodyFlips++;
                else _pr57509TrafficFlips++;
            }
        }
    }
    void Pr57509EndSlice()
    {
        if(!_pr57509Active)return;
        if(_pr57509WriterChanges>0||_pr57509Slice==1||_pr57509Slice%97==0)
            Console.WriteLine($"PR57_509_WRITER_GUARD slice={_pr57509Slice} "+
                $"writerChanges={_pr57509WriterChanges} subscriptions={_pr57509Subscriptions.Count} "+
                $"guardChecks={_pr57509Checks} stableChecks={_pr57509StableChecks} "+
                $"bodyNegativeToEligible={_pr57509BodyFlips} trafficNegativeToEligible={_pr57509TrafficFlips} "+
                "scope=ONE_POSITION_WAITING_WRITER_PRIOR_SAME_SLICE_READERS_ONLY "+
                "notExactCertificate=True notAllWriters=True intrusive=True");
        _pr57509Active=false;
    }
}
'''
path=root/'Pr57509WriterGuard.CiOnly.cs'
assert not path.exists()
path.write_text(probe)
print('PR57_509_DISPOSABLE_WRITER_GUARD_INJECTION_PASS')
