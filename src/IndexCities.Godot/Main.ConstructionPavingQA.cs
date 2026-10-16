using Godot;
using IndexCities.Simulation;
using System.IO;
namespace IndexCities.Game;

public partial class Main
{
    async void ConstructionPavingSmoke()
    {
        var directory=Path.GetFullPath(_reviewSaveDir??ProjectSettings.GlobalizePath("res://../../artifacts/verification/construction-paving"));
        Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Pavimentação exige janela renderizada");
            var missing=new[]{"vehicle.truck_dump.a","vehicle.truck_mixer.a","vehicle.truck_flatbed.a","vehicle.asphalt_paver.a","vehicle.road_roller.a"}.Where(id=>!ResourceLoader.Exists("res://Assets/Construction/Scenes/"+id+".tscn")).ToArray();
            var fallback=OS.GetCmdlineUserArgs().Contains("--construction-paving-fallback");
            Require(missing.Length==0||fallback,"Modelos finais ausentes: "+string.Join(",",missing));
            _speed=0;_autosaveMinutes=0;_clockBudget=0;ClearTool();ClearSelection();
            CitySaves.VerificationDirectory=Path.Combine(directory,"cities");
            var captures=new List<string>();
            async System.Threading.Tasks.Task Shot(string name,Point at,float zoom=65){
                _focus=ToV(at);_zoom=zoom;UpdateCamera();_world.Sync(_sim.State);RefreshHud();
                await RecoveryCapture(Path.Combine(directory,name+".png"),GetViewport());
                File.WriteAllText(Path.Combine(directory,name+".json"),_sim.SaveToJson());captures.Add(name);
            }
            foreach(var shape in new[]{"straight","L"}){
                _sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});_cityId=CitySaves.NewId();
                Require(_sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success,"Prefeitura real para materiais distintos");
                var result=_sim.PlaceRoad(new(-192,0),shape=="straight"?new(-120,0):new(-144,-40));
                Require(result.Success,result.Message);var work=_sim.State.Projects.Single(p=>p.Id==result.Id);
                _world.Reset(_sim.State);BuildCatalog();
                await Shot(shape+"-ordered",work.Position,100);
                var seenMaterials=new HashSet<ResourceKind>();var emptyBodies=new HashSet<TruckBody>();var applied=false;var rolled=false;var saved=false;var stopped=false;
                for(var step=0;step<4000&&(_sim.State.Projects.Any(p=>p.Status!=WorkStatus.Completed)||seenMaterials.Count<5||emptyBodies.Count<3);step++){
                    _sim.Advance(2);
                    foreach(var truck in _sim.State.Vehicles.Where(v=>v.Machine==PavingMachine.None&&v.Body!=TruckBody.Cargo&&v.Status==VehicleStatus.Travelling&&v.ShipmentId==null&&v.Cargo.Count==0).ToArray())
                        if(emptyBodies.Add(truck.Body))await Shot(shape+"-empty-return-"+truck.Body,truck.Position,38);
                    foreach(var truck in _sim.State.Vehicles.Where(v=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Unloading&&v.LoadTimer<=9&&v.Cargo.Count>0).ToArray()){
                        var shipment=_sim.State.Shipments.FirstOrDefault(s=>s.Id==truck.ShipmentId);
                        if(shipment is not {PickedUp:true,Complete:false})continue;
                        var kind=truck.Cargo.First().Key;if(!seenMaterials.Add(kind))continue;
                        if(kind==ResourceKind.Asphalt){var receiver=_sim.State.Vehicles.Single(v=>v.Id==shipment.ReceivingMachineId);Require(receiver is {MachinePhase:MachinePhase.OnSite,FeedingReady:true,CrewAboard:true}&&receiver.Position.Distance(truck.Position)>6.5&&receiver.Position.Distance(truck.Position)<6.7&&Math.Cos(receiver.Heading-truck.Heading)>.99,"Asfalto descarrega no funil da pavimentadora efetivamente presente");}
                        await Shot(shape+"-unload-"+kind,truck.Position,38);
                        Require(_world.RenderedVehicle(truck.Id)==(truck.Body switch{TruckBody.Dump=>"vehicle.truck_dump.a",TruckBody.Mixer=>"vehicle.truck_mixer.a",TruckBody.Flatbed=>"vehicle.truck_flatbed.a",_=>"vehicle.truck_cargo.a"}),"Modelo corresponde à carroceria persistente");
                    }
                    if(!applied&&work.AppliedRoadPoints.Count>=Math.Max(3,work.RoadPoints.Count/2)){applied=true;var machine=_sim.State.Vehicles.Single(v=>v.Id==work.PaverId);await Shot(shape+"-applying",machine.Position,42);}
                    if(applied&&!stopped&&work.AppliedRoadPoints.Count>=4&&work.AppliedRoadPoints.Count<work.RoadPoints.Count){
                        stopped=true;var checkpoint=_sim.SaveToJson();var original=_sim;
                        // Explicit resource-shortage branch, kept as a separate
                        // fixture. Continue the unchanged paid city afterwards.
                        _sim=CitySimulation.FromJson(checkpoint);var stoppedWork=_sim.State.Projects.Single(p=>p.Id==work.Id);
                        var machine=_sim.State.Vehicles.Single(v=>v.Id==stoppedWork.PaverId);machine.Fuel=0;var progress=stoppedWork.Progress;var paid=stoppedWork.CostPaid;
                        _sim.Advance(12);Require(stoppedWork.Progress==progress&&stoppedWork.CostPaid==paid&&stoppedWork.Cause==WorkCause.PavingOutOfFuel,"Parada causal sem combustível não cobra nem avança");
                        await Shot(shape+"-stopped-no-fuel",machine.Position,42);_sim=original;_world.Reset(_sim.State);
                    }
                    if(!rolled&&work.RolledRoadPoints.Count>=2&&work.RolledRoadPoints.Count<work.RoadPoints.Count){rolled=true;var machine=_sim.State.Vehicles.Single(v=>v.Id==work.RollerId);await Shot(shape+"-rolling",machine.Position,42);
                        var node=_sim.State.Roads.Single(r=>r.Position==work.RolledRoadPoints.Last());Require(node.ConstructionOnly&&_sim.FindRoadRoute(_sim.State.Roads.First().Id,node.Id).Count==0,"Tráfego normal não recebe acesso intermediário");}
                    if(!saved&&work.AppliedRoadPoints.Count>=3&&work.Progress<1){saved=true;var json=_sim.SaveToJson();var clone=CitySimulation.FromJson(json);var active=_sim;
                        _sim.Advance(20);clone.Advance(20);Require(_sim.SaveToJson()==clone.SaveToJson(),"Save/load no canteiro preserva dinheiro, material, máquinas e determinismo");
                        File.WriteAllText(Path.Combine(directory,shape+"-mid-work.json"),json);Require(ReferenceEquals(active,_sim),"Continuação usa a cidade original");}
                }
                Require(applied&&rolled&&saved&&stopped,"Aplicação, acabamento, parada e persistência observados");
                Require(work.Status==WorkStatus.Completed&&_sim.State.Projects.All(p=>p.Status==WorkStatus.Completed),"Obras e Prefeitura concluíram por logística real");
                Require(new[]{ResourceKind.Aggregate,ResourceKind.Asphalt,ResourceKind.Concrete,ResourceKind.Steel,ResourceKind.Wood}.All(seenMaterials.Contains),"Todos os materiais foram observados em descarga real");
                Require(emptyBodies.Count==3,"Basculante, betoneira e carroceria aberta voltaram vazios com o mesmo modelo");
                _sim.Advance(600);Require(_sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).All(v=>v.Status==VehicleStatus.Outside),"As mesmas máquinas retornaram ao exterior");
                Require(work.MachineryPaid>0&&_sim.MonetaryTotal==_sim.State.Tuning.TotalMoney,"Custo de máquina real e conservação monetária");
                await Shot(shape+"-completed",new(-156,-12),110);
            }
            _sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});var cancelResult=_sim.PlaceRoad(new(-192,0),new(-192,48));var cancelled=_sim.State.Projects.Single(p=>p.Id==cancelResult.Id);
            for(var i=0;i<2000&&cancelled.AppliedRoadPoints.Count<3;i++)_sim.Advance(2);
            Require(cancelled.AppliedRoadPoints.Count>=3,"Cancelamento exercita máquinas já no canteiro");
            var ids=_sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).Select(v=>v.Id).ToArray();var materials=cancelled.Delivered.ToDictionary(k=>k.Key,k=>k.Value);
            Require(_sim.CancelProject(cancelled.Id).Success,"Cancelamento real");var cost=cancelled.CostPaid;var roundTrip=CitySimulation.FromJson(_sim.SaveToJson());
            _sim.Advance(3600);roundTrip.Advance(3600);Require(_sim.SaveToJson()==roundTrip.SaveToJson(),"Cancelamento e retorno persistem sem duplicação");
            Require(cancelled.CostPaid==cost&&materials.All(k=>cancelled.Delivered[k.Key]==k.Value),"Cancelar não cobra novamente nem devolve material consumido");
            Require(ids.All(id=>_sim.State.Vehicles.Single(v=>v.Id==id).Status==VehicleStatus.Outside),"Máquinas canceladas retornam fisicamente");
            _world.Reset(_sim.State);await Shot("cancelled-returned",cancelled.Position,95);_sim.CheckInvariants();
            File.WriteAllText(Path.Combine(directory,"evidence.txt"),string.Join(System.Environment.NewLine,captures));
            GD.Print($"INDEXCITIES_CONSTRUCTION_PAVING_OK models={(missing.Length==0?"final":"fallback-NOT-FINAL")} captures={captures.Count} directory={directory} straight=L=physical money=conserved save-load=deterministic cancel=physical-return");
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
    }
}
