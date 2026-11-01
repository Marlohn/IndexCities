namespace IndexCities.Simulation;

// PR57 #595: opt-in physical membership frontier, original insertion order.
// This mirrors only Vehicle references; all physical fields remain live.
// A tow created mid-slice is appended synchronously before the next reader.
// Unknown count changes fall back to canonical State.Vehicles. The shadow
// checks every reference, not only counts, and fails on a mismatch.
public sealed partial class CitySimulation
{
    static readonly bool Pr57595MembershipEnabled =
        Environment.GetEnvironmentVariable("PR57_595_MEMBERSHIP") == "1";
    static readonly bool Pr57595MembershipShadow =
        Environment.GetEnvironmentVariable("PR57_595_SHADOW") == "1";
    List<Vehicle>? _pr57595Members;
    bool _pr57595Invalid;
    long _pr57595Slices, _pr57595Reads, _pr57595Publishes, _pr57595Fallbacks;

    void Pr57595MembershipBeginSlice()
    {
        if (!Pr57595MembershipEnabled) return;
        _pr57595Members = State.Vehicles.ToList();
        _pr57595Invalid = false;
    }

    void Pr57595MembershipPublishAdded(Vehicle vehicle)
    {
        if (_pr57595Members is null || _pr57595Invalid) return;
        if (State.Vehicles.Count != _pr57595Members.Count + 1 ||
            !ReferenceEquals(State.Vehicles[^1], vehicle))
        {
            _pr57595Invalid = true;
            return;
        }
        _pr57595Members.Add(vehicle);
        _pr57595Publishes++;
    }

    List<Vehicle> Pr57595MembershipCandidates()
    {
        var members = _pr57595Members;
        if (members is null || _pr57595Invalid || members.Count != State.Vehicles.Count)
        {
            _pr57595Fallbacks++;
            return State.Vehicles;
        }
        if (Pr57595MembershipShadow)
            for (var i = 0; i < members.Count; i++)
                if (!ReferenceEquals(members[i], State.Vehicles[i]))
                    throw new InvalidOperationException(
                        $"PR57_595_MEMBERSHIP_DIVERGENCE index={i} slice={_pr57595Slices}");
        _pr57595Reads++;
        return members;
    }

    void Pr57595MembershipEndSlice()
    {
        if (_pr57595Members is null) return;
        _pr57595Members = null;
        if (++_pr57595Slices % 100 == 0)
            Console.WriteLine(
                $"PR57_595_MEMBERSHIP slices={_pr57595Slices} reads={_pr57595Reads} " +
                $"publishes={_pr57595Publishes} fallbacks={_pr57595Fallbacks} " +
                $"shadow={Pr57595MembershipShadow}");
    }
}
