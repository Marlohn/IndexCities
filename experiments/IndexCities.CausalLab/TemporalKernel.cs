using System.Diagnostics;
using System.Text.Json;
using IndexCities.Simulation;

namespace IndexCities.CausalLab;

/// <summary>
/// P0.2: prova EXECUTÁVEL de um calendário de limiar em quantidades exatas,
/// usando as mesmas regras locais de fome da V1. Restrições intencionais:
/// SIM em casa, sem alterações externas, saúde e família fora deste teste.
/// NÃO cria novos acontecimentos de gameplay e NÃO acelera CitySimulation.
/// </summary>
internal static class TemporalKernel
{
    private const long Threshold = 12L * 3600 * FixedPoint.TimeScale;
    private static readonly long Delta = FixedPoint.Quantize(2, FixedPoint.TimeScale);

    private struct HungerState
    {
        public long HungerUnits;
        public int Pantry;
        public int Meals;
        public long FirstMealAt;
        public long LastMealAt;
        public HungerState(long hungerUnits, int pantry)
        {
            HungerUnits = hungerUnits;
            Pantry = pantry;
            FirstMealAt = -1;
            LastMealAt = -1;
        }
    }

    private static HungerState Stepwise(long initial, int pantry, int steps)
    {
        var state = new HungerState(initial, pantry);
        for (var i = 1; i <= steps; i++)
        {
            state.HungerUnits = checked(state.HungerUnits + Delta);
            if (state.HungerUnits < Threshold || state.Pantry == 0) continue;
            state.HungerUnits = checked(state.HungerUnits - Threshold);
            state.Pantry--;
            state.Meals++;
            if (state.FirstMealAt < 0) state.FirstMealAt = i * 2L;
            state.LastMealAt = i * 2L;
        }
        return state;
    }

    private static HungerState ByEvent(long initial, int pantry, int steps)
    {
        var state = new HungerState(initial, pantry);
        var remaining = steps;
        var completed = 0;
        while (remaining > 0)
        {
            if (state.Pantry == 0)
            {
                state.HungerUnits = checked(state.HungerUnits + remaining * Delta);
                break;
            }

            var missing = Threshold - state.HungerUnits;
            var until = missing <= 0 ? 1 : checked((missing + Delta - 1) / Delta);
            if (until > remaining)
            {
                state.HungerUnits = checked(state.HungerUnits + remaining * Delta);
                break;
            }

            var stepsUntil = (int)until;
            state.HungerUnits = checked(state.HungerUnits + stepsUntil * Delta - Threshold);
            state.Pantry--;
            completed += stepsUntil;
            remaining -= stepsUntil;
            state.Meals++;
            if (state.FirstMealAt < 0) state.FirstMealAt = completed * 2L;
            state.LastMealAt = completed * 2L;
        }
        return state;
    }

    private static bool Same(HungerState a, HungerState b)
        => a.HungerUnits == b.HungerUnits && a.Pantry == b.Pantry &&
           a.Meals == b.Meals && a.FirstMealAt == b.FirstMealAt &&
           a.LastMealAt == b.LastMealAt;

    public static int Run()
    {
        const int cases = 600;
        const int maxSteps = 90000;
        var rng = new Random(20261022);
        var casesData = new (long Initial, int Pantry, int Steps)[cases];
        for (var i = 0; i < casesData.Length; i++)
            casesData[i] = (
                rng.NextInt64(0, Threshold * 3),
                rng.Next(0, 6),
                rng.Next(0, maxSteps + 1));

        var wall = Stopwatch.StartNew();
        var expected = casesData.Select(x => Stepwise(x.Initial, x.Pantry, x.Steps)).ToArray();
        wall.Stop();
        var baselineMs = wall.Elapsed.TotalMilliseconds;

        wall.Restart();
        var actual = casesData.Select(x => ByEvent(x.Initial, x.Pantry, x.Steps)).ToArray();
        wall.Stop();
        var eventMs = wall.Elapsed.TotalMilliseconds;

        var mismatch = -1;
        for (var i = 0; i < expected.Length; i++)
            if (!Same(expected[i], actual[i])) { mismatch = i; break; }

        // Outro teste: cronologia geracional de 80 anos em tempo discreto, sem
        // iterar 1,26 bilhão de passos por indivíduo. Quantidade e horário
        // das refeições reais seriam maior no jogo, com idas a lojas e família.
        const long days80Years = 29220;
        var totalSteps80Years = days80Years * 43200;
        var hunger = ByEvent(0, 1000, checked((int)Math.Min(1000000L, totalSteps80Years)));
        var report = new
        {
            scope = "Somente limiar de 12h para uma pessoa em casa com despensa fixa",
            comparedCases = cases,
            comparedSteps = casesData.Sum(x => (long)x.Steps),
            allCasesExact = mismatch == -1,
            firstMismatch = mismatch,
            stepwiseWallMs = baselineMs,
            byEventWallMs = eventMs,
            note = "Microbenchmark de cálculo de fome; não projetar speedup de cidade",
            eightyYearCalendarSteps = totalSteps80Years,
            sampleFirstMillionSteps = new { hunger.Meals, hunger.LastMealAt, hunger.Pantry },
        };
        Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        return mismatch == -1 ? 0 : 2;
    }
}
