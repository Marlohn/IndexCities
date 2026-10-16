using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    IEnumerable<string> ServiceFactors(Building building,double? treatmentPotential=null)
    {
        if(!building.Completed){yield return "Obra ainda não concluída";yield break;}
        if(!_sim.HasExteriorAccess(building))yield return "Sem ligação viária com o exterior";
        if(building.Kind==BuildingKind.WaterIntake&&!_sim.HasNaturalWaterSource(building))yield return "Fora da faixa de captação da fonte natural";
        var hired=_sim.State.Citizens.Count(c=>c.Alive&&c.WorkplaceId==building.Id);
        var scheduled=_sim.WorkersScheduled(building);var present=_sim.WorkersPresent(building);
        if(hired==0)yield return "Ninguém contratado ainda: a instalação precisa de funcionários no local durante o turno para operar";
        else if(scheduled==0)yield return "Há "+hired+" contratados, mas ninguém previsto na escala atual. A capacidade depende de funcionários no local durante o turno";
        else if(present<scheduled){
            if(present==0)yield return "Sem funcionários no local agora: 0 de "+scheduled+" previstos no turno presentes. A capacidade depende da chegada dos contratados ao posto e das demais condições do serviço";
            string Situation(Citizen citizen){
                if(citizen.VehicleId is {} id){var vehicle=_sim.State.Vehicles.FirstOrDefault(v=>v.Id==id);return "Em veículo #"+id+(vehicle==null?"":" · "+vehicle.Cause);}
                if(citizen.ConstructionProjectId is {} project)return "Designado à obra #"+project;
                return (citizen.Moving?"Em deslocamento a pé":"Fora do posto")+" · "+citizen.Activity;
            }
            foreach(var group in _sim.ScheduledWorkers(building).Where(c=>!_sim.WorkerPresentAt(c,building)).GroupBy(Situation))yield return group.Count()+" sem presença: "+group.Key;
        }
        var required=(int)Math.Ceiling(CitySimulation.ServiceStaffReference(building));
        if(hired>0&&present<required)yield return "Equipe: "+present+(present==1?" presente":" presentes")+"; capacidade plena exige ao menos "+required+" presentes";
        if(building.MaintenanceRatio<.999)yield return "Manutenção: "+building.MaintenanceRatio.ToString("P0",Pt)+" da condição plena";
        if(building.Kind!=BuildingKind.WindPower&&building.ElectricityDemand>0&&building.ElectricityRatio<.999)yield return "Energia: "+building.ElectricityRatio.ToString("P0",Pt)+" da demanda atendida";
        if(building.Kind==BuildingKind.WaterTreatment){
            var potential=treatmentPotential??_sim.CurrentWaterTreatmentPotential;
            if(_sim.State.RawWaterCapacity<=0)yield return "Captação de água bruta: zero no último cálculo";
            else if(_sim.State.RawWaterCapacity<potential)yield return "Captação do último cálculo ("+_sim.State.RawWaterCapacity.ToString("N0",Pt)+") abaixo do potencial atual do conjunto de tratamentos ("+potential.ToString("N0",Pt)+")";
            if(_sim.State.LakePollution>0)yield return "Poluição reduz o potencial de tratamento a "+_sim.WaterPollutionTreatmentFactor.ToString("P0",Pt)+" após equipe, manutenção e energia";
        }
    }

    string WaterServiceDiagnosis(double potential)
    {
        if(_sim.State.WaterDemand<=_sim.State.PotableCapacity+.5)return "Água: capacidade calculada atende à demanda atual";
        var raw=_sim.State.RawWaterCapacity;
        if(potential<=0)return "Água em déficit no último cálculo; tratamento sem potencial nas condições atuais. Confira as estações abaixo";
        if(raw<potential)return "Água em déficit: captação do último cálculo abaixo do potencial atual de tratamento";
        if(potential<_sim.State.WaterDemand)return "Água em déficit: potencial atual de tratamento abaixo da demanda do último cálculo";
        return "Água em déficit no último cálculo; confira as condições atuais das estações abaixo";
    }

    string ServiceStaff(Building building)
    {
        var hired=_sim.State.Citizens.Count(c=>c.Alive&&c.WorkplaceId==building.Id);
        var present=_sim.WorkersPresent(building);var scheduled=_sim.WorkersScheduled(building);
        return present+(present==1?" presente":" presentes")+" · "+scheduled+(scheduled==1?" previsto agora":" previstos agora")+" · "+hired+" contratados / "+building.Definition.Jobs+" vagas";
    }

    void ServiceInspection(Node parent,Building building)
    {
        var group=Card(parent,"Condições do serviço");
        InspectionText(group,()=>"Capacidade instalada: "+(building.Completed?building.Definition.Capacity:0).ToString("N0",Pt),14);
        InspectionText(group,()=>ServiceStaff(building),14);
        InspectionText(group,()=>string.Join("\n",ServiceFactors(building)),14,hideEmpty:true);
        if(building.Kind==BuildingKind.WaterTreatment)InspectionText(group,()=>"Potencial nas condições atuais: "+_sim.WaterTreatmentPotential(building).ToString("N0",Pt)+" antes do limite da captação",14);
        if(building.Kind==BuildingKind.WindPower)InspectionText(group,()=>"Vento atual: "+_sim.State.WindFactor.ToString("P0",Pt)+" da referência",14);
        InspectionTooltip(group,()=>"Condições atuais; capacidade operacional é o último cálculo da simulação. Contratação e escala não garantem presença nem produção.");
    }
}
