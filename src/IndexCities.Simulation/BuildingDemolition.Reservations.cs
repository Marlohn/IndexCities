namespace IndexCities.Simulation;

// SPEC construcao: a purchase paid but still at the farm survives demolition of its
// destination. The active shipment is the existing stock reservation; VehicleId=0
// means no carrier is assigned. No new money or persistence state is needed.
public sealed partial class CitySimulation
{
    bool PurchaseAwaitingDestination(Shipment s)=>!s.Complete&&!s.Cancelled&&!s.PickedUp&&s.ProjectId==null&&s.Purpose==ShipmentPurpose.Food&&PrivateCargo(s)&&s.VehicleId==0&&RemovedBuilding(s.DestinationBuildingId)!=null&&BuildingById(s.SourceBuildingId) is {Kind:BuildingKind.Farm};

    public string ReservedPurchasesAt(Building source)=>string.Join("\n",State.Shipments.Where(s=>s.SourceBuildingId==source.Id&&PurchaseAwaitingDestination(s)).Select(s=>"Compra paga reservada: Alimentos "+s.Cargo.GetValueOrDefault(ResourceKind.Food).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" de "+ReadableOwnerName(s.GoodsOwnerAccount)+" nesta fazenda. Aguarda outro destino apto da mesma empresa e transporte real, sem nova cobrança. Caminhão e motorista livres."));

    void RetryPurchasesAwaitingDestination()
    {
        foreach(var s in State.Shipments.Where(PurchaseAwaitingDestination).ToList()){
            var source=BuildingById(s.SourceBuildingId)!;
            var v=LocalDeliveryVehicle(source);var access=Access(source);
            if(v==null||access==null||s.Cargo.Values.Sum()>v.CargoCapacity+.001||v.Position.Distance(DockAt(access,source,false))>.1)continue;
            var driver=State.Citizens.FirstOrDefault(c=>c.Alive&&c.WorkplaceId==source.Id&&c.VehicleId==null&&c.BoardingVehicleId==null&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(source.Door)<3&&ShiftActive(c));
            if(driver==null||CargoAlternative(v,s,RemovedBuilding(s.DestinationBuildingId)!,access.Id) is not {} found)continue;
            if(s.Cargo.Any(k=>source.Quantity(k.Key)<k.Value)||!EnsureFuel(v,found.Access.Id))continue;
            s.DestinationBuildingId=found.Building.Id;s.Destination=found.Access.Position;s.VehicleId=v.Id;
            v.ShipmentId=s.Id;v.DriverId=driver.Id;
            driver.BoardingVehicleId=v.Id;driver.Purpose=JourneyPurpose.Boarding;driver.Path=[driver.Position,v.Position];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.Activity="Caminhando para embarcar no caminhão";
            v.Status=VehicleStatus.Unloading;v.LoadTimer=15;v.Cause="Retomando compra paga: carregando na fazenda com motorista real, sem nova cobrança";
            Event("Compra paga de "+ReadableOwnerName(s.GoodsOwnerAccount)+" retoma da fazenda a "+found.Building.Definition.Name+" #"+found.Building.Id+" da mesma empresa, sem nova cobrança");
        }
    }
}
