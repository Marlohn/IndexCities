namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // Read-only conditions shared by inspection and the actual calculation.
    // Jobs and shift membership never imply that a person reached the building.
    public IEnumerable<Citizen> ScheduledWorkers(Building building)=>State.Citizens.Where(c=>c.WorkplaceId==building.Id&&ShiftActive(c));
    public int WorkersScheduled(Building building)=>ScheduledWorkers(building).Count();
    public const double NaturalWaterSourceReach=22;
    public bool HasNaturalWaterSource(Building building)=>building.Kind==BuildingKind.WaterIntake&&DistanceToLakeShore(building.Position)<=NaturalWaterSourceReach;
    public static double ServiceStaffReference(Building building)=>Math.Max(1.0,building.Definition.Utility?building.Definition.Jobs/3.0:building.Definition.Jobs);
    static double InfrastructureStaffFactor(Building building,int present)=>Math.Clamp(present/ServiceStaffReference(building),0,1);
    public double ServiceStaffFactor(Building building)=>InfrastructureStaffFactor(building,WorkersPresent(building));
    double InfrastructureTreatmentPotential(Building building,int present)=>building.Definition.Capacity*InfrastructureStaffFactor(building,present)*building.MaintenanceRatio*building.ElectricityRatio/(1+State.LakePollution*.65);
    public double WaterPollutionTreatmentFactor=>1/(1+State.LakePollution*.65);
    public double WaterTreatmentPotential(Building building)=>building.Completed&&!building.Removed&&building.Kind==BuildingKind.WaterTreatment&&HasExteriorAccess(building)?InfrastructureTreatmentPotential(building,WorkersPresent(building)):0;
    public double CurrentWaterTreatmentPotential=>State.Buildings.Sum(WaterTreatmentPotential);
    void UpdateInfrastructure(double dt)
    {
        var detail=CostProfile;
        var detailStarted=detail is null?0L:System.Diagnostics.Stopwatch.GetTimestamp();
        // Per-step working sets are reused buffers, refilled in list order.
        var active=_active;active.Clear();foreach(var b in State.Buildings)if(b.Completed&&!b.Removed)active.Add(b);
        // Presence does not change inside this calculation. Keep the public
        // query live, but count every building's present workers in one pass
        // over the citizens (the same WorkerPresentAt rule per worker).
        var present=_present;present.Clear();var byId=_activeById;byId.Clear();foreach(var b in active){present.Add(b.Id,0);byId.Add(b.Id,b);}
        if(Pr57LivingRosterEnabled)
        {
            // Share the exact per-step workforce groups with OperateBusinesses.
            // This occurs after relocations and before any subsequent job changes.
            Pr57PrepareStaff();
            foreach(var (site,workers) in _staff)
                if(byId.TryGetValue(site,out var workplace))
                    foreach(var c in workers)if(WorkerPresentAt(c,workplace))present[site]++;
        }
        else foreach(var c in State.Citizens)if(c.WorkplaceId is {} site&&byId.TryGetValue(site,out var workplace)&&WorkerPresentAt(c,workplace))present[site]++;
        if(detail is not null)detail.MarkDetail(0,ref detailStarted);
        double StaffFactor(Building b)=>InfrastructureStaffFactor(b,present[b.Id]);
        State.WindFactor=Math.Clamp(.55+.25*Math.Sin(State.ElapsedSeconds/21600+State.Seed)+.15*Math.Sin(State.ElapsedSeconds/172800+State.Seed*.3),.15,.95);
        foreach(var b in active){
            var occupied=b.Kind==BuildingKind.House&&FamilyById(b.FamilyId) is {} f?f.Members.Count(id=>CitizenById(id) is {Alive:true,Resident:true}):0;
            var workers=present[b.Id];var inUse=b.Kind==BuildingKind.House?occupied>0:workers>0;
            b.WaterDemand=b.Kind==BuildingKind.House?occupied*6:inUse?workers*2+(b.Kind==BuildingKind.Farm?b.Definition.Width*b.Definition.Depth*.015:2):0;
            b.ElectricityDemand=b.Kind==BuildingKind.House?occupied*8:inUse?workers*4+(b.Kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment or BuildingKind.SewageTreatment?12:3):0;
            if(b.WaterRatio<.5&&inUse)b.AddWaterOutageSeconds(dt);else {b.AddWaterOutageSeconds(-dt*2);b.WaterOutageSecondsUnits=Math.Max(0,b.WaterOutageSecondsUnits);}
        }
        if(detail is not null)detail.MarkDetail(1,ref detailStarted);
        var wind=active.Where(b=>b.Kind==BuildingKind.WindPower&&HasExteriorAccess(b)).ToList();
        foreach(var b in wind)b.OperationalCapacity=b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*State.WindFactor;
        State.ElectricityCapacity=wind.Sum(b=>b.OperationalCapacity);State.AddElectricityAvailableToday(State.ElectricityCapacity*dt/86400);
        var night=State.Now.Hour<6||State.Now.Hour>=18;var streets=_streets;streets.Clear();foreach(var n in State.Roads)if(!n.Highway)streets.Add(n);
        // Consumers: active buildings, then streets, in that order (as one list).
        var allocation=_allocation;allocation.Reset(active.Count+streets.Count);
        for(var i=0;i<active.Count;i++)allocation.Set(i,active[i].Position,active[i].ElectricityDemand);
        for(var j=0;j<streets.Count;j++){var n=streets[j];allocation.Set(active.Count+j,n.Position,night&&!n.ConstructionOnly ? .12*n.MaintenanceRatio : 0);}
        var streetDemand=0.0;for(var j=0;j<streets.Count;j++)streetDemand+=allocation.Demands[active.Count+j];
        State.ElectricityDemand=active.Sum(b=>b.ElectricityDemand)+streetDemand;
        _sources.Clear();foreach(var b in wind)_sources.Add(b.Position);allocation.Allocate(State.ElectricityCapacity,_sources);
        for(var i=0;i<active.Count;i++){var b=active[i];b.ElectricityRatio=b.ElectricityDemand<=0?0:allocation.Result[i]/b.ElectricityDemand;b.AddElectricityConsumedToday(b.ElectricityDemand*b.ElectricityRatio*dt/86400);}
        for(var j=0;j<streets.Count;j++){var n=streets[j];var demand=allocation.Demands[active.Count+j];n.ElectricityRatio=demand<=0?0:allocation.Result[active.Count+j]/demand;State.AddStreetElectricityConsumedToday(demand*n.ElectricityRatio*dt/86400);}
        if(detail is not null)detail.MarkDetail(2,ref detailStarted);
        var intakes=active.Where(b=>b.Kind==BuildingKind.WaterIntake&&HasExteriorAccess(b)&&HasNaturalWaterSource(b)).ToList();
        foreach(var b in active.Where(b=>b.Kind==BuildingKind.WaterIntake))b.OperationalCapacity=intakes.Contains(b)?b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*b.ElectricityRatio:0;
        State.RawWaterCapacity=intakes.Sum(b=>b.OperationalCapacity);
        var treatments=active.Where(b=>b.Kind==BuildingKind.WaterTreatment&&HasExteriorAccess(b)).ToList();
        var nominal=treatments.Sum(b=>InfrastructureTreatmentPotential(b,present[b.Id]));
        State.PotableCapacity=Math.Min(State.RawWaterCapacity,nominal);State.AddWaterAvailableToday(State.PotableCapacity*dt/86400);State.WaterDemand=active.Sum(b=>b.WaterDemand);
        var water=_allocation;water.Reset(active.Count);for(var i=0;i<active.Count;i++)water.Set(i,active[i].Position,active[i].WaterDemand);
        _sources.Clear();foreach(var b in treatments)_sources.Add(b.Position);water.Allocate(State.PotableCapacity,_sources);
        for(var i=0;i<active.Count;i++){var b=active[i];b.WaterRatio=b.WaterDemand<=0?0:water.Result[i]/b.WaterDemand;b.AddWaterConsumedToday(b.WaterDemand*b.WaterRatio*dt/86400);b.SewageProduced=b.WaterDemand*b.WaterRatio*.8;}
        foreach(var b in treatments)b.OperationalCapacity=nominal==0?0:State.PotableCapacity*InfrastructureTreatmentPotential(b,present[b.Id])/nominal;
        if(detail is not null)detail.MarkDetail(3,ref detailStarted);
        var sewage=active.Where(b=>b.Kind==BuildingKind.SewageTreatment&&HasExteriorAccess(b)).ToList();foreach(var b in sewage)b.OperationalCapacity=b.Definition.Capacity*StaffFactor(b)*b.MaintenanceRatio*b.ElectricityRatio;
        State.SewageCapacity=sewage.Sum(b=>b.OperationalCapacity);State.SewageDemand=active.Sum(b=>b.SewageProduced);State.UntreatedSewage=Math.Max(0,State.SewageDemand-State.SewageCapacity);
        var sewageAllocation=_allocation;sewageAllocation.Reset(active.Count);for(var i=0;i<active.Count;i++)sewageAllocation.Set(i,active[i].Position,active[i].SewageProduced);
        _sources.Clear();foreach(var b in sewage)_sources.Add(b.Position);sewageAllocation.Allocate(State.SewageCapacity,_sources);
        for(var i=0;i<active.Count;i++){var b=active[i];b.UntreatedSewage=Math.Max(0,b.SewageProduced-sewageAllocation.Result[i]);}
        UpdatePollution(dt,active);
        if(detail is not null)detail.MarkDetail(4,ref detailStarted);
        foreach(var b in active){
            var staff=StaffFactor(b);var service=b.Definition.Utility;
            if(b.Kind==BuildingKind.House){b.Cause=b.FamilyId==null?"Disponível no mercado":b.WaterRatio<.5?"Abastecimento de água insuficiente":b.ElectricityRatio<.5?"Energia insuficiente":"Residência ocupada";continue;}
            if(!service){var waterEffect=Math.Max(0,1-b.WaterOutageSeconds/(State.Tuning.WaterClosureHours*3600));b.OperationalCapacity=b.Definition.Capacity*staff*b.MaintenanceRatio*(.25+.75*b.ElectricityRatio)*(.4+.6*b.WaterRatio)*waterEffect;if(b.Kind==BuildingKind.Farm)b.OperationalCapacity=b.Definition.Width*b.Definition.Depth*.10*staff*b.WaterRatio*b.ElectricityRatio*waterEffect;}
            b.Cause=!b.Definition.Private&&b.MaintenanceRatio<.9?"Manutenção pendente; capacidade reduzida":!HasExteriorAccess(b)?"Sem acesso viário":b.Definition.Private&&b.CompanyId==null?"Procurando empresa":b.OperationClosed?"Operação encerrada; imóvel preservado":!Open(b)&&!service?"Fora do horário de funcionamento":present[b.Id]==0?"Sem funcionários presentes no turno":b.Kind==BuildingKind.WaterIntake&&b.OperationalCapacity==0?"Captação sem fonte natural viável/energia":b.Kind==BuildingKind.WaterTreatment&&State.RawWaterCapacity==0?"Falta captação de água bruta":b.Kind!=BuildingKind.WindPower&&b.ElectricityRatio<.1?"Energia insuficiente":b.WaterOutageSeconds>=State.Tuning.WaterClosureHours*3600?"Parada após falta persistente de água":b.Kind is BuildingKind.Grocery or BuildingKind.FuelStation&&b.Quantity(b.Kind==BuildingKind.Grocery?ResourceKind.Food:ResourceKind.Fuel)<.1?"Estoque insuficiente":b.WaterRatio<.5&&!service?"Capacidade reduzida por falta de água":"Operando";
            if(b.Definition.Private&&(b.CompanyId==null||b.OperationClosed||!CompanyById(b.CompanyId)!.Active)||!HasExteriorAccess(b))b.OperationalCapacity=0;
        }
        if(detail is not null)detail.MarkDetail(5,ref detailStarted);
    }
    readonly List<Building> _active=[];readonly Dictionary<int,int> _present=new();readonly Dictionary<int,Building> _activeById=new();
    readonly List<RoadNode> _streets=[];readonly List<Point> _sources=[];readonly CapacityAllocation _allocation=new();
}

