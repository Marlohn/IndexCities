#!/usr/bin/env python3
"""PR57 #399: targeted canonical writer attribution without global path snapshots.

Apply ONLY to a disposable checkout. Observe exact Route contents for the 11
#397 gap witnesses around TryDrive and each market-followup submethod, plus
direct non-null MovementAdmission clears inside physical ReleaseManoeuvre.
Never changes decisions, state, order, persistence, or skips a slice.
"""
from pathlib import Path

root = Path("src/IndexCities.Simulation")
files = {
    name: (root / name).read_text(encoding="utf-8")
    for name in ("CitySimulation.cs", "Mobility.cs", "Mobility.SharedSpace.cs")
}

def patch(name, old, new):
    source = files[name]
    assert source.count(old) == 1, (name, old[:100], source.count(old))
    files[name] = source.replace(old, new, 1)

patch("CitySimulation.cs",
    '    static readonly bool Pr57CauseTransitionProbe=Environment.GetEnvironmentVariable("PR57_CAUSE_TRANSITION_PROBE")=="1";',
    """    static readonly bool Pr57399Enabled=Environment.GetEnvironmentVariable("PR57_399_WRITER_METHODS")=="1";
    // Eleven REAL vehicle IDs from the #397 physical-to-operational Route gaps.
    static readonly HashSet<int> Pr57399Targets=new()
        {1487,1380,655,695,1507,596,677,1282,689,1468,1386};
    readonly Dictionary<int,int[]> _pr57399TryBefore=new(),_pr57399FollowBefore=new();
    readonly Dictionary<string,long> _pr57399ByMethod=new(StringComparer.Ordinal);
    long _pr57399RouteChanges,_pr57399TryChanges,_pr57399ReleaseChanges;
    long _pr57399Physical,_pr57399Captures,_pr57399Comparisons,_pr57399Logs,_pr57399Ticks;
    int _pr57399Step;
    bool _pr57399InPhysicalCleanup;
    void Pr57399Snapshot(Dictionary<int,int[]> destination)
    {
        destination.Clear();
        foreach(var v in State.Vehicles)
            if(Pr57399Targets.Contains(v.Id))destination.Add(v.Id,v.Route.ToArray());
        _pr57399Captures++;
    }
    void Pr57399Compare(Dictionary<int,int[]> previous,string method)
    {
        _pr57399Comparisons++;
        foreach(var v in State.Vehicles)
        {
            if(!Pr57399Targets.Contains(v.Id))continue;
            if(previous.TryGetValue(v.Id,out var old)&&old.SequenceEqual(v.Route))continue;
            _pr57399RouteChanges++;
            _pr57399ByMethod[method]=_pr57399ByMethod.GetValueOrDefault(method)+1;
            if(method=="TryDrive")_pr57399TryChanges++;
            if(_pr57399Logs++<80)
                Console.WriteLine($"PR57_399_ROUTE step={_pr57399Step} physical={_pr57399Physical} method={method} vehicle={v.Id} previousCount={(previous.TryGetValue(v.Id,out var before)?before.Length:-1)} currentCount={v.Route.Count}");
        }
    }
    bool Pr57399TryDrive(Citizen c,Building destination,JourneyPurpose purpose)
    {
        if(!Pr57399Enabled||_pr57399Step>1800)return TryDrive(c,destination,purpose);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        Pr57399Snapshot(_pr57399TryBefore);
        var result=TryDrive(c,destination,purpose);
        Pr57399Compare(_pr57399TryBefore,"TryDrive");
        _pr57399Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        return result;
    }
    void Pr57399Followup(string method)
    {
        if(!Pr57399Enabled||_pr57399Step>1800)return;
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        Pr57399Compare(_pr57399FollowBefore,method);
        Pr57399Snapshot(_pr57399FollowBefore);
        _pr57399Ticks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
    }
    void Pr57399Release(Vehicle v)
    {
        _pr57399ReleaseChanges++;
        if(_pr57399Logs++<80)
            Console.WriteLine($"PR57_399_RELEASE physical={_pr57399Physical} vehicle={v.Id} status={v.Status} directAdmissionNull=True");
    }
    static readonly bool Pr57CauseTransitionProbe=Environment.GetEnvironmentVariable("PR57_CAUSE_TRANSITION_PROBE")=="1";""")

patch("CitySimulation.cs",
    "        Pr57WorkAccessBeginStep();\n        Pr57BeginStep();",
    """        Pr57WorkAccessBeginStep();
        Pr57BeginStep();
        if(Pr57399Enabled)_pr57399Step++;""")

