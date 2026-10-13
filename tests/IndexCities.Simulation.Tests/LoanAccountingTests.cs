using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class LoanAccountingTests
{
    static CitySimulation Borrower(){var sim=new CitySimulation(42);Assert.False(sim.Borrow(12000).Success);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40)).Success);Assert.True(sim.CancelProject(sim.State.Projects.Single().Id).Success);Assert.True(sim.Borrow(12000).Success);sim.State.Wallets["global"]+=sim.CityBalance;sim.State.Wallets["city"]=0;return sim;}
    static void Due(CitySimulation sim){sim.State.Now=sim.State.Loans.Single().NextDue;sim.State.NextDaily=sim.State.Now.Date.AddDays(1);sim.State.NextTaxDate=sim.State.Now.AddMonths(1);sim.State.EconomyNextAt=sim.State.ElapsedSeconds+3600;sim.Advance(2);sim.CheckInvariants();}
    [Fact] public void Overdue_installment_does_not_forgive_principal_and_real_recovery_reduces_it()
    {
        var sim=Borrower();Due(sim);var loan=sim.State.Loans.Single();var debt=Assert.Single(sim.State.Obligations,o=>o.Kind=="Empréstimo");Assert.Equal(12000,loan.Principal);Assert.Equal(11000,loan.ScheduledPrincipal);Assert.Equal(1000,debt.PrincipalOutstanding);Assert.Equal(1036,debt.Outstanding);Assert.False(sim.Borrow(1000).Success);
        sim.State.Wallets["city"]+=1036;sim.State.Wallets["global"]-=1036;sim.State.EconomyNextAt=sim.State.ElapsedSeconds;sim.Advance(2);Assert.Equal(11000,loan.Principal);Assert.Equal(0,debt.Outstanding);Assert.Equal(0,debt.PrincipalOutstanding);Assert.True(sim.Borrow(1000).Success);sim.CheckInvariants();
    }
    [Fact] public void Unpaid_full_term_preserves_principal_without_duplicate_installments_or_new_penalties()
    {
        var sim=Borrower();for(var i=0;i<12;i++)Due(sim);var loan=sim.State.Loans.Single();Assert.Equal(0,loan.RemainingMonths);Assert.Equal(0,loan.ScheduledPrincipal);Assert.Equal(12000,loan.Principal);Assert.Equal(12000,sim.State.Obligations.Sum(o=>o.PrincipalOutstanding));Assert.Equal(12432,sim.State.Obligations.Sum(o=>o.Outstanding));var count=sim.State.Obligations.Count;sim.State.Now=sim.State.Now.AddMonths(1);sim.Advance(2);Assert.Equal(count,sim.State.Obligations.Count);Assert.Equal(sim.SaveToJson(),CitySimulation.FromJson(sim.SaveToJson()).SaveToJson());
    }
    static Citizen EssentialWorker(CitySimulation sim,decimal accrued)
    {
        var building=new Building{Id=sim.State.NextId++,Kind=BuildingKind.WindPower,Completed=true,OwnerAccount="city",MaintenanceDueAt=sim.State.ElapsedSeconds+100000000};sim.State.Buildings.Add(building);var citizen=new Citizen{Id=sim.State.NextId++,Name="Servidor do cenário",WorkplaceId=building.Id,WagesAccrued=accrued};sim.State.Citizens.Add(citizen);sim.State.Wallets[citizen.Account]=0;return citizen;
    }
    static void FinanceEvent(CitySimulation sim,string method)=>typeof(CitySimulation).GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(sim,null);
    [Fact] public void Loan_due_preserves_earned_essential_wages_and_records_the_unpaid_installment()
    {
        var sim=Borrower();var worker=EssentialWorker(sim,200);sim.State.Wallets["city"]+=500;sim.State.Wallets["global"]-=500;Due(sim);
        Assert.Equal(200,sim.CityBalance);Assert.Equal(200,sim.MunicipalCriticalWagesPending);Assert.Equal(0,sim.Balance(worker.Account));var debt=Assert.Single(sim.State.Obligations,o=>o.Kind=="Empréstimo");Assert.Equal(736,debt.Outstanding);Assert.Equal(11736,sim.State.Loans.Single().Principal);Assert.False(sim.Borrow(1).Success);
        FinanceEvent(sim,"DailyFinance");Assert.Equal(200,sim.Balance(worker.Account));Assert.Equal(0,sim.CityBalance);Assert.Equal(736,debt.Outstanding);sim.CheckInvariants();
    }
    [Fact] public void Real_cash_pays_overdue_essential_wages_before_recovering_municipal_loan_debt()
    {
        var sim=Borrower();Due(sim);var worker=EssentialWorker(sim,0);var salary=new Obligation{Id=sim.State.NextId++,Debtor="city",Creditor=worker.Account,Kind="Salário",BuildingId=worker.WorkplaceId,Outstanding=180,Due=sim.State.Now.AddDays(-1)};sim.State.Obligations.Add(salary);sim.State.Wallets["city"]+=300;sim.State.Wallets["global"]-=300;
        FinanceEvent(sim,"RecoverDebt");Assert.Equal(180,sim.Balance(worker.Account));Assert.Equal(0,salary.Outstanding);Assert.Equal(0,sim.CityBalance);Assert.Equal(916,sim.State.Obligations.Single(o=>o.Kind=="Empréstimo").Outstanding);Assert.Equal(11916,sim.State.Loans.Single().Principal);sim.CheckInvariants();
    }
}
