using IndexCities.Simulation;
using System.Diagnostics;
using System.Text.Json;
namespace IndexCities.Simulation.Tests;
internal static class MonthlySoak
{
    public static int Run(string argument)
    {
        try{
            if(!int.TryParse(argument[12..],out var days)||days<1||days>366)throw new ArgumentException("Soak: dias entre 1 e 366");
            var folder=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../artifacts/verification"));var input=Path.Combine(folder,"city-cycle-snapshot.json");var sim=CitySimulation.FromJson(File.ReadAllText(input));var started=Stopwatch.StartNew();var history=new List<object>();var initialMoney=sim.MonetaryTotal;var initialTax=sim.State.Ledger.Where(l=>l.Category=="Imposto imobiliário").Sum(l=>l.Amount);var initialMaintenance=sim.State.Projects.Count(p=>p.IsMaintenance&&p.Status==WorkStatus.Completed);
            var borrow=sim.Borrow(5000);if(!borrow.Success)throw new InvalidOperationException(borrow.Message);
            for(var day=1;day<=days;day++){
                for(var hour=0;hour<24;hour++){sim.Advance(3600);sim.CheckInvariants();}
                var sample=new{day,at=sim.State.Now,cityCash=sim.CityBalance,residents=sim.Population,living=sim.State.Citizens.Count(c=>c.Alive),companies=sim.State.Companies.Count(c=>c.Active),water=sim.State.PotableCapacity,power=sim.State.ElectricityCapacity,foodProduced=sim.State.Buildings.Sum(b=>b.FoodProduced),foodBought=sim.State.Ledger.Where(l=>l.Category=="Alimentos").Sum(l=>l.Amount),maintenanceCompleted=sim.State.Projects.Count(p=>p.IsMaintenance&&p.Status==WorkStatus.Completed)-initialMaintenance,maintenancePending=sim.State.Projects.Count(p=>p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress),taxPaid=sim.State.Ledger.Where(l=>l.Category=="Imposto imobiliário").Sum(l=>l.Amount)-initialTax,loanPrincipal=sim.State.Loans.Sum(l=>l.Principal),loanPayments=sim.State.Ledger.Where(l=>l.Category=="Empréstimo"&&l.From=="city").Sum(l=>l.Amount),overdue=sim.State.Obligations.Where(o=>o.Outstanding>0).Sum(o=>o.Outstanding),elapsedRealSeconds=started.Elapsed.TotalSeconds};history.Add(sample);Console.WriteLine("SOAK "+JsonSerializer.Serialize(sample));File.WriteAllText(Path.Combine(folder,"monthly-soak-progress.json"),JsonSerializer.Serialize(history,new JsonSerializerOptions{WriteIndented=true}));
            }
            if(sim.MonetaryTotal!=initialMoney)throw new InvalidOperationException("Soak alterou oferta monetária");var snapshot=sim.SaveToJson();if(CitySimulation.FromJson(snapshot).SaveToJson()!=snapshot)throw new InvalidOperationException("Soak não preservou estado no save");File.WriteAllText(Path.Combine(folder,"monthly-soak-snapshot.json"),snapshot);Console.WriteLine("INDEXCITIES_SOAK_OK days="+days+" totalMoney="+sim.MonetaryTotal+" realSeconds="+started.Elapsed.TotalSeconds);return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
