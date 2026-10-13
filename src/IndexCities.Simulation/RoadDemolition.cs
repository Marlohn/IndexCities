namespace IndexCities.Simulation;

public sealed record RoadDemolitionQuote(bool Allowed,decimal Cost,List<int> IsolatedBuildingIds,int InterruptedWorks,int AffectedVehicleRoutes,string Message);

public sealed partial class CitySimulation
{
    public RoadDemolitionQuote QuoteRoadDemolition(int nodeId)
    {
        if(!State.CityHallPlaced||!_roads.TryGetValue(nodeId,out var node)||node.Highway)return new(false,0,[],0,0,"Selecione um trecho urbano; a rodovia externa é preservada na V1");
        var connected=new HashSet<int>();var queue=new Queue<int>();queue.Enqueue(EntryNode().Id);
        while(queue.Count>0){var id=queue.Dequeue();if(id==nodeId||!connected.Add(id))continue;foreach(var neighbour in _roads[id].Neighbours)if(neighbour!=nodeId&&_roads[neighbour].MaintenanceRatio>.05)queue.Enqueue(neighbour);}
        bool BuildingConnected(Building building){var access=State.Roads.Where(r=>r.Id!=nodeId&&!r.Highway&&r.MaintenanceRatio>.05&&r.Position.Distance(building.Door)<=State.Tuning.RoadWidth/2+3).MinBy(r=>r.Position.Distance(building.Door));return access!=null&&connected.Contains(access.Id);}
        var isolated=State.Buildings.Where(b=>!b.Removed&&HasExteriorAccess(b)&&!BuildingConnected(b)).Select(b=>b.Id).ToList();
        var works=State.Projects.Count(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&HasExteriorWorkAccess(p)&&(p.BuildingId is {} buildingId?isolated.Contains(buildingId):!State.Roads.Any(r=>r.Id!=nodeId&&connected.Contains(r.Id)&&p.RoadPoints.Any(point=>point.Distance(r.Position)<=State.Tuning.RoadStep+.05))));
        var routes=State.Vehicles.Count(v=>v.Status!=VehicleStatus.Outside&&v.Route.Skip(v.RouteIndex).Contains(nodeId));
        var cost=Money(Math.Max(0,State.Tuning.RoadDemolitionCostPerSquareMetre)*(decimal)(State.Tuning.RoadStep*State.Tuning.RoadWidth));
        return new(true,cost,isolated,works,routes,isolated.Count>0?"O corte poderá interromper moradores, trabalho, serviços e entregas nos locais indicados":"Nenhuma construção atualmente conectada perde acesso ao exterior neste corte");
    }
    public CommandResult DemolishRoad(int nodeId)
    {
        var quote=QuoteRoadDemolition(nodeId);if(!quote.Allowed)return new(false,quote.Message);if(quote.Cost>Available)return new(false,"Caixa livre insuficiente para a demolição");
        if(!Transfer("city","global",quote.Cost,"Demolição","Remoção física de trecho urbano; sem reembolso de obra ou materiais",nodeId))return new(false,"Não foi possível pagar a demolição");
        State.Roads.Remove(_roads[nodeId]);RebuildRoadGraph();SetDefaultJunctions();
        foreach(var vehicle in State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside)){if(vehicle.IntersectionClaim==nodeId)vehicle.IntersectionClaim=null;if(vehicle.Route.Skip(vehicle.RouteIndex).Contains(nodeId)){vehicle.Status=VehicleStatus.Waiting;vehicle.Cause="Via removida; aguarda rota física pela rede remanescente";}}
        foreach(var citizen in State.Citizens.Where(c=>c.CrossingNodeId==nodeId))citizen.CrossingNodeId=null;
        RetireMaintenanceWithoutTargets();
        Event("Rua removida; custo pago, sem restituição, "+quote.IsolatedBuildingIds.Count+" construções perderam conexão exterior");return new(true,State.LastMessage);
    }
}
