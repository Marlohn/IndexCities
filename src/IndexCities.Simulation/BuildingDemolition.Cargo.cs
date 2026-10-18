namespace IndexCities.Simulation;

// Carga privada já embarcada para um prédio demolido (SPEC construcao, Intervenção
// municipal sobre propriedades privadas): mantém titular e quantidade e busca outro
// prédio compatível da mesma empresa titular, com capacidade, acesso e percurso reais.
// Sem destino viável, aguarda fisicamente em local seguro, sem reembolso, descarte,
// apropriação municipal ou entrega fictícia. Nenhum dinheiro se move aqui.
public sealed partial class CitySimulation
{
    Building? RemovedBuilding(int? id)=>id is null?null:State.Buildings.FirstOrDefault(b=>b.Id==id&&b.Removed);
    // Picked-up cargo whose building destination no longer exists. Construction/maintenance
    // material (ProjectId) and truck purchases follow their own rules.
    bool AwaitsRemovedDestination(Shipment s)=>s.PickedUp&&!s.Complete&&!s.Cancelled&&s.ProjectId==null&&s.Purpose!=ShipmentPurpose.Vehicle&&RemovedBuilding(s.DestinationBuildingId)!=null;
    bool PrivateCargo(Shipment s)=>s.GoodsOwnerAccount!="city";
    // Same titular company, same kind of building (the one that stores these goods),
    // physical room counting what is already on its way, road access and a real route.
    (Building Building,RoadNode Access,List<int> Route)? CargoAlternative(Vehicle v,Shipment s,Building removed,int fromNode)
    {
        var quantity=s.Cargo.Values.Sum();
        foreach(var b in State.Buildings.Where(b=>b.Id!=removed.Id&&b.Kind==removed.Kind&&b.Completed&&!b.Removed&&b.Demolition==null&&b.CompanyId!=null&&b.OwnerAccount==s.GoodsOwnerAccount).OrderBy(b=>b.Position.Distance(v.Position)).ThenBy(b=>b.Id)){
            var incoming=State.Shipments.Where(x=>x.Id!=s.Id&&!x.Complete&&!x.Cancelled&&x.DestinationBuildingId==b.Id).Sum(x=>x.Cargo.Values.Sum());
            if(b.Stock.Values.Sum()+incoming+quantity>b.Definition.Capacity+.001)continue;
            var access=Access(b);if(access==null)continue;
            var route=FindRoadRoute(fromNode,access.Id);if(route.Count==0&&fromNode!=access.Id)continue;
            return (b,access,route.Count==0?[access.Id]:route);
        }
        return null;
    }
    // Read-only view for the demolition preview: today's viable alternative, if any.
    string? CargoAlternativeName(Shipment s,Building target)
    {
        var v=State.Vehicles.FirstOrDefault(x=>x.Id==s.VehicleId);var start=v==null?null:NearestRoad(v.Position);
        return v==null||start==null?null:CargoAlternative(v,s,target,start.Id) is {} found?found.Building.Definition.Name+" #"+found.Building.Id:null;
    }
    // Called when a truck stops, arrives or must replan with a removed destination, and
    // at the economy cadence while it waits. Municipal material keeps its own rule.
    void HandleRemovedCargoDestination(Vehicle v,Shipment s)
    {
        var removed=RemovedBuilding(s.DestinationBuildingId)!;
        if(!PrivateCargo(s)){v.HoldingPosition=null;s.Cancelled=true;RedirectCancelled(v,s);return;}
        if(TryRedirectPrivateCargo(v,s,removed))return;
        if(v.External)HoldCargoSafely(v,s,removed);
        else HoldCargoInPlace(v,s,removed,"Destino "+removed.Definition.Name+" demolido: sem outro prédio apto de "+ReadableOwnerName(s.GoodsOwnerAccount)+"; carga e titular preservados no caminhão");
    }
    bool TryRedirectPrivateCargo(Vehicle v,Shipment s,Building removed)
    {
        // A truck still on its way changes course at its next road node, keeping the
        // lane segment it is on; otherwise it starts from the road next to it.
        var travelling=v.Status==VehicleStatus.Travelling&&v.RefuellingAt==null&&v.Detour.Count==0&&v.RouteIndex>0&&v.RouteIndex<v.Route.Count;
        var start=travelling?_roads.GetValueOrDefault(v.Route[v.RouteIndex]):NearestRoad(v.Position);
        if(start==null||!travelling&&start.Position.Distance(v.Position)>State.Tuning.RoadWidth/2+3)return false;
        if(CargoAlternative(v,s,removed,start.Id) is not {} found)return false;
        var ahead=travelling?v.Position.Distance(start.Position):0;
        if(v.External){
            var back=FindRoadRoute(found.Access.Id,EntryNode().Id);
            if(back.Count==0||v.Fuel<(ahead+RouteLength(found.Route)+RouteLength(back))*FuelRate(v)+4)return false;
            v.Route=travelling?[..v.Route.Take(v.RouteIndex),..found.Route]:found.Route;if(!travelling)v.RouteIndex=0;
        }else if(travelling){
            if(v.Fuel<(ahead+RouteLength(found.Route))*FuelRate(v)+4)return false;
            v.Route=[..v.Route.Take(v.RouteIndex),..found.Route];
        }else if(!EnsureFuel(v,found.Access.Id))return false;
        var owner=ReadableOwnerName(s.GoodsOwnerAccount);
        s.DestinationBuildingId=found.Building.Id;s.Destination=found.Access.Position;
        v.HoldingPosition=null;v.DestinationBuildingId=found.Building.Id;if(v.RefuellingAt==null)v.DockPosition=DockAt(found.Access,found.Building,false);
        v.Status=VehicleStatus.Travelling;v.Cause="Destino demolido: carga de "+owner+" redirecionada a "+found.Building.Definition.Name+" #"+found.Building.Id+" da mesma empresa";
        Event("Carga de "+owner+" para "+removed.Definition.Name+" demolido segue a "+found.Building.Definition.Name+" #"+found.Building.Id+" da mesma empresa, sem reembolso nem nova cobrança");
        return true;
    }
    // A truck already standing at its reserved roadside spot simply keeps waiting there.
    void HoldCargoSafely(Vehicle v,Shipment s,Building removed)
    {
        var owner=ReadableOwnerName(s.GoodsOwnerAccount);var waiting="Destino "+removed.Definition.Name+" demolido: carga de "+owner+" aguarda outro prédio apto da mesma empresa";
        if(v.HoldingPosition is {} held&&v.DockPosition is {} dock&&dock.Distance(held)<.01&&v.Position.Distance(held)<.1){v.Status=VehicleStatus.Waiting;v.Cause=waiting+", estacionada fora das faixas";return;}
        var start=NearestRoad(v.Position);
        if(start==null||start.Position.Distance(v.Position)>State.Tuning.RoadWidth/2+3){HoldCargoInPlace(v,s,removed,waiting+"; sem via próxima para chegar a um local seguro, posição mantida");return;}
        if(HoldingSlot(v) is not {} slot){HoldCargoInPlace(v,s,removed,waiting+"; não há espaço físico de espera livre, posição mantida");return;}
        var route=FindRoadRoute(start.Id,slot.Road.Id);
        if(route.Count==0&&start.Id!=slot.Road.Id){HoldCargoInPlace(v,s,removed,waiting+"; sem rota física até um local seguro, posição mantida");return;}
        if(route.Count==0)route=[slot.Road.Id];
        if(v.Fuel<(RouteLength(route)+v.Position.Distance(slot.Position))*FuelRate(v)){HoldCargoInPlace(v,s,removed,waiting+"; combustível insuficiente para chegar a um local seguro, posição mantida");return;}
        v.HoldingPosition=slot.Position;v.Route=route;v.RouteIndex=0;v.DockPosition=slot.Position;v.Status=VehicleStatus.Travelling;v.Cause=waiting+"; seguindo a um local seguro fora das faixas";
    }
    // No safe trip possible: the truck keeps its pose, cargo and titular, with the cause
    // shown. The held position marks it for the periodic retry instead of every step.
    void HoldCargoInPlace(Vehicle v,Shipment s,Building removed,string cause){v.HoldingPosition=v.Position;v.Route.Clear();v.RouteIndex=0;v.Status=VehicleStatus.Waiting;v.Cause=cause;}
    // Economy cadence: conditions that can open a destination (stock sold, new building,
    // road repaired) are rechecked periodically rather than on every step.
    void RetryCargoAwaitingDestination()
    {
        foreach(var v in State.Vehicles.Where(v=>v.HoldingPosition!=null&&v.Status==VehicleStatus.Waiting&&v.ShipmentId!=null).ToList())
            if(State.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId) is {} s&&AwaitsRemovedDestination(s))HandleRemovedCargoDestination(v,s);
    }
}
