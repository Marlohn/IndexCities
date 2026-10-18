namespace IndexCities.Simulation;

// Execução da ordem Demolir de prédio concluído (SPEC construcao: Demolição e
// Intervenção municipal sobre propriedades privadas). Custos e indenização são
// pagos uma única vez na confirmação; residência ocupada aguarda a desocupação
// assistida curta e a retirada física é instantânea quando elegível.
public sealed partial class CitySimulation
{
    public const string StaleDemolitionPreview="Os dados da demolição mudaram enquanto a cidade avançava; confira a prévia atualizada antes de confirmar";
    string VacatingDaysText=>State.Tuning.AssistedVacatingDays.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" dia(s)";
    public CommandResult DemolishBuilding(int buildingId,BuildingDemolitionQuote? reviewed=null)
    {
        var quote=QuoteBuildingDemolition(buildingId);
        if(!quote.Allowed)return new(false,quote.Message);
        if(reviewed!=null&&!SameDemolitionImpact(reviewed,quote))return new(false,StaleDemolitionPreview);
        var building=BuildingById(buildingId)!;
        // The quote validated the owner's wallet and the free cash for both payments,
        // so either both transfers happen or the order stops before any mutation.
        if(quote.Compensation>0&&!Transfer("city",quote.OwnerAccount!,quote.Compensation,"Indenização","Intervenção municipal: 100% do valor de mercado de "+building.Definition.Name+" ao titular, pago na confirmação",building.Id))return new(false,"Indenização não transferida; nada foi cobrado nem removido");
        if(!Transfer("city","global",quote.RemovalCost,"Demolição","Remoção física de "+building.Definition.Name+"; sem reembolso de obra ou materiais",building.Id))throw new InvalidOperationException("Demolição validada sem Caixa para a remoção");
        building.Demolition=new(){OrderedAt=State.Now,DeadlineSeconds=State.ElapsedSeconds+Math.Max(0,State.Tuning.AssistedVacatingDays)*86400,RemovalCostPaid=quote.RemovalCost,CompensationPaid=quote.Compensation,Beneficiary=quote.Compensation>0?quote.OwnerAccount:null,BeneficiaryName=quote.Compensation>0?quote.OwnerName:"",FamiliesAtOrder=quote.FamilyIds.Count};
        if(quote.WaitsForResidents){
            Event("Demolição de "+building.Definition.Name+" confirmada"+(quote.Compensation>0?"; indenização "+Cash(quote.Compensation)+" paga a "+quote.OwnerName:"")+"; "+quote.FamilyIds.Count+" família(s) em desocupação assistida de até "+VacatingDaysText);
            foreach(var c in State.Citizens.Where(c=>quote.ResidentIds.Contains(c.Id)))Personal(c,"Ordem municipal de demolição da moradia: procurando outra casa existente por até "+VacatingDaysText);
            SeekHomesForDisplaced(building);
        }
        ProgressBuildingDemolition(building);
        return new(true,State.LastMessage,building.Id);
    }
    // Revalidation before charging: money, beneficiary and affected people/stock
    // must match what the player reviewed, otherwise the preview is shown again.
    // Stock tolerates the small continuous drift of production and sales.
    static bool SameDemolitionImpact(BuildingDemolitionQuote a,BuildingDemolitionQuote b)=>a.BuildingId==b.BuildingId&&a.RemovalCost==b.RemovalCost&&a.Compensation==b.Compensation&&a.TotalCost==b.TotalCost&&a.OwnerAccount==b.OwnerAccount&&a.WaitsForResidents==b.WaitsForResidents
        &&a.ResidentIds.Order().SequenceEqual(b.ResidentIds.Order())&&a.WorkerIds.Order().SequenceEqual(b.WorkerIds.Order())
        &&a.Stock.Count==b.Stock.Count&&a.Stock.All(entry=>b.Stock.TryGetValue(entry.Key,out var other)&&Math.Abs(entry.Value-other)<=Math.Max(1,entry.Value*.05))
        &&SameShipments(a.Shipments,b.Shipments);
    // A shipment that is loaded, picked up or redirected since the preview is a material change.
    static bool SameShipments(List<ShipmentImpact> a,List<ShipmentImpact> b)=>a.Count==b.Count&&a.Zip(b).All(pair=>pair.First.ShipmentId==pair.Second.ShipmentId&&pair.First.Kind==pair.Second.Kind&&pair.First.GoodsOwnerAccount==pair.Second.GoodsOwnerAccount&&Math.Abs(pair.First.Quantity-pair.Second.Quantity)<=.000001);
    // Families still linked to the building by HomeId. Only those with a living member
    // still in the city must vacate; a link left by a family already gone is stale.
    List<Family> LinkedFamilies(Building building)=>State.Families.Where(f=>f.HomeId==building.Id).ToList();
    List<Citizen> ResidentMembers(Family family)=>State.Citizens.Where(c=>family.Members.Contains(c.Id)&&c.Alive&&c.Resident).ToList();
    public List<Family> VacatingFamilies(Building building)=>LinkedFamilies(building).Where(f=>ResidentMembers(f).Count>0).ToList();
    // SPEC construcao: displaced families get the first chance at a vacant house in
    // each market review, before new migrants (ReviewMarkets runs right after).
    // Only a house the family can really pay is bought; nothing stays reserved.
    void SeekHomesForAllDisplaced()
    {
        foreach(var building in State.Buildings.Where(b=>b.Demolition!=null&&!b.Removed).ToList())SeekHomesForDisplaced(building);
    }
    void ProgressBuildingDemolitions()
    {
        foreach(var building in State.Buildings.Where(b=>b.Demolition!=null&&!b.Removed).ToList())ProgressBuildingDemolition(building);
    }
    void ProgressBuildingDemolition(Building building)
    {
        var order=building.Demolition!;var families=VacatingFamilies(building);
        if(families.Count>0&&State.ElapsedSeconds<order.DeadlineSeconds){
            var hours=(order.DeadlineSeconds-State.ElapsedSeconds)/3600;
            building.Cause="Demolição confirmada"+(order.CompensationPaid>0?", indenização paga":"")+": "+families.Count+" família(s) procurando outra casa; prazo "+hours.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+" h";return;
        }
        foreach(var family in LinkedFamilies(building)){
            // Only residents still in the city lose a home; a stale link is cleared silently,
            // with no money, person or physical trip touched.
            foreach(var c in ResidentMembers(family))Personal(c,"Perdeu a moradia: o prazo da desocupação assistida terminou sem outra casa compatível");
            family.HomeId=null;
        }
        RemoveDemolishedBuilding(building,families.Count);
    }
    // Assisted vacating uses the existing purchase of a vacant house for sale at
    // market value with the family's own money; no free home or new balance.
    void SeekHomesForDisplaced(Building building)
    {
        foreach(var family in VacatingFamilies(building).Where(f=>!f.Emigrating)){
            var members=family.Members.Select(id=>State.Citizens.First(c=>c.Id==id)).Where(c=>c.Alive&&c.Resident).ToList();if(members.Count==0)continue;
            var funds=members.Sum(c=>Balance(c.Account));
            var home=State.Buildings.Where(h=>h.Kind==BuildingKind.House&&h.Id!=building.Id&&h.Completed&&!h.Removed&&h.Demolition==null&&h.OwnerAccount==null&&h.FamilyId==null&&HasExteriorAccess(h))
                .Select(h=>(House:h,Price:MarketValue(h))).Where(x=>funds>=x.Price+150+AnnualPropertyTax(x.House)).OrderBy(x=>x.Price).ThenBy(x=>x.House.Position.Distance(building.Position)).Select(x=>x.House).FirstOrDefault();
            if(home==null){foreach(var c in members)c.Decision="Procurando outra casa durante a desocupação assistida: nenhuma casa à venda compatível com o dinheiro da família";continue;}
            var price=MarketValue(home);var buyer=members.OrderByDescending(c=>Balance(c.Account)).First();
            foreach(var helper in members.Where(c=>c.Id!=buyer.Id)){var shortage=price-Balance(buyer.Account);if(shortage<=0)break;Transfer(helper.Account,buyer.Account,Math.Min(shortage,Balance(helper.Account)),"Cooperação familiar","Contribuição para compra com titular único",home.Id);}
            if(!Transfer(buyer.Account,"global",price,"Aquisição residencial","Compra de casa existente pelo valor de mercado durante desocupação assistida",home.Id))continue;
            home.OwnerAccount=buyer.Account;home.FamilyId=family.Id;family.HomeId=home.Id;if(building.FamilyId==family.Id)building.FamilyId=null;building.Demolition!.FamiliesRelocated++;
            var drove=false;
            foreach(var c in members){Personal(c,"Mudou-se: família comprou "+home.Definition.Name+" com recursos próprios durante a desocupação assistida");
                if(c.Moving||c.VehicleId!=null||c.BoardingVehicleId!=null||c.Position.Distance(building.Door)>3)continue;
                if(!drove&&TryDrive(c,home,JourneyPurpose.Home)){drove=true;continue;}
                StartJourney(c,home,JourneyPurpose.Home);}
            Event("Família desocupou a residência a demolir: comprou outra casa existente com recursos próprios");
        }
    }
    void RemoveDemolishedBuilding(Building building,int displacedFamilies)
    {
        var name=building.Definition.Name;var owner=building.OwnerAccount;var lost=building.Stock.Where(entry=>entry.Value>.0001).ToDictionary(entry=>entry.Key,entry=>entry.Value);
        // Stock still in the building but already sold to another titular is named apart.
        var reservedForOthers=State.Shipments.Where(s=>!s.Complete&&!s.PickedUp&&s.SourceBuildingId==building.Id&&s.GoodsOwnerAccount!=owner).GroupBy(s=>s.GoodsOwnerAccount).Select(g=>ReadableOwnerName(g.Key)+" "+g.Sum(s=>s.Cargo.Values.Sum()).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)).ToList();
        var workers=State.Citizens.Where(c=>c.WorkplaceId==building.Id).ToList();
        foreach(var c in workers){SettleWages(c,building);c.WorkplaceId=null;Personal(c,"Vínculo encerrado: "+name+" demolido por ordem municipal; salário devido pago ou registrado como dívida");}
        ReleaseShipmentsOf(building);
        // Vehicles heading to refuel at a removed station replan on the remaining network.
        foreach(var v in State.Vehicles.Where(v=>v.RefuellingAt==building.Id)){v.RefuellingAt=null;v.SavedRoute=[];v.Route.Clear();v.RouteIndex=0;if(v.Status==VehicleStatus.Travelling)v.Status=VehicleStatus.Waiting;v.Cause="Posto demolido antes do abastecimento; replanejando";}
        if(building.Kind==BuildingKind.WorksDepot)foreach(var project in State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress))project.LocalReserved.Clear();
        building.Stock.Clear();building.OperationalCapacity=0;building.FamilyId=null;building.OperationClosed=true;
        if(CompanyById(building.CompanyId) is {} company)company.Buildings.Remove(building.Id);
        building.CompanyId=null;building.OwnerAccount=null;building.Removed=true;building.Cause="Demolido por ordem municipal";
        RetireMaintenanceWithoutTargets();
        // Private cargo already on a truck to this building keeps titular and quantity: a
        // truck on its way changes course now when its company has another apt building;
        // the others decide when they stop or arrive (HandleRemovedCargoDestination).
        var onboard=State.Shipments.Where(s=>s.DestinationBuildingId==building.Id&&AwaitsRemovedDestination(s)).ToList();
        var redirected=onboard.Count(s=>PrivateCargo(s)&&State.Vehicles.FirstOrDefault(v=>v.Id==s.VehicleId&&v.ShipmentId==s.Id) is {Status:VehicleStatus.Travelling} v&&TryRedirectPrivateCargo(v,s,building));
        var parts=new List<string>{"Demolição concluída: "+name+" #"+building.Id+" retirado"};var order=building.Demolition;
        if(order is {CompensationPaid:>0})parts.Add("indenização "+Cash(order.CompensationPaid)+" paga na confirmação a "+order.BeneficiaryName);
        if(order is {FamiliesRelocated:>0})parts.Add(order.FamiliesRelocated+" família(s) mudaram-se comprando outra casa");
        if(displacedFamilies>0)parts.Add(displacedFamilies+" família(s) ficaram sem moradia ao fim do prazo");
        if(workers.Count>0)parts.Add(workers.Count+" emprego(s) encerrado(s)");
        if(lost.Count>0)parts.Add("estoque perdido ("+string.Join(", ",lost.Select(entry=>entry.Key+" "+entry.Value.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)))+") de "+(owner==null?"sem titular":ReadableOwnerName(owner))+(reservedForOthers.Count>0?", incluindo reservas já vendidas a "+string.Join(", ",reservedForOthers):""));
        if(onboard.Count>0)parts.Add(onboard.Count+" carga(s) já embarcada(s) para o prédio mantêm titular e quantidade"+(redirected>0?" ("+redirected+" redirecionada(s) a outro prédio da mesma empresa)":"")+(onboard.Count>redirected?"; sem destino apto agora, buscam outro prédio da mesma empresa ou espera segura":""));
        Event(string.Join("; ",parts)+"; sem reembolso de obra ou materiais");
    }
    // Reservations still inside the removed structure are lost with its stock;
    // a local order not yet loaded for a removed destination stays at its origin.
    // Either way the trucks sent to load are released without cargo.
    void ReleaseShipmentsOf(Building building)
    {
        foreach(var s in State.Shipments.Where(s=>!s.Complete&&!s.PickedUp&&(s.SourceBuildingId==building.Id||s.ProjectId==null&&s.DestinationBuildingId==building.Id)).ToList()){
            // Only the destination is removed: sold goods still physically at an intact
            // farm stay reserved to the buyer. Detach the empty carrier, not the purchase.
            var preserve=s.SourceBuildingId!=building.Id&&s.ProjectId==null&&s.Purpose==ShipmentPurpose.Food&&PrivateCargo(s)&&BuildingById(s.SourceBuildingId) is {Kind:BuildingKind.Farm};
            if(s.ProjectId is {} pid&&State.Projects.FirstOrDefault(p=>p.Id==pid) is {} project)foreach(var kv in s.Cargo)project.LocalReserved[kv.Key]=Math.Max(0,project.LocalReserved.GetValueOrDefault(kv.Key)-kv.Value);
            var v=State.Vehicles.FirstOrDefault(x=>x.Id==s.VehicleId&&x.ShipmentId==s.Id);
            // External trucks follow the existing cancellation: nothing to load, back to the exterior.
            if(v==null||v.External){if(preserve&&v==null){s.VehicleId=0;continue;}s.Cancelled=true;if(v==null)s.Complete=true;continue;}
            s.Complete=!preserve;s.Cancelled=!preserve;if(preserve)s.VehicleId=0;v.ShipmentId=null;
            if(State.Citizens.FirstOrDefault(c=>c.Id==v.DriverId) is {} driver&&driver.VehicleId!=v.Id){driver.BoardingVehicleId=null;driver.Path.Clear();driver.PathIndex=0;v.DriverId=null;}
            else ReleaseDriver(v);
            v.Status=VehicleStatus.Parked;v.LoadTimer=0;v.Cause=preserve?"Destino demolido: compra paga reservada ao comprador na fazenda; caminhão e motorista livres":s.SourceBuildingId==building.Id?"Carga reservada perdida com "+building.Definition.Name+" demolido":"Entrega cancelada: "+building.Definition.Name+" de destino demolido; carga permanece na origem";
        }
    }
}
