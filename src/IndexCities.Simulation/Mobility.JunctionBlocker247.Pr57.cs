namespace IndexCities.Simulation;

// PR57 #247: selected BODY blocker identities and net third-party changes
// between actual admission reads in the authentic 14h queue. Observational,
// opt-in and intrusive. Snapshots omit transient writes, pedestrians, signal
// phases, negative eligibility and other domain objects; NOT an invalidation
// oracle and never used by physics or persistence.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Junction247Enabled =
        Environment.GetEnvironmentVariable("PR57_247_JUNCTION_BLOCKERS") == "1";

    sealed class Pr57Junction247Pair
    {
        public long Calls, Repeats, SameReason, ChangedReason, SameBlocker,
            DifferentBlocker, SameReasonDifferentBlocker, SameBlockerStateChanged,
            OtherNetChanged, NonBlockerNetChanged, WorldNetStable, NetChangedActors;
        public readonly HashSet<int> Blockers = new();
        public Pr57JunctionExit LastReason;
        public int LastBlocker;
        public Pr57DepState LastBlockerState;
        public Dictionary<int, Pr57DepState>? LastWorld;
    }

    readonly Dictionary<int, (int Id, Pr57DepState State)> _pr57Junction247Pending = new();
    readonly Dictionary<(int Reader, int Junction), Pr57Junction247Pair> _pr57Junction247Pairs = new();
    long _pr57Junction247Missing;

    // Called by the already existing selected-blocker hook at the ACTUAL
    // blocking return, before Cause is updated. No independent predicate.
    void Pr57Junction247SelectedBlocker(Vehicle reader, Vehicle other, string kind)
    {
        if (!Pr57Junction247Enabled || CostProfile is null ||
            !Pr57State244PersistentIds.Contains(reader.Id) ||
            kind is not ("bendBody" or "onwardBody")) return;
        _pr57Junction247Pending[reader.Id] = (other.Id, Pr57DepSnapshot(other));
    }

    void Pr57Junction247Decision(Vehicle reader, RoadNode junction, Pr57JunctionExit reason)
    {
        if (!Pr57Junction247Enabled || CostProfile is null ||
            !Pr57State244PersistentIds.Contains(reader.Id)) return;
        if (reason is not (Pr57JunctionExit.BendBody or Pr57JunctionExit.OnwardBody))
        {
            _pr57Junction247Pending.Remove(reader.Id);
            return;
        }
        if (!_pr57Junction247Pending.Remove(reader.Id, out var selected))
        {
            _pr57Junction247Missing++;
            return;
        }
        var key = (reader.Id, junction.Id);
        if (!_pr57Junction247Pairs.TryGetValue(key, out var pair))
            _pr57Junction247Pairs[key] = pair = new Pr57Junction247Pair();
        var world = Pr57DepWorld();
        pair.Calls++;
        pair.Blockers.Add(selected.Id);
        if (pair.LastWorld is { } previous)
        {
            pair.Repeats++;
            if (pair.LastReason == reason) pair.SameReason++;
            else pair.ChangedReason++;
            if (pair.LastBlocker == selected.Id)
            {
                pair.SameBlocker++;
                if (pair.LastBlockerState != selected.State)
                    pair.SameBlockerStateChanged++;
            }
            else
            {
                pair.DifferentBlocker++;
                if (pair.LastReason == reason) pair.SameReasonDifferentBlocker++;
            }
            var changed = 0;
            var nonBlocker = false;
            foreach (var (id, state) in world)
            {
                if (id == reader.Id) continue;
                if (previous.TryGetValue(id, out var old) && old == state) continue;
                changed++;
                if (id != selected.Id && id != pair.LastBlocker) nonBlocker = true;
            }
            foreach (var id in previous.Keys)
            {
                if (id == reader.Id || world.ContainsKey(id)) continue;
                changed++;
                if (id != selected.Id && id != pair.LastBlocker) nonBlocker = true;
            }
            pair.NetChangedActors += changed;
            if (changed == 0) pair.WorldNetStable++;
            else pair.OtherNetChanged++;
            if (nonBlocker) pair.NonBlockerNetChanged++;
        }
        pair.LastReason = reason;
        pair.LastBlocker = selected.Id;
        pair.LastBlockerState = selected.State;
        pair.LastWorld = world;
    }

    void Pr57Junction247Report(long slices)
    {
        if (!Pr57Junction247Enabled) return;
        Console.WriteLine($"PR57_247_SUMMARY slices={slices} pairs={_pr57Junction247Pairs.Count} missingSelected={_pr57Junction247Missing} observational=True netSnapshotsNotWriteJournal=True");
        foreach (var entry in _pr57Junction247Pairs.OrderBy(x => x.Key.Reader).ThenBy(x => x.Key.Junction))
        {
            var (reader, junction) = entry.Key;
            var p = entry.Value;
            Console.WriteLine(
                $"PR57_247_PAIR slices={slices} reader={reader} junction={junction} " +
                $"calls={p.Calls} repeats={p.Repeats} distinctBlockers={p.Blockers.Count} " +
                $"blockerIds={string.Join(",",p.Blockers.OrderBy(x=>x))} " +
                $"sameReason={p.SameReason} changedReason={p.ChangedReason} " +
                $"sameBlocker={p.SameBlocker} differentBlocker={p.DifferentBlocker} " +
                $"sameReasonDifferentBlocker={p.SameReasonDifferentBlocker} " +
                $"sameBlockerStateChanged={p.SameBlockerStateChanged} " +
                $"otherNetChanged={p.OtherNetChanged} nonBlockerNetChanged={p.NonBlockerNetChanged} " +
                $"worldNetStable={p.WorldNetStable} totalNetChangedActors={p.NetChangedActors} " +
                "scope=selected-body-denials-only projectionNotCausalClosure=True");
        }
    }
}
