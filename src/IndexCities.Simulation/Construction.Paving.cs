namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    static bool IsPaving(WorkProject p)=>!p.IsMaintenance&&p.RoadPoints.Count>0;

    int PavingComponents(IReadOnlyList<Point> points)
    {
        var pending=points.ToHashSet();var count=0;
        while(pending.Count>0){count++;var queue=new Queue<Point>();queue.Enqueue(pending.First());pending.Remove(queue.Peek());
            while(queue.Count>0){var at=queue.Dequeue();foreach(var next in pending.Where(p=>p.Distance(at)<=State.Tuning.RoadStep+.01).ToArray()){pending.Remove(next);queue.Enqueue(next);}}}
        return Math.Max(1,count);
    }

    decimal QuoteMachinery(Point position,double seconds)
    {
        var distance=Math.Abs(position.X-State.Entry.X)+Math.Abs(position.Z-State.Entry.Z);
        return Money(2*(State.Tuning.PavingMachineHire+State.Tuning.FreightBase+(decimal)(distance*2)*State.Tuning.FreightPerMetre+State.Tuning.ImportPrices[ResourceKind.Fuel]*(decimal)State.Tuning.FuelTank)+(State.Tuning.PavingMachineHourlyPrice+State.Tuning.ExternalCrewHourlyPrice)*(decimal)((seconds+120)/3600));
    }

    void EnsurePavingFleet()
    {
        if(State.PavingFleetInitialized)return;
        State.PavingFleetInitialized=true;
        for(var team=0;team<State.Tuning.ExternalPavingTeams;team++)
            foreach(var kind in new[]{PavingMachine.Paver,PavingMachine.Roller})
                State.Vehicles.Add(new(){Id=Id(),Machine=kind,External=true,ExternalDriverId=$"operador externo {team+1} {kind}",Position=State.Entry,Status=VehicleStatus.Outside,CargoCapacity=0});
    }

    void OrderPavingMachines(WorkProject p,RoadNode access)
    {
        if(p.PaverId!=null||p.RollerId!=null)return;
        if(!p.RoadPoints.Any(target=>!p.RolledRoadPoints.Contains(target)&&PavingSiteRoute(p,access.Position,target).Count>0)){p.Cause=WorkCause.PavingAwaitingSiteLink;return;}
        EnsurePavingFleet();
        var paver=State.Vehicles.FirstOrDefault(v=>v.Machine==PavingMachine.Paver&&v.Status==VehicleStatus.Outside&&v.ServicePhase==ServicePhase.None&&v.MachineProjectId==null);
        var roller=State.Vehicles.FirstOrDefault(v=>v.Machine==PavingMachine.Roller&&v.Status==VehicleStatus.Outside&&v.ServicePhase==ServicePhase.None&&v.MachineProjectId==null);
        if(paver==null||roller==null){p.Cause=WorkCause.AwaitingPavingMachines;return;}
        var route=FindRoadRoute(EntryNode().Id,access.Id,true);if(route.Count==0)return;
        var minimumFuel=(2*RouteLength(route)+2*p.RoadPoints.Count*State.Tuning.RoadStep+50)*State.Tuning.FuelPerMetre;
        if(minimumFuel>State.Tuning.FuelTank){p.Cause=WorkCause.PavingInsufficientRange;return;}
        var fuel=new[]{paver,roller}.Sum(v=>Math.Max(0,State.Tuning.FuelTank-v.Fuel));
        if(State.OutsideStock[ResourceKind.Fuel]<fuel){p.Cause=WorkCause.SupplierOutOfFuel;return;}
        var fee=Money(2*(State.Tuning.PavingMachineHire+State.Tuning.FreightBase+(decimal)(RouteLength(route)*2)*State.Tuning.FreightPerMetre)+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]);
        if(!ProjectPay(p,fee,"Máquinas de pavimentação","Contratação, deslocamento e combustível de pavimentadora e rolo externos")){p.Cause=WorkCause.NoBudgetForPaving;return;}
        p.MachineryPaid+=fee;State.OutsideStock.AddAmount(ResourceKind.Fuel,-(fuel));
        foreach(var machine in new[]{paver,roller}){
            machine.Fuel=State.Tuning.FuelTank;machine.MachineProjectId=p.Id;machine.MachinePhase=MachinePhase.ToSite;
            machine.CrewAboard=true;machine.CrewCount=1;machine.Route=route.ToList();machine.RouteIndex=0;
            var bay=PavingBay(access,machine.Machine,p);machine.Route=FindRoadRoute(EntryNode().Id,bay.Road.Id,true);machine.DockPosition=bay.Position;machine.Status=VehicleStatus.Travelling;
            machine.FeedingReady=false;machine.SitePath=[];machine.SitePathIndex=0;machine.WorkTarget=null;machine.Cause="Equipe e máquina a caminho da obra "+p.Id;
        }
        p.PaverId=paver.Id;p.RollerId=roller.Id;p.Cause=WorkCause.PavingMachinesInTransit;
    }

    bool PavingMachineArrived(Vehicle v)
    {
        if(v.Machine==PavingMachine.None)return false;
        if(v.MachinePhase==MachinePhase.ToSite){v.MachinePhase=MachinePhase.OnSite;v.Status=VehicleStatus.Parked;v.IntersectionClaim=null;v.Cause="Máquina e operador no canteiro";return true;}
        if(v.MachinePhase==MachinePhase.Returning&&v.Position.Distance(State.Entry)<8){
            var project=State.Projects.FirstOrDefault(p=>p.Id==v.MachineProjectId);
            if(project==null||project.Status is WorkStatus.Completed or WorkStatus.Cancelled||(v.Machine==PavingMachine.Paver?project.PaverId:project.RollerId)!=v.Id)v.MachineProjectId=null;
            v.MachinePhase=MachinePhase.Available;v.Status=VehicleStatus.Outside;v.CrewAboard=false;v.CrewCount=0;v.Route.Clear();v.IntersectionClaim=null;v.Cause="Máquina no exterior; respeita vínculo da equipe contratada";return true;}
        return false;
    }

    // Only this work's physical site and actual roads are traversable. A planned
    // point belonging to another work is never inserted to bridge a gap.
    List<Point> PavingSiteRoute(WorkProject p,Point from,Point target)
    {
        var points=p.RoadPoints.Concat(State.Roads.Where(r=>r.MaintenanceRatio>.05).Select(r=>r.Position)).Distinct().ToList();
        var bayMachine=State.Vehicles.FirstOrDefault(v=>v.MachineProjectId==p.Id&&v.DockPosition is {} dock&&dock.Distance(from)<.1&&v.Route.Count>0&&v.RouteIndex>=v.Route.Count);
        var start=bayMachine!=null&&_roads.TryGetValue(bayMachine.Route[^1],out var bayRoad)?bayRoad.Position:points.MinBy(a=>a.Distance(from));
        if(start.Distance(from)>State.Tuning.RoadWidth+5||!points.Contains(target))return [];
        var parents=new Dictionary<Point,Point?>{{start,null}};var queue=new Queue<Point>();queue.Enqueue(start);
        while(queue.Count>0&&!parents.ContainsKey(target)){
            var at=queue.Dequeue();
            foreach(var next in points.Where(a=>a.Distance(at)<=State.Tuning.RoadStep+.01))if(!parents.ContainsKey(next)){parents[next]=at;queue.Enqueue(next);}
        }
        if(!parents.ContainsKey(target))return [];
        var result=new List<Point>();for(Point? at=target;at!=null;at=parents[at.Value])result.Add(at.Value);result.Reverse();return result;
    }

    bool ProgressPaving(WorkProject p,double dt)
    {
        if(p.CrewVehicleId is {} legacyId&&State.Vehicles.FirstOrDefault(v=>v.Id==legacyId&&v.Status==VehicleStatus.Parked) is {} legacyCrew){p.CrewVehicleId=null;ReturnExternal(legacyCrew);}
        var access=WorkAccess(p);
        if(access==null){p.Cause=WorkCause.WorkRouteInterrupted;return false;}
        var paver=State.Vehicles.FirstOrDefault(v=>v.Id==p.PaverId&&v.MachineProjectId==p.Id);
        var roller=State.Vehicles.FirstOrDefault(v=>v.Id==p.RollerId&&v.MachineProjectId==p.Id);
        if(paver==null||roller==null){p.Cause=WorkCause.AwaitingPavingMachines;return false;}
        if(paver.MachinePhase==MachinePhase.ToSite||roller.MachinePhase==MachinePhase.ToSite){p.Cause=new[]{paver,roller}.Any(v=>v.Status==VehicleStatus.Stranded)?WorkCause.PavingOutOfFuel:new[]{paver,roller}.Any(v=>v.Status==VehicleStatus.Waiting)?WorkCause.WorkRouteInterrupted:WorkCause.PavingMachinesInTransit;return false;}
        if(paver.MachinePhase==MachinePhase.OnSite&&(!paver.CrewAboard||paver.CrewCount<1)||roller.MachinePhase==MachinePhase.OnSite&&(!roller.CrewAboard||roller.CrewCount<1)){p.Cause=WorkCause.AwaitingCrewOnSite;return false;}
        if(paver.MachinePhase==MachinePhase.OnSite&&!paver.FeedingReady){PositionPavingReceiver(p,paver,access,dt);return false;}
        if(!p.Required.All(k=>p.Delivered.GetValueOrDefault(k.Key)+.001>=k.Value)){p.Cause=WorkCause.AwaitingPhysicalDelivery;return false;}
        if(State.Vehicles.Any(v=>v.Machine==PavingMachine.None&&(v.SiteProjectId==p.Id&&v.SitePathBeforeRoad&&v.SitePath.Count>0||v.ShipmentId is {} id&&State.Shipments.Any(s=>s.Id==id&&s.ProjectId==p.Id&&s.ReceivingMachineId==paver.Id&&!s.Complete)))){p.Cause=WorkCause.AwaitingPhysicalDelivery;return false;}
        p.Cause=WorkCause.PavingInProgress;
        if(paver.MachinePhase==MachinePhase.OnSite){AdvancePavingMachine(p,paver,dt);
            if(paver.WorkTarget==null&&!p.RoadPoints.Any(target=>!p.AppliedRoadPoints.Contains(target)&&PavingSiteRoute(p,paver.Position,target).Count>0))BeginLeavingSite(paver);}
        // One physical work lane: finish after the paver has cleared the site.
        // It avoids overlapping full-size equipment at bends and short streets.
        if(PaverClearedRollerEntry(paver,roller)&&roller.MachinePhase==MachinePhase.OnSite){AdvancePavingMachine(p,roller,dt);
            if(roller.WorkTarget==null&&p.AppliedRoadPoints.All(p.RolledRoadPoints.Contains)){
                BeginLeavingSite(roller);
                if(p.RolledRoadPoints.Count<p.RoadPoints.Count){p.PaverId=null;p.RollerId=null;ReleasePavingMachines(p);p.Cause=WorkCause.PavingAwaitingSiteLink;}
            }}
        p.Progress=(p.AppliedRoadPoints.Count+p.RolledRoadPoints.Count)/(2.0*p.RoadPoints.Count);
        if(p.Progress>0)p.Status=WorkStatus.InProgress;
        return p.RolledRoadPoints.Count>=p.RoadPoints.Count&&PavingSiteCleared(p);
    }

    bool PavingSiteCleared(WorkProject p)=>!State.Vehicles.Any(v=>v.MachineProjectId==p.Id&&v.MachinePhase is MachinePhase.ToSite or MachinePhase.OnSite or MachinePhase.LeavingSite);

    static bool PaverClearedRollerEntry(Vehicle paver,Vehicle roller)
    {
        if(paver.MachinePhase==MachinePhase.Available)return true;
        if(paver.MachinePhase!=MachinePhase.Returning||roller.Route.Count==0)return false;
        var bay=paver.Route.IndexOf(roller.Route[^1]);
        return (bay<0||paver.RouteIndex>bay+2)&&paver.Position.Distance(roller.Position)>12;
    }

    void PositionPavingReceiver(WorkProject p,Vehicle paver,RoadNode access,double dt)
    {
        if(p.Delivered.GetValueOrDefault(ResourceKind.Aggregate)+.001<p.Required.GetValueOrDefault(ResourceKind.Aggregate)){p.Cause=WorkCause.AwaitingPhysicalDelivery;return;}
        var direction=paver.Route.Count>1?_roads[paver.Route[^2]].Position-_roads[paver.Route[^1]].Position:new Point(-1,0);
        paver.Heading=Math.Atan2(direction.X,direction.Z);
        paver.FeedingReady=true;paver.SitePath=[];paver.SitePathIndex=0;paver.Cause="Pavimentadora posicionada para receber Asfalto";p.Cause=WorkCause.AwaitingPhysicalDelivery;
    }

    void AdvancePavingMachine(WorkProject p,Vehicle machine,double dt)
    {
        var done=machine.Machine==PavingMachine.Paver?p.AppliedRoadPoints:p.RolledRoadPoints;
        if(done.Count>=p.RoadPoints.Count)return;
        if(machine.WorkTarget==null){
            // Not one stopped by this machine's own body, nor the queue held behind it:
            // they pass only once the machine moves.
            if(done.Count==0&&State.Vehicles.Any(v=>v.Machine==PavingMachine.None&&v.Status is VehicleStatus.Travelling or VehicleStatus.Unloading&&v.Position.Distance(machine.Position)<25&&!HeldByBody(v,machine))){p.Cause=WorkCause.PavingSiteOccupied;return;}
            foreach(var target in p.RoadPoints.Where(point=>!done.Contains(point)&&(machine.Machine==PavingMachine.Paver||p.AppliedRoadPoints.Contains(point))).OrderBy(point=>point.Distance(machine.Position))){
                var route=PavingApproachRoute(p,machine.Position,target);if(route.Count==0)continue;
                machine.SitePath=route;machine.SitePathIndex=0;machine.WorkTarget=target;break;
            }
            if(machine.WorkTarget==null){machine.Cause="Aguarda trecho aplicado ou ligação real do canteiro";p.Cause=WorkCause.PavingAwaitingSiteLink;return;}
        }
        if(machine.SitePath.Skip(machine.SitePathIndex).Any(point=>p.RoadPoints.All(at=>at.Distance(point)>2)&&State.Roads.All(r=>r.Position.Distance(point)>2))){machine.SitePath=[];machine.WorkTarget=null;p.Cause=WorkCause.WorkRouteInterrupted;return;}
        var workSpeed=Math.Min(2,State.Tuning.RoadStep*p.RoadPoints.Count*2/p.WorkSeconds);
        var hourly=State.Tuning.PavingMachineHourlyPrice+State.Tuning.ExternalCrewHourlyPrice;
        var remaining=dt;
        while(remaining>.000001&&machine.SitePathIndex<machine.SitePath.Count){
            var target=machine.SitePath[machine.SitePathIndex];var delta=target-machine.Position;var distance=delta.Distance(default);
            if(distance<.000001){machine.SitePathIndex++;continue;}
            var executing=machine.SitePathIndex==machine.SitePath.Count-1;
            var speed=executing?workSpeed:State.Tuning.VehicleSpeed;var travel=Math.Min(distance,speed*remaining);
            var due=Money(p.CrewCostDue+(executing?(decimal)(travel/speed/3600)*hourly:0));
            if(due>p.BudgetRemaining){p.Cause=WorkCause.NoBudgetForPaving;machine.Cause=p.Cause;break;}
            if(machine.Fuel<travel*State.Tuning.FuelPerMetre){p.Cause=WorkCause.PavingOutOfFuel;machine.Cause=p.Cause;break;}
            var previousHeading=machine.Heading;machine.Heading=Math.Atan2(delta.X,delta.Z);travel=MachineMayEnter(machine)?SafeSiteTravel(machine,target,travel):0;
            if(travel<=.000001){machine.Heading=previousHeading;p.Cause=WorkCause.PavingSiteOccupied;machine.Cause=p.Cause;break;}
            machine.Position=machine.Position.MoveTowards(target,travel);remaining-=travel/speed;
            machine.AddFuel(-(travel*State.Tuning.FuelPerMetre));machine.AddFuelBurnedLastStep(travel*State.Tuning.FuelPerMetre);
            if(executing){p.CrewCostDue+=(decimal)(travel/speed/3600)*hourly;
                if(p.CrewCostDue>=1){var payable=Money(p.CrewCostDue);if(ProjectPay(p,payable,"Máquinas de pavimentação","Aplicação/acabamento efetivos; operadores e máquinas")){p.MachineryPaid+=payable;p.CrewCostDue=0;}}
            }
            machine.Cause=executing?(machine.Machine==PavingMachine.Paver?"Aplicando asfalto na obra "+p.Id:"Compactando asfalto na obra "+p.Id):"Deslocando máquina dentro do acesso real";
            if(travel>=distance-.000001)machine.SitePathIndex++;else if(remaining>.000001)break;
        }
        if(machine.SitePathIndex>=machine.SitePath.Count){var point=machine.WorkTarget.Value;done.Add(point);machine.WorkTarget=null;machine.SitePath=[];machine.SitePathIndex=0;
            if(machine.Machine==PavingMachine.Roller){var road=AddRoad(point);road.ConstructionOnly=true;road.WorkProjectId=p.Id;p.BuiltRoadNodes.Add(road.Id);RebuildRoadGraph();SetDefaultJunctions();}}
    }

    static void BeginLeavingSite(Vehicle machine){machine.WorkTarget=null;machine.SitePath=[];machine.SitePathIndex=0;machine.MachinePhase=MachinePhase.LeavingSite;machine.Status=VehicleStatus.Parked;}

    void ReplanPavingMachine(Vehicle machine)
    {
        if(machine.MachinePhase==MachinePhase.Returning){ReturnExternal(machine);return;}
        if(machine.MachinePhase!=MachinePhase.ToSite||machine.MachineProjectId is not {} projectId)return;
        var project=State.Projects.First(p=>p.Id==projectId);var access=WorkAccess(project);var start=NearestRoad(machine.Position);
        if(project.Status is WorkStatus.Completed or WorkStatus.Cancelled){machine.MachinePhase=MachinePhase.Returning;ReturnExternal(machine);return;}
        if(start==null||access==null||start.Position.Distance(machine.Position)>State.Tuning.RoadWidth/2+3)return;
        var route=FindRoadRoute(start.Id,access.Id,true);if(route.Count==0)return;
        var bay=PavingBay(access,machine.Machine,project);var toBay=FindRoadRoute(start.Id,bay.Road.Id,true);if(toBay.Count==0)return;
        machine.Route=toBay;machine.RouteIndex=0;machine.DockPosition=bay.Position;machine.Status=VehicleStatus.Travelling;
    }

    void ReleasePavingMachines(WorkProject p)
    {
        foreach(var machine in State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.MachineProjectId==p.Id&&v.MachinePhase is not (MachinePhase.Returning or MachinePhase.LeavingSite))){
            if(machine.Status==VehicleStatus.Outside){machine.MachineProjectId=null;continue;}
            machine.WorkTarget=null;machine.SitePath=[];machine.SitePathIndex=0;
            if(machine.MachinePhase==MachinePhase.ToSite){machine.MachinePhase=MachinePhase.Returning;ReturnExternal(machine);continue;}
            BeginLeavingSite(machine);
        }
    }

    void ReturnPavingMachines(double dt)
    {
        foreach(var machine in State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.MachinePhase==MachinePhase.LeavingSite)){
            var p=State.Projects.First(p=>p.Id==machine.MachineProjectId);
            var access=WorkAccess(p);if(access==null){machine.Cause="Máquina sem acesso real para sair";continue;}
            if(machine.Machine==PavingMachine.Roller&&State.Vehicles.Any(other=>other.Machine==PavingMachine.Paver&&other.MachineProjectId==p.Id&&!PaverClearedRollerEntry(other,machine))){machine.Cause=WorkCause.PavingSiteOccupied;continue;}
            if(machine.SitePath.Count==0){machine.SitePath=PavingExitRoute(p,machine.Position,access.Position);machine.SitePathIndex=0;}
            var path=machine.SitePath;if(path.Count==0){machine.Cause="Máquina aguarda ligação do canteiro para retornar";continue;}
            var remaining=dt;MoveConstructionSitePath(machine,ref remaining,State.Tuning.VehicleSpeed);
            if(machine.SitePathIndex>=path.Count){machine.MachinePhase=MachinePhase.Returning;machine.DockPosition=null;machine.SitePath=[];ReturnExternal(machine);}
        }
    }
}