patch("CitySimulation.cs",
    "        if(reviewEconomy){RetryPurchasesAwaitingDestination();RestockBusinesses();ReviewEmigration();RecoverDebt();RetryCargoAwaitingDestination();ReviewVehicleRevisions();ResumeHeldDeliveries();RefuelIdleShopTows();}",
    """        if(reviewEconomy){
            if(Pr57399Enabled&&_pr57399Step<=1800)Pr57399Snapshot(_pr57399FollowBefore);
            RetryPurchasesAwaitingDestination();Pr57399Followup("RetryPurchases");
            RestockBusinesses();Pr57399Followup("RestockBusinesses");
            ReviewEmigration();Pr57399Followup("ReviewEmigration");
            RecoverDebt();Pr57399Followup("RecoverDebt");
            RetryCargoAwaitingDestination();Pr57399Followup("RetryCargo");
            ReviewVehicleRevisions();Pr57399Followup("VehicleRevisions");
            ResumeHeldDeliveries();Pr57399Followup("ResumeHeldDeliveries");
            RefuelIdleShopTows();Pr57399Followup("RefuelIdleShopTows");
        }""")

patch("CitySimulation.cs",
    "        if(profile is not null)profile.FinishStep();",
    """        if(profile is not null)profile.FinishStep();
        if(Pr57399Enabled&&_pr57399Step==1800)
            Console.WriteLine($"PR57_399_METHODS steps={_pr57399Step} physical={_pr57399Physical} targets=11 tryDriveChanges={_pr57399TryChanges} routeChanges={_pr57399RouteChanges} releaseSoFar={_pr57399ReleaseChanges} captures={_pr57399Captures} comparisons={_pr57399Comparisons} methods={string.Join(',',_pr57399ByMethod.OrderBy(k=>k.Key).Select(k=>$"{k.Key}:{k.Value}"))} ticks={_pr57399Ticks} frequency={System.Diagnostics.Stopwatch.Frequency} observational=True noSkippedSlices=True");""")

patch("Mobility.cs",
    "        if(TryDrive(c,destination,purpose))return true;",
    "        if(Pr57399TryDrive(c,destination,purpose))return true;")

patch("Mobility.cs",
    "    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)\n    {\n        Pr57SetupGapStart(dt,operationsDt);",
    """    void MoveVehiclesUnsplit(double dt,double? operationsDt=null)
    {
        if(Pr57399Enabled&&dt>0)_pr57399Physical++;
        Pr57SetupGapStart(dt,operationsDt);""")

patch("Mobility.cs",
    "        foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}",
    """        _pr57399InPhysicalCleanup=Pr57399Enabled&&dt>0&&_pr57399Physical<=12000;
        foreach(var vehicle in State.Vehicles){ClearPassedJunction(vehicle);ReleaseManoeuvre(vehicle);}
        _pr57399InPhysicalCleanup=false;""")

patch("Mobility.cs",
    "        SyncTowedVehicles();",
    """        SyncTowedVehicles();
        if(Pr57399Enabled&&dt>0&&_pr57399Physical==12000)
            Console.WriteLine($"PR57_399_CLEANUP physical={_pr57399Physical} directAdmissionClears={_pr57399ReleaseChanges} observational=True noSkippedSlices=True");""")

patch("Mobility.SharedSpace.cs",
    '        if(v.Status is VehicleStatus.Outside or VehicleStatus.Towed||v.PassengerCar){v.ManoeuvreDock=null;v.DockCorridor.Clear();v.MovementAdmission=null;v.ManoeuvreSpaces.Clear();Pr57ReservationWrite(v,"release-clear");return;}',
    """        if(v.Status is VehicleStatus.Outside or VehicleStatus.Towed||v.PassengerCar){
            var pr57399HadAdmission=Pr57399Enabled&&_pr57399InPhysicalCleanup&&v.MovementAdmission!=null;
            v.ManoeuvreDock=null;v.DockCorridor.Clear();v.MovementAdmission=null;v.ManoeuvreSpaces.Clear();
            if(pr57399HadAdmission)Pr57399Release(v);
            Pr57ReservationWrite(v,"release-clear");return;}""")

for name, content in files.items():
    (root / name).write_text(content, encoding="utf-8")
print("PR57_399_INJECTED targetedMethods=True physicalRelease=True observational=True")
