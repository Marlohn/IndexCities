namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    public static TruckBody BodyFor(ResourceKind resource)=>resource switch{
        ResourceKind.Aggregate or ResourceKind.Asphalt=>TruckBody.Dump,
        ResourceKind.Concrete=>TruckBody.Mixer,ResourceKind.Steel or ResourceKind.Wood=>TruckBody.Flatbed,_=>TruckBody.Cargo};

    Vehicle? AvailableTruck(ResourceKind? resource=null)
    {
        // Fixed specialist vehicles; the original fleet count is unchanged.
        var trucks=State.Vehicles.Where(v=>v.External&&!v.PassengerCar&&v.Machine==PavingMachine.None&&v.HomeBuildingId==null).ToList();
        if(trucks.Count(v=>v.Status!=VehicleStatus.Outside)>=State.Tuning.ExternalTruckCount)return null;
        return trucks.FirstOrDefault(v=>v.Status==VehicleStatus.Outside&&(resource==null||v.Body==BodyFor(resource.Value)));
    }

    void MigrateConstructionState()
    {
        if(State.ConstructionStateVersion>=1)return;
        var bodies=new[]{TruckBody.Dump,TruckBody.Dump,TruckBody.Dump,TruckBody.Mixer,TruckBody.Flatbed,TruckBody.Cargo};
        // Missing Body, rather than its default Cargo value, identifies trucks
        // from before differentiation. Explicit modern bodies never opt in.
        var legacy=State.Vehicles.Where(v=>!v.PassengerCar&&v.Machine==PavingMachine.None&&!v.HasExplicitBody).OrderBy(v=>v.Id).ToList();
        var trucks=State.Vehicles.Where(v=>v.External&&!v.PassengerCar&&v.Machine==PavingMachine.None&&v.HomeBuildingId==null).ToList();
        var assigned=trucks.Except(legacy).Select(v=>v.Body).ToList();
        foreach(var v in legacy){
            var cargo=v.Cargo.Count>0?v.Cargo:v.Status!=VehicleStatus.Outside?State.Shipments.LastOrDefault(s=>s.VehicleId==v.Id&&s.Cargo.Count>0)?.Cargo:null;
            v.Body=cargo is {Count:>0}?BodyFor(cargo.Keys.OrderBy(k=>k).First()):TruckBody.Cargo;
            if(!trucks.Contains(v))continue;
            var target=bodies.FirstOrDefault(body=>assigned.Count(a=>a==body)<bodies.Count(bodyInMix=>bodyInMix==body));
            assigned.Add(target);v.LegacyBodyAfterReturn=target;
            CompleteLegacyTruckConversion(v);
        }
        foreach(var p in State.Projects.Where(IsPaving)){
            p.AppliedRoadPoints=p.RoadPoints.Where(point=>State.Roads.Any(r=>p.BuiltRoadNodes.Contains(r.Id)&&r.Position==point)).ToList();
            p.RolledRoadPoints=p.AppliedRoadPoints.ToList();p.PavingVersion=1;
            if(p.Status is WorkStatus.Completed or WorkStatus.Cancelled)continue;
            foreach(var road in State.Roads.Where(r=>p.BuiltRoadNodes.Contains(r.Id))){road.ConstructionOnly=true;road.WorkProjectId=p.Id;}
            p.MachineryQuoted=QuoteMachinery(p.Position,p.WorkSeconds);
        }
        State.ConstructionStateVersion=1;
    }

    void CompleteLegacyTruckConversion(Vehicle v)
    {
        if(v.LegacyBodyAfterReturn is not {} target||!v.External||v.HomeBuildingId!=null||v.Status!=VehicleStatus.Outside||v.ShipmentId!=null||v.Cargo.Values.Any(q=>q>.000001))return;
        v.Body=target;v.LegacyBodyAfterReturn=null;
    }

    RoadNode? ConstructionAccess(Building b)=>State.Roads.Where(r=>!r.Highway&&r.MaintenanceRatio>.05&&_workExteriorConnected.Contains(r.Id)&&r.Position.Distance(b.Door)<=State.Tuning.RoadWidth/2+3).MinBy(r=>r.Position.Distance(b.Door));

    bool ConstructionTrip(Vehicle v)=>v.ConstructionAccess||v.Machine!=PavingMachine.None||v.ShipmentId is {} id&&State.Shipments.Any(s=>s.Id==id&&s.Purpose is ShipmentPurpose.Construction or ShipmentPurpose.Maintenance);
}
