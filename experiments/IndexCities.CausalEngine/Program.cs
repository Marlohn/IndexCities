using System.Diagnostics;
using System.Text.Json;

namespace IndexCities.CausalEngine;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            int sims = 1000, days = 7, seed = 42;
            string? history = null;
            bool adversarial = false;
            foreach (var arg in args)
            {
                if (arg.StartsWith("--sims=")) sims = int.Parse(arg[7..]);
                else if (arg.StartsWith("--days=")) days = int.Parse(arg[7..]);
                else if (arg.StartsWith("--seed=")) seed = int.Parse(arg[7..]);
                else if (arg.StartsWith("--history=")) history = arg[10..];
                else if (arg == "--adversarial") adversarial = true;
                else if (arg == "--self-test") return EngineSelfTest.Run();
                else throw new ArgumentException("Unknown argument: " + arg);
            }
            if (sims < 4 || sims > 100_000 || days < 1 || days > 29_220)
                throw new ArgumentOutOfRangeException("Requires 4..100000 SIMs and 1..29220 days.");
            var config = new LabConfig(sims, days, seed, history, adversarial);
            using var engine = new CausalCity(config);
            var sw = Stopwatch.StartNew();
            engine.Run();
            sw.Stop();
            engine.AssertConservation();
            Console.WriteLine(JsonSerializer.Serialize(engine.Report(sw.Elapsed.TotalSeconds),
                new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("EXPERIMENT FAILED: " + ex);
            return 1;
        }
    }
}