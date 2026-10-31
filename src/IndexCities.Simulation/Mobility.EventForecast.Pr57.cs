namespace IndexCities.Simulation;

// PR57 #543: a bounded first vertical cut of a per-Step physical event timeline.
// Only the LOCAL TimeToMovementChange forecast is reused; entry admission,
// blocked/held tests, global slices, movement, and all writers remain canonical.
// The 2-second upper bound is the public Godot quantum. This is experimental:
// an external in-place road mutation not reflected in _roadEpoch needs the
// shadow mode until every writer is certified.
public sealed partial class CitySimulation
{
    static readonly bool Pr57EventForecastEnabled =
        Environment.GetEnvironmentVariable("PR57_EVENT_FORECAST") == "1";
    static readonly bool Pr57EventForecastVerify =
        Environment.GetEnvironmentVariable("PR57_EVENT_FORECAST_VERIFY") == "1";
    static readonly bool Pr57EventForecastDiag =
        Environment.GetEnvironmentVariable("PR57_EVENT_FORECAST_DIAG") == "1";

    sealed class Pr57EventForecastEntry
    {
        public Point Position;
        public double Heading, Fuel, FuelRate, VehicleSpeed, HighwaySpeed;
        public int RouteIndex, RoadEpoch;
        public VehicleStatus Status;
        public int[] Route = [];
        public double Candidate;
        public int Reason;
    }

    readonly Dictionary<Vehicle, Pr57EventForecastEntry> _pr57EventForecasts =
        new(ReferenceEqualityComparer.Instance);
    bool _pr57EventForecastActive;
    long _pr57EventForecastSteps, _pr57EventForecastHits,
         _pr57EventForecastMisses, _pr57EventForecastInvalidations,
         _pr57EventForecastShadowChecks;

    void Pr57EventForecastBeginStep(double dt)
    {
        _pr57EventForecastActive = Pr57EventForecastEnabled
            && CostProfile is null && dt > 0 && dt <= 2;
        if(!_pr57EventForecastActive)return;
        _pr57EventForecasts.Clear();
        _pr57EventForecastSteps++;
    }

    void Pr57EventForecastEndStep()
    {
        if(!_pr57EventForecastActive)return;
        _pr57EventForecastActive=false;
        _pr57EventForecasts.Clear();
        if(Pr57EventForecastDiag && _pr57EventForecastSteps%100==0)
            Console.WriteLine($"PR57_543_EVENT_FORECAST steps={_pr57EventForecastSteps} hits={_pr57EventForecastHits} misses={_pr57EventForecastMisses} invalidations={_pr57EventForecastInvalidations} shadow={_pr57EventForecastShadowChecks}");
    }

    double Pr57ForecastMovementChange(Vehicle v,double horizon,out int reason)
    {
        if(!_pr57EventForecastActive || horizon>2 || horizon<=0)
            return TimeToMovementChange(v,horizon,out reason);

        var rate=v.Status==VehicleStatus.Travelling?FuelRate(v):0;
        var hit=_pr57EventForecasts.TryGetValue(v,out var entry)
            && entry!.Position.Equals(v.Position)
            && entry.Heading==v.Heading && entry.Fuel==v.Fuel
            && entry.FuelRate==rate && entry.Status==v.Status
            && entry.RouteIndex==v.RouteIndex && entry.RoadEpoch==_roadEpoch
            && entry.VehicleSpeed==State.Tuning.VehicleSpeed
            && entry.HighwaySpeed==State.Tuning.HighwaySpeed
            && v.Route.SequenceEqual(entry.Route);
        if(hit)_pr57EventForecastHits++;
        else
        {
            if(entry is not null)_pr57EventForecastInvalidations++;
            _pr57EventForecastMisses++;
            // Forecast to the fixed 2s quantum, then clip to the current
            // horizon. A shorter horizon cannot change an earlier event.
            var candidate=TimeToMovementChange(v,2,out var cause);
            entry=new Pr57EventForecastEntry {
                Position=v.Position, Heading=v.Heading, Fuel=v.Fuel,
                FuelRate=rate, Status=v.Status, RouteIndex=v.RouteIndex,
                RoadEpoch=_roadEpoch, VehicleSpeed=State.Tuning.VehicleSpeed,
                HighwaySpeed=State.Tuning.HighwaySpeed, Route=v.Route.ToArray(),
                Candidate=candidate, Reason=cause
            };
            _pr57EventForecasts[v]=entry;
        }
        var result=entry!.Candidate<horizon?entry.Candidate:horizon;
        reason=entry.Candidate<horizon?entry.Reason:-1;
        if(Pr57EventForecastVerify)
        {
            var original=TimeToMovementChange(v,horizon,out var originalReason);
            _pr57EventForecastShadowChecks++;
            if(result!=original || reason!=originalReason)
                throw new InvalidOperationException(
                    $"PR57_543_EVENT_FORECAST_SHADOW_MISMATCH vehicle={v.Id} cached={result:R}/{reason} original={original:R}/{originalReason}");
        }
        return result;
    }
}
