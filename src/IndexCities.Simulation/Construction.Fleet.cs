namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    public static TruckBody BodyFor(ResourceKind resource)=>resource switch{
        ResourceKind.Aggregate or ResourceKind.Asphalt=>TruckBody.Dump,
        ResourceKind.Concrete=>TruckBody.Mixer,ResourceKind.Steel or ResourceKind.Wood=>TruckBody.Flatbed,_=>TruckBody.Cargo};

    Vehicle? AvailableTruck(ResourceKind? resource=null)
    {
        // Fixed specialist vehicles; the original fleet count is unchanged.
        // Isolated PR57 diagnostic: count the *actual* LINQ predicate visits, not
        // the size of State.Vehicles. No persistent index or altered ordering.
        var trace=Environment.GetEnvironmentVariable("PR57_TRACE_SHIPMENT_1040")=="1"
            &&_pr57SelectorTraceCount<32;
        var eligibleVisits=0;
        var busyVisits=0;
        var searchVisits=0;
        var t0=trace?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var trucks=State.Vehicles.Where(v=>{
            if(trace)eligibleVisits++;
            return v.External&&!v.PassengerCar&&v.Machine==PavingMachine.None&&v.HomeBuildingId==null&&!IsTow(v);
        }).ToList();
        var t1=trace?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        var busy=trucks.Count(v=>{
            if(trace)busyVisits++;
            return v.Status!=VehicleStatus.Outside;
        });
        var t2=trace?System.Diagnostics.Stopwatch.GetTimestamp():0L;
        Vehicle? chosen=null;
        if(busy<State.Tuning.ExternalTruckCount)
            chosen=trucks.FirstOrDefault(v=>{
                if(trace)searchVisits++;
                return v.Status==VehicleStatus.Outside&&v.ServicePhase==ServicePhase.None&&!v.BrokenDown&&(resource==null||v.Body==BodyFor(resource.Value));
            });
        if(trace)
            Console.WriteLine($"PR57_SELECTOR_VISITS call={_pr57SelectorTraceCount+1} source={State.Vehicles.Count} eligible={trucks.Count} eligibleVisits={eligibleVisits} busyVisits={busyVisits} searchVisits={searchVisits} busy={busy} cap={State.Tuning.ExternalTruckCount} chosen={chosen?.Id.ToString()??"none"} materializeTicks={t1-t0} countTicks={t2-t1} searchTicks={System.Diagnostics.Stopwatch.GetTimestamp()-t2} frequency={System.Diagnostics.Stopwatch.Frequency} scope=intrusive-real-dispatch");
        return chosen;
    }

    RoadNode? ConstructionAccess(Building b)=>_pr57BuildingAccessActive?Pr57IndexedBuildingAccess(b):State.Roads.Where(r=>!r.Highway&&r.MaintenanceRatio>.05&&_workExteriorConnected.Contains(r.Id)&&r.Position.Distance(b.Door)<=State.Tuning.RoadWidth/2+3).MinBy(r=>r.Position.Distance(b.Door));

    bool ConstructionTrip(Vehicle v)=>v.ConstructionAccess||v.Machine!=PavingMachine.None||v.ShipmentId is {} id&&State.Shipments.Any(s=>s.Id==id&&s.Purpose is ShipmentPurpose.Construction or ShipmentPurpose.Maintenance);
}
