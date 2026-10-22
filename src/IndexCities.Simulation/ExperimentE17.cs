using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace IndexCities.Simulation;

// E17: CPU budget attribution on the actual integrated 2-second simulation.
// Observational only, transient, not a save field, does not change game rules.
public sealed partial class CitySimulation
{
    public bool E17Profile { get; set; }
    readonly long[] _e17PhaseTicks = new long[10];
    long _e17Steps;
    static readonly string[] E17Names =
    [
        "Vehicles", "PavingMachines", "Citizens", "Construction",
        "MaintenanceAndRelocations", "Infrastructure", "Businesses",
        "Needs", "Calendar", "DecisionsFinanceAndRest"
    ];

    void E17Record(int phase, ref long mark)
    {
        var now = Stopwatch.GetTimestamp();
        _e17PhaseTicks[phase] += now - mark;
        mark = now;
    }

    public string E17Summary()
    {
        var total = _e17PhaseTicks.Sum();
        var sb = new StringBuilder();
        sb.Append("E17_TOTAL steps=").Append(_e17Steps)
          .Append(" measuredSeconds=").Append((total / (double)Stopwatch.Frequency).ToString("F4", CultureInfo.InvariantCulture))
          .Append(" resolution=").Append(Stopwatch.Frequency).AppendLine();
        for (var i = 0; i < _e17PhaseTicks.Length; i++)
        {
            var elapsed = _e17PhaseTicks[i] / (double)Stopwatch.Frequency;
            sb.Append("E17_PHASE name=").Append(E17Names[i])
              .Append(" seconds=").Append(elapsed.ToString("F4", CultureInfo.InvariantCulture))
              .Append(" pct=").Append((total==0 ? 0 : 100.0*_e17PhaseTicks[i]/total).ToString("F2", CultureInfo.InvariantCulture))
              .AppendLine();
        }
        // Amdahl upper bound assumes the named domain becomes literally free;
        // it is NOT an achievable speed-up from an actual code change.
        var traffic = _e17PhaseTicks[0] / (double)Math.Max(1,total);
        var utility = _e17PhaseTicks[5] / (double)Math.Max(1,total);
        sb.Append("E17_UPPER_BOUND perfectVehiclesSpeedup=")
          .Append((1.0/Math.Max(1e-9,1-traffic)).ToString("F3", CultureInfo.InvariantCulture))
          .Append(" perfectVehiclesAndInfrastructureSpeedup=")
          .Append((1.0/Math.Max(1e-9,1-traffic-utility)).ToString("F3", CultureInfo.InvariantCulture))
          .Append(" (zero-cost domains; impossible as written)");
        return sb.ToString();
    }
}
