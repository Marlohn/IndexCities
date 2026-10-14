using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void NeighbourhoodSmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/road-reference/rendered-"+Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(directory);
        try{
            Require(DisplayServer.GetName()!="headless","Bairro exige Godot renderizado");
            _speed=0;_autosaveMinutes=0;
            CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"cities");
            _sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0,ExternalCompanyCandidates=0});
            _cityId=CitySaves.NewId();_world.Reset(_sim.State);Choose(BuildingKind.CityHall);BuildCatalog();
            await RecoveryWorldPoint(new(-100,-60));
            Require(_sim.State.CityHallPlaced,"Prefeitura encomendada pelo gesto normal");
            await RecoveryButton(RecoveryRoadButton());
            Require(_placementGridVisible,"Grade automática na ferramenta de rua");
            await RecoveryWorldPoint(new(-192,0));
            await RecoveryPreviewWorldPoint(new(-192,48));
            await RecoveryClick(_camera.UnprojectPosition(ToV(new(-192,48))));
            Require(_roadStart==new Point(-192,48)&&_roadEnd==null,"Extremidade inicia próximo trecho sem primeiro clique");
            await RecoveryMove(_camera.UnprojectPosition(ToV(new(-192,48))));
            Require(_roadEnd==null&&!_confirmation.Visible,"Cursor na extremidade recém-confirmada não acusa traçado inválido");
            var first=_sim.State.Projects.Last();
            Require(first.RoadPoints.Count>0&&first.Progress==0,"Encomenda não conclui obra");
            await RecoveryPreviewWorldPoint(new(-144,72));
            var before=_sim.SaveToJson();
            var shape=_sim.RoadShape(_roadStart!.Value,_roadEnd!.Value,_reverse).ToArray();
            await RecoveryKey(Key.Tab);
            Require(!shape.SequenceEqual(_sim.RoadShape(_roadStart!.Value,_roadEnd!.Value,_reverse)),"Tab muda a dobra da prévia");
            Require(before==_sim.SaveToJson(),"Trocar dobra não cria recursos ou obras");
            var quote=_sim.QuoteRoad(_roadStart.Value,_roadEnd.Value,_reverse);
            Require(quote.PhysicallyValid&&!quote.Accessible,"Ligação em obra é válida, sem acesso funcional antecipado");
            await RecoveryCapture(System.IO.Path.Combine(directory,"pending-L-preview.png"),GetViewport());
            await RecoveryClick(_camera.UnprojectPosition(ToV(new(-144,72))));
            Require(_roadStart==new Point(-144,72),"L confirmado mantém sequência");
            await RecoveryPreviewWorldPoint(new(-120,72));
            await RecoveryClick(_camera.UnprojectPosition(ToV(new(-120,72))));
            Require(_roadStart==new Point(-120,72),"Extensão usa um único clique no próximo fim");
            var count=_sim.State.Projects.Count;before=_sim.SaveToJson();
            await RecoveryKey(Key.G);
            Require(!_placementGridVisible&&_roadStart==new Point(-120,72),"Ocultar grade preserva sequência");
            Require(before==_sim.SaveToJson(),"Grade não altera simulação");
            await RecoveryKey(Key.G);await RecoveryKey(Key.F5);
            var start=_roadStart;var end=_roadEnd;LoadCity(_cityId);await RecoveryFrame();
            Require(_sim.SaveToJson()==before&&_roadStart==start&&_roadEnd==end&&_roadTool,"Save/load mantém sequência sem encomendar");
            Require(_sim.State.Projects.Count==count,"Retomar não cria obra extra");
            await RecoveryKey(Key.Escape);
            Require(!_roadTool&&!_placementGridVisible,"Esc encerra sequência e grade automática");
            await RecoveryKey(Key.G);
            Require(_placementGridVisible&&_building==null&&!_roadTool,"G funciona fora de construção");
            await RecoveryCapture(System.IO.Path.Combine(directory,"grid-outside-construction.png"),GetViewport());
            await RecoveryKey(Key.G);
            await RecoveryButton(RecoveryRoadButton());
            await RecoveryWorldPoint(new(-216,32));
            await RecoveryPreviewWorldPoint(new(-168,32));
            await RecoveryClick(_camera.UnprojectPosition(ToV(new(-168,32))));
            var points=_sim.State.Projects.SelectMany(p=>p.RoadPoints).ToArray();
            Require(points.Length==points.Distinct().Count(),"Cruzamento entre obras não duplica ponto ou material");
            await RecoveryKey(Key.Escape);
            await RecoveryButton(RecoveryCatalog(BuildingKind.House));
            await RecoveryPreviewWorldPoint(new(-168,86));
            quote=_sim.QuoteBuilding(BuildingKind.House,_cursor,_rotation);
            Require(quote.PhysicallyValid&&!quote.Accessible,"Casa encaixa na rua em obra sem acesso falso");
            await RecoveryCapture(System.IO.Path.Combine(directory,"house-planned-access.png"),GetViewport());
            await RecoveryClick(_camera.UnprojectPosition(ToV(new(-168,86))));
            Require(_sim.State.Buildings.Count==2,"Casa criada como projeto real");
            ClearTool();var saved=_sim.SaveToJson();var loaded=CitySimulation.FromJson(saved);
            _sim.Advance(14400);loaded.Advance(14400);
            Require(_sim.SaveToJson()==loaded.SaveToJson(),"Continuação preserva JSON completo");
            Require(_sim.State.Projects.Where(p=>p.RoadPoints.Count>0).All(p=>p.Status==WorkStatus.Completed),"Caminhões e equipes concluíram ruas reais");
            Require(points.All(point=>_sim.State.Roads.Count(node=>node.Position==point)==1),"Todo ponto encomendado foi construído exatamente uma vez");
            var crossing=_sim.State.Roads.Single(node=>node.Position==new Point(-192,32));
            Require(crossing.Neighbours.Count==4,"Cruzamento concluído possui quatro ligações reais");
            Require(_sim.MonetaryTotal==_sim.State.Tuning.TotalMoney,"Oferta monetária preservada");
            _sim.CheckInvariants();_world.Sync(_sim.State);_focus=new(-168,0,44);_zoom=125;UpdateCamera();
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"after-city.json"),_sim.SaveToJson());
            await RecoveryCapture(System.IO.Path.Combine(directory,"completed-neighbourhood.png"),GetViewport());
            GD.Print("INDEXCITIES_NEIGHBOURHOOD_OK saved=isolated shared-points=unique money="+_sim.MonetaryTotal+" directory="+directory);
            GetTree().Quit();
        }catch(Exception error){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"failure.txt"),error.ToString());GD.PushError(error.ToString());GetTree().Quit(1);}
    }
}
