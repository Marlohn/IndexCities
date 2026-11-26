#!/usr/bin/env python3
"""PR57 #417: exact-site, opt-in tow-target writer/read census.
Only a disposable CI checkout is modified. Canonical decisions are untouched.
"""
from pathlib import Path

def edit(path, pairs):
    p=Path(path)
    s=p.read_text()
    for old,new in pairs:
        count=s.count(old)
        assert count==1, (path,old[:100],count)
        s=s.replace(old,new,1)
    p.write_text(s)

edit("src/IndexCities.Simulation/CitySimulation.cs", [
    ('    void Step(double dt)\n    {\n        if(Pr57Writer289Enabled)',
     '    void Step(double dt)\n    {\n        if(Pr57417Enabled)Pr57417StartStep();\n        if(Pr57Writer289Enabled)'),
    ('        MoveVehicles(dt);\n        Pr57310Boundary("vehicles");',
     '        MoveVehicles(dt);\n        if(Pr57417Enabled&&_pr57417Step==10800)Pr57417Summary();\n        Pr57310Boundary("vehicles");')
])
edit("src/IndexCities.Simulation/Mobility.cs", [
    ('    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)\n    {\n        Pr57SetupGapStart(dt,operationsDt);',
     '    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)\n    {\n        if(Pr57417Enabled)Pr57417StartPass(operationsDt);\n        Pr57SetupGapStart(dt,operationsDt);'),
    ('                // Lot manoeuvres, service queues, exterior service and calls for parked broken vehicles.\n                if((v.ServicePhase!=ServicePhase.None||v.ServiceShopId!=null||v.PushPath.Count>0||v.Status==VehicleStatus.Outside)&&TendService(v,operationTime))continue;',
     '                // Lot manoeuvres, service queues, exterior service and calls for parked broken vehicles.\n                if(Pr57417Enabled)Pr57417ServiceGate(v);\n                if((v.ServicePhase!=ServicePhase.None||v.ServiceShopId!=null||v.PushPath.Count>0||v.Status==VehicleStatus.Outside)&&TendService(v,operationTime))continue;')
])
edit("src/IndexCities.Simulation/Towing.cs", [
    ('        Pr57DirtyTouch(t);\n        if(tow.TowPhase==TowPhase.ToVehicle){',
     '        Pr57DirtyTouch(t);\n        if(Pr57417Enabled)Pr57417TowArrival(tow,t);\n        if(tow.TowPhase==TowPhase.ToVehicle){'),
    ('t.PushHeading=tow.Heading;t.ServiceBay=null;}',
     't.PushHeading=tow.Heading;t.ServiceBay=null;if(Pr57417Enabled)Pr57417TowWrite(tow,t);}')
])
Path("src/IndexCities.Simulation/Mobility.TowWriter417.Pr57.cs").write_text(r'''namespace IndexCities.Simulation;

// PR57 #417: observational exact writer/service-gate census.
// Only same/next Step; no cache, skipping, or canonical decision changes.
public sealed partial class CitySimulation
{
    static readonly bool Pr57417Enabled=
        Environment.GetEnvironmentVariable("PR57_417_TOW_WRITER_READ")=="1";
    readonly Dictionary<int,(long Step,long Pass,int TowId,string Mode)> _pr57417Writers=new();
    long _pr57417Step,_pr57417Pass,_pr57417Unsplit,_pr57417Operations,_pr57417Physical;
    long _pr57417Arrivals,_pr57417ArrivalsParked,_pr57417ArrivalsPhysical;
    long _pr57417Writes,_pr57417WritesUnsplit,_pr57417WritesPhysical;
    long _pr57417GateReads,_pr57417SameStepReads,_pr57417SamePassReads;
    long _pr57417NextStepReads,_pr57417Witnesses;
    string _pr57417Mode="none";

    void Pr57417StartStep(){_pr57417Step++;}
    void Pr57417StartPass(double? operationsDt)
    {
        _pr57417Pass++;
        _pr57417Mode=operationsDt!=null?"operations":
            _vehicleMovementBatch==null?"unsplit":"physical";
        switch(_pr57417Mode)
        {
            case "operations":_pr57417Operations++;break;
            case "physical":_pr57417Physical++;break;
            default:_pr57417Unsplit++;break;
        }
    }
    void Pr57417TowArrival(Vehicle tow,Vehicle? target)
    {
        _pr57417Arrivals++;
        if(target?.Status==VehicleStatus.Parked)_pr57417ArrivalsParked++;
        if(_pr57417Mode=="physical")_pr57417ArrivalsPhysical++;
    }
    void Pr57417TowWrite(Vehicle tow,Vehicle target)
    {
        _pr57417Writes++;
        if(_pr57417Mode=="physical")_pr57417WritesPhysical++;
        if(_pr57417Mode=="unsplit")_pr57417WritesUnsplit++;
        _pr57417Writers[target.Id]=(_pr57417Step,_pr57417Pass,tow.Id,_pr57417Mode);
        if(_pr57417Witnesses<12)
        {
            _pr57417Witnesses++;
            Console.WriteLine("PR57_417_WRITE step="+_pr57417Step+
                " pass="+_pr57417Pass+" mode="+_pr57417Mode+
                " tow="+tow.Id+" target="+target.Id+
                " pushCount="+target.PushPath.Count+
                " bay="+(target.ServiceBay?.ToString()??"null"));
        }
    }
    void Pr57417ServiceGate(Vehicle target)
    {
        _pr57417GateReads++;
        if(!_pr57417Writers.TryGetValue(target.Id,out var w))return;
        if(w.Step==_pr57417Step)
        {
            _pr57417SameStepReads++;
            if(w.Pass==_pr57417Pass)_pr57417SamePassReads++;
        }
        else if(w.Step==_pr57417Step-1)_pr57417NextStepReads++;
        else return;
        if(_pr57417Witnesses<12)
        {
            _pr57417Witnesses++;
            Console.WriteLine("PR57_417_READ step="+_pr57417Step+
                " pass="+_pr57417Pass+" mode="+_pr57417Mode+
                " target="+target.Id+" writerStep="+w.Step+
                " writerPass="+w.Pass+" writerMode="+w.Mode+
                " pushCount="+target.PushPath.Count+
                " bay="+(target.ServiceBay?.ToString()??"null"));
        }
    }
    void Pr57417Summary()
    {
        Console.WriteLine("PR57_417_SUMMARY steps="+_pr57417Step+
            " passes="+_pr57417Pass+" unsplit="+_pr57417Unsplit+
            " operations="+_pr57417Operations+" physical="+_pr57417Physical+
            " arrivals="+_pr57417Arrivals+" arrivalsParked="+_pr57417ArrivalsParked+
            " arrivalsPhysical="+_pr57417ArrivalsPhysical+
            " writes="+_pr57417Writes+" writesUnsplit="+_pr57417WritesUnsplit+
            " writesPhysical="+_pr57417WritesPhysical+
            " gateReads="+_pr57417GateReads+
            " sameStepReads="+_pr57417SameStepReads+
            " samePassReads="+_pr57417SamePassReads+
            " nextStepReads="+_pr57417NextStepReads+
            " witnesses="+_pr57417Witnesses+
            " observational=True causalClosure=False");
    }
}
''')
print("PR57_417_INJECTED observational=True canonicalUnchanged=True")
