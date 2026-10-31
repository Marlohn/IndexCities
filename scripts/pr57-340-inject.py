#!/usr/bin/env python3
"""PR57 #340: checkout-only observational fingerprint of canonical BodyOnWay prefixes.

Targets four actual REAL junction readers identified by #339. No decision reuse,
no skipped slice, no persisted fields. Hashes are only a conservative projection:
not exact invalidation certificates, not a write journal, and not collision-free.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
sim = root / "src/IndexCities.Simulation"

def replace_one(path, old, new):
    content = path.read_text(encoding="utf-8")
    if content.count(old) != 1:
        raise RuntimeError(f"{path}: expected one anchor, got {content.count(old)}")
    path.write_text(content.replace(old, new, 1), encoding="utf-8")

shared = sim / "Mobility.SharedSpace.cs"
replace_one(shared,
    "        Pr57261Hit(0);\n        // #305",
    "        Pr57340Begin(v,poses,pr57305Onward);\n        Pr57261Hit(0);\n        // #305")
replace_one(shared,
    "        if(poses.Count==0)return null;var reach=poses.Max",
    "        if(poses.Count==0){Pr57340End(null);return null;}var reach=poses.Max")
replace_one(shared,
    "            if(census){visited++;Pr57306Read(v,o);}",
    "            if(census){visited++;Pr57306Read(v,o);}\n            Pr57340Candidate(o);")
replace_one(shared,
    "                    if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);\n                    return o;",
    "                    if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);\n                    Pr57340End(o);\n                    return o;")
replace_one(shared,
    "        if(census)Pr57305Body(v,null,poses.Count,visited,eligible,nearNegative,geometry,started);\n        return null;",
    "        if(census)Pr57305Body(v,null,poses.Count,visited,eligible,nearNegative,geometry,started);\n        Pr57340End(null);\n        return null;")
replace_one(sim / "Mobility.cs",
    "var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);",
    "var onwardBody=BodyOnWay(v,onward,Pr57305Enabled || Pr57340Enabled);")
replace_one(sim / "Mobility.JunctionReturns.Pr57.cs",
    "        Pr57Junction247Report(slices);",
    "        Pr57Junction247Report(slices);\n        Pr57340Report(slices);")

new_file = sim / "Mobility.BodyPrefix340.Pr57.cs"
if new_file.exists():
    raise RuntimeError("Refusing to overwrite existing #340 partial")
new_file.write_text('''namespace IndexCities.Simulation;

// #340: observational FNV fingerprint of the ACTUALLY VISITED ordered prefix
// in BodyOnWay, excluding the selected blocker. Includes rejected candidates.
// Not an exact readset: dimensions/road edits/transient writes are not fully
// represented; hash collisions are possible. Never used by gameplay.
public sealed partial class CitySimulation
{
    static readonly bool Pr57340Enabled =
        Environment.GetEnvironmentVariable("PR57_340_BODY_PREFIX") == "1";
    const ulong Pr57340Offset = 14695981039346656037UL;
    const ulong Pr57340Prime = 1099511628211UL;

    readonly record struct Pr57340Row(
        ulong Self, ulong Prefix, ulong SelectedState, int Selected, int Visited);
    sealed class Pr57340Stats
    {
        public long Calls, Repeats, SameSelected, SameSelf, SameSelectedState,
            StableSelfSelected, PrefixStable, PrefixChanged, SelectedChanged,
            SelfChanged, Visited, MaxVisited, Printed;
        public Pr57340Row? Previous;
    }
    readonly Dictionary<int, Pr57340Stats> _pr57340Stats = new();
    bool _pr57340Active;
    int _pr57340Reader, _pr57340Visited;
    ulong _pr57340Self, _pr57340Prefix, _pr57340BeforeLast, _pr57340LastCandidate;

    static ulong Pr57340Mix(ulong hash, long value) =>
        unchecked((hash ^ (ulong)value) * Pr57340Prime);
    static ulong Pr57340Double(ulong hash, double value) =>
        Pr57340Mix(hash, BitConverter.DoubleToInt64Bits(value));

    void Pr57340Begin(Vehicle v,
        List<(Vehicle Body, Point Position, double Heading)> poses, bool onward)
    {
        _pr57340Active = Pr57340Enabled && onward &&
            v.Id is 1243 or 1282 or 1071 or 1380;
        if (!_pr57340Active) return;
        _pr57340Reader = v.Id;
        _pr57340Visited = 0;
        _pr57340Prefix = Pr57340Offset;
        _pr57340BeforeLast = Pr57340Offset;
        _pr57340LastCandidate = 0;
        var hash = Pr57340Offset;
        hash = Pr57340Mix(hash, v.Id);
        hash = Pr57340Double(hash, v.Position.X);
        hash = Pr57340Double(hash, v.Position.Z);
        hash = Pr57340Double(hash, v.Heading);
        hash = Pr57340Mix(hash, v.RouteIndex);
        hash = Pr57340Mix(hash, (long)v.Status);
        hash = Pr57340Mix(hash, v.TowTargetId ?? -1);
        hash = Pr57340Mix(hash, v.PassengerCar ? 1 : 0);
        hash = Pr57340Mix(hash, (long)v.Machine);
        hash = Pr57340Mix(hash, poses.Count);
        foreach (var pose in poses)
        {
            hash = Pr57340Double(hash, pose.Position.X);
            hash = Pr57340Double(hash, pose.Position.Z);
            hash = Pr57340Double(hash, pose.Heading);
        }
        _pr57340Self = hash;
    }

    void Pr57340Candidate(Vehicle o)
    {
        if (!_pr57340Active) return;
        // Prefix includes every actual loop visit, including early-guard rejects.
        // It is intentionally a conservative projection, not a minimal dependency.
        var hash = Pr57340Offset;
        hash = Pr57340Mix(hash, o.Id);
        hash = Pr57340Double(hash, o.Position.X);
        hash = Pr57340Double(hash, o.Position.Z);
        hash = Pr57340Double(hash, o.Heading);
        hash = Pr57340Mix(hash, (long)o.Status);
        hash = Pr57340Mix(hash, o.TowTruckId ?? -1);
        hash = Pr57340Mix(hash, o.TowTargetId ?? -1);
        hash = Pr57340Mix(hash, o.PassengerCar ? 1 : 0);
        hash = Pr57340Mix(hash, (long)o.Machine);
        _pr57340BeforeLast = _pr57340Prefix;
        _pr57340Prefix = Pr57340Mix(_pr57340Prefix, unchecked((long)hash));
        _pr57340LastCandidate = hash;
        _pr57340Visited++;
    }

    void Pr57340End(Vehicle? selected)
    {
        if (!_pr57340Active) return;
        _pr57340Active = false;
        if (!_pr57340Stats.TryGetValue(_pr57340Reader, out var stats))
            _pr57340Stats[_pr57340Reader] = stats = new Pr57340Stats();
        var row = new Pr57340Row(
            _pr57340Self,
            selected is null ? _pr57340Prefix : _pr57340BeforeLast,
            selected is null ? 0 : _pr57340LastCandidate,
            selected?.Id ?? -1, _pr57340Visited);
        stats.Calls++;
        stats.Visited += row.Visited;
        stats.MaxVisited = Math.Max(stats.MaxVisited, row.Visited);
        if (stats.Previous is { } old)
        {
            stats.Repeats++;
            if (old.Selected == row.Selected) stats.SameSelected++;
            else stats.SelectedChanged++;
            if (old.Self == row.Self) stats.SameSelf++;
            else stats.SelfChanged++;
            if (old.SelectedState == row.SelectedState) stats.SameSelectedState++;
            if (old.Self == row.Self && old.Selected == row.Selected &&
                old.SelectedState == row.SelectedState)
            {
                stats.StableSelfSelected++;
                if (old.Prefix == row.Prefix && old.Visited == row.Visited)
                    stats.PrefixStable++;
                else
                {
                    stats.PrefixChanged++;
                    if (stats.Printed++ < 8)
                        Console.WriteLine(
                            $"PR57_340_PREFIX_CHANGE reader={_pr57340Reader} " +
                            $"selected={row.Selected} oldVisited={old.Visited} " +
                            $"newVisited={row.Visited} clock={VehicleMovementTime:F9} " +
                            "scope=ACTUAL_VISITED_PREFIX_PROJECTION_NOT_WRITE_JOURNAL");
                }
            }
        }
        stats.Previous = row;
    }

    void Pr57340Report(long slices)
    {
        if (!Pr57340Enabled) return;
        foreach (var (id, s) in _pr57340Stats.OrderBy(x => x.Key))
            Console.WriteLine(
                $"PR57_340_SUMMARY slices={slices} reader={id} calls={s.Calls} " +
                $"repeats={s.Repeats} sameSelected={s.SameSelected} " +
                $"sameSelf={s.SameSelf} sameSelectedState={s.SameSelectedState} " +
                $"stableSelfSelected={s.StableSelfSelected} " +
                $"prefixStable={s.PrefixStable} prefixChanged={s.PrefixChanged} " +
                $"selectedChanged={s.SelectedChanged} selfChanged={s.SelfChanged} " +
                $"visited={s.Visited} maxVisited={s.MaxVisited} " +
                "scope=VISITED_PREFIX_ONLY_HASH_NOT_CAUSAL_CLOSURE");
    }
}
''', encoding="utf-8")
print("PR57_340_INJECTED: actual BodyOnWay prefix fingerprints, observational only")
