namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    public static TruckBody BodyFor(ResourceKind resource)=>resource switch{
        ResourceKind.Aggregate or ResourceKind.Asphalt=>TruckBody.Dump,
        ResourceKind.Concrete=>TruckBody.Mixer,ResourceKind.Steel or ResourceKind.Wood=>TruckBody.Flatbed,_=>TruckBody.Cargo};

    Vehicle? AvailableTruck(ResourceKind? resource=null)
    {
        // Fixed specialist vehicles; the original fleet count is unchanged.
        var trucks=State.Vehicles.Where(v=>v.External&&!v.PassengerCar&&v.Machine==PavingMachine.None&&v.HomeBuildingId==null&&!IsTow(v)).ToList();
        if(trucks.Count(v=>v.Status!=VehicleStatus.Outside)>=State.Tuning.ExternalTruckCount)return null;
        return trucks.FirstOrDefault(v=>v.Status==VehicleStatus.Outside&&v.ServicePhase==ServicePhase.None&&!v.BrokenDown&&(resource==null||v.Body==BodyFor(resource.Value)));
    }

    RoadNode? ConstructionAccess(Building b)=>_pr57BuildingAccessActive?Pr57IndexedBuildingAccess(b):State.Roads.Where(r=>!r.Highway&&r.MaintenanceRatio>.05&&_workExteriorConnected.Contains(r.Id)&&r.Position.Distance(b.Door)<=State.Tuning.RoadWidth/2+3).MinBy(r=>r.Position.Distance(b.Door));

    bool ConstructionTrip(Vehicle v)=>v.ConstructionAccess||v.Machine!=PavingMachine.None||v.ShipmentId is {} id&&State.Shipments.Any(s=>s.Id==id&&s.Purpose is ShipmentPurpose.Construction or ShipmentPurpose.Maintenance);
}
