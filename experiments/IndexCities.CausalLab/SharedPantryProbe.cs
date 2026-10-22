using System.Text.Json;
using IndexCities.Simulation;

namespace IndexCities.CausalLab;

/// <summary>
/// P0.3: agenda exata para uma despensa compartilhada, com entregas em passos
/// conhecidos, SIMs em casa, sem saúde/viagens e ordem estável de IDs.
/// Simula apenas esse contrato, não o ciclo completo de economia.
/// </summary>
internal static class SharedPantryProbe
{
    private const long Threshold = 12L * 3600 * FixedPoint.TimeScale;
    private static readonly long Delta = FixedPoint.Quantize(2, FixedPoint.TimeScale);

    private sealed record Outcome(long[] Hunger, int Pantry, string[] Meals);

    private static Outcome Slow(long[] initial, int initialFood,
        IReadOnlyDictionary<int, int> deliveries, int steps)
    {
        var hunger = (long[])initial.Clone();
        var pantry = initialFood;
        var meals = new List<string>();
        for (var step = 1; step <= steps; step++)
        {
            pantry += deliveries.GetValueOrDefault(step);
            for (var i = 0; i < hunger.Length; i++)
            {
                hunger[i] = checked(hunger[i] + Delta);
                if (hunger[i] < Threshold || pantry == 0) continue;
                pantry--;
                hunger[i] -= Threshold;
                meals.Add($"{step}:{i}");
            }
        }
        return new Outcome(hunger, pantry, meals.ToArray());
    }

    private static Outcome Scheduled(long[] initial, int initialFood,
        IReadOnlyDictionary<int, int> deliveries, int steps)
    {
        var hunger = (long[])initial.Clone();
        var pantry = initialFood;
        var meals = new List<string>();
        var time = 0;
        // Próxima entrega: informação externa que obriga notificar a família.
        var arrivalSteps = deliveries.Keys.Where(t => t > 0 && t <= steps)
            .OrderBy(t => t).ToArray();
        while (time < steps)
        {
            var next = steps + 1;
            foreach (var arrival in arrivalSteps)
                if (arrival > time) { next = arrival; break; }

            if (pantry > 0)
            {
                // Sem chegada externa, único evento que pede execução é o
                // primeiro momento em que um morador atingirá o limiar.
                foreach (var h in hunger)
                {
                    var missing = Threshold - h;
                    var until = missing <= 0 ? 1 : checked((missing + Delta - 1) / Delta);
                    var candidate = time + until;
                    if (candidate < next) next = (int)candidate;
                }
            }

            if (next > steps)
            {
                var skipAll = steps - time;
                for (var i = 0; i < hunger.Length; i++)
                    hunger[i] = checked(hunger[i] + skipAll * Delta);
                break;
            }

            var skip = next - time - 1;
            for (var i = 0; i < hunger.Length; i++)
                hunger[i] = checked(hunger[i] + skip * Delta);
            time += skip;
            time++;
            pantry += deliveries.GetValueOrDefault(time);
            // Mesmo instante, a ordem dos cidadãos prevalece. Depois de
            // cada consumo, estoque é atualizado ANTES do próximo.
            for (var i = 0; i < hunger.Length; i++)
            {
                hunger[i] = checked(hunger[i] + Delta);
                if (hunger[i] < Threshold || pantry == 0) continue;
                pantry--;
                hunger[i] -= Threshold;
                meals.Add($"{time}:{i}");
            }
        }
        return new Outcome(hunger, pantry, meals.ToArray());
    }

    private static bool Equivalent(Outcome x, Outcome y)
        => x.Pantry == y.Pantry && x.Hunger.SequenceEqual(y.Hunger) &&
           x.Meals.SequenceEqual(y.Meals);

    public static int Run()
    {
        const int cases = 1500;
        var random = new Random(7788121);
        var tests = 0;
        var firstFailure = -1;
        var totalMeals = 0;
        for (var iteration = 0; iteration < cases; iteration++)
        {
            var people = random.Next(2, 7);
            var initial = Enumerable.Range(0, people)
                .Select(_ => random.NextInt64(0, 2 * Threshold)).ToArray();
            var pantry = random.Next(0, 5);
            var steps = random.Next(1, 2500);
            var deliveries = new Dictionary<int, int>();
            for (var j = 0; j < 4; j++)
            {
                var at = random.Next(1, steps + 1);
                deliveries[at] = deliveries.GetValueOrDefault(at) + random.Next(0, 3);
            }

            var slow = Slow(initial, pantry, deliveries, steps);
            var scheduled = Scheduled(initial, pantry, deliveries, steps);
            tests++;
            totalMeals += scheduled.Meals.Length;
            if (Equivalent(slow, scheduled)) continue;
            firstFailure = iteration;
            break;
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            scope = "Somente refeicao em casa, despensa compartilhada e entregas em instantes conhecidos",
            comparedCases = tests, correct = firstFailure == -1,
            firstFailure, eventsCompared = totalMeals,
            note = "Nao equivale a atualizar saude/agua, nao integra caminhos e lojas; sem alegacao de ganho na cidade",
        }, new JsonSerializerOptions { WriteIndented = true }));
        return firstFailure == -1 ? 0 : 2;
    }
}
