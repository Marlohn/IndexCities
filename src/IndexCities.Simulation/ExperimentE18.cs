namespace IndexCities.Simulation;

// E18: falsification experiment. The current traffic event horizon is the
// minimum of route boundaries from EVERY vehicle, even with one remote car
// entering a road. Probe whether considering only potential local influences
// can reduce global slices. This is not proven causally safe and must never
// be enabled in the game.
public sealed partial class CitySimulation
{
    public bool E18RouteScopeProbe { get; set; }
    long _e18ForecastCalls, _e18RouteCandidates, _e18RouteSkipped, _e18RouteKept;
    long _e18Samples, _e18SliceLong, _e18SliceTiny;
    double _e18SimulatedSeconds;
    readonly record struct E18Influence(int Id, Point Center, double Radius);

    List<E18Influence> E18EntryInfluences(List<Vehicle> entries, double horizon)
    {
        var zones = new List<E18Influence>(entries.Count);
        var maxSpeed = Math.Max(State.Tuning.VehicleSpeed, State.Tuning.HighwaySpeed);
        foreach(var entry in entries)
        {
            if(!TryLaneEntry(entry, out var lane))continue;
            // Very deliberately overbroad local catchment: moving body travel
            // during this horizon plus entry path and nominal stop margins.
            // This does NOT certify reservations, route ordering or rounding.
            var radius = Math.Max(40, lane.Distance * Math.Max(1, maxSpeed / lane.Speed)
                                    + maxSpeed * horizon * 2 + 20);
            zones.Add(new(entry.Id, entry.Position, radius));
        }
        return zones;
    }

    bool E18RouteMayAffectEntry(Vehicle v, List<E18Influence> zones)
    {
        foreach(var zone in zones)
            if(v.Id==zone.Id || v.Position.Distance(zone.Center)<=zone.Radius)
                return true;
        return false;
    }

    void E18AfterSlice(double seconds)
    {
        if(!E18RouteScopeProbe)return;
        _e18Samples++;
        _e18SimulatedSeconds+=seconds;
        if(seconds>=1.99)_e18SliceLong++;
        if(seconds<.05)_e18SliceTiny++;
    }

    public string E18Summary()=>
        $"E18_RESULT forecasts={_e18ForecastCalls} routeCandidates={_e18RouteCandidates} " +
        $"routeSkipped={_e18RouteSkipped} routeKept={_e18RouteKept} " +
        $"slices={_e18Samples} longSlices={_e18SliceLong} tinySlices={_e18SliceTiny} " +
        $"simulatedSeconds={_e18SimulatedSeconds:F2} " +
        "(experiment intentionally unproven; different save means rejected)";
}
