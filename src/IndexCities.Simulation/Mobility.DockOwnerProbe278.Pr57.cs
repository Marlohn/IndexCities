namespace IndexCities.Simulation;

// PR57 #278: strictly observational, bounded ownership and writer-churn census
// for the real blocked delivery 23. No memoization, decisions or skipped slices.
public sealed partial class CitySimulation
{
    static readonly bool Pr57278Enabled =
        Environment.GetEnvironmentVariable("PR57_278_DOCK_OWNERS") == "1";

    sealed class Pr57278OwnerRow
    {
        public long Calls, Poses, FullChanges, PhysicalChanges, ClaimChanges, ExitChanges;
        public string? LastFull, LastPhysical, LastClaim, LastExit;
    }

    readonly Dictionary<(int Window,int Owner),Pr57278OwnerRow> _pr57278Owners = new();
    readonly long[] _pr57278Calls = new long[2];
    readonly long[] _pr57278OwnerVisits = new long[2];
    readonly long[] _pr57278Poses = new long[2];
    readonly long[] _pr57278ZeroOwners = new long[2];
    readonly long[] _pr57278ProbeTicks = new long[2];
    readonly long[] _pr57278ProbeBytes = new long[2];

    bool Pr57278Sample(Vehicle v) =>
        Pr57278Enabled && _pr57273Sampling && v.Id == 23;

    void Pr57278Owner(Vehicle reader,Vehicle owner,
        List<(Vehicle Body,Point Position,double Heading)> poses)
    {
        if (!Pr57278Sample(reader)) return;
        var w = _pr57273Window;
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        var key = (w,owner.Id);
        if (!_pr57278Owners.TryGetValue(key,out var row))
            _pr57278Owners[key] = row = new Pr57278OwnerRow();

        // Compare exact serialized strings, not hashes: equality is collision-free
        // for these projections. This is intentionally an intrusive diagnostic.
        var full = System.Text.Json.JsonSerializer.Serialize(owner);
        var physical = System.Text.Json.JsonSerializer.Serialize(new {
            owner.Position,owner.Heading,owner.Status,owner.Route,owner.RouteIndex,
            owner.SitePath,owner.SitePathIndex
        });
        var claim = System.Text.Json.JsonSerializer.Serialize(new {
            owner.DockPosition,owner.ManoeuvreDock,owner.ManoeuvreDeparture,
            owner.MovementAdmission,owner.ManoeuvreSpaces,owner.DockCorridor
        });
        var exit = string.Join("|",poses.Select(p =>
            $"{p.Body.Id}:{p.Position.X:R},{p.Position.Z:R},{p.Heading:R}"));

        if (row.Calls > 0)
        {
            if (row.LastFull != full) row.FullChanges++;
            if (row.LastPhysical != physical) row.PhysicalChanges++;
            if (row.LastClaim != claim) row.ClaimChanges++;
            if (row.LastExit != exit) row.ExitChanges++;
        }
        row.Calls++;
        row.Poses += poses.Count;
        row.LastFull = full;
        row.LastPhysical = physical;
        row.LastClaim = claim;
        row.LastExit = exit;
        _pr57278ProbeTicks[w] += System.Diagnostics.Stopwatch.GetTimestamp() - start;
        _pr57278ProbeBytes[w] += GC.GetAllocatedBytesForCurrentThread() - bytes;
    }

    void Pr57278End(Vehicle reader,int owners,int poses)
    {
        if (!Pr57278Sample(reader)) return;
        var w = _pr57273Window;
        _pr57278Calls[w]++;
        _pr57278OwnerVisits[w] += owners;
        _pr57278Poses[w] += poses;
        if (owners == 0) _pr57278ZeroOwners[w]++;
        if (w != 1 || _pr57278Calls[w] != 100) return;

        for (var window=0;window<2;window++)
        {
            Console.WriteLine(
                $"PR57_278_SUM window={window} reader=23 calls={_pr57278Calls[window]} " +
                $"ownerVisits={_pr57278OwnerVisits[window]} exitPoses={_pr57278Poses[window]} " +
                $"zeroOwners={_pr57278ZeroOwners[window]} " +
                $"probeMs={_pr57278ProbeTicks[window]*1000.0/System.Diagnostics.Stopwatch.Frequency:F6} " +
                $"probeBytes={_pr57278ProbeBytes[window]} " +
                "observational=True exactStringEquality=True noSkip=True");
            foreach (var pair in _pr57278Owners
                .Where(p=>p.Key.Window==window).OrderBy(p=>p.Key.Owner))
            {
                var r=pair.Value;
                Console.WriteLine(
                    $"PR57_278_OWNER window={window} owner={pair.Key.Owner} " +
                    $"calls={r.Calls} poses={r.Poses} fullChanges={r.FullChanges} " +
                    $"physicalChanges={r.PhysicalChanges} claimChanges={r.ClaimChanges} " +
                    $"exitChanges={r.ExitChanges} observational=True");
            }
        }
    }
}
