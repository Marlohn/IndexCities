using Godot;
using IndexCities.Simulation;
using System.IO;
namespace IndexCities.Game;

public partial class Main
{
    // Reproduces the CornerCity fixture of PedestrianCrossingTests with the real
    // 2 s quantum: the leaving roller must yield to the passing truck without
    // turning into it, then the street and the house finish.
    async void ConstructionCornerSmoke()
    {
        var directory=Path.GetFullPath(_reviewSaveDir??ProjectSettings.GlobalizePath("res://../../artifacts/verification/construction-corner"));
        Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Esquina exige janela renderizada");
            var missing=new[]{"vehicle.truck_flatbed.a","vehicle.asphalt_paver.a","vehicle.road_roller.a"}.Where(id=>!ResourceLoader.Exists("res://Assets/Construction/Scenes/"+id+".tscn")).ToArray();
            Require(missing.Length==0,"Modelos finais ausentes: "+string.Join(",",missing));
            _speed=0;_autosaveMinutes=0;_clockBudget=0;ClearTool();ClearSelection();
            CitySaves.VerificationDirectory=Path.Combine(directory,"cities");
            var captures=new List<string>();
            async System.Threading.Tasks.Task Shot(string name,Point at,float zoom=40){
                _focus=ToV(at);_zoom=zoom;UpdateCamera();_world.Sync(_sim.State);RefreshHud();
                await RecoveryCapture(Path.Combine(directory,name+".png"),GetViewport());
                File.WriteAllText(Path.Combine(directory,name+".json"),_sim.SaveToJson());captures.Add(name);
            }
            _sim=new CitySimulation(42);_cityId=CitySaves.NewId();
            Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura");
            var road=_sim.PlaceRoad(new(-192,0),new(-128,132),true);Require(road.Success,road.Message);
            var house=_sim.PlaceBuilding(BuildingKind.House,new(-190,146));Require(house.Success,house.Message);
            var work=_sim.State.Projects.Single(p=>p.Id==road.Id);
            _world.Reset(_sim.State);BuildCatalog();
            await Shot("corner-ordered",new(-170,70),110);
            var yielded=false;var merged=false;
            for(var step=0;step<3600&&!(work.Status==WorkStatus.Completed&&_sim.State.Buildings.Single(b=>b.Id==house.Id).Completed);step++){
                var before=_sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.MachinePhase==MachinePhase.LeavingSite).ToDictionary(v=>v.Id,v=>(v.Position,v.Heading));
                _sim.Advance(2);
                foreach(var machine in _sim.State.Vehicles.Where(v=>before.ContainsKey(v.Id)&&v.MachinePhase==MachinePhase.LeavingSite&&v.Position==before[v.Id].Position))
                    Require(machine.Heading==before[machine.Id].Heading,$"Máquina {machine.Id} parada não gira contra veículo vizinho");
                var roller=_sim.State.Vehicles.FirstOrDefault(v=>v.Id==work.RollerId);
                var truck=roller==null?null:_sim.State.Vehicles.Where(v=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Travelling).MinBy(v=>v.Position.Distance(roller.Position));
                if(!yielded&&roller is {MachinePhase:MachinePhase.LeavingSite}&&truck!=null&&truck.Position.Distance(roller.Position)<8){yielded=true;await Shot("corner-roller-yields",roller.Position);}
                if(yielded&&!merged&&roller is {MachinePhase:MachinePhase.LeavingSite}&&truck!=null&&Math.Abs(roller.Position.X+193.65)<.1&&truck.Position.Z<roller.Position.Z){merged=true;await Shot("corner-truck-passed",roller.Position);}
            }
            Require(yielded&&merged,"Rolo cedeu ao caminhão e seguiu atrás dele");
            Require(work.Status==WorkStatus.Completed,"Rua da esquina concluída por execução física");
            Require(_sim.State.Buildings.Single(b=>b.Id==house.Id).Completed,"Casa dependente concluída");
            Require(_sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).All(v=>v.Status==VehicleStatus.Outside),"Máquinas retornaram ao exterior");
            _sim.CheckInvariants();
            await Shot("corner-completed",new(-170,110),70);
            File.WriteAllText(Path.Combine(directory,"evidence.txt"),string.Join(System.Environment.NewLine,captures));
            GD.Print($"INDEXCITIES_CONSTRUCTION_CORNER_OK models=final captures={captures.Count} directory={directory} street=completed house=completed roller=yielded-without-turning");
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
}
