namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // #570: an exact ordered physical-actor frontier, opt-in and never serialized.
    // Validate references, IDs and WaitingSince bits at EVERY movement slice.
    // External edits, list replacement, and all intra-slice writers are covered
    // by this complete input check. Never skip actors, scans or global slices.
    static readonly bool Pr57OrderFrontier570Enabled =
        Environment.GetEnvironmentVariable("PR57_ORDER_FRONTIER_570") == "1";
    static readonly bool Pr57OrderFrontier570Diag =
        Environment.GetEnvironmentVariable("PR57_ORDER_FRONTIER_570_DIAG") == "1";
    Vehicle[] _pr57Order570Source = [], _pr57Order570Sorted = [];
    long[] _pr57Order570WaitBits = [];
    int[] _pr57Order570Ids = [];
    long _pr57Order570Calls, _pr57Order570Reused, _pr57Order570Resorted;

    Vehicle[] Pr57OrderedVehicles570()
    {
        if (!Pr57OrderFrontier570Enabled)
        {
            var canonical = State.Vehicles.ToArray();
            Array.Sort(canonical, static (a,b) =>
                a.WaitingSince.CompareTo(b.WaitingSince) is var c and not 0 ? c : a.Id.CompareTo(b.Id));
            return canonical;
        }
        var vehicles = State.Vehicles;
        var count = vehicles.Count;
        var unchanged = _pr57Order570Calls > 0 && _pr57Order570Source.Length == count;
        if (unchanged)
            for (var i = 0; i < count; i++)
            {
                var v = vehicles[i];
                if (!ReferenceEquals(v, _pr57Order570Source[i]) ||
                    v.Id != _pr57Order570Ids[i] ||
                    BitConverter.DoubleToInt64Bits(v.WaitingSince) != _pr57Order570WaitBits[i])
                { unchanged = false; break; }
            }
        if (unchanged) _pr57Order570Reused++;
        else
        {
            if (_pr57Order570Source.Length != count)
            {
                _pr57Order570Source = new Vehicle[count];
                _pr57Order570Sorted = new Vehicle[count];
                _pr57Order570WaitBits = new long[count];
                _pr57Order570Ids = new int[count];
            }
            for (var i = 0; i < count; i++)
            {
                var v = vehicles[i];
                _pr57Order570Source[i] = _pr57Order570Sorted[i] = v;
                _pr57Order570Ids[i] = v.Id;
                _pr57Order570WaitBits[i] = BitConverter.DoubleToInt64Bits(v.WaitingSince);
            }
            Array.Sort(_pr57Order570Sorted, static (a,b) =>
                a.WaitingSince.CompareTo(b.WaitingSince) is var c and not 0 ? c : a.Id.CompareTo(b.Id));
            _pr57Order570Resorted++;
        }
        _pr57Order570Calls++;
        if (Pr57OrderFrontier570Diag && _pr57Order570Calls % 200 == 0)
            Console.WriteLine($"PR57_ORDER_FRONTIER_570_DIAG active=True calls={_pr57Order570Calls} reuses={_pr57Order570Reused} resorts={_pr57Order570Resorted} vehicles={count}");
        return _pr57Order570Sorted;
    }
}
