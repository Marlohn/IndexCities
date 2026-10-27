namespace IndexCities.Simulation;

// PR57 #221: bounded observational signature of the *selected* s5 physical
// reads and outcomes. This is not an exact dependency set or a cache key:
// reservation list contents, negative predicates, road edits, time and
// transient writes by other actors remain outside this signature.
public sealed partial class CitySimulation
{
    static readonly bool Pr57S5TemporalEnabled =
        Environment.GetEnvironmentVariable("PR57_221_S5_TEMPORAL") == "1";

    readonly record struct Pr57S5Record(
        ulong Signature, long SiteCalls, long SiteZero, long SiteRestricted,
        long CrossingCalls, long CrossingCandidates, long CrossingRestricted,
        long TurnCalls, long TurnAccepted, long ClockBits, bool SignalPhase);

    readonly Dictionary<int, Pr57S5Record> _pr57S5Previous = new();
    readonly Dictionary<int, Pr57S5Record> _pr57S5Current = new();
    bool _pr57S5Vehicle, _pr57S5Stage;
    ulong _pr57S5Hash;
    long _pr57S5SiteCalls, _pr57S5SiteZero, _pr57S5SiteRestricted;
    long _pr57S5CrossingCalls, _pr57S5CrossingCandidates, _pr57S5CrossingRestricted;
    long _pr57S5TurnCalls, _pr57S5TurnAccepted, _pr57S5ClockBits;
    bool _pr57S5SignalPhase;
    long _pr57S5Repeated, _pr57S5Same, _pr57S5Changed, _pr57S5ClockAdvanced;
    long _pr57S5PhaseChanged, _pr57S5CrossingReadVisits, _pr57S5SiteVisits;
    long _pr57S5ZeroSiteVisits, _pr57S5RestrictedSiteVisits, _pr57S5RestrictedCrossingVisits;
    long _pr57S5TurnVisits, _pr57S5AcceptedTurnVisits;

    void Pr57S5BeginSlice()
    {
        if (!Pr57S5TemporalEnabled || !_pr57ExecActive) return;
        if (_pr57ExclusiveSamples == 1) _pr57S5Previous.Clear();
        _pr57S5Current.Clear();
        _pr57S5Repeated = _pr57S5Same = _pr57S5Changed = _pr57S5ClockAdvanced = 0;
        _pr57S5PhaseChanged = _pr57S5CrossingReadVisits = _pr57S5SiteVisits = 0;
        _pr57S5ZeroSiteVisits = _pr57S5RestrictedSiteVisits = 0;
        _pr57S5RestrictedCrossingVisits = _pr57S5TurnVisits = _pr57S5AcceptedTurnVisits = 0;
    }

    void Pr57S5BeginVehicle()
    {
        if (!Pr57S5TemporalEnabled || !_pr57ExecVehicleActive) return;
        _pr57S5Vehicle = true;
        _pr57S5Stage = false;
        _pr57S5Hash = 14695981039346656037UL;
        _pr57S5SiteCalls = _pr57S5SiteZero = _pr57S5SiteRestricted = 0;
        _pr57S5CrossingCalls = _pr57S5CrossingCandidates = _pr57S5CrossingRestricted = 0;
        _pr57S5TurnCalls = _pr57S5TurnAccepted = 0;
        _pr57S5ClockBits = 0;
        _pr57S5SignalPhase = false;
    }

    void Pr57S5Mark(int stage)
    {
        if (!_pr57S5Vehicle) return;
        _pr57S5Stage = stage == 5;
        if (_pr57S5Stage)
        {
            // Capture the physical clock separately: it advances even if the
            // selected geometric reads and outcomes below are identical.
            var t = VehicleMovementTime;
            _pr57S5ClockBits = BitConverter.DoubleToInt64Bits(t);
            _pr57S5SignalPhase = t % 60 < 30;
        }
    }

