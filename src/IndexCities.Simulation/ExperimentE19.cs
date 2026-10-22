namespace IndexCities.Simulation;

// E19 — first conservative temporal-prediction cache, scoped to one physical
// movement batch. Not a production event engine or a change to game rules.
// No independent clocks: all vehicles still run at exactly the same slices.
// The physics is ALWAYS the authority; predictions only bound recalculation.
public sealed partial class CitySimulation
{
    public bool E19TemporalPredictions { get; set; }
    public bool E19VerifyPredictions { get; set; }

    sealed class E19RoutePrediction
    {
        public Point Position;
        public double Heading;
        public long FuelUnits;
        public VehicleStatus Status;
        public int RouteIndex;
        public int[] Route = [];
        public double FuelRate;
        public double Horizon;
        public double NextRelativeChange;
    }

    readonly Dictionary<int,E19RoutePrediction> _e19Predictions=new();
    long _e19Lookups,_e19Hits,_e19Misses,_e19Invalidated,_e19RouteChecks;
    long _e19OracleChecks,_e19OracleMismatch,_e19MovementSlices;
    double _e19MaxPredictionError;

    // Forecast values are RELATIVE to the car's current position. A stopped
    // car's nominal arrival therefore moves forward with each physical slice;
    // caching an ABSOLUTE timestamp would produce a false prediction.
    // Route and fuel are compared exactly, including in-place list mutation.
    double E19RouteEvent(Vehicle v,double horizon,double fullHorizon)
    {
        _e19Lookups++;
        var rate=FuelRate(v);
        if(_e19Predictions.TryGetValue(v.Id,out var prior))
        {
            var same=prior.Position==v.Position
                &&prior.Heading==v.Heading
                &&prior.FuelUnits==v.FuelUnits
                &&prior.Status==v.Status
                &&prior.RouteIndex==v.RouteIndex
                &&prior.FuelRate==rate
                &&prior.Horizon>=fullHorizon
                &&prior.Route.Length==v.Route.Count;
            if(same)
            {
                _e19RouteChecks++;
                same=v.Route.SequenceEqual(prior.Route);
            }
            if(same)
            {
                _e19Hits++;
                return Math.Min(horizon,prior.NextRelativeChange);
            }
            _e19Invalidated++;
        }
        _e19Misses++;
        // Forecast using the ENTIRE remaining batch window, not a horizon
        // already clipped by a previous vehicle in deterministic list order.
        // This makes a later reduced bound safe to reuse.
        var prediction=TimeToMovementChange(v,fullHorizon);
        _e19Predictions[v.Id]=new()
        {
            Position=v.Position,Heading=v.Heading,FuelUnits=v.FuelUnits,
            Status=v.Status,RouteIndex=v.RouteIndex,Route=v.Route.ToArray(),
            FuelRate=rate,Horizon=fullHorizon,NextRelativeChange=prediction
        };
        return Math.Min(horizon,prediction);
    }

    // Shadow oracle checks the ORIGINAL forecast, with its exact passed
    // horizon, BEFORE any physics updates. On mismatch, use the original value
    // to preserve gameplay and count it; no silent correctness claims.
    double E19PredictAndVerify(Vehicle v,double horizon,double fullHorizon)
    {
        var predicted=E19RouteEvent(v,horizon,fullHorizon);
        if(!E19VerifyPredictions)return predicted;
        var original=Math.Min(horizon,TimeToMovementChange(v,horizon));
        _e19OracleChecks++;
        var error=Math.Abs(predicted-original);
        _e19MaxPredictionError=Math.Max(_e19MaxPredictionError,error);
        if(predicted!=original){_e19OracleMismatch++;return original;}
        return predicted;
    }

    public string E19Summary()=>
        $"E19_RESULT lookups={_e19Lookups} hits={_e19Hits} misses={_e19Misses} " +
        $"invalidated={_e19Invalidated} routeChecks={_e19RouteChecks} " +
        $"oracleChecks={_e19OracleChecks} oracleMismatch={_e19OracleMismatch} " +
        $"maxPredictionError={_e19MaxPredictionError:G17} slices={_e19MovementSlices} " +
        "(one global clock, original movement physics; candidate limited to route forecasts)";
}
