using System.Diagnostics;
using System.Text.Json;
using IndexCities.Simulation;

namespace IndexCities.CausalLab;

/// <summary>
/// P0 — um teste isolado de equivalência numérica/temporal, NÃO um novo motor.
/// Obtém o comportamento de quantização diretamente do FixedPoint do jogo.
/// Não executa o mundo completo nem afirma ganhos de performance em 10k SIMs.
/// </summary>
internal static class Program
{
    const double Quantum = 2.0;
    const int DaySteps = 43_200;

    private static int Main()
    {
        try
        {
            var report = new Dictionary<string, object>
            {
                ["scope"] = "P0: contraexemplos e equivalência de acumuladores isolados; NÃO benchmark integrado",
                ["source"] = "FixedPoint.cs, Pollution.cs, Infrastructure.cs, Economy.cs do motor real",
                ["production_below_half_unit"] = Production(0.000010584, 10),
                ["production_above_half_unit"] = Production(0.000011016, 10),
                ["pollution_10min"] = Pollution(300),
                ["wind_energy_1day"] = WindEnergy(),
                ["hunger_crossing"] = Hunger(),
                ["piecewise_constant_is_exact"] = PiecewiseConstant(),
                ["randomized_constant_intervals"] = CheckRandomizedConstantIntervals(),
            };
            Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("P0 FAILED: " + exception);
            return 1;
        }
    }

    private static object Production(double operatingCapacity, int steps)
    {
        // OperateBusinesses: quantidade gerada num quantum = capacidade * dt / 43200.
        // No mundo integrado, disponibilidade de insumos e teto de estoque podem
        // interromper o intervalo; este experimento cobre apenas capacidade fixa.
        var delta = operatingCapacity * Quantum / 43200;
        long reference = 0;
        for (var i = 0; i < steps; i++)
            reference = FixedPoint.Add(reference, delta, FixedPoint.ResourceScale);

        var perQuantum = FixedPoint.Quantize(delta, FixedPoint.ResourceScale);
        var safe = checked((long)steps * perQuantum);
        var unsafeBulk = FixedPoint.Quantize(delta * steps, FixedPoint.ResourceScale);

        Require(reference == safe, "Acumulador linear não reproduziu quantização por passo");
        Require(reference != unsafeBulk, "Contraexemplo de acumulação ingênua deixou de divergir");
        return new
        {
            capacity = operatingCapacity,
            steps,
            referenceUnits = reference,
            exactCompositionUnits = safe,
            naiveBulkUnits = unsafeBulk,
            naiveEquivalent = reference == unsafeBulk,
        };
    }

    private static object Pollution(int steps)
    {
        // Pollution.cs: a poluição decai com o fator decimal de Math.Exp,
        // *arredondando cada quantum para 20 casas*, e pode ser removida
        // quando cruza o limiar. Aqui medimos um único elemento ainda ativo.
        const decimal initial = 0.31415926535897932384m;
        var value = initial;
        var perStepFactor = (decimal)Math.Exp(-.15 * Quantum / 86400);
        for (var i = 0; i < steps; i++)
            value = FixedPoint.Pollution(value * perStepFactor);

        var elapsed = steps * Quantum;
        var naiveFactor = (decimal)Math.Exp(-.15 * elapsed / 86400);
        var naive = FixedPoint.Pollution(initial * naiveFactor);
        Require(value != naive, "Contraexemplo de decaimento agrupado deixou de divergir");
        return new
        {
            steps,
            original = value,
            naiveSingleDecay = naive,
            naiveEquivalent = value == naive,
        };
    }

    private static double Wind(double elapsed, int seed)
        => Math.Clamp(
            .55 + .25 * Math.Sin(elapsed / 21600 + seed) +
            .15 * Math.Sin(elapsed / 172800 + seed * .3),
            .15, .95);

    private static object WindEnergy()
    {
        // Infrastructure.cs usa WindFactor amostrado no INÍCIO de cada Step.
        // Snapshot simplificado: uma fonte de 100 unidades de capacidade nominal
        // e demais entradas fixas. Isso NÃO representa a distribuição completa.
        long reference = 0;
        for (var i = 0; i < DaySteps; i++)
        {
            var dt = Quantum;
            var produced = 100 * Wind(i * dt) * dt / 86400;
            reference = FixedPoint.Add(reference, produced, FixedPoint.ResourceScale);
        }

