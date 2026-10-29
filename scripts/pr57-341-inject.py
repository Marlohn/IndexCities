#!/usr/bin/env python3
"""PR57 #341: checkout-only, immediate setter journal for a bounded physical projection.

Every setter remains canonical and unmodified in effect. The observer is installed
only after 17:00 in the pinned REAL replay, and never supplies a gameplay result.
Eight scalar fields are covered; collections, roads, citizens and other writers
are NOT covered. No causal certificate is inferred.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
sim = root / "src/IndexCities.Simulation"

def replace_one(path, old, new):
    content = path.read_text(encoding="utf-8")
    if content.count(old) != 1:
        raise RuntimeError(f"{path}: anchor count {content.count(old)} (expected 1)")
    path.write_text(content.replace(old, new, 1), encoding="utf-8")

model = sim / "Model.cs"
source = model.read_text(encoding="utf-8")
start = source.index("public sealed class Vehicle\n{")
end = source.index("public sealed class Shipment\n{", start)
vehicle = source[start:end]
vehicle = vehicle.replace("public sealed class Vehicle\n{",
    "public sealed class Vehicle\n{\n"
    "    // #341: checkout-only observer, not serialized or consulted by simulation.\n"
    "    public static Action<Vehicle,int>? Pr57341OnWrite;\n", 1)
fields = [
    ("Position", "Point", 0),
    ("Heading", "double", 1),
    ("Status", "VehicleStatus", 2),
    ("RouteIndex", "int", 3),
    ("TowTruckId", "int?", 4),
    ("TowTargetId", "int?", 5),
    ("PassengerCar", "bool", 6),
    ("Machine", "PavingMachine", 7),
]
for name, typ, index in fields:
    old = f"public {typ} {name} {{get;set;}}"
    if vehicle.count(old) != 1:
        raise RuntimeError(f"Vehicle.{name}: expected exactly one property, found {vehicle.count(old)}")
    backing = "_pr57341" + name
    if name == "Position":
        different = (
            f"BitConverter.DoubleToInt64Bits({backing}.X)!=BitConverter.DoubleToInt64Bits(value.X)"
            f"||BitConverter.DoubleToInt64Bits({backing}.Z)!=BitConverter.DoubleToInt64Bits(value.Z)"
        )
    elif name == "Heading":
        different = f"BitConverter.DoubleToInt64Bits({backing})!=BitConverter.DoubleToInt64Bits(value)"
    else:
        different = f"!EqualityComparer<{typ}>.Default.Equals({backing},value)"
    replacement = (
        f"private {typ} {backing};\n"
        f"    public {typ} {name} {{get=>{backing};set{{"
        f"var changed={different};"
        f"{backing}=value;"
        f"if(changed)Pr57341OnWrite?.Invoke(this,{index});"
        f"}}}}"
    )
    vehicle = vehicle.replace(old, replacement, 1)
source = source[:start] + vehicle + source[end:]
model.write_text(source, encoding="utf-8")

shared = sim / "Mobility.SharedSpace.cs"
replace_one(shared,
    "        Pr57261Hit(0);\n        // #305",
    "        Pr57341Begin(v,pr57305Onward);\n"
    "        Pr57261Hit(0);\n        // #305")
replace_one(shared,
    "        if(poses.Count==0)return null;var reach=poses.Max",
    "        if(poses.Count==0){Pr57341End(null);return null;}var reach=poses.Max")
replace_one(shared,
    "            if(census){visited++;Pr57306Read(v,o);}",
    "            if(census){visited++;Pr57306Read(v,o);}\n"
    "            Pr57341Candidate(o);")
replace_one(shared,
    "                    if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);\n                    return o;",
    "                    if(census)Pr57305Body(v,o,poses.Count,visited,eligible,nearNegative,geometry,started);\n"
    "                    Pr57341End(o);\n                    return o;")
replace_one(shared,
    "        if(census)Pr57305Body(v,null,poses.Count,visited,eligible,nearNegative,geometry,started);\n        return null;",
    "        if(census)Pr57305Body(v,null,poses.Count,visited,eligible,nearNegative,geometry,started);\n"
    "        Pr57341End(null);\n        return null;")
replace_one(sim / "Mobility.cs",
    "var onwardBody=BodyOnWay(v,onward,Pr57305Enabled);",
    "var onwardBody=BodyOnWay(v,onward,Pr57305Enabled || Pr57341Enabled);")
replace_one(sim / "Mobility.JunctionReturns.Pr57.cs",
    "        Pr57Junction247Report(slices);",
    "        Pr57Junction247Report(slices);\n        Pr57341Report(slices);")

target = sim / "Mobility.SetterJournal341.Pr57.cs"
if target.exists():
    raise RuntimeError("Refusing to overwrite #341 journal")
target.write_text(r'''namespace IndexCities.Simulation;

// #341: exact immediate notification of CHANGED assignments to eight Vehicle
// scalar properties, including transient and nonlocal writes. Prefix membership
// is only the prior canonical BodyOnWay visit projection, NOT an exact readset.
// No decisions or scheduling depend on this observer.
public sealed partial class CitySimulation
{
    static readonly bool Pr57341Enabled =
        Environment.GetEnvironmentVariable("PR57_341_SETTER_JOURNAL") == "1";

    sealed class Pr57341Reader
    {
        public readonly HashSet<int> Prefix = new();
        public int Selected = -1;
        public long Reads, Intervals, PrefixDirtyIntervals, SelectedDirtyIntervals,
            SelfDirtyIntervals, PrefixWrites, SelectedWrites, SelfWrites,
            CandidateVisits, PrefixSizeTotal, MaxPrefixSize;
        public bool PrefixDirty, SelectedDirty, SelfDirty;
    }

    readonly Dictionary<int, Pr57341Reader> _pr57341Readers = new();
    readonly long[] _pr57341FieldWrites = new long[8];
    readonly long[] _pr57341EphemeralFields = new long[8];
    HashSet<Vehicle>? _pr57341Canonical;
    long _pr57341EphemeralWrites;
    long _pr57341AllWrites, _pr57341PrefixFanout, _pr57341SelectedFanout,
        _pr57341SelfFanout, _pr57341OutsideReads, _pr57341DuringReads;
    bool _pr57341Started, _pr57341Inside;
    int _pr57341ActiveReader = -1;

    void Pr57341Written(Vehicle v, int field)
    {
        // Only objects in the real CityState fleet can invalidate a live read.
        // The engine also constructs temporary Vehicle geometry proxies.
        if (_pr57341Canonical is null || !_pr57341Canonical.Contains(v))
        {
            _pr57341EphemeralWrites++;
            _pr57341EphemeralFields[field]++;
            return;
        }
        _pr57341AllWrites++;
        _pr57341FieldWrites[field]++;
        if (_pr57341Inside) _pr57341DuringReads++;
        else _pr57341OutsideReads++;
        foreach (var (readerId, stats) in _pr57341Readers)
        {
            if (readerId == v.Id)
            {
                stats.SelfWrites++;
                stats.SelfDirty = true;
                _pr57341SelfFanout++;
            }
            if (stats.Selected == v.Id)
            {
                stats.SelectedWrites++;
                stats.SelectedDirty = true;
                _pr57341SelectedFanout++;
            }
            if (stats.Prefix.Contains(v.Id))
            {
                stats.PrefixWrites++;
                stats.PrefixDirty = true;
                _pr57341PrefixFanout++;
            }
        }
    }

    void Pr57341Begin(Vehicle reader, bool onward)
    {
        _pr57341Inside = false;
        _pr57341ActiveReader = -1;
        if (!Pr57341Enabled || !onward ||
            reader.Id is not (1071 or 1243 or 1282 or 1380) ||
            State.Now < new DateTime(2026, 1, 7, 17, 0, 0)) return;
        if (!_pr57341Started)
        {
            // Installed after replay deserialization, never used by physics.
            _pr57341Canonical = new HashSet<Vehicle>(State.Vehicles, ReferenceEqualityComparer.Instance);
            Vehicle.Pr57341OnWrite = Pr57341Written;
            _pr57341Started = true;
        }
        if (!_pr57341Readers.TryGetValue(reader.Id, out var stats))
            _pr57341Readers[reader.Id] = stats = new Pr57341Reader();
        if (stats.Reads > 0)
        {
            stats.Intervals++;
            if (stats.PrefixDirty) stats.PrefixDirtyIntervals++;
            if (stats.SelectedDirty) stats.SelectedDirtyIntervals++;
            if (stats.SelfDirty) stats.SelfDirtyIntervals++;
        }
        stats.PrefixDirty = stats.SelectedDirty = stats.SelfDirty = false;
        stats.Prefix.Clear();
        stats.Reads++;
        _pr57341ActiveReader = reader.Id;
        _pr57341Inside = true;
    }

    void Pr57341Candidate(Vehicle other)
    {
        if (_pr57341ActiveReader < 0) return;
        var stats = _pr57341Readers[_pr57341ActiveReader];
        stats.Prefix.Add(other.Id);
        stats.CandidateVisits++;
    }

    void Pr57341End(Vehicle? selected)
    {
        if (_pr57341ActiveReader < 0) return;
        var stats = _pr57341Readers[_pr57341ActiveReader];
        if (selected is not null) stats.Prefix.Remove(selected.Id);
        stats.Selected = selected?.Id ?? -1;
        stats.PrefixSizeTotal += stats.Prefix.Count;
        stats.MaxPrefixSize = Math.Max(stats.MaxPrefixSize, stats.Prefix.Count);
        _pr57341Inside = false;
        _pr57341ActiveReader = -1;
    }

    void Pr57341Report(long slices)
    {
        if (!Pr57341Enabled) return;
        Console.WriteLine(
            $"PR57_341_WRITES slices={slices} started={_pr57341Started} " +
            $"canonicalChangedAssignments={_pr57341AllWrites} fieldWrites={string.Join(",",_pr57341FieldWrites)} " +
            $"ephemeralChangedAssignments={_pr57341EphemeralWrites} ephemeralFields={string.Join(",",_pr57341EphemeralFields)} " +
            $"prefixFanout={_pr57341PrefixFanout} selectedFanout={_pr57341SelectedFanout} " +
            $"selfFanout={_pr57341SelfFanout} duringReads={_pr57341DuringReads} " +
            $"outsideReads={_pr57341OutsideReads} " +
            "scope=EIGHT_SCALAR_FIELDS_IMMEDIATE_SETTERS_NOT_CAUSAL_CLOSURE");
        foreach (var (id, s) in _pr57341Readers.OrderBy(x => x.Key))
            Console.WriteLine(
                $"PR57_341_READER slices={slices} reader={id} reads={s.Reads} " +
                $"intervals={s.Intervals} prefixDirtyIntervals={s.PrefixDirtyIntervals} " +
                $"selectedDirtyIntervals={s.SelectedDirtyIntervals} selfDirtyIntervals={s.SelfDirtyIntervals} " +
                $"prefixWrites={s.PrefixWrites} selectedWrites={s.SelectedWrites} " +
                $"selfWrites={s.SelfWrites} visits={s.CandidateVisits} " +
                $"prefixSizeTotal={s.PrefixSizeTotal} maxPrefixSize={s.MaxPrefixSize} " +
                "scope=PREVIOUS_VISITED_PREFIX_EIGHT_FIELDS_NOT_ALL_WRITERS");
    }
}
''', encoding="utf-8")
print("PR57_341_INJECTED: immediate eight-field Vehicle setters + prior-prefix fanout")
