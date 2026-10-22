using System.Diagnostics;
using IndexCities.Simulation;

namespace IndexCities.Headless;

/// <summary>
/// Microsonda de limite superior do custo de percorrer cidadãos inativos.
/// Não modifica nenhum cidadão e NÃO simula uma agenda de eventos.
/// </summary>
internal static class IdleSchedulingProbe
{
    public static void Run(CitySimulation sim)
    {
        var citizens=sim.State.Citizens;
        var now=sim.State.ElapsedSeconds;
        var alive=citizens.Count(c=>c.Alive);
        var stationary=citizens.Count(c=>c.Alive&&!c.Moving&&c.VehicleId==null&&c.ConstructionProjectId==null);
        var asleep=citizens.Count(c=>c.Alive&&!c.Moving&&c.VehicleId==null&&c.ConstructionProjectId==null&&now<c.NextDecisionAt);
        var busy=alive-stationary;
        // Exatamente as 43.200 passagens de um dia de quanta de 2 segundos.
        // A fotografia é mantida fixa: é um MICROBENCHMARK DE VARREDURA,
        // não estimativa da agenda real nem equivalência entre executores.
        const int passes=43_200;
        long stationaryChecks=0,asleepChecks=0;
        var sw=Stopwatch.StartNew();
        for(var step=0;step<passes;step++)
        {
            foreach(var c in citizens)
            {
                if(!c.Alive||c.VehicleId!=null||c.Moving||c.ConstructionProjectId!=null)continue;
                stationaryChecks++;
                if(now<c.NextDecisionAt)asleepChecks++;
            }
        }
        sw.Stop();
        Console.WriteLine($"IDLE_PROBE alive={alive} stationary={stationary} busy={busy} asleepSnapshot={asleep} passes={passes} stationaryChecks={stationaryChecks} asleepChecks={asleepChecks} scanWall={sw.Elapsed.TotalSeconds:F4}s");
        Console.WriteLine("IDLE_PROBE_LIMIT scanWall mede somente predicados numa fotografia fixa, inclui JIT/cache e nao representa ganho de uma agenda nem elimina as dependencias de outros sistemas.");
    }
}
