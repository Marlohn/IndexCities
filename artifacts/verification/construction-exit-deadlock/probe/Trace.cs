using IndexCities.Simulation;
static class Trace
{
    public static void Run(string shape,int seed,double from,double to)
    {
        var sim=Sweep.Build(shape,seed)!;
        string V(Vehicle v)=>$"V{v.Id} {v.Machine}/{v.Body} {v.MachinePhase} {v.Status} ({v.Position.X:F2},{v.Position.Z:F2}) h={v.Heading:F2} r{v.RouteIndex}/{v.Route.Count} sp{v.SitePathIndex}/{v.SitePath.Count} proj={v.MachineProjectId}: {v.Cause}";
        while(sim.State.ElapsedSeconds<to){sim.Advance(2);if(sim.State.ElapsedSeconds>=from){Console.WriteLine($"--- t={sim.State.ElapsedSeconds}");foreach(var v in sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside))Console.WriteLine("  "+V(v));}}
        foreach(var p in sim.State.Projects)Console.WriteLine($"P{p.Id} {p.Status} {p.Progress:F2} {p.AppliedRoadPoints.Count}/{p.RolledRoadPoints.Count}/{p.RoadPoints.Count} paver={p.PaverId} roller={p.RollerId}: {p.Cause}");
    }
}