// Capacity shared among consumers by demand, weighted by distance to the
// nearest source; results by consumer position. Reusable buffers, same
// arithmetic and summation order on every step.
sealed class CapacityAllocation
{
    public Point[] Positions=[];public double[] Demands=[];public double[] Result=[];
    double[] _weights=[];int[] _pending=[];int _count;
    public void Reset(int count)
    {
        _count=count;if(Demands.Length>=count)return;
        var size=Math.Max(count,Demands.Length*2);Positions=new Point[size];Demands=new double[size];Result=new double[size];_weights=new double[size];_pending=new int[size];
    }
    public void Set(int index,Point position,double demand){Positions[index]=position;Demands[index]=demand;}
    public void Allocate(double capacity,List<Point> sources)
    {
        var total=0.0;for(var i=0;i<_count;i++){Result[i]=0;total+=Demands[i];}
        if(total<=0||capacity<=0)return;
        if(capacity>=total){for(var i=0;i<_count;i++)Result[i]=Demands[i];return;}
        var pending=0;for(var i=0;i<_count;i++)if(Demands[i]>0)_pending[pending++]=i;var remaining=capacity;
        // Distances and demands stay fixed throughout this allocation. Sum only
        // the remaining consumers, in the same order as the previous loop.
        for(var k=0;k<pending;k++){var i=_pending[k];_weights[i]=Demands[i]/(1+(sources.Count==0?256:NearestSource(sources,Positions[i]))/128);}
        while(pending>0&&remaining>.000001){
            var sum=0.0;for(var k=0;k<pending;k++)sum+=_weights[_pending[k]];
            var allocated=0.0;for(var k=0;k<pending;k++){var i=_pending[k];var amount=Math.Min(Demands[i]-Result[i],remaining*_weights[i]/sum);Result[i]+=amount;allocated+=amount;}
            remaining-=allocated;
            var kept=0;for(var k=0;k<pending;k++){var i=_pending[k];if(!(Result[i]>=Demands[i]-.000001))_pending[kept++]=i;}pending=kept;
            if(allocated<.000001)break;
        }
    }
    static double NearestSource(List<Point> sources,Point position){var nearest=sources[0].Distance(position);for(var i=1;i<sources.Count;i++){var d=sources[i].Distance(position);if(d<nearest)nearest=d;}return nearest;}
}
