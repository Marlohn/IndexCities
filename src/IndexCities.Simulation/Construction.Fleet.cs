namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    public static TruckBody BodyFor(ResourceKind resource)=>resource switch{
        ResourceKind.Aggregate or ResourceKind.Asphalt=>TruckBody.Dump,
        ResourceKind.Concrete=>TruckBody.Mixer,ResourceKind.Steel or ResourceKind.Wood=>TruckBody.Flatbed,_=>TruckBody.Cargo};

    Vehicle? AvailableTruck(ResourceKind? resource=null)
    {
        // Specialist bodies share the pre-existing finite dispatch capacity.
        // Extra parked supplier bodies do not multiply drivers or active trips.
        var trucks=State.Vehicles.Where(v=>v.External&&!v.PassengerCar&&v.Machine==PavingMachine.None&&v.HomeBuildingId==null).ToList();
        if(trucks.Count(v=>v.Status!=VehicleStatus.Outside)>=State.Tuning.ExternalTruckCount)return null;
        return trucks.FirstOrDefault(v=>v.Status==VehicleStatus.Outside&&(resource==null||v.Body==BodyFor(resource.Value)));
    }

    void MigrateConstructionState()
    {
        if(State.ConstructionStateVersion>=1)return;
        var bodies=new[]{TruckBody.Dump,TruckBody.Mixer,TruckBody.Flatbed,TruckBody.Cargo,TruckBody.Dump,TruckBody.Cargo};var i=0;
        foreach(var v in State.Vehicles.Where(v=>!v.PassengerCar&&v.Machine==PavingMachine.None)){
            var cargo=v.Cargo.Count>0?v.Cargo:State.Shipments.LastOrDefault(s=>s.VehicleId==v.Id&&s.Cargo.Count>0)?.Cargo;
            v.Body=cargo is {Count:>0}?BodyFor(cargo.First().Key):v.External&&v.HomeBuildingId==null?bodies[i++%bodies.Length]:TruckBody.Cargo;
        }
        foreach(var body in Enum.GetValues<TruckBody>()){
            var count=State.Vehicles.Count(v=>v.External&&v.HomeBuildingId==null&&!v.PassengerCar&&v.Machine==PavingMachine.None&&v.Body==body);
            for(;count<State.Tuning.ExternalTruckCount;count++)
                State.Vehicles.Add(new(){Id=Id(),Body=body,External=true,ExternalDriverId=$"motorista externo especializado {body} {count+1}",Position=State.Entry,Status=VehicleStatus.Outside});
        }
        foreach(var p in State.Projects.Where(IsPaving)){
            p.AppliedRoadPoints=p.RoadPoints.Where(point=>State.Roads.Any(r=>p.BuiltRoadNodes.Contains(r.Id)&&r.Position==point)).ToList();
            p.RolledRoadPoints=p.AppliedRoadPoints.ToList();p.PavingVersion=1;
            if(p.Status is WorkStatus.Completed or WorkStatus.Cancelled)continue;
            p.MachineryQuoted=QuoteMachinery(p.Position,p.WorkSeconds);
            // Real uncommitted city money only; never mint money to migrate a work.
            p.BudgetRemaining+=Math.Min(Available,p.MachineryQuoted);
        }
        State.ConstructionStateVersion=1;
    }

    RoadNode? ConstructionAccess(Building b)=>State.Roads.Where(r=>!r.Highway&&r.MaintenanceRatio>.05&&_workExteriorConnected.Contains(r.Id)&&r.Position.Distance(b.Door)<=State.Tuning.RoadWidth/2+3).MinBy(r=>r.Position.Distance(b.Door));

    bool ConstructionTrip(Vehicle v)=>v.ConstructionAccess||v.Machine!=PavingMachine.None||v.ShipmentId is {} id&&State.Shipments.Any(s=>s.Id==id&&s.Purpose is ShipmentPurpose.Construction or ShipmentPurpose.Maintenance);
}