        long hourly = 0;
        for (var hour = 0; hour < 24; hour++)
        {
            var produced = 100 * Wind(hour * 3600) * 3600 / 86400;
            hourly = FixedPoint.Add(hourly, produced, FixedPoint.ResourceScale);
        }

        Require(reference != hourly, "Contraexemplo de vento por hora deixou de divergir");
        return new
        {
            originalUnits = reference,
            naiveHourlyUnits = hourly,
            naiveEquivalent = reference == hourly,
        };
    }

    private static object Hunger()
    {
        // Economy.UpdateNeeds: 1 alimento é retirado assim que a fome cruza
        // 12 horas, se houver estoque doméstico e o SIM estiver em casa.
        // Os demais efeitos de saúde/água/morte ficam FORA deste microteste.
        var hunger = FixedPoint.Quantize(12 * 3600 - 60, FixedPoint.TimeScale);
        var threshold = 12L * 3600 * FixedPoint.TimeScale;
        var pantry = 1;
        var referenceAt = -1;
        for (var elapsed = 2; elapsed <= 120; elapsed += 2)
        {
            hunger = FixedPoint.Add(hunger, Quantum, FixedPoint.TimeScale);
            if (hunger < threshold || pantry == 0) continue;
            pantry--;
            hunger -= threshold;
            referenceAt = elapsed;
            break;
        }

        var naiveCoarseAt = 120;
        Require(referenceAt == 60, "Cruzamento de 12 horas esperado em 60 s");
        Require(referenceAt != naiveCoarseAt, "Evento em fim de intervalo não deveria coincidir");
        return new
        {
            originalFoodConsumptionAtSecond = referenceAt,
            naiveTwoMinuteConsumptionAtSecond = naiveCoarseAt,
            equivalent = referenceAt == naiveCoarseAt,
        };
    }

    private static object PiecewiseConstant()
    {
        // Alternativa válida para quantidades lineares sem limiares:
        // compor o efeito QUANTIZADO em cada trecho de entradas constantes.
        var segments = new (double Capacity, int Steps)[]
        {
            (0.000010584, 20),
            (0.000011016, 10),
            (0.000010584, 30),
            (0.000011016, 40),
        };
        long reference = 1729, compact = 1729;
        foreach (var (capacity, steps) in segments)
        {
            var delta = capacity * Quantum / 43200;
            for (var i = 0; i < steps; i++)
                reference = FixedPoint.Add(reference, delta, FixedPoint.ResourceScale);
            var perStepUnits = FixedPoint.Quantize(delta, FixedPoint.ResourceScale);
            compact = checked(compact + steps * perStepUnits);
        }

        Require(reference == compact, "Mudança de capacidade alterou composição");
        return new { equivalent = reference == compact, referenceUnits = reference, compactUnits = compact, segmentCount = segments.Length };
    }

    private static object CheckRandomizedConstantIntervals()
    {
        var rng = new Random(1729);
        const int cases = 2000;
        var stepsTotal = 0;
        var stopwatch = Stopwatch.StartNew();
        for (var caseNumber = 0; caseNumber < cases; caseNumber++)
        {
            var initial = rng.NextInt64(-1_000_000, 1_000_000);
            var rate = (rng.NextDouble() - .4) * .015;
            var steps = rng.Next(1, 500);
            var delta = rate * Quantum / 43200;
            long reference = initial;
            for (var i = 0; i < steps; i++)
                reference = FixedPoint.Add(reference, delta, FixedPoint.ResourceScale);
            long compact = checked(initial + steps * FixedPoint.Quantize(delta, FixedPoint.ResourceScale));
            Require(reference == compact, "Composição inválida no caso " + caseNumber);
            stepsTotal += steps;
        }
        stopwatch.Stop();
        return new { cases, stepsChecked = stepsTotal, allEquivalent = true, wallMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            warning = "Microteste local, NÃO projetar tempo de execução de cidade" };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
