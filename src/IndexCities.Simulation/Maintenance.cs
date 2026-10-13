namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    double MaintenanceInterval=>State.Tuning.PublicMaintenanceIntervalHours*3600;
    void RetireMaintenanceWithoutTargets()
    {
        // A disconnected target still needs maintenance. A demolished target no
        // longer exists; release only unpaid commitments using normal cancellation.
        foreach(var project in State.Projects.Where(p=>p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).ToList())RetireMaintenanceWithoutTargets(project);
    }
    bool RetireMaintenanceWithoutTargets(WorkProject project)
    {
        if(!project.IsMaintenance||!(project.MaintenanceRoadIds.Count>0?project.MaintenanceRoadIds.All(id=>!_roads.ContainsKey(id)):project.BuildingId!=null&&BuildingById(project.BuildingId)==null))return false;
        CancelWork(project);project.Cause="Manutenção encerrada: infraestrutura removida; pagamentos e carga preservados";
        Event("Manutenção sem infraestrutura existente encerrada; compromisso não pago liberado");return true;
    }
    static string MaintenanceCategory(WorkProject project)=>project.MaintenanceRoadIds.Count>0?"Manutenção / ruas":"Manutenção / instalações";
    int MaintenancePriority(WorkProject project)=>BuildingById(project.BuildingId)?.Kind switch{BuildingKind.WindPower or BuildingKind.WaterIntake or BuildingKind.WaterTreatment or BuildingKind.SewageTreatment=>0,BuildingKind.WorksDepot=>1,_=>project.MaintenanceRoadIds.Count>0?1:2};
    void UpdateMaintenanceCondition(double dt)
    {
        var graphChanged=false;
        foreach(var building in State.Buildings.Where(b=>b.Completed&&!b.Removed&&!b.Definition.Private)){if(building.MaintenanceDueAt<=0)building.MaintenanceDueAt=State.ElapsedSeconds+MaintenanceInterval;if(State.ElapsedSeconds>=building.MaintenanceDueAt)building.MaintenanceRatio=Math.Max(0,building.MaintenanceRatio-.03*dt/86400);}
        foreach(var road in State.Roads.Where(r=>!r.Highway)){if(road.MaintenanceDueAt<=0)road.MaintenanceDueAt=State.ElapsedSeconds+MaintenanceInterval;var before=road.MaintenanceRatio;if(State.ElapsedSeconds>=road.MaintenanceDueAt)road.MaintenanceRatio=Math.Max(0,road.MaintenanceRatio-.03*dt/86400);if(before>.05&&road.MaintenanceRatio<=.05)graphChanged=true;}
        if(graphChanged)RebuildRoadGraph();
    }
    void ScheduleMaintenance(bool essentialOnly=false)
    {
        var pending=State.Projects.Where(p=>p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress).ToList();
        foreach(var building in State.Buildings.Where(b=>b.Completed&&!b.Removed&&!b.Definition.Private&&State.ElapsedSeconds>=b.MaintenanceDueAt&&(!essentialOnly||b.Definition.Utility||b.Kind==BuildingKind.WorksDepot)).OrderBy(b=>b.Definition.Utility?0:b.Kind==BuildingKind.WorksDepot?1:2)){
            if(pending.Any(p=>p.BuildingId==building.Id))continue;
            var materials=building.Definition.Materials.Where(k=>k.Key is ResourceKind.Concrete or ResourceKind.Steel).ToDictionary(k=>k.Key,k=>Math.Max(.02,k.Value*.003));var labour=Math.Max(60,building.Definition.Width*building.Definition.Depth*.15);
            var project=CreateMaintenance(building.Position,materials,labour);if(project==null)continue;project.BuildingId=building.Id;pending.Add(project);
        }
        var reserved=pending.SelectMany(p=>p.MaintenanceRoadIds).ToHashSet();
        var due=State.Roads.Where(r=>!r.Highway&&State.ElapsedSeconds>=r.MaintenanceDueAt&&!reserved.Contains(r.Id)).OrderBy(r=>r.Position.Distance(State.Entry)).ToList();
        foreach(var area in due.GroupBy(r=>((int)Math.Floor(r.Position.X/64),(int)Math.Floor(r.Position.Z/64))))foreach(var roads in area.Chunk(16)){
            var length=roads.Length*State.Tuning.RoadStep;var materials=new Dictionary<ResourceKind,double>{{ResourceKind.Aggregate,length*.004},{ResourceKind.Asphalt,length*.006}};
            var project=CreateMaintenance(roads[0].Position,materials,Math.Max(60,roads.Length*20));if(project==null)continue;project.MaintenanceRoadIds=roads.Select(r=>r.Id).ToList();project.RoadPoints=roads.Select(r=>r.Position).ToList();
        }
    }
    WorkProject? CreateMaintenance(Point location,Dictionary<ResourceKind,double> materials,double labour)
    {
        var quote=QuoteMaterials(materials,location,labour);if(quote>Available)return null;
        var project=new WorkProject{Id=Id(),IsMaintenance=true,Position=location,Required=materials,WorkSeconds=labour,BudgetRemaining=quote,Cause="Manutenção aguardando materiais e equipe reais"};State.Projects.Add(project);return project;
    }
    void RelocateMaintenanceCrew(WorkProject project,RoadNode access)
    {
        if(!project.IsMaintenance)return;
        foreach(var citizen in project.LocalCrew.Select(id=>State.Citizens.First(c=>c.Id==id)).Where(c=>c.Alive&&!c.Moving&&ShiftActive(c)&&c.Position.Distance(access.Position)>=State.Tuning.RoadWidth)){var target=CrewPedestrianSite(access);var route=PedestrianPath(citizen.Position,target,access);if(route.Count==0)continue;citizen.Path=route;citizen.PathIndex=0;citizen.PathRoadRevision=State.RoadRevision;citizen.Purpose=JourneyPurpose.Construction;citizen.Activity="Indo à manutenção física";}
    }
    void ProgressRoadMaintenance(WorkProject project)
    {
        if(project.MaintenanceRoadIds.Count==0)return;var finished=(int)Math.Floor(project.Progress*project.MaintenanceRoadIds.Count);var changed=false;
        while(project.MaintainedRoadIds.Count<finished){var id=project.MaintenanceRoadIds[project.MaintainedRoadIds.Count];project.MaintainedRoadIds.Add(id);if(!_roads.TryGetValue(id,out var road))continue;changed|=road.MaintenanceRatio<=.05;road.MaintenanceRatio=1;road.MaintenanceDueAt=State.ElapsedSeconds+MaintenanceInterval;}
        if(changed)RebuildRoadGraph();
    }
    void CompleteMaintenance(WorkProject project)
    {
        ProgressRoadMaintenance(project);if(BuildingById(project.BuildingId) is {} building){building.MaintenanceRatio=1;building.MaintenanceDueAt=State.ElapsedSeconds+MaintenanceInterval;building.PaidMaintenance+=project.CostPaid;}
        project.Cause="Manutenção realizada com materiais e trabalho efetivos";Event(project.MaintenanceRoadIds.Count>0?"Manutenção viária concluída":"Manutenção de instalação municipal concluída");
    }
}
