using System.Diagnostics;
using System.Text.Json;

namespace IndexCities.CausalEngine;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length >= 2 && args[0] == "inspect")
            {
                var file = args[1];
                var person = args.Length >= 3 ? int.Parse(args[2]) : 0;
                var fromDay = args.Length >= 4 ? int.Parse(args[3]) : 0;
                var untilDay = args.Length >= 5 ? int.Parse(args[4]) : 29220;
                var limit = args.Length >= 6 ? int.Parse(args[5]) : 100;
                if (fromDay < 0 || untilDay < fromDay || limit < 1)
                    throw new ArgumentException("Invalid history pagination/window");
                long count = 0, emitted = 0;
                foreach (var fact in HistorySink.Read(file))
                {
                    if (fact.Owner != person || fact.At < fromDay * CausalCity.DaySeconds ||
                        fact.At >= untilDay * CausalCity.DaySeconds) continue;
                    count++;
                    if (emitted++ < limit)
                        Console.WriteLine(JsonSerializer.Serialize(new {
                            day = fact.At / CausalCity.DaySeconds,
                            secondOfDay = fact.At % CausalCity.DaySeconds,
                            sim = fact.Owner, kind = fact.Type, amount = fact.Amount
                        }));
                }
                Console.Error.WriteLine($"HISTORY_QUERY sim={person} found={count} emitted={Math.Min(count, limit)}");
                return 0;
            }
            int sims = 1000, days = 7, seed = 42;
            string? history = null;
            bool adversarial = false, wageFeedback = false;
            foreach (var arg in args)
            {
                if (arg.StartsWith("--sims=")) sims = int.Parse(arg[7..]);
                else if (arg.StartsWith("--days=")) days = int.Parse(arg[7..]);
                else if (arg.StartsWith("--seed=")) seed = int.Parse(arg[7..]);
                else if (arg.StartsWith("--history=")) history = arg[10..];
                else if (arg == "--adversarial") adversarial = true;
                else if (arg == "--wage-feedback") wageFeedback = true;
                else if (arg == "--self-test") return EngineSelfTest.Run();
                else throw new ArgumentException("Unknown argument: " + arg);
            }
            if (sims < 4 || sims > 100_000 || days < 1 || days > 29_220)
                throw new ArgumentOutOfRangeException("Requires 4..100000 SIMs and 1..29220 days.");
            var config = new LabConfig(sims, days, seed, history, adversarial, wageFeedback);
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