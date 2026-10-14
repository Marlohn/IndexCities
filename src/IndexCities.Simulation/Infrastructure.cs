namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    void UpdateInfrastructure(double dt)
    {
        var active=State.Buildings.Where(b=>b.Completed&&!b.Removed).ToList();
        // Presence does not change inside this calculation. Keep the public
        // query live, but avoid scanning the same citizens for every consumer.
        var present=active.ToDictionary(b=>b.Id,b=>WorkersPresent(b));
        double StaffFactor(Building b)=>Math.Clamp(present[b.Id]/Math.Max(1.0,b.Definition.Utility?b.Definition.Jobs/3.0:b.Definition.Jobs),0,1);
        State.WindFactor=Math.Clamp(.55+.25*Math.Sin(State.ElapsedSeconds/21600+State.Seed)+.15*Math.Sin(State.ElapsedSeconds/172800+State.Seed*.3),.15,.95);
        foreach(var b in active){
            var occupied=b.Kind==BuildingKind.House&&FamilyById(b.FamilyId) is {} f?f.Members.Count(id=>State.Citizens.Any(c=>c.Id==id&&c.Alive&&c.Resident)):0;
            var workers=present[b.Id];var inUse=b.Kind==BuildingKind.House?occupied>0:workers>0;
            b.WaterDemand=b.Kind==BuildingKind.House?occupied*6:inUse?workers*2+(b.Kind==BuildingKind.Farm?b.Definition.Width*b.Definition.Depth*.015:2):0;
            b.ElectricityDemand=b.Kind==BuildingKind.House?occupied*8:inUse?workers*4+(b.Kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment or BuildingKind.SewageTreatment?12:3):0;
            if(b.WaterRatio<.5&&inUse)b.WaterOutageSeconds+=dt;else b.WaterOutageSeconds=Math.Max(0,b.WaterOutageSeconds-dt*2);
        }
        var wind=active.Where(b=>b.Kind==BuildingKind.WindPower&&HasExteriorAccess(b)).ToList();
        foreach(var b in wind)b.OperationalCapacity=b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*State.WindFactor;
        State.ElectricityCapacity=wind.Sum(b=>b.OperationalCapacity);State.ElectricityAvailableToday+=State.ElectricityCapacity*dt/86400;
        var night=State.Now.Hour<6||State.Now.Hour>=18;var streetDemands=State.Roads.Where(n=>!n.Highway).ToDictionary(n=>n.Id,n=>night ? .12*n.MaintenanceRatio : 0);
        State.ElectricityDemand=active.Sum(b=>b.ElectricityDemand)+streetDemands.Values.Sum();
        var allocation=AllocateCapacity(active.Select(b=>(b.Id,b.Position,b.ElectricityDemand)).Concat(State.Roads.Where(r=>!r.Highway).Select(r=>(-r.Id,r.Position,streetDemands[r.Id]))).ToList(),State.ElectricityCapacity,wind.Select(b=>b.Position).ToList());
        foreach(var b in active){b.ElectricityRatio=b.ElectricityDemand<=0?0:allocation.GetValueOrDefault(b.Id)/b.ElectricityDemand;b.ElectricityConsumedToday+=b.ElectricityDemand*b.ElectricityRatio*dt/86400;}
        foreach(var n in State.Roads.Where(n=>!n.Highway)){n.ElectricityRatio=streetDemands[n.Id]<=0?0:allocation.GetValueOrDefault(-n.Id)/streetDemands[n.Id];State.StreetElectricityConsumedToday+=streetDemands[n.Id]*n.ElectricityRatio*dt/86400;}
        var intakes=active.Where(b=>b.Kind==BuildingKind.WaterIntake&&HasExteriorAccess(b)&&Math.Abs(b.Position.Distance(State.LakeCentre)-State.LakeRadius)<=22).ToList();
        foreach(var b in active.Where(b=>b.Kind==BuildingKind.WaterIntake))b.OperationalCapacity=intakes.Contains(b)?b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*b.ElectricityRatio:0;
        State.RawWaterCapacity=intakes.Sum(b=>b.OperationalCapacity);
        var treatments=active.Where(b=>b.Kind==BuildingKind.WaterTreatment&&HasExteriorAccess(b)).ToList();
        var nominal=treatments.Sum(b=>b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*b.ElectricityRatio/(1+State.LakePollution*.65));
        State.PotableCapacity=Math.Min(State.RawWaterCapacity,nominal);State.WaterAvailableToday+=State.PotableCapacity*dt/86400;State.WaterDemand=active.Sum(b=>b.WaterDemand);
        var water=AllocateCapacity(active.Select(b=>(b.Id,b.Position,b.WaterDemand)).ToList(),State.PotableCapacity,treatments.Select(b=>b.Position).ToList());
        foreach(var b in active){b.WaterRatio=b.WaterDemand<=0?0:water.GetValueOrDefault(b.Id)/b.WaterDemand;b.WaterConsumedToday+=b.WaterDemand*b.WaterRatio*dt/86400;b.SewageProduced=b.WaterDemand*b.WaterRatio*.8;}
        foreach(var b in treatments)b.OperationalCapacity=nominal==0?0:State.PotableCapacity*(b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*b.ElectricityRatio/(1+State.LakePollution*.65))/nominal;
        var sewage=active.Where(b=>b.Kind==BuildingKind.SewageTreatment&&HasExteriorAccess(b)).ToList();foreach(var b in sewage)b.OperationalCapacity=b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*b.ElectricityRatio;
        State.SewageCapacity=sewage.Sum(b=>b.OperationalCapacity);State.SewageDemand=active.Sum(b=>b.SewageProduced);State.UntreatedSewage=Math.Max(0,State.SewageDemand-State.SewageCapacity);
        var sewageAllocation=AllocateCapacity(active.Select(b=>(b.Id,b.Position,b.SewageProduced)).ToList(),State.SewageCapacity,sewage.Select(b=>b.Position).ToList());
        foreach(var b in active)b.UntreatedSewage=Math.Max(0,b.SewageProduced-sewageAllocation.GetValueOrDefault(b.Id));
        UpdatePollution(dt,active);
        foreach(var b in active){
            var staff=StaffFactor(b);var service=b.Definition.Utility;
            if(b.Kind==BuildingKind.House){b.Cause=b.FamilyId==null?"Disponível no mercado":b.WaterRatio<.5?"Abastecimento de água insuficiente":b.ElectricityRatio<.5?"Energia insuficiente":"Residência ocupada";continue;}
            if(!service){var waterEffect=Math.Max(0,1-b.WaterOutageSeconds/(State.Tuning.WaterClosureHours*3600));b.OperationalCapacity=b.Definition.Capacity*staff*b.MaintenanceRatio*(.25+.75*b.ElectricityRatio)*(.4+.6*b.WaterRatio)*waterEffect;if(b.Kind==BuildingKind.Farm)b.OperationalCapacity=b.Definition.Width*b.Definition.Depth*.10*staff*b.WaterRatio*b.ElectricityRatio*waterEffect;}
            b.Cause=!b.Definition.Private&&b.MaintenanceRatio<.9?"Manutenção pendente; capacidade reduzida":!HasExteriorAccess(b)?"Sem acesso viário":b.Definition.Private&&b.CompanyId==null?"Procurando empresa":b.OperationClosed?"Operação encerrada; imóvel preservado":!Open(b)&&!service?"Fora do horário de funcionamento":present[b.Id]==0?"Sem funcionários presentes no turno":b.Kind==BuildingKind.WaterIntake&&b.OperationalCapacity==0?"Captação sem fonte natural viável/energia":b.Kind==BuildingKind.WaterTreatment&&State.RawWaterCapacity==0?"Falta captação de água bruta":b.Kind!=BuildingKind.WindPower&&b.ElectricityRatio<.1?"Energia insuficiente":b.WaterOutageSeconds>=State.Tuning.WaterClosureHours*3600?"Parada após falta persistente de água":b.Kind is BuildingKind.Grocery or BuildingKind.FuelStation&&b.Quantity(b.Kind==BuildingKind.Grocery?ResourceKind.Food:ResourceKind.Fuel)<.1?"Estoque insuficiente":b.WaterRatio<.5&&!service?"Capacidade reduzida por falta de água":"Operando";
            if(b.Definition.Private&&(b.CompanyId==null||b.OperationClosed||!CompanyById(b.CompanyId)!.Active)||!HasExteriorAccess(b))b.OperationalCapacity=0;
        }
    }
    static Dictionary<int,double> AllocateCapacity(List<(int Id,Point Position,double Demand)> consumers,double capacity,List<Point> sources)
    {
        var result=consumers.ToDictionary(c=>c.Id,c=>0.0);var total=consumers.Sum(c=>c.Demand);if(total<=0||capacity<=0)return result;if(capacity>=total){foreach(var c in consumers)result[c.Id]=c.Demand;return result;}
        var pending=consumers.Where(c=>c.Demand>0).ToList();var remaining=capacity;
        // Distances and demands stay fixed throughout this allocation. Sum only
        // the remaining consumers, in the same order as the previous loop.
        var weights=pending.ToDictionary(c=>c.Id,c=>c.Demand/(1+(sources.Count==0?256:sources.Min(p=>p.Distance(c.Position)))/128));
        while(pending.Count>0&&remaining>.000001){var sum=pending.Sum(c=>weights[c.Id]);var allocated=0.0;foreach(var c in pending){var amount=Math.Min(c.Demand-result[c.Id],remaining*weights[c.Id]/sum);result[c.Id]+=amount;allocated+=amount;}remaining-=allocated;pending.RemoveAll(c=>result[c.Id]>=c.Demand-.000001);if(allocated<.000001)break;}return result;
    }
}
