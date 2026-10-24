using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IndexCities.Headless;
using IndexCities.Simulation;

namespace IndexCities.CausalLab;

/// <summary>
/// Auditoria integrada: executar o núcleo verdadeiro em blocos diferentes,
/// com mesmo estado inicial, mesma seed e 2 s físicos. NÃO é motor acelerado.
/// </summary>
internal static class IntegratedProbe
{
    public static int Run()
    {
        Console.WriteLine("P0_INTEGRATED creating-real-fixture grid=3x400 seed=42");
        var stopwatch = Stopwatch.StartNew();
        var (source, fixtureInfo) = Fixtures.Grid(42, 3, 400, true);
        Console.WriteLine($"P0_INTEGRATED fixture={fixtureInfo} created_s={stopwatch.Elapsed.TotalSeconds:F3}");

        source.Advance(86400); // funcionamento e povoamento reais; não é parte da medição
        var originalJson = source.SaveToJson();
        var grouped = CitySimulation.FromJson(originalJson);
        var perTwoSeconds = CitySimulation.FromJson(originalJson);
        var perMinute = CitySimulation.FromJson(originalJson);

        string Sha(string json) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var startHash = Sha(originalJson);
        var points = new List<object>();
        bool equal = true;
        double totalGrouped = 0, totalShort = 0, totalMinute = 0;
        const int windows = 24;
        for (var window = 1; window <= windows; window++)
        {
            stopwatch.Restart();
            grouped.Advance(3600);
            totalGrouped += stopwatch.Elapsed.TotalSeconds;

            stopwatch.Restart();
            for (var i = 0; i < 1800; i++) perTwoSeconds.Advance(2);
            totalShort += stopwatch.Elapsed.TotalSeconds;

            stopwatch.Restart();
            for (var i = 0; i < 60; i++) perMinute.Advance(60);
            totalMinute += stopwatch.Elapsed.TotalSeconds;

            var a = Sha(grouped.SaveToJson());
            var b = Sha(perTwoSeconds.SaveToJson());
            var c = Sha(perMinute.SaveToJson());
            var same = a == b && a == c;
            equal &= same;
            points.Add(new { simulatedSecond = window * 3600, equivalent = same,
                groupedHash = a[..16], twoSecondHash = b[..16], minuteHash = c[..16] });
            if (!same) break; // primeira divergência é mais informativa
        }

        grouped.CheckInvariants();
        perTwoSeconds.CheckInvariants();
        perMinute.CheckInvariants();
        var snapshot = grouped.SaveToJson();
        var loaded = CitySimulation.FromJson(snapshot);
        var roundtrip = Sha(loaded.SaveToJson()) == Sha(snapshot);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            scenario = "Grid 3x400, seed 42, 1 dia real de warmup e 1 dia inteiro medido",
            startHash = startHash[..16],
            simPopulation = source.Population,
            simVehicles = source.State.Vehicles.Count,
            sameStateAcrossCallPartitions = equal,
            saveLoadRoundtrip = roundtrip,
            wallSeconds = new { grouped600 = totalGrouped, split2 = totalShort, split60 = totalMinute },
            checkpoints = points,
            note = "Executa motor atual sem otimizar; detecta efeito de fronteiras de chamada e estabelece referencia causal",
        }, new JsonSerializerOptions { WriteIndented = true }));
        return equal && roundtrip ? 0 : 2;
    }
}