    void Pr57S5SiteObserve(Vehicle vehicle, double requested, double allowed,
        bool machinesOnly, int reservations, int owners)
    {
        if (!_pr57S5Stage) return;
        _pr57S5SiteCalls++;
        if (requested == 0) _pr57S5SiteZero++;
        if (allowed < requested - 0.000001) _pr57S5SiteRestricted++;
        var h = _pr57S5Hash;
        h = Pr57PoseMix(h, 11);
        h = Pr57PoseMix(h, vehicle.Id);
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(requested));
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(allowed));
        h = Pr57PoseMix(h, machinesOnly ? 1 : 0);
        h = Pr57PoseMix(h, reservations);
        _pr57S5Hash = Pr57PoseMix(h, owners);
    }

    void Pr57S5CrossingObserve(Vehicle vehicle, double requested, double allowed,
        List<Vehicle>? candidates)
    {
        if (!_pr57S5Stage) return;
        _pr57S5CrossingCalls++;
        if (allowed < requested - 0.000001) _pr57S5CrossingRestricted++;
        var h = _pr57S5Hash;
        h = Pr57PoseMix(h, 17);
        h = Pr57PoseMix(h, vehicle.Id);
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(requested));
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(allowed));
        h = Pr57PoseMix(h, candidates?.Count ?? -1);
        if (candidates != null)
            foreach (var other in candidates)
            {
                _pr57S5CrossingCandidates++;
                h = Pr57PoseMix(h, other.Id);
                h = Pr57PoseMix(h, (int)other.Status);
                h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(other.Position.X));
                h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(other.Position.Z));
                h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(other.Heading));
                h = Pr57PoseMix(h, other.IntersectionClaim ?? -1);
            }
        _pr57S5Hash = h;
    }

    void Pr57S5TurnObserve(Vehicle vehicle, double requested, bool accepted)
    {
        if (!_pr57S5Stage) return;
        _pr57S5TurnCalls++;
        if (accepted) _pr57S5TurnAccepted++;
        var h = Pr57PoseMix(_pr57S5Hash, 23);
        h = Pr57PoseMix(h, vehicle.Id);
        h = Pr57PoseMix(h, BitConverter.DoubleToInt64Bits(requested));
        _pr57S5Hash = Pr57PoseMix(h, accepted ? 1 : 0);
    }

    void Pr57S5FinishVehicle(int id, bool repeatedFull)
    {
        if (!_pr57S5Vehicle) return;
        _pr57S5Vehicle = _pr57S5Stage = false;
        var now = new Pr57S5Record(_pr57S5Hash, _pr57S5SiteCalls,
            _pr57S5SiteZero, _pr57S5SiteRestricted, _pr57S5CrossingCalls,
            _pr57S5CrossingCandidates, _pr57S5CrossingRestricted,
            _pr57S5TurnCalls, _pr57S5TurnAccepted,
            _pr57S5ClockBits, _pr57S5SignalPhase);
        _pr57S5Current[id] = now;
        if (!repeatedFull || !_pr57S5Previous.TryGetValue(id, out var previous)) return;
        _pr57S5Repeated++;
        if (previous.Signature == now.Signature &&
            previous.SiteCalls == now.SiteCalls &&
            previous.CrossingCalls == now.CrossingCalls &&
            previous.TurnCalls == now.TurnCalls) _pr57S5Same++;
        else _pr57S5Changed++;
        if (previous.ClockBits != now.ClockBits) _pr57S5ClockAdvanced++;
        if (previous.SignalPhase != now.SignalPhase) _pr57S5PhaseChanged++;
        _pr57S5SiteVisits += now.SiteCalls;
        _pr57S5ZeroSiteVisits += now.SiteZero;
        _pr57S5RestrictedSiteVisits += now.SiteRestricted;
        _pr57S5CrossingReadVisits += now.CrossingCandidates;
        _pr57S5RestrictedCrossingVisits += now.CrossingRestricted;
        _pr57S5TurnVisits += now.TurnCalls;
        _pr57S5AcceptedTurnVisits += now.TurnAccepted;
    }

    void Pr57S5EndSlice()
    {
        if (!Pr57S5TemporalEnabled || !_pr57ExecActive) return;
        Console.WriteLine(
            $"PR57_221_S5 sample={_pr57ExclusiveSamples} eligibleSlice={_pr57ExclusiveEligibleSlices} " +
            $"repeatedFull={_pr57S5Repeated} sameSelectedSignature={_pr57S5Same} " +
            $"changedSelectedSignature={_pr57S5Changed} clockAdvanced={_pr57S5ClockAdvanced} " +
            $"signalPhaseChanged={_pr57S5PhaseChanged} siteCalls={_pr57S5SiteVisits} " +
            $"zeroSiteCalls={_pr57S5ZeroSiteVisits} restrictedSiteCalls={_pr57S5RestrictedSiteVisits} " +
            $"crossingCandidates={_pr57S5CrossingReadVisits} " +
            $"restrictedCrossingCalls={_pr57S5RestrictedCrossingVisits} " +
            $"turnCalls={_pr57S5TurnVisits} acceptedTurns={_pr57S5AcceptedTurnVisits} " +
            "scope=selectedS5OutcomesAndCrossingCandidates " +
            "notExactReadset=True negativeInputsOmitted=True timeOmittedFromSignature=True " +
            "transientWritersOmitted=True observational=True intrusive=True");
        _pr57S5Previous.Clear();
        foreach (var pair in _pr57S5Current) _pr57S5Previous.Add(pair.Key, pair.Value);
    }
}
