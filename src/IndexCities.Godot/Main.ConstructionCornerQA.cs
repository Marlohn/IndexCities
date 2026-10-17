using Godot;
using IndexCities.Simulation;
using System.IO;
namespace IndexCities.Game;

public partial class Main
{
    // Physical penetration of two simulated bodies, without safety margin.
    static double CornerQABodyPenetration(Vehicle a,Vehicle b)
    {
        static double HalfLength(Vehicle v)=>v.PassengerCar?2.1:v.Machine==PavingMachine.Paver?2.31:v.Machine==PavingMachine.Roller?1.99:3.6;
        static double HalfWidth(Vehicle v)=>v.PassengerCar?.9:v.Machine==PavingMachine.Paver?1.27:v.Machine==PavingMachine.Roller?1.16:1.45;
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        var af=new Point(Math.Sin(a.Heading),Math.Cos(a.Heading));var ar=new Point(af.Z,-af.X);
        var bf=new Point(Math.Sin(b.Heading),Math.Cos(b.Heading));var br=new Point(bf.Z,-bf.X);var delta=b.Position-a.Position;
        return new[]{af,ar,bf,br}.Min(axis=>HalfLength(a)*Math.Abs(Dot(af,axis))+HalfWidth(a)*Math.Abs(Dot(ar,axis))+HalfLength(b)*Math.Abs(Dot(bf,axis))+HalfWidth(b)*Math.Abs(Dot(br,axis))-Math.Abs(Dot(delta,axis)));
    }

    // The poses the machine still passes through: its turn toward the next
    // point, then 0.5 m samples along the rest of its site path.
    static IEnumerable<Vehicle> CornerQARemainingPath(Vehicle machine)
    {
        var at=machine.Position;
        for(var index=machine.SitePathIndex;index<machine.SitePath.Count;index++){
            var target=machine.SitePath[index];var length=at.Distance(target);if(length<=.001)continue;
            var heading=Math.Atan2(target.X-at.X,target.Z-at.Z);
            for(var along=0.0;;along=Math.Min(length,along+.5)){yield return new Vehicle{Machine=machine.Machine,Position=at.MoveTowards(target,along),Heading=heading};if(along>=length)break;}
            at=target;
        }
    }

    // Reproduces the CornerCity fixture of PedestrianCrossingTests with the real
    // 2 s quantum: the truck arriving behind the leaving roller waits clear of
    // the roller's remaining path, then follows it; no body turns into or
    // overlaps another at any step, and the street and the house finish.
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
            Vehicle? waiting=null;Point waitedAt=default;var followed=false;
            for(var step=0;step<3600&&!(work.Status==WorkStatus.Completed&&_sim.State.Buildings.Single(b=>b.Id==house.Id).Completed);step++){
                var before=_sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.MachinePhase==MachinePhase.LeavingSite).ToDictionary(v=>v.Id,v=>(v.Position,v.Heading));
                _sim.Advance(2);
                var t=_sim.State.ElapsedSeconds;
                foreach(var machine in _sim.State.Vehicles.Where(v=>before.ContainsKey(v.Id)&&v.MachinePhase==MachinePhase.LeavingSite&&v.Position==before[v.Id].Position))
                    Require(machine.Heading==before[machine.Id].Heading,$"t={t}: máquina {machine.Id} parada não gira contra veículo vizinho");
                // The exterior gateway is the shared staging origin of every
                // external vehicle; bodies are compared only inside the map.
                bool InMap(Vehicle v)=>v.Status!=VehicleStatus.Outside&&Math.Abs(v.Position.X)<_sim.State.Tuning.MapHalfSize&&Math.Abs(v.Position.Z)<_sim.State.Tuning.MapHalfSize;
                foreach(var machine in _sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&InMap(v)))
                    foreach(var other in _sim.State.Vehicles.Where(o=>o.Id!=machine.Id&&InMap(o)&&o.Position.Distance(machine.Position)<12))
                        Require(CornerQABodyPenetration(machine,other)<=0,$"t={t}: corpos de {machine.Id} e {other.Id} não se sobrepõem (nem por giro no lugar)");
                var roller=_sim.State.Vehicles.FirstOrDefault(v=>v.Id==work.RollerId);
                if(roller is not {MachinePhase:MachinePhase.LeavingSite})continue;
                // Any vehicle held in a queue next to the leaving roller stands
                // outside the roller's remaining path, never inside it.
                foreach(var held in _sim.State.Vehicles.Where(v=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Travelling&&v.Cause=="Fila na faixa"&&v.Position.Distance(roller.Position)<20))
                    Require(CornerQARemainingPath(roller).All(pose=>CornerQABodyPenetration(held,pose)<=0),$"t={t}: veículo {held.Id} espera fora do trajeto restante do rolo");
                var truck=_sim.State.Vehicles.Where(v=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Travelling).MinBy(v=>v.Position.Distance(roller.Position));
                if(waiting==null&&truck is {Cause:"Fila na faixa"}&&truck.Position.Distance(roller.Position)<20){waiting=truck;waitedAt=truck.Position;await Shot("corner-truck-yields",roller.Position);}
                if(waiting!=null&&!followed&&Math.Abs(roller.Position.X+193.65)<.1&&Math.Abs(waiting.Position.X+193.65)<.1&&waiting.Position.Z>roller.Position.Z&&waiting.Position.Distance(waitedAt)>4){followed=true;await Shot("corner-roller-passed",roller.Position);}
            }
            Require(waiting!=null&&followed,"Caminhão esperou fora do trajeto do rolo, que entrou primeiro na faixa, e depois seguiu atrás dele");
            Require(work.Status==WorkStatus.Completed,"Rua da esquina concluída por execução física");
            Require(_sim.State.Buildings.Single(b=>b.Id==house.Id).Completed,"Casa dependente concluída");
            Require(_sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).All(v=>v.Status==VehicleStatus.Outside),"Máquinas retornaram ao exterior");
            _sim.CheckInvariants();
            await Shot("corner-completed",new(-170,110),70);
            File.WriteAllText(Path.Combine(directory,"evidence.txt"),string.Join(System.Environment.NewLine,captures));
            GD.Print($"INDEXCITIES_CONSTRUCTION_CORNER_OK models=final captures={captures.Count} directory={directory} street=completed house=completed roller=entered-first-truck-followed overlap=none turn-in-place=none");
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
}
