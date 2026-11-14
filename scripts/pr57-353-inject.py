#!/usr/bin/env python3
"""PR57 #353: checkout-only observational canonical setter journal, two REAL windows.

No decisions or state writes are removed/reordered. NOT a complete causal journal:
list in-place changes, citizens, signals, roads, editor, nested objects are missing.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
model = root / 'src/IndexCities.Simulation/Model.cs'
mob = root / 'src/IndexCities.Simulation/Mobility.cs'

def patch_one(text, old, new):
    assert text.count(old) == 1, (old, text.count(old))
    return text.replace(old, new, 1)

source = model.read_text()
head, tail = source.split('public sealed class Vehicle\n{', 1)
assert 'Pr57353Write' not in tail
props = {
    'Position': ('Point', None, 'old!=value'),
    'Heading': ('double', None, 'BitConverter.DoubleToInt64Bits(old)!=BitConverter.DoubleToInt64Bits(value)'),
    'Status': ('VehicleStatus', None, 'old!=value'),
    'Route': ('List<int>', '=[]', '!ReferenceEquals(old,value)'),
    'RouteIndex': ('int', None, 'old!=value'),
    'DockPosition': ('Point?', None, 'old!=value'),
    'SitePathBeforeRoad': ('bool', None, 'old!=value'),
    'SitePathIndex': ('int', None, 'old!=value'),
    'WaitingSince': ('double', None, 'BitConverter.DoubleToInt64Bits(old)!=BitConverter.DoubleToInt64Bits(value)'),
    'IntersectionClaim': ('int?', None, 'old!=value'),
    'ManoeuvreDock': ('Point?', None, 'old!=value'),
    'MovementAdmission': ('LocalMovementAdmission?', None, '!ReferenceEquals(old,value)'),
    'ManoeuvreSpaces': ('List<int>', '=[]', '!ReferenceEquals(old,value)'),
    'DockCorridor': ('List<MovementPose>', '=[]', '!ReferenceEquals(old,value)'),
    'Detour': ('List<Point>', '=[]', '!ReferenceEquals(old,value)'),
    'TowPhase': ('TowPhase', None, 'old!=value'),
    'TowTargetId': ('int?', None, 'old!=value'),
    'TowTruckId': ('int?', None, 'old!=value'),
}
for name, (typ, init, predicate) in props.items():
    old = f'public {typ} {name} {{get;set;}}' + (init or '') + (';' if init else '')
    field = '_pr57353' + name
    new = (f'private {typ} {field}' + (init or '') + ';\n    '
           f'public {typ} {name} {{get=>{field};set{{var old={field};{field}=value;'
           f'if({predicate})Pr57353Write?.Invoke(this,"{name}");}}}}')
    tail = patch_one(tail, old, new)
tail = '    public static Action<Vehicle,string>? Pr57353Write; // #353 injected observer only\n' + tail
model.write_text(head + 'public sealed class Vehicle\n{\n' + tail)

s = mob.read_text()
s = patch_one(s, 'Pr57ExclusiveBeginSlice(dt,operationsDt,scanProfile is not null);',
              'Pr57ExclusiveBeginSlice(dt,operationsDt,scanProfile is not null);\n        Pr57353BeginSlice();')
s = patch_one(s, 'Pr57ExclusiveEndSlice();',
              'Pr57353EndSlice();\n        Pr57ExclusiveEndSlice();')
s = patch_one(s, 'Pr57351Observe(v,pr57LaneList,blocker,leaderDistance,step,enteringLane);',
              'Pr57351Observe(v,pr57LaneList,blocker,leaderDistance,step,enteringLane);\n                Pr57353Read(v,blocker,leaderDistance,step,enteringLane);')
mob.write_text(s)

out = root / 'src/IndexCities.Simulation/Mobility.WriterJournal353.Pr57.cs'
assert not out.exists()
out.write_text('''namespace IndexCities.Simulation;

// #353: opt-in, bounded writer-side event probe for 20 queue readers in two
// consecutive three-slice windows. No engine decision consumes these events.
// Auto-property setter events do NOT cover in-place list/object mutation,
// roads, pedestrians, signal time, editor or non-vehicle writers. NOT closure.
public sealed partial class CitySimulation
{
    static readonly bool Pr57353Enabled =
        Environment.GetEnvironmentVariable("PR57_353_JOURNAL") == "1";
    readonly record struct Pr57353ReadMark(long Version, double Time, Point Entry,
        int RoadEpoch, int FleetCount);
    readonly record struct Pr57353Event(long Version, int Id, string Field);
    HashSet<Vehicle>? _pr57353Canonical;
    readonly Dictionary<int,Pr57353ReadMark> _pr57353Readers = new();
    readonly HashSet<int> _pr57353Seen = new();
    long _pr57353Slice = -1;
    readonly List<Pr57353Event> _pr57353Events = new();
    readonly Dictionary<string,long> _pr57353Fields = new(StringComparer.Ordinal);
    bool _pr57353Active;
    long _pr57353Version, _pr57353Temp, _pr57353HookTicks;
    long _pr57353Eligible, _pr57353Compared, _pr57353Dirty, _pr57353Clean;
    long _pr57353ExposedEvents, _pr57353SelfEvents, _pr57353WinnerEvents;
    long _pr57353OtherEvents, _pr57353ClockChanged, _pr57353EntryChanged;
    long _pr57353RoadChanged, _pr57353FleetChanged;
    long _pr57353Window;

    void Pr57353BeginSlice()
    {
        if (!Pr57353Enabled) return;
        var n = _pr57ExclusiveEligibleSlices;
        if (!_pr57ExclusiveActive || n is not (40001 or 50001)) return;
        if (_pr57353Active) throw new InvalidOperationException("#353 overlapping window");
        _pr57353Active = true;
        _pr57353Window = n;
        _pr57353Canonical = new HashSet<Vehicle>(State.Vehicles,
            System.Collections.Generic.ReferenceEqualityComparer.Instance);
        _pr57353Readers.Clear(); _pr57353Seen.Clear(); _pr57353Slice = -1;
        _pr57353Events.Clear(); _pr57353Fields.Clear();
        _pr57353Version = _pr57353Temp = _pr57353HookTicks = 0;
        _pr57353Eligible = _pr57353Compared = _pr57353Dirty = _pr57353Clean = 0;
        _pr57353ExposedEvents = _pr57353SelfEvents = _pr57353WinnerEvents = 0;
        _pr57353OtherEvents = _pr57353ClockChanged = _pr57353EntryChanged = 0;
        _pr57353RoadChanged = _pr57353FleetChanged = 0;
        Vehicle.Pr57353Write = Pr57353Write;
    }

    void Pr57353Write(Vehicle vehicle, string field)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_pr57353Canonical?.Contains(vehicle) == true)
        {
            _pr57353Version++;
            _pr57353Events.Add(new(_pr57353Version, vehicle.Id, field));
            _pr57353Fields.TryGetValue(field, out var count);
            _pr57353Fields[field] = count + 1;
        }
        else _pr57353Temp++;
        _pr57353HookTicks += System.Diagnostics.Stopwatch.GetTimestamp() - start;
    }

    void Pr57353Read(Vehicle reader, Vehicle? winner, double distance,
        double step, bool entering)
    {
        if (!_pr57353Active || !_pr57ExclusiveActive || entering ||
            winner is null || winner.Status == VehicleStatus.Stranded ||
            distance > 8 || step > .00001) return;
        var slice = _pr57ExclusiveEligibleSlices;
        if (_pr57353Slice != slice) { _pr57353Slice = slice; _pr57353Seen.Clear(); }
        if (!_pr57353Seen.Add(reader.Id)) return;
        _pr57353Eligible++;
        var now = new Pr57353ReadMark(_pr57353Version, VehicleMovementTime,
            State.Entry, _roadEpoch, State.Vehicles.Count);
        if (_pr57353Readers.TryGetValue(reader.Id, out var old))
        {
            _pr57353Compared++;
            if (now.Version == old.Version) _pr57353Clean++;
            else _pr57353Dirty++;
            if (now.Time != old.Time) _pr57353ClockChanged++;
            if (now.Entry != old.Entry) _pr57353EntryChanged++;
            if (now.RoadEpoch != old.RoadEpoch) _pr57353RoadChanged++;
            if (now.FleetCount != old.FleetCount) _pr57353FleetChanged++;
            foreach (var e in _pr57353Events)
            {
                if (e.Version <= old.Version) continue;
                _pr57353ExposedEvents++;
                if (e.Id == reader.Id) _pr57353SelfEvents++;
                else if (e.Id == winner.Id) _pr57353WinnerEvents++;
                else _pr57353OtherEvents++;
            }
        }
        _pr57353Readers[reader.Id] = now;
    }

    void Pr57353EndSlice()
    {
        if (!_pr57353Active) return;
        var n = _pr57ExclusiveEligibleSlices;
        if (n != _pr57353Window + 2) return;
        Vehicle.Pr57353Write = null;
        _pr57353Active = false;
        var ms = _pr57353HookTicks * 1000.0 /
            System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine(
            $"PR57_353_JOURNAL window={_pr57353Window} eligible={_pr57353Eligible} " +
            $"compared={_pr57353Compared} clean={_pr57353Clean} dirty={_pr57353Dirty} " +
            $"canonicalEvents={_pr57353Version} tempEvents={_pr57353Temp} " +
            $"exposedEvents={_pr57353ExposedEvents} selfEvents={_pr57353SelfEvents} " +
            $"winnerEvents={_pr57353WinnerEvents} otherEvents={_pr57353OtherEvents} " +
            $"clockChanged={_pr57353ClockChanged} entryChanged={_pr57353EntryChanged} " +
            $"roadChanged={_pr57353RoadChanged} fleetChanged={_pr57353FleetChanged} " +
            $"hookMs={ms:F6} fields={string.Join(",",_pr57353Fields.OrderBy(x=>x.Key).Select(x=>$"{x.Key}:{x.Value}"))} " +
            "scope=SELECTED_SCALAR_AND_REFERENCE_SETTERS_ONLY " +
            "nestedCollectionMutationUnseen=True externalDependenciesUnclosed=True " +
            "noSkippedSlices=True observational=True intrusive=True");
        _pr57353Canonical = null;
        _pr57353Readers.Clear(); _pr57353Events.Clear();
    }
}
''')
print('PR57_353_INJECTED selected canonical setter journal; not causal closure')
