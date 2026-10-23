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
            int retailBasketsPerShift = 5, municipalCollectionIntervalDays = 6;
            bool adversarial = false, wageFeedback = false, cohortRenewal = false, demandStaffing = false, vacancyMarket = false, freightLabor = false, constructionPilot = false, fairHouseholdHiring = false, demographicUpperBound = false, costBasedFoodPrice = false, capacityPricedFood = false, attendanceAwareFarms = false, crossFarmFreight = false, farmCostPricing = false, boundedRetailService = false, basketRetailService = false, retailShopFallback = false, municipalSanitationPilot = false, municipalWasteTreatmentPilot = false, fairSanitationDispatch = false, attendanceAwareMunicipalShifts = false, taxInclusiveProductPrice = false;
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
                else if (arg == "--cost-based-food-price") costBasedFoodPrice = true;
                else if (arg == "--capacity-priced-food") { capacityPricedFood = true; costBasedFoodPrice = true; }
                else if (arg == "--attendance-aware-farms") attendanceAwareFarms = true;
                else if (arg == "--cross-farm-freight") crossFarmFreight = true;
                else if (arg == "--farm-cost-pricing") { farmCostPricing = true; capacityPricedFood = true; costBasedFoodPrice = true; }
                else if (arg == "--bounded-retail-service") boundedRetailService = true;
                else if (arg == "--basket-retail-service") { boundedRetailService = true; basketRetailService = true; }
                else if (arg == "--retail-shop-fallback") retailShopFallback = true;
                else if (arg == "--municipal-sanitation-pilot") municipalSanitationPilot = true;
                else if (arg == "--municipal-waste-treatment-pilot") municipalWasteTreatmentPilot = true;
                else if (arg == "--fair-sanitation-dispatch") fairSanitationDispatch = true;
                else if (arg == "--attendance-aware-municipal-shifts") attendanceAwareMunicipalShifts = true;
                else if (arg == "--tax-inclusive-product-price") taxInclusiveProductPrice = true;
                else if (arg.StartsWith("--municipal-collection-interval=")) municipalCollectionIntervalDays = int.Parse(arg["--municipal-collection-interval=".Length..]);
                else if (arg.StartsWith("--retail-baskets-per-shift=")) retailBasketsPerShift = int.Parse(arg["--retail-baskets-per-shift=".Length..]);
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
            if (retailBasketsPerShift is < 1 or > 100 ||
                (retailBasketsPerShift != 5 && !basketRetailService))
                throw new ArgumentException("Basket throughput requires --basket-retail-service and 1..100 baskets per shift");
            if (municipalCollectionIntervalDays is < 1 or > 30 ||
                (municipalCollectionIntervalDays != 6 && !municipalSanitationPilot))
                throw new ArgumentException("Collection interval 1..30 requires municipal pilot");
            if (attendanceAwareMunicipalShifts && !municipalSanitationPilot)
                throw new ArgumentException("Actual municipal attendance requires real staffed collection");
            if (fairSanitationDispatch && !municipalSanitationPilot)
                throw new ArgumentException("Fair physical residential service requires real municipal sanitation");
            if (taxInclusiveProductPrice && !(municipalSanitationPilot && farmCostPricing && capacityPricedFood))
                throw new ArgumentException("Tax-inclusive actual-cost pricing requires municipal levy and real farm/store pricing");
            if (municipalWasteTreatmentPilot && !municipalSanitationPilot)
                throw new ArgumentException("Treatment requires actual collected waste and municipal payment");
            if (municipalSanitationPilot && !(vacancyMarket && constructionPilot))
                throw new ArgumentException("Municipal sanitation pilot requires real vacancies and construction job slot");
            if (retailShopFallback && !basketRetailService)
                throw new ArgumentException("Alternative-shop routing requires physical basket checkout");
            if (boundedRetailService && !vacancyMarket)
                throw new ArgumentException("Checkout throughput requires real staffed retail");
            if (farmCostPricing && !freightLabor)
                throw new ArgumentException("Farm-cost pricing requires real freight invoices");
            if (crossFarmFreight && !freightLabor)
                throw new ArgumentException("Alternate farm freight requires real farm drivers");
            if (attendanceAwareFarms && !vacancyMarket)
                throw new ArgumentException("Attendance-adjusted farm employment requires --vacancy-market");
            if (costBasedFoodPrice && !vacancyMarket)
                throw new ArgumentException("Cost-based food pricing requires --vacancy-market");
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
                    history is not null || wageFeedback || cohortRenewal || demandStaffing || vacancyMarket || freightLabor || constructionPilot || fairHouseholdHiring || costBasedFoodPrice || attendanceAwareFarms || crossFarmFreight || farmCostPricing || boundedRetailService || basketRetailService || retailShopFallback || retailBasketsPerShift != 5 || municipalSanitationPilot || municipalWasteTreatmentPilot || fairSanitationDispatch || attendanceAwareMunicipalShifts || taxInclusiveProductPrice || municipalCollectionIntervalDays != 6 || days != 7)
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
            var config = new LabConfig(sims, days, seed, history, adversarial, wageFeedback, cohortRenewal, demandStaffing, vacancyMarket, freightLabor, constructionPilot, fairHouseholdHiring, costBasedFoodPrice, capacityPricedFood, attendanceAwareFarms, crossFarmFreight, farmCostPricing, boundedRetailService, basketRetailService, retailShopFallback, retailBasketsPerShift, municipalSanitationPilot, municipalCollectionIntervalDays, municipalWasteTreatmentPilot, fairSanitationDispatch, attendanceAwareMunicipalShifts, taxInclusiveProductPrice);
            using var engine = new CausalCity(config);
            var sw = Stopwatch.StartNew();
            engine.Run();
            // Finalizing GZip and committing the last compressed bytes are part
            // of the actual six-hour performance budget, not post-benchmark work.
            engine.CompleteHistory();
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