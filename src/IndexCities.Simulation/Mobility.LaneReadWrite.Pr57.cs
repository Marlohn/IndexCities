namespace IndexCities.Simulation;

// PR57 #163: sampled, intrusive observation only. This is NOT an exact
// dependency set: transient writes, pedestrian/signal/road edits, reservations
// outside the fields below and external edits are not tracked. No gameplay
// path reads these counters; no snapshots survive a movement slice.
public sealed partial class CitySimulation
{
    static readonly bool Pr57LaneReadWriteEnabled =
        Environment.GetEnvironmentVariable("PR57_LANE_READWRITE_PROBE") == "1";

    readonly record struct Pr57LaneReadState(
        Point Position, double Heading, VehicleStatus Status, int RouteIndex,
        int? IntersectionClaim, double WaitingSince, TowPhase TowPhase,
        int? TowTargetId, Point? DockPosition, Point? ManoeuvreDock,
        int ManoeuvreCount, int DetourCount, int RouteCount,
        bool SitePathBeforeRoad, int SitePathIndex);

    readonly record struct Pr57LaneQuery(
        Pr57LaneReadState Self, bool Entering, double Reach, long Epoch);

    Dictionary<int, Pr57LaneReadState>? _pr57LaneReadPrevious;
    readonly Dictionary<int, Pr57LaneQuery> _pr57LaneLastQuery = new();
    bool _pr57LaneReadActive;
    long _pr57LaneSlices, _pr57LaneSamples, _pr57LaneQueries, _pr57LaneVisits,
        _pr57LaneWorldEpoch, _pr57LaneWorldUnchanged, _pr57LaneThirdPartyChanged,
        _pr57LaneChangedVehicles, _pr57LaneChangedThirdPartyVehicles,
        _pr57LanePoseChanges, _pr57LaneNonPoseChanges, _pr57LaneExactRepeats,
        _pr57LaneSameSelfRepeats, _pr57LaneFirstQueries;

    static Pr57LaneReadState Pr57LaneReadSnapshot(Vehicle v) => new(
        v.Position, v.Heading, v.Status, v.RouteIndex, v.IntersectionClaim,
        v.WaitingSince, v.TowPhase, v.TowTargetId, v.DockPosition,
        v.ManoeuvreDock, v.ManoeuvreSpaces.Count, v.Detour.Count,
        v.Route.Count, v.SitePathBeforeRoad, v.SitePathIndex);

    void Pr57LaneReadBeginSlice()
    {
        // One in 37 complete slices, chosen without changing any work in the
        // simulation. This is intentionally a bounded O(queries * vehicles)
        // diagnostic and must never be used as a wall-time benchmark.
        _pr57LaneReadActive = ++_pr57LaneSlices % 37 == 0;
        _pr57LaneReadPrevious = null;
        _pr57LaneLastQuery.Clear();
        if (_pr57LaneReadActive) _pr57LaneSamples++;
    }

    void Pr57LaneReadBeforeScan(Vehicle self, bool entering, double reach, int visits)
    {
        if (!_pr57LaneReadActive) return;
        _pr57LaneQueries++;
        _pr57LaneVisits += visits;
        var world = State.Vehicles.ToDictionary(v => v.Id, Pr57LaneReadSnapshot);
        var changed = 0;
        var changedThirdParty = 0;
        if (_pr57LaneReadPrevious is { } previous)
        {
            foreach (var (id, now) in world)
            {
                if (!previous.TryGetValue(id, out var old) || old != now)
                {
                    changed++;
                    if (id != self.Id) changedThirdParty++;
                    if (!previous.TryGetValue(id, out old) ||
                        !Equals(old.Position, now.Position) || old.Heading != now.Heading)
                        _pr57LanePoseChanges++;
                    else _pr57LaneNonPoseChanges++;
                }
            }
            changed += previous.Keys.Count(id => !world.ContainsKey(id));
            if (changed == 0) _pr57LaneWorldUnchanged++;
            else _pr57LaneWorldEpoch++;
            if (changedThirdParty > 0) _pr57LaneThirdPartyChanged++;
            _pr57LaneChangedVehicles += changed;
            _pr57LaneChangedThirdPartyVehicles += changedThirdParty;
        }
        else _pr57LaneFirstQueries++;

        var selfState = world[self.Id];
        if (_pr57LaneLastQuery.TryGetValue(self.Id, out var prior))
        {
            if (prior.Self == selfState && prior.Entering == entering &&
                prior.Reach == reach)
            {
                _pr57LaneSameSelfRepeats++;
                if (prior.Epoch == _pr57LaneWorldEpoch)
                    _pr57LaneExactRepeats++;
            }
        }
        _pr57LaneLastQuery[self.Id] =
            new(selfState, entering, reach, _pr57LaneWorldEpoch);
        _pr57LaneReadPrevious = world;
    }

    void Pr57LaneReadEndSlice()
    {
        if (!_pr57LaneReadActive) return;
        Console.WriteLine(
            $"PR57_LANE_RW samples={_pr57LaneSamples} slices={_pr57LaneSlices} queries={_pr57LaneQueries} visits={_pr57LaneVisits} first={_pr57LaneFirstQueries} worldUnchanged={_pr57LaneWorldUnchanged} queriesThirdPartyChanged={_pr57LaneThirdPartyChanged} changedVehicles={_pr57LaneChangedVehicles} changedThirdPartyVehicles={_pr57LaneChangedThirdPartyVehicles} poseChanges={_pr57LanePoseChanges} nonPoseChanges={_pr57LaneNonPoseChanges} sameSelfQuery={_pr57LaneSameSelfRepeats} identicalQueryAndEpoch={_pr57LaneExactRepeats} sampled=True observational=True exactDependencies=False");
        _pr57LaneReadPrevious = null;
        _pr57LaneLastQuery.Clear();
    }
}
