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
        return Money(2*(State.Tuning.PavingMachineHire+State.Tuning.FreightBase+(decimal)(distance*2)*State.Tuning.FreightPerMetre+State.Tuning.ImportPrices[ResourceKind.Fuel]*(decimal)State.Tuning.FuelTank)+(State.Tuning.PavingMachineHourlyPrice+State.Tuning.ExternalCrewHourlyPrice)*(decimal)((seconds*2+120)/3600));
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
        if(!p.Required.All(k=>p.Delivered.GetValueOrDefault(k.Key)+.001>=k.Value))return;
        if(!p.RoadPoints.Any(target=>!p.RolledRoadPoints.Contains(target)&&PavingSiteRoute(p,access.Position,target).Count>0)){p.Cause=WorkCause.PavingAwaitingSiteLink;return;}
        EnsurePavingFleet();
        var paver=State.Vehicles.FirstOrDefault(v=>v.Machine==PavingMachine.Paver&&v.Status==VehicleStatus.Outside&&v.MachineProjectId==null);
        var roller=State.Vehicles.FirstOrDefault(v=>v.Machine==PavingMachine.Roller&&v.Status==VehicleStatus.Outside&&v.MachineProjectId==null);
        if(paver==null||roller==null){p.Cause=WorkCause.AwaitingPavingMachines;return;}
        var route=FindRoadRoute(EntryNode().Id,access.Id,true);if(route.Count==0)return;
        var fuel=new[]{paver,roller}.Sum(v=>Math.Max(0,State.Tuning.FuelTank-v.Fuel));
        if(State.OutsideStock[ResourceKind.Fuel]<fuel){p.Cause=WorkCause.SupplierOutOfFuel;return;}
        var fee=Money(2*(State.Tuning.PavingMachineHire+State.Tuning.FreightBase+(decimal)(RouteLength(route)*2)*State.Tuning.FreightPerMetre)+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]);
        if(!ProjectPay(p,fee,"Máquinas de pavimentação","Contratação, deslocamento e combustível de pavimentadora e rolo externos")){p.Cause=WorkCause.NoBudgetForPaving;return;}
        p.MachineryPaid+=fee;State.OutsideStock[ResourceKind.Fuel]-=fuel;
        foreach(var machine in new[]{paver,roller}){
            machine.Fuel=State.Tuning.FuelTank;machine.MachineProjectId=p.Id;machine.MachinePhase=MachinePhase.ToSite;
            machine.CrewAboard=true;machine.CrewCount=1;machine.Route=route.ToList();machine.RouteIndex=0;
            machine.DockPosition=PavingDock(access,machine.Machine);machine.Status=VehicleStatus.Travelling;
            machine.SitePath=[];machine.SitePathIndex=0;machine.WorkTarget=null;machine.Cause="Equipe e máquina a caminho da obra "+p.Id;
        }
        p.PaverId=paver.Id;p.RollerId=roller.Id;p.Cause=WorkCause.PavingMachinesInTransit;
    }

    Point PavingDock(RoadNode access,PavingMachine kind)
    {
        var dock=DockAt(access,null,true);
        return kind==PavingMachine.Paver?dock:access.Position*2-dock;
    }

    bool PavingMachineArrived(Vehicle v)
    {
        if(v.Machine==PavingMachine.None)return false;
        if(v.MachinePhase==MachinePhase.ToSite){v.MachinePhase=MachinePhase.OnSite;v.Status=VehicleStatus.Parked;v.IntersectionClaim=null;v.Cause="Máquina e operador no canteiro";return true;}
        if(v.MachinePhase==MachinePhase.Returning&&v.Position.Distance(State.Entry)<8){v.MachinePhase=MachinePhase.Available;v.MachineProjectId=null;v.Status=VehicleStatus.Outside;v.CrewAboard=false;v.CrewCount=0;v.Route.Clear();v.IntersectionClaim=null;v.Cause="Máquina disponível no exterior";return true;}
        return false;
    }

    // Only this work's physical site and actual roads are traversable. A planned
    // point belonging to another work is never inserted to bridge a gap.
    List<Point> PavingSiteRoute(WorkProject p,Point from,Point target)
    {
        var points=p.RoadPoints.Concat(State.Roads.Where(r=>r.MaintenanceRatio>.05).Select(r=>r.Position)).Distinct().ToList();
        var access=WorkAccess(p);
        var atArrivalBay=access!=null&&(from.Distance(PavingDock(access,PavingMachine.Paver))<.1||from.Distance(PavingDock(access,PavingMachine.Roller))<.1);
        var start=atArrivalBay?access!.Position:points.MinBy(a=>a.Distance(from));
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
        if(!p.Required.All(k=>p.Delivered.GetValueOrDefault(k.Key)+.001>=k.Value)){p.Cause=WorkCause.AwaitingPhysicalDelivery;return false;}
        var paver=State.Vehicles.FirstOrDefault(v=>v.Id==p.PaverId);
        var roller=State.Vehicles.FirstOrDefault(v=>v.Id==p.RollerId);
        if(paver==null||roller==null){p.Cause=WorkCause.AwaitingPavingMachines;return false;}
        if(paver.MachinePhase==MachinePhase.ToSite||roller.MachinePhase==MachinePhase.ToSite){p.Cause=new[]{paver,roller}.Any(v=>v.Status==VehicleStatus.Stranded)?WorkCause.PavingOutOfFuel:new[]{paver,roller}.Any(v=>v.Status==VehicleStatus.Waiting)?WorkCause.WorkRouteInterrupted:WorkCause.PavingMachinesInTransit;return false;}
        if(paver.MachinePhase==MachinePhase.OnSite&&(!paver.CrewAboard||paver.CrewCount<1)||roller.MachinePhase==MachinePhase.OnSite&&(!roller.CrewAboard||roller.CrewCount<1)){p.Cause=WorkCause.AwaitingCrewOnSite;return false;}
        p.Cause=WorkCause.PavingInProgress;
        if(paver.MachinePhase==MachinePhase.OnSite){AdvancePavingMachine(p,paver,dt);
            if(paver.WorkTarget==null&&!p.RoadPoints.Any(target=>!p.AppliedRoadPoints.Contains(target)&&PavingSiteRoute(p,paver.Position,target).Count>0))BeginLeavingSite(paver);}
        // One physical work lane: finish after the paver has cleared the site.
        // It avoids overlapping full-size equipment at bends and short streets.
        if(paver.MachinePhase is MachinePhase.Returning or MachinePhase.Available&&roller.MachinePhase==MachinePhase.OnSite){AdvancePavingMachine(p,roller,dt);
            if(roller.WorkTarget==null&&p.AppliedRoadPoints.All(p.RolledRoadPoints.Contains)){
                BeginLeavingSite(roller);
                if(p.RolledRoadPoints.Count<p.RoadPoints.Count){p.PaverId=null;p.RollerId=null;p.Cause=WorkCause.PavingAwaitingSiteLink;}
            }}
        p.Progress=(p.AppliedRoadPoints.Count+p.RolledRoadPoints.Count)/(2.0*p.RoadPoints.Count);
        if(p.Progress>0)p.Status=WorkStatus.InProgress;
        return p.RolledRoadPoints.Count>=p.RoadPoints.Count;
    }

    void AdvancePavingMachine(WorkProject p,Vehicle machine,double dt)
    {
        var done=machine.Machine==PavingMachine.Paver?p.AppliedRoadPoints:p.RolledRoadPoints;
        if(done.Count>=p.RoadPoints.Count)return;
        if(machine.WorkTarget==null){
            foreach(var target in p.RoadPoints.Where(point=>!done.Contains(point)&&(machine.Machine==PavingMachine.Paver||p.AppliedRoadPoints.Contains(point))).OrderBy(point=>point.Distance(machine.Position))){
                var route=PavingSiteRoute(p,machine.Position,target);if(route.Count==0)continue;
                machine.SitePath=route;machine.SitePathIndex=0;machine.WorkTarget=target;break;
            }
            if(machine.WorkTarget==null){machine.Cause="Aguarda trecho aplicado ou ligação real do canteiro";p.Cause=WorkCause.PavingAwaitingSiteLink;return;}
        }
        if(machine.SitePath.Skip(machine.SitePathIndex).Any(point=>!p.RoadPoints.Contains(point)&&State.Roads.All(r=>r.Position!=point))){machine.SitePath=[];machine.WorkTarget=null;p.Cause=WorkCause.WorkRouteInterrupted;return;}
        var speed=Math.Min(2,State.Tuning.RoadStep*p.RoadPoints.Count*2/p.WorkSeconds);
        var distanceBudget=speed*dt;var moved=0.0;
        // Pay only work actually performed, retaining fractional cents in the
        // project's existing labour accumulator, settled on completion/cancel.
        var hourly=State.Tuning.PavingMachineHourlyPrice+State.Tuning.ExternalCrewHourlyPrice;
        var maxCost=Money(p.CrewCostDue+(decimal)(dt/3600)*hourly);
        if(maxCost>p.BudgetRemaining){p.Cause=WorkCause.NoBudgetForPaving;machine.Cause=p.Cause;return;}
        if(machine.Fuel<Math.Min(distanceBudget,machine.SitePath.Skip(machine.SitePathIndex).Aggregate((At:machine.Position,Length:0.0),(sum,point)=>(point,sum.Length+sum.At.Distance(point))).Length)*State.Tuning.FuelPerMetre){p.Cause=WorkCause.PavingOutOfFuel;machine.Cause=p.Cause;return;}
        while(distanceBudget>.000001&&machine.SitePathIndex<machine.SitePath.Count){
            var target=machine.SitePath[machine.SitePathIndex];var delta=target-machine.Position;var distance=delta.Distance(default);var travel=Math.Min(distanceBudget,distance);
            if(distance>.001)machine.Heading=Math.Atan2(delta.X,delta.Z);
            machine.Position=machine.Position.MoveTowards(target,travel);distanceBudget-=travel;moved+=travel;
            if(travel>=distance-.000001)machine.SitePathIndex++;else break;
        }
        if(moved>0){machine.Fuel-=moved*State.Tuning.FuelPerMetre;machine.FuelBurnedLastStep+=moved*State.Tuning.FuelPerMetre;p.CrewCostDue+=(decimal)(moved/speed/3600)*hourly;
            if(p.CrewCostDue>=1){var due=Money(p.CrewCostDue);if(ProjectPay(p,due,"Máquinas de pavimentação","Aplicação/acabamento efetivos; operadores e máquinas")){p.MachineryPaid+=due;p.CrewCostDue=0;}}
        }
        machine.Cause=machine.Machine==PavingMachine.Paver?"Aplicando asfalto na obra "+p.Id:"Compactando asfalto na obra "+p.Id;
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
        machine.Route=route;machine.RouteIndex=0;machine.DockPosition=PavingDock(access,machine.Machine);machine.Status=VehicleStatus.Travelling;
    }

    void ReleasePavingMachines(WorkProject p)
    {
        foreach(var machine in State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.MachineProjectId==p.Id&&v.MachinePhase is not (MachinePhase.Returning or MachinePhase.LeavingSite))){
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
            if(machine.SitePath.Count==0){machine.SitePath=PavingSiteRoute(p,machine.Position,access.Position);machine.SitePathIndex=0;}
            var path=machine.SitePath;if(path.Count==0){machine.Cause="Máquina aguarda ligação do canteiro para retornar";continue;}
            var remaining=2*dt;
            while(remaining>.000001&&machine.SitePathIndex<path.Count){var target=path[machine.SitePathIndex];var length=machine.Position.Distance(target);var moved=Math.Min(length,remaining);if(machine.Fuel<moved*State.Tuning.FuelPerMetre){machine.Cause="Máquina sem combustível para retornar";break;}if(length>.001){var delta=target-machine.Position;machine.Heading=Math.Atan2(delta.X,delta.Z);}machine.Position=machine.Position.MoveTowards(target,moved);machine.Fuel-=moved*State.Tuning.FuelPerMetre;remaining-=moved;if(moved>=length-.001)machine.SitePathIndex++;else break;}
            if(machine.SitePathIndex>=path.Count){machine.MachinePhase=MachinePhase.Returning;machine.DockPosition=null;machine.SitePath=[];ReturnExternal(machine);}
        }
    }
}
