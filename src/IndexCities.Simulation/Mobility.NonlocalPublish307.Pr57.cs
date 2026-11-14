namespace IndexCities.Simulation;

// PR57 #307: opt-in, observation-only publication for two ACTUAL nonlocal tow
// writers. No canonical reader uses this projection; other writers, public
// edits, pedestrians, road geometry and reservations are NOT covered.
public sealed partial class CitySimulation
{
    // Instance flag permits adversarial tests without process-wide env changes.
    bool _pr57307Nonlocal = Environment.GetEnvironmentVariable("PR57_307_NONLOCAL_PUBLISH") == "1";
    long _pr57307Published, _pr57307PublicationTicks;

    void Pr57307Publish(Vehicle target)
    {
        if (!_pr57307Nonlocal || !_pr57306Active) return;
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_pr57306Index.TryGetValue(target.Id, out var index))
        {
            _pr57306Rows![index] = Pr57306Capture(target);
            _pr57307Published++;
        }
        _pr57307PublicationTicks += System.Diagnostics.Stopwatch.GetTimestamp() - started;
    }
}
