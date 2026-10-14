using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    void ShowCitizenInspection(Citizen citizen)
    {
        _inspector.Visible=true;
        var current=Card(_inspection,citizen.Name);
        InspectionText(current,()=>citizen.Activity,18);
        InspectionText(current,()=>citizen.Decision,14,true);
        InspectionButton(current,()=>"◎ Acompanhar",()=>{_focus=ToV(citizen.Position);UpdateCamera();});
        var history=Card(_inspection,"Últimos acontecimentos");
        for(var i=0;i<8;i++) {
            var index=i;
            InspectionText(history,()=>citizen.History.TakeLast(8).Reverse().ElementAtOrDefault(index) is {} entry?entry.At.ToString("dd/MM HH:mm")+" "+entry.Description:"",14,true);
        }
        var needs=Card(_inspection,"Bem-estar");
        InspectionText(needs,()=>"Saúde "+citizen.Health.ToString("N0")+" • satisfação "+citizen.Satisfaction.ToString("N0"),14);
        InspectionText(needs,()=>"Sem comer há "+citizen.HungerHours.ToString("N1")+" h",14);
        var resources=Card(_inspection,"Recursos");
        InspectionText(resources,()=>"Carteira "+Cash(_sim.Balance(citizen.Account)),16);
        InspectionText(resources,()=>"Salário acumulado: "+Cash(citizen.WagesAccrued),14);
        InspectionText(resources,()=>"Alimentos transportados: "+citizen.BagFood.ToString("N1"),14);
    }
}
