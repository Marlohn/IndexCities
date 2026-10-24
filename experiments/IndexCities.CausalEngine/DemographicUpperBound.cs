using System.Text.Json;

namespace IndexCities.CausalEngine;

// A mathematical ceiling, NOT a city simulation: removes all money/food/water
// constraints. Children still require two living adult carers in the SAME
// household, a free lifetime household slot, and the fixture's birth spacing.
// No birth is counted toward the real city; this never proves SPEC fidelity.
internal static class DemographicUpperBound
{
    private sealed class Family
    {
        public readonly List<DateTime> Births = [];
        public int LastBirthDay = -10_000;
    }

    private static int Age(DateTime born, DateTime now)
    {
        var years = now.Year - born.Year;
        return born.AddYears(years) > now ? years - 1 : years;
    }

    private static object Simulate(int sims, int days, int seed, bool unrestrictedAppointments)
    {
        var epoch = new DateTime(2020, 1, 1);
        var families = Enumerable.Range(0, Math.Max(1, sims / 4))
            .Select(_ => new Family()).ToArray();
        for (var id = 0; id < sims; id++)
            families[id % families.Length].Births.Add(epoch.AddYears(-(16 + (id * 47 + seed) % 49)));

        long births = 0, deaths = 0;
        var minimum = sims;
        var maximum = sims;
        var alive = sims;
        var yearly = new List<object>();
        for (var day = 0; day < days; day++)
        {
            var date = epoch.AddDays(day);
            if (day != 0)
            {
                void TryFamily(Family family)
                {
                    if (family.Births.Count >= 16 || day - family.LastBirthDay < 4 * 365)
                        return;
                    var carers = 0;
                    foreach (var born in family.Births)
                        if (Age(born, date) is >= 18 and < 60) carers++;
                    if (carers < 2) return;
                    family.Births.Add(date);
                    family.LastBirthDay = day;
                    births++;
                    alive++;
                    maximum = Math.Max(maximum, alive);
                }
                if (unrestrictedAppointments)
                {
                    foreach (var family in families) TryFamily(family);
                }
                else
                {
                    var appointment = (int)(((long)day * 151 + seed) % families.Length);
                    TryFamily(families[appointment]);
                }
                // Exact fixture mortality at age 85. No starvation or illness.
                // An agent dies once, on the birthday it first turns 85.
                foreach (var family in families)
                    foreach (var born in family.Births)
                        if (date.Year > born.Year && date.Date == born.AddYears(85).Date)
                        {
                            deaths++;
                            alive--;
                        }
            }
            minimum = Math.Min(minimum, alive);
            if ((day + 1) % 365 == 0 || day + 1 == days)
            {
                var counted = families.Sum(f => f.Births.Count(born => Age(born, date) < 85));
                if (counted != alive) throw new InvalidOperationException("Demographic-only census disagrees");
                if ((day + 1) % 3650 == 0 || day + 1 == days)
                    yearly.Add(new { days = day + 1, alive, births, deaths });
            }
        }
        return new
        {
            appointments = unrestrictedAppointments ? "Every eligible family daily" : "Exactly the laboratory's rotating daily family",
            physicallyAndEconomicallyValid = false,
            demographicOnly = true,
            aliveEnd = alive,
            minimumAlive = minimum,
            peakAlive = maximum,
            births,
            deaths,
            staysAbove10kWithoutEconomicConstraints = minimum >= 10000,
            checkpoints = yearly
        };
    }

    public static int Run()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var rotating = Simulate(10000, 29220, 42, false);
        var unrestricted = Simulate(10000, 29220, 42, true);
        sw.Stop();
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            status = "DEMOGRAPHIC_THEORETICAL_UPPER_BOUND_ONLY",
            warning = "Removes money, food and water prerequisites. This is NOT a 10k/80-year successful causal city or an approved product birth rule.",
            households = 2500, years = 80, days = 29220,
            rotatingFixtureUpperBound = rotating,
            noAppointmentLimitUpperBound = unrestricted,
            wallSeconds = sw.Elapsed.TotalSeconds
        }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
