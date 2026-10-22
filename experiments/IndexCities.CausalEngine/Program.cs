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
            int sims = 1000, days = 7, seed = 42, annualLoadCycles = 0;
            string? history = null;
            bool adversarial = false, wageFeedback = false, cohortRenewal = false, demandStaffing = false, vacancyMarket = false, freightLabor = false, constructionPilot = false, fairHouseholdHiring = false, demographicUpperBound = false;
            foreach (var arg in args)
            {
                if (arg.StartsWith("--sims=")) sims = int.Parse(arg[7..]);
                else if (arg.StartsWith("--days=")) days = int.Parse(arg[7..]);
                else if (arg.StartsWith("--seed=")) seed = int.Parse(arg[7..]);
                else if (arg.StartsWith("--history=")) history = arg[10..];
                else if (arg.StartsWith("--annual-load-cycles=")) annualLoadCycles = int.Parse(arg[21..]);
                else if (arg == "--adversarial") adversarial = true;
                else if (arg == "--wage-feedback") wageFeedback = true;
                else if (arg == "--cohort-renewal") cohortRenewal = true;
                else if (arg == "--demand-staffing") demandStaffing = true;
                else if (arg == "--vacancy-market") vacancyMarket = true;
                else if (arg == "--freight-labor") freightLabor = true;
                else if (arg == "--construction-pilot") constructionPilot = true;
                else if (arg == "--fair-household-hiring") fairHouseholdHiring = true;
                else if (arg == "--demographic-upper-bound") demographicUpperBound = true;
                else if (arg == "--self-test") return EngineSelfTest.Run();
                else throw new ArgumentException("Unknown argument: " + arg);
            }
            if (demographicUpperBound)
            {
                if (args.Length != 1) throw new ArgumentException("Demographic bound takes no city options");
                return DemographicUpperBound.Run();
            }
            if (sims < 4 || sims > 100_000 || days < 1 || days > 29_220)
                throw new ArgumentOutOfRangeException("Requires 4..100000 SIMs and 1..29220 days.");
            if (fairHouseholdHiring && !vacancyMarket)
                throw new ArgumentException("Fair household hiring is only for --vacancy-market");
            if (constructionPilot && !vacancyMarket)
                throw new ArgumentException("Construction pilot requires --vacancy-market for real jobs");
            if (freightLabor && !vacancyMarket)
                throw new ArgumentException("Real freight labor requires --vacancy-market");
            if (vacancyMarket && (demandStaffing || wageFeedback))
                throw new ArgumentException(
                    "Vacancy market requires its own payroll policy, not the two earlier wage counterfactuals");
            // Isolate computational feasibility from the pathological finances
            // and demography of the incomplete laboratory economy. This is a
            // repeatable *load benchmark*, emphatically NOT 80 continuous
            // years of one city or an implementation of the full product.
            if (annualLoadCycles != 0)
            {
                if (annualLoadCycles < 1 || annualLoadCycles > 80 ||
                    history is not null || wageFeedback || cohortRenewal || demandStaffing || vacancyMarket || freightLabor || constructionPilot || fairHouseholdHiring || days != 7)
                    throw new ArgumentException(
                        "Annual workload mode requires 1..80 cycles, no --days, history, or gameplay counterfactuals");
                var watch = Stopwatch.StartNew();
                var lowestPopulation = sims;
                long representedDays = 0, actualTrips = 0, actualSales = 0, actualBirths = 0;
                for (var year = 0; year < annualLoadCycles; year++)
                {
                    // The 80 independent, living annual workloads cover the
                    // exact Gregorian day count for 2020 through 2099.
                    var length = DateTime.IsLeapYear(2020 + year) ? 366 : 365;
                    using var annual = new CausalCity(new LabConfig(sims, length, seed, null, adversarial));
                    annual.Run();
                    annual.AssertConservation();
                    if (annual.MinimumActive < sims)
                        throw new InvalidOperationException(
                            $"Load benchmark lost live agents during cycle {year}");
                    representedDays += length;
                    lowestPopulation = Math.Min(lowestPopulation, annual.MinimumActive);
                    actualTrips += annual.Journeys;
                    actualSales += annual.Sales;
                    actualBirths += annual.Births;
                    if ((year + 1) % 10 == 0)
                        Console.Error.WriteLine($"LAB_LOAD years={year + 1} days={representedDays} trips={actualTrips}");
                }
                watch.Stop();
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    status = "TECHNICAL_REPEAT_LOAD_ONLY_NOT_CONTINUOUS_CITY",
                    warning = "Independent one-year cities reset each cycle. NO 80-year continuous demographics, economic stability or 80-year persistent history; NOT a SPEC-complete validation.",
                    baselinePopulationPerIndependentCycle = sims,
                    minLivingAgentsAcrossIndependentCycles = lowestPopulation,
                    independentAnnualCycles = annualLoadCycles,
                    representedGregorianDays = representedDays,
                    totalIndividualJourneys = actualTrips,
                    totalRealFoodSalesUnitsAcrossCycles = actualSales,
                    totalBirthsAcrossCycles = actualBirths,
                    allCycleMoneyAndFoodConserved = true,
                    historyPersistent = false,
                    wallSeconds = watch.Elapsed.TotalSeconds,
                    equivalentHoursAtMeasuredWorkload = watch.Elapsed.TotalHours
                }, new JsonSerializerOptions { WriteIndented = true }));
                return 0;
            }
            var config = new LabConfig(sims, days, seed, history, adversarial, wageFeedback, cohortRenewal, demandStaffing, vacancyMarket, freightLabor, constructionPilot, fairHouseholdHiring);
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