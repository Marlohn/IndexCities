namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    void DailyFinance()
    {
        var waterCost=UtilityOperatingCost(true);var powerCost=UtilityOperatingCost(false);
        foreach(var c in State.Citizens.Where(c=>c.Alive&&c.WagesAccrued>0).OrderBy(c=>BuildingById(c.WorkplaceId)?.Definition.Utility==true?0:1)){
            var b=BuildingById(c.WorkplaceId);if(b==null)continue;SettleWages(c,b);c.WorkSecondsToday=0;
        }
        foreach(var b in State.Buildings.Where(b=>b.Completed&&!b.Removed)){
            if(b.OwnerAccount!=null&&b.OwnerAccount!="city"){
                var water=Money((decimal)b.WaterConsumedToday*State.WaterUnitPrice);var power=Money((decimal)b.ElectricityConsumedToday*State.ElectricityUnitPrice);var paidWater=Pay(b.OwnerAccount,"city",water,"Água","Consumo efetivo de água potável",b.Id,b.Kind==BuildingKind.House);Owe(b.OwnerAccount,"city",water-paidWater,"Água",b.Id);var paidPower=Pay(b.OwnerAccount,"city",power,"Energia","Consumo efetivo de eletricidade",b.Id,b.Kind==BuildingKind.House);Owe(b.OwnerAccount,"city",power-paidPower,"Energia",b.Id);
            }
            if(b.Kind==BuildingKind.Farm)ReviewFarmDemand(b);
            b.WaterConsumedToday=0;b.ElectricityConsumedToday=0;b.FoodSoldToday=0;
        }
        State.WaterUnitPrice=StableUtilityPrice(State.WaterUnitPrice,State.Tuning.WaterUnitPrice,waterCost,State.WaterAvailableToday);State.ElectricityUnitPrice=StableUtilityPrice(State.ElectricityUnitPrice,State.Tuning.ElectricityUnitPrice,powerCost,State.ElectricityAvailableToday);
        State.WaterWageCostToday=0;State.ElectricityWageCostToday=0;State.WaterAvailableToday=0;State.ElectricityAvailableToday=0;State.StreetElectricityConsumedToday=0;RecoverDebt();ReviewInsolvency();
        foreach(var c in State.Citizens.Where(c=>c.Alive&&c.Resident&&c.WorkplaceId!=null)){
            var job=BuildingById(c.WorkplaceId);var employer=job?.CompanyId==null?"city":"company:"+job.CompanyId;
            var overdue=State.Obligations.Where(o=>o.Debtor==employer&&o.Creditor==c.Account&&o.Kind=="Salário"&&o.Outstanding>0).ToList();
            if(overdue.Any(o=>State.Now-o.Due>TimeSpan.FromDays(5))&&Balance(c.Account)<30){c.WorkplaceId=null;Personal(c,"Saiu autonomamente do emprego por atraso persistente; crédito salarial preservado");}
        }
    }
    decimal UtilityOperatingCost(bool water)
    {
        bool Matches(Building? building)=>building!=null&&(water?building.Kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment:building.Kind==BuildingKind.WindPower);
        var wages=water?State.WaterWageCostToday:State.ElectricityWageCostToday;
        var maintenance=State.Projects.Where(p=>p.IsMaintenance&&p.CompletedAt>=State.Now.AddDays(-1)&&Matches(BuildingById(p.BuildingId))).Sum(p=>p.CostPaid);return wages+maintenance;
    }
    static decimal StableUtilityPrice(decimal previous,decimal reference,decimal incurredCost,double suppliedCapacity)
    {
        if(suppliedCapacity<=.001)return previous;var target=Math.Max(reference,incurredCost/(decimal)suppliedCapacity);return decimal.Round(Math.Clamp(target,previous*.8m,previous*1.2m),4,MidpointRounding.AwayFromZero);
    }
    void SettleWages(Citizen c,Building b)
    {if(c.WagesAccrued<=0)return;var employer=b.CompanyId==null?"city":"company:"+b.CompanyId;var due=Money(c.WagesAccrued);var paid=Pay(employer,c.Account,due,"Salários / "+b.Definition.Name,"Remuneração efetivamente devida",b.Id);Owe(employer,c.Account,due-paid,"Salário",b.Id);c.WagesAccrued=0;c.LastPaidAt=State.ElapsedSeconds;Personal(c,"Salário pago "+paid+"; devido "+due);}
    void PropertyTaxes()
    {
        foreach(var b in State.Buildings.Where(b=>b.Completed&&!b.Removed&&b.Definition.Private&&b.OwnerAccount!=null)){
            var due=Money(MarketValue(b)*State.TaxRates[b.Definition.TaxCategory]/12m);var paid=Pay(b.OwnerAccount!,"city",due,"Imposto imobiliário","Valor de mercado × alíquota anual da categoria ÷ 12",b.Id,b.Kind==BuildingKind.House);Owe(b.OwnerAccount!,"city",due-paid,"Imposto",b.Id);
        }
    }
    bool CriticalMunicipalEmployer(Building? building)=>building!=null&&(building.Definition.Utility||building.Kind==BuildingKind.WorksDepot&&State.Projects.Any(p=>p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress));
    public decimal MunicipalCriticalWagesPending=>Money(State.Citizens.Where(c=>c.Alive&&c.WagesAccrued>0&&BuildingById(c.WorkplaceId) is {} b&&b.CompanyId==null&&CriticalMunicipalEmployer(b)).Sum(c=>c.WagesAccrued));
    decimal MunicipalDebtBudget=>Math.Max(0,Available-MunicipalCriticalWagesPending);
    void RecoverDebt()
    {
        foreach(var group in State.Obligations.Where(o=>o.Outstanding>0).GroupBy(o=>o.Debtor)){
            var budget=group.Key=="city"?MunicipalDebtBudget:Spendable(group.Key);if(group.Key.StartsWith("sim:"))budget=Math.Max(0,budget-40);if(group.Key.StartsWith("company:"))budget=Math.Max(0,budget-100);
            if(budget<=0)continue;
            var cohorts=group.GroupBy(o=>group.Key=="city"&&o.Kind=="Salário"&&CriticalMunicipalEmployer(BuildingById(o.BuildingId))?0:1).OrderBy(cohort=>cohort.Key);
            foreach(var cohort in cohorts){var debts=cohort.ToList();var total=debts.Sum(o=>o.Outstanding);var payment=Math.Min(budget,total);
                foreach(var debt in debts){var amount=Math.Min(budget,Math.Min(Spendable(group.Key),Math.Min(debt.Outstanding,Money(payment*debt.Outstanding/total))));if(amount<=0||!Transfer(debt.Debtor,debt.Creditor,amount,"Quitação de dívida",debt.Kind+" vencido",debt.BuildingId))continue;budget-=amount;
                    if(debt.LoanId is {} id&&State.Loans.FirstOrDefault(l=>l.Id==id) is {} loan){var interest=Math.Max(0,debt.Outstanding-debt.PrincipalOutstanding);var principal=Math.Min(debt.PrincipalOutstanding,Math.Max(0,amount-interest));loan.Principal=Math.Max(0,loan.Principal-principal);debt.PrincipalOutstanding-=principal;}debt.Outstanding-=amount;}
            }
        }
    }
    void ProcessLoans()
    {
        if(State.Loans.Any(l=>l.RemainingMonths>0&&State.Now>=l.NextDue))ScheduleMaintenance(true);
        foreach(var loan in State.Loans.Where(l=>l.RemainingMonths>0))while(State.Now>=loan.NextDue&&loan.RemainingMonths>0){var principal=loan.RemainingMonths==1?loan.ScheduledPrincipal:Math.Min(loan.ScheduledPrincipal,loan.MonthlyPrincipal);var interest=Money(loan.Principal*loan.Rate);var due=Money(principal+interest);var paid=Pay("city","global",Math.Min(due,MunicipalDebtBudget),"Empréstimo","Parcela e juros contratados",loan.Id);var paidPrincipal=Math.Min(principal,Math.Max(0,paid-interest));loan.Principal-=paidPrincipal;loan.ScheduledPrincipal-=principal;Owe("city","global",due-paid,"Empréstimo",null,loan.Id,principal-paidPrincipal);loan.RemainingMonths--;loan.NextDue=loan.NextDue.AddMonths(1);}
    }
    void ReviewInsolvency()
    {
        foreach(var company in State.Companies.Where(c=>c.Active).ToList()){
            var debt=State.Obligations.Where(o=>o.Debtor==company.Account&&o.Outstanding>0).ToList();var total=debt.Sum(o=>o.Outstanding);
            if(total==0||Balance(company.Account)>total){if(company.LossStartedAt!=0)company.Decision="Saiu do risco: caixa voltou a cobrir as dívidas";company.LossStartedAt=0;continue;}
            if(company.LossStartedAt==0){company.LossStartedAt=State.ElapsedSeconds;Event(company.Name+" em risco de falência: dívidas maiores que o caixa");}
            // The risk period is visible with the real main cause before any closure.
            var main=debt.GroupBy(o=>o.Kind).OrderByDescending(g=>g.Sum(o=>o.Outstanding)).First();var daysLeft=Math.Max(0,14-(State.ElapsedSeconds-company.LossStartedAt)/86400);
            company.Decision="Em risco de falência: dívidas "+Money(total)+" contra caixa "+Balance(company.Account)+"; maior dívida: "+main.Key+" ("+Money(main.Sum(o=>o.Outstanding))+"); pode encerrar em "+daysLeft.ToString("F0")+" dia(s) se não se recuperar";
            if(State.ElapsedSeconds-company.LossStartedAt<14*86400||Balance(company.Account)>20)continue;
            foreach(var b in State.Buildings.Where(b=>b.CompanyId==company.Id)){
                foreach(var c in State.Citizens.Where(c=>c.WorkplaceId==b.Id)){Owe(company.Account,c.Account,Money(c.WagesAccrued),"Salário",b.Id);c.WagesAccrued=0;c.WorkplaceId=null;Personal(c,"Empresa encerrada; crédito participa da liquidação real");}
                b.OperationClosed=true;b.CompanyId=null;b.OwnerAccount=null;b.Cause="Imóvel sem proprietário após falência";
            }
            // Unsold assets remain physically present and unclaimed. No fictitious buyer funds liquidation.
            debt=State.Obligations.Where(o=>o.Debtor==company.Account&&o.Outstanding>0).ToList();total=debt.Sum(o=>o.Outstanding);var funds=Balance(company.Account);
            foreach(var o in debt){var amount=Math.Min(Balance(company.Account),total==0?0:Money(funds*o.Outstanding/total));Transfer(company.Account,o.Creditor,amount,"Liquidação","Rateio proporcional entre credores reais",o.BuildingId);o.Outstanding=0;}
            Transfer(company.Account,"global",Balance(company.Account),"Encerramento","Saldo sem outro titular após quitação",company.Id);company.Active=false;company.Decision="Insolvência persistente; liquidação real, sem comprador fictício";
            foreach(var v in State.Vehicles.Where(v=>v.OwnerAccount==company.Account)){v.OwnerAccount="unclaimed";v.Cause="Veículo sem titular após falência";}
            Event(company.Name+" encerrou atividade por insolvência; perdas de credores registradas");
        }
    }
}
