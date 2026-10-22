namespace IndexCities.Simulation;

// E08: instrumentation ONLY, activated explicitly by Headless. Neither this
// class nor any of its counters is persisted or consulted by gameplay.
public sealed class TrafficProbeSlice
{
    public long OuterTicks, VehicleVisits, UnsplitCalls, LiveEntryChecks, MovementEventCalls, PredictedEntries, EntrySnapshots;
    public long LaneAttempts, LeaderInspections, LaneMoved, LaneBlocked, SafeTravelCalls, SafeSweepPositions;
    public long SafeBodyChecks, SafeReservedChecks, SafeBodyStops, SafeReservedStops, ManoeuvreCalls, ManoeuvrePoses;
    public long TrafficSamples, SampledVehicleCount, SampledTravellingCount;
    public string Print(int i)
        => $"E08 bucket={i*6:D2}-{(i+1)*6:D2}h ticks={OuterTicks} unsplit={UnsplitCalls} visits={VehicleVisits} entryChecks={LiveEntryChecks} eventCalls={MovementEventCalls} predictedEntries={PredictedEntries} snapshots={EntrySnapshots} laneAttempts={LaneAttempts} leaderInspections={LeaderInspections} laneMoved={LaneMoved} laneBlocked={LaneBlocked} safeCalls={SafeTravelCalls} safeSweeps={SafeSweepPositions} safeBodyChecks={SafeBodyChecks} safeReservedChecks={SafeReservedChecks} safeBodyStops={SafeBodyStops} safeReservedStops={SafeReservedStops} manoeuvres={ManoeuvreCalls} manoeuvrePoses={ManoeuvrePoses} trafficSamples={TrafficSamples} sampledVehicles={SampledVehicleCount} sampledTravelling={SampledTravellingCount}";
}
public sealed class TrafficProbe
{
    readonly double _start;
    public readonly TrafficProbeSlice[] Slices=[new(),new(),new(),new()];
    public TrafficProbe(double start)=>_start=start;
    internal TrafficProbeSlice At(double elapsed)
        =>Slices[Math.Clamp((int)Math.Floor((elapsed-_start)/21600.0),0,3)];
    public IEnumerable<string> Report()
    {
        for(var i=0;i<Slices.Length;i++)yield return Slices[i].Print(i);
    }
}
public sealed partial class CitySimulation
{
    TrafficProbe? _e08Probe;
    TrafficProbeSlice? _e08Slice;
    public TrafficProbe StartTrafficProbe()
    {
        _e08Probe=new TrafficProbe(State.ElapsedSeconds);
        _e08Slice=null;
        return _e08Probe;
    }
    void TickTrafficProbe()
    {
        if(_e08Probe==null)return;
        var bucket=_e08Slice=_e08Probe.At(State.ElapsedSeconds);
        bucket.OuterTicks++;
        // One snapshot per 900 movement updates (~30 simulated minutes).
        if(bucket.OuterTicks%900==1)
        {
            bucket.TrafficSamples++;
            bucket.SampledVehicleCount+=State.Vehicles.Count;
            bucket.SampledTravellingCount+=State.Vehicles.Count(v=>v.Status==VehicleStatus.Travelling);
        }
    }
}
