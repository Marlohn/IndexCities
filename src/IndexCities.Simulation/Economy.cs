namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    public decimal MarketValue(Building b)
    {
        var access=HasExteriorAccess(b)?1:.65;
        var utilities=.8+.1*b.WaterRatio+.1*b.ElectricityRatio;
        var pollution=LocalPollution(b.Position);
        var available=State.Buildings.Count(x=>x.Kind==b.Kind&&x.Completed&&!x.Removed&&x.OwnerAccount==null);
        var potential=b.Kind==BuildingKind.House?State.FamilyCandidates.Count(x=>!x.Migrated&&x.Capital>b.Definition.BaseValue):Math.Max(0,Population/3.0);
        var demand=Math.Clamp(.85+potential/Math.Max(1,available)*.012,.85,1.2);
        return Money(b.Definition.BaseValue*(decimal)(access*utilities*demand*Math.Max(.55,1-pollution*.15)));
    }
    public string MarketExplanation(Building b)=>"Valor: porte "+b.Definition.BaseValue+", acesso "+(HasExteriorAccess(b)?"viável":"indisponível")+", água "+b.WaterRatio.ToString("P0")+", energia "+b.ElectricityRatio.ToString("P0")+", poluição local "+LocalPollution(b.Position).ToString("F2")+" e procura financeiramente viável";
    void ReviewMarkets()
    {
        var houses=State.Buildings.Where(b=>b.Kind==BuildingKind.House&&b.Completed&&!b.Removed&&b.OwnerAccount==null&&b.FamilyId==null&&HasExteriorAccess(b)).OrderBy(b=>b.Id).ToList();
        // Every vacant house is offered each review; residents still travelling in
        // count against the same jobs, so demand stays bounded by real factors.
        var residents=State.Citizens.Count(c=>c.Alive&&c.Resident);
        foreach(var house in houses){var price=MarketValue(house);var potentialJobs=State.Buildings.Where(b=>b.Kind!=BuildingKind.House&&b.Completed&&!b.Removed&&HasExteriorAccess(b)).Sum(b=>b.Definition.Jobs);
            var candidate=State.FamilyCandidates.FirstOrDefault(c=>!c.Migrated&&c.Capital>=price+150+AnnualPropertyTax(house)&&Balance("global")>=c.Capital&&(potentialJobs>residents||c.RiskTolerance>.7&&c.Capital>price+1500));
            if(candidate==null){house.Cause="Sem família com capital e oportunidade viáveis";continue;}
            var family=new Family{Id=Id(),HomeId=house.Id};
            for(var i=0;i<candidate.Adults;i++){var c=new Citizen{Id=Id(),Name=new[]{"Ana","Bruno","Clara","Daniel","Eva","Felipe","Lia","Miguel"}[(candidate.Id+i)%8]+" "+candidate.Id,FamilyId=family.Id,Age=22+(int)(NextRandom()*28),Position=State.Entry+new Point(0,State.Tuning.RoadWidth/2+1),WeeklyDayOff=(candidate.Id+i)%7,Shift=0};State.Wallets[c.Account]=0;family.Members.Add(c.Id);State.Citizens.Add(c);}
            var buyer=CitizenWithId(family.Members[0]);var other=family.Members.Count==2?CitizenWithId(family.Members[1]):null;
            var share=other==null?candidate.Capital:Money(candidate.Capital*.7m);Transfer("global",buyer.Account,share,"Migração","Capital externo explícito",buyer.Id);if(other!=null)Transfer("global",other.Account,candidate.Capital-share,"Migração","Capital externo explícito",other.Id);
            if(Balance(buyer.Account)<price&&other!=null)Transfer(other.Account,buyer.Account,price-Balance(buyer.Account),"Cooperação familiar","Contribuição para compra com titular único",house.Id);
            if(!Transfer(buyer.Account,"global",price,"Aquisição residencial","Primeira aquisição pelo valor de mercado",house.Id))throw new InvalidOperationException("Compra aprovada sem capital");
            house.OwnerAccount=buyer.Account;house.FamilyId=family.Id;State.Families.Add(family);candidate.Migrated=true;candidate.Cause="Moradia acessível, capital, imposto anual e risco de oportunidades avaliados";
            foreach(var c in State.Citizens.Where(c=>c.FamilyId==family.Id)){c.Resident=true;Personal(c,"Decidiu migrar; moradia adquirida com recursos reais");StartJourney(c,house,JourneyPurpose.Arriving);}
            BuyCarOnMigration(family,house,candidate);
            residents+=candidate.Adults;Event("Família adquiriu casa e iniciou viagem física de chegada");
        }
        foreach(var b in State.Buildings.Where(b=>b.Completed&&!b.Removed&&b.Definition.Private&&b.Kind!=BuildingKind.House&&b.OwnerAccount==null)){
            if(!HasExteriorAccess(b)){b.Cause="Sem rota para funcionários e mercadorias";continue;}
            var score=BusinessOpportunity(b);var price=MarketValue(b);var carryingCost=AnnualPropertyTax(b);
            var company=State.Companies.FirstOrDefault(c=>c.Active&&Balance(c.Account)>price+3000+carryingCost&&score>.35);
            if(company==null){var candidate=State.CompanyCandidates.FirstOrDefault(c=>!c.Entered&&c.Capital>price+3000+carryingCost&&Balance("global")>=c.Capital&&score*c.RiskTolerance>.24);if(candidate==null){b.Cause="Oportunidade econômica insuficiente; continua disponível";continue;}
                company=new Company{Id=Id(),Name=(b.Kind==BuildingKind.Farm?"Campo ":b.Kind==BuildingKind.FuelStation?"Rota ":"Mercado ")+candidate.Id};State.Companies.Add(company);State.Wallets[company.Account]=0;Transfer("global",company.Account,candidate.Capital,"Capital empresarial","Empresa externa com capital finito",company.Id);candidate.Entered=true;candidate.Cause="Acesso, mercado potencial, impostos, capital e risco avaliados";
            }
            if(!Transfer(company.Account,"global",price,"Imóvel empresarial","Primeira aquisição pelo valor de mercado",b.Id))continue;b.OwnerAccount=company.Account;b.CompanyId=company.Id;company.Buildings.Add(b.Id);company.Decision="Adquiriu imóvel por oportunidade observável, sem clientes ou contratações garantidos";b.Cause="Aguardando equipe e estoque";Event(company.Name+" adquiriu "+b.Definition.Name);
        }
        foreach(var b in State.Buildings.Where(b=>b.Completed&&!b.Removed&&!b.Definition.Private&&b.Definition.Jobs>0)){var vacancies=Math.Max(0,b.Definition.Jobs-State.Citizens.Count(c=>c.Alive&&c.WorkplaceId==b.Id));var alternatives=State.Buildings.Where(a=>a.Completed&&!a.Removed&&a.CompanyId!=null&&!a.OperationClosed).Select(a=>a.SalaryOffer).DefaultIfEmpty(State.Tuning.HourlySalary).Max();var publicJobs=State.Buildings.Where(a=>a.Completed&&!a.Removed&&!a.Definition.Private).Sum(a=>a.Definition.Jobs);var affordable=Available/Math.Max(1,publicJobs*8*7);var target=Math.Min(affordable,Math.Max(State.Tuning.HourlySalary,alternatives)*(.95m+(decimal)vacancies/Math.Max(1,b.Definition.Jobs)*.2m));b.SalaryOffer=Money(Math.Max(.1m,b.SalaryOffer+(target-b.SalaryOffer)*.08m));}
        foreach(var b in State.Buildings.Where(b=>b.CompanyId!=null&&b.Completed&&!b.Removed)){
            var company=CompanyById(b.CompanyId)!;if(!company.Active)continue;var demand=BusinessOpportunity(b);
            b.OperationClosed=demand<.06&&State.ElapsedSeconds-b.LastCustomerAt>14*86400&&Balance(company.Account)<500;
            if(b.OperationClosed)b.Cause="Operação encerrada por baixa demanda e caixa; imóvel permanece da empresa";
            var vacancies=Math.Max(0,b.Definition.Jobs-State.Citizens.Count(c=>c.WorkplaceId==b.Id&&c.Alive));var ratio=vacancies/Math.Max(1.0,b.Definition.Jobs);
            var competition=State.Buildings.Where(other=>other.Id!=b.Id&&other.Completed&&!other.Removed&&!other.OperationClosed&&(!other.Definition.Private||other.CompanyId!=null)).Select(other=>other.SalaryOffer).DefaultIfEmpty(State.Tuning.HourlySalary).Max();
            var companyJobs=company.Buildings.Select(id=>BuildingById(id)).Where(site=>site!=null&&!site.OperationClosed).Sum(site=>site!.Definition.Jobs);var liabilities=State.Obligations.Where(o=>o.Debtor==company.Account&&o.Outstanding>0).Sum(o=>o.Outstanding);var affordable=Math.Max(0,Balance(company.Account)-liabilities)/Math.Max(1,companyJobs*8*7);
            var target=Math.Min(affordable,(State.Tuning.HourlySalary*.75m+competition*.25m)*(decimal)(.9+ratio*.25));b.SalaryOffer=Money(Math.Max(.1m,b.SalaryOffer+(target-b.SalaryOffer)*.08m));
        }
    }
    decimal AnnualPropertyTax(Building building)=>MarketValue(building)*State.TaxRates[building.Definition.TaxCategory];
    double BusinessOpportunity(Building b)
    {
        var homes=State.Buildings.Count(h=>h.Kind==BuildingKind.House&&h.Completed&&!h.Removed&&HasExteriorAccess(h));
        var competitors=State.Buildings.Count(x=>x.Id!=b.Id&&x.Kind==b.Kind&&x.CompanyId!=null&&!x.Removed&&!x.OperationClosed);
        var opportunity=b.Kind switch{
            BuildingKind.Grocery=>Math.Clamp((Population+homes*.6)/(5.0+competitors*8),0,1),
            BuildingKind.Farm=>Math.Clamp((Population+State.Buildings.Count(x=>x.Kind==BuildingKind.Grocery&&x.Completed&&!x.Removed)*4)/(8.0+competitors*24),0,1),
            BuildingKind.FuelStation=>Math.Clamp((State.Vehicles.Count(v=>!v.External)+State.Projects.Count(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress)*.5+homes*.2)/(3.0+competitors*8),0,1),_=>0};
        return opportunity/(1+(double)State.TaxRates[b.Definition.TaxCategory]*12);
    }
    void ReviewJobs()
    {
        foreach(var c in State.Citizens.Where(c=>c.Alive&&c.Resident&&c.Age>=18&&c.InsideMap)){
            var current=BuildingById(c.WorkplaceId);if(current!=null&&(current.Removed||current.OperationClosed||current.CompanyId is {} ci&&!CompanyById(ci)!.Active)){SettleWages(c,current);c.WorkplaceId=null;current=null;Personal(c,"Vínculo encerrado: operação deixou de existir");}
            var home=BuildingById(FamilyById(c.FamilyId)?.HomeId);if(home==null)continue;
            var options=State.Buildings.Where(b=>b.Completed&&!b.Removed&&b.Kind!=BuildingKind.House&&!b.OperationClosed&&(!b.Definition.Private||b.CompanyId!=null&&CompanyById(b.CompanyId)!.Active)&&Access(b)!=null&&State.Citizens.Count(x=>x.Alive&&x.WorkplaceId==b.Id)<b.Definition.Jobs).Where(b=>FindRoadRoute(Access(home)?.Id??-1,Access(b)!.Id).Count>0).Where(b=>Spendable(b.CompanyId==null?"city":"company:"+b.CompanyId)>b.SalaryOffer*8*3).OrderByDescending(b=>b.SalaryOffer-(decimal)(home.Position.Distance(b.Position)/400)).ToList();
            var best=options.FirstOrDefault();if(best==null){if(current==null)c.Decision="Sem vaga real com rota e empregador viável";continue;}
            if(current!=null&&best.Id!=current.Id&&best.SalaryOffer<current.SalaryOffer*1.15m&&!State.Obligations.Any(o=>o.Debtor==current.OwnerAccount&&o.Creditor==c.Account&&o.Kind=="Salário"&&o.Outstanding>0))continue;
            if(current?.Id==best.Id)continue;var old=c.WorkplaceId;if(current!=null)SettleWages(c,current);c.WorkplaceId=best.Id;c.Shift=State.Citizens.Count(x=>x.Alive&&x.WorkplaceId==best.Id&&x.Id!=c.Id)%(best.Definition.Utility?3:best.Kind is BuildingKind.Grocery or BuildingKind.FuelStation?2:1);c.NextDecisionAt=State.ElapsedSeconds;
            Personal(c,(old==null?"Contratado":"Mudou autonomamente de emprego")+" em "+best.Definition.Name+"; salário e rota avaliados");
        }
    }
    void AccrueWage(Citizen c,double seconds){var job=BuildingById(c.WorkplaceId);if(job==null)return;var earned=(decimal)(seconds/3600)*job.SalaryOffer;c.WagesAccrued+=earned;if(job.Kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment)State.WaterWageCostToday+=earned;else if(job.Kind==BuildingKind.WindPower)State.ElectricityWageCostToday+=earned;}
    // Citizens grouped by WorkplaceId in list order; lists are reused per step.
    readonly Dictionary<int,List<Citizen>> _staff=new();
    Dictionary<int,List<Citizen>> StaffByWorkplace()
    {
        foreach(var list in _staff.Values)list.Clear();
        foreach(var c in State.Citizens)if(c.WorkplaceId is {} site){if(!_staff.TryGetValue(site,out var list))_staff[site]=list=[];list.Add(c);}
        return _staff;
    }
    void OperateBusinesses(double dt)
    {
        // Workplaces do not change during this loop: group the citizens once,
        // keeping building order and citizen order within each building.
        var staff=StaffByWorkplace();
        foreach(var b in State.Buildings.Where(b=>b.Completed&&!b.Removed)){
            if(staff.TryGetValue(b.Id,out var workers))foreach(var c in workers.Where(c=>c.Alive&&c.VehicleId==null&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(b.Door)<3&&ShiftActive(c))){c.WorkSecondsToday+=dt;AccrueWage(c,dt);}
            if(b.Kind!=BuildingKind.Farm||b.CompanyId==null||b.OperationalCapacity<=0||!Open(b))continue;
            var target=FarmReserveTarget(b);if(b.Quantity(ResourceKind.Food)>=target){b.Cause="Estoque de segurança completo; produção acompanha pedidos reais";continue;}
            var amount=Math.Min(target-b.Quantity(ResourceKind.Food),b.OperationalCapacity*dt/43200);if(amount<=0)continue;b.AddStock(ResourceKind.Food,amount);b.FoodProduced+=amount;
            b.SalePrice=Money(Math.Max(2,State.Tuning.ImportPrices[ResourceKind.Food]*.85m));
        }
    }
    // Adaptive production: a small safety reserve plus recent real orders
    // (dispatched, or requested while the farm lacked stock); never population alone.
    double FarmReserveTarget(Building farm)=>Math.Min(farm.Definition.Capacity,Math.Max(16,farm.FoodDemandEstimate*1.5));
    void ReviewFarmDemand(Building farm)=>farm.FoodDemandEstimate=farm.FoodDemandEstimate*.7+farm.FoodSoldToday*.3;
    bool TryBuyFood(Citizen c,Building shop)
    {
        if(shop.CompanyId==null||!Open(shop)||shop.OperationalCapacity<=0||shop.Quantity(ResourceKind.Food)<.1){Personal(c,"Compra não realizada: operação/estoque indisponíveis");return false;}
        var family=FamilyById(c.FamilyId)!;var desired=Math.Max(0,family.Members.Count*State.Tuning.PurchaseFoodPerPerson-family.Pantry);if(desired<=.1)return true;
        var need=Money((decimal)Math.Min(desired,shop.Quantity(ResourceKind.Food))*shop.SalePrice);
        foreach(var helper in family.Members.Select(id=>CitizenWithId(id)).Where(s=>s.Id!=c.Id&&s.Alive)){var shortage=Math.Max(0,need-Balance(c.Account));if(shortage<=0)break;Transfer(helper.Account,c.Account,Math.Min(shortage,Balance(helper.Account)),"Cooperação familiar","Contribuição real para alimentação",c.Id);}
        var amount=Math.Min(desired,Math.Min(shop.Quantity(ResourceKind.Food),(double)(Balance(c.Account)/shop.SalePrice)));amount=Math.Floor(amount*100)/100;if(amount<.1){Personal(c,"Compra não realizada: família sem saldo para alimentos");return false;}
        var price=Money((decimal)amount*shop.SalePrice);if(!Transfer(c.Account,"company:"+shop.CompanyId,price,"Alimentos","Compra presencial de alimento existente",shop.Id))return false;
        shop.AddStock(ResourceKind.Food,-amount);shop.FoodSoldToday+=amount;shop.LastCustomerAt=State.ElapsedSeconds;c.BagFood+=amount;Personal(c,"Comprou "+amount.ToString("F2")+" alimentos por "+price+"; transportando à residência");return true;
    }
    void RestockBusinesses()
    {
        foreach(var b in State.Buildings.Where(b=>b.Completed&&!b.Removed&&!b.OperationClosed&&b.CompanyId!=null&&CompanyById(b.CompanyId)!.Active)){
            var account="company:"+b.CompanyId;var resource=b.Kind==BuildingKind.Grocery?ResourceKind.Food:ResourceKind.Fuel;
            if(b.Kind==BuildingKind.Farm){AcquireFarmTruck(b);continue;}if(b.Kind is not (BuildingKind.Grocery or BuildingKind.FuelStation))continue;
            var target=Math.Min(b.Definition.Capacity,b.Kind==BuildingKind.Grocery?Math.Max(16,Population*5):Math.Max(40,State.Vehicles.Count(v=>!v.External)*40));
            var pending=State.Shipments.Where(s=>s.DestinationBuildingId==b.Id&&!s.Complete).Sum(s=>s.Cargo.GetValueOrDefault(resource));var amount=Math.Min(100,target-b.Quantity(resource)-pending);if(amount<2||Access(b)==null)continue;
            var importedCost=State.Tuning.ImportPrices[resource]+(State.Tuning.FreightBase+(decimal)(b.Position.Distance(State.Entry)*2)*State.Tuning.FreightPerMetre)/(decimal)amount;
            var farms=b.Kind==BuildingKind.Grocery?State.Buildings.Where(f=>f.Kind==BuildingKind.Farm&&f.Completed&&!f.Removed&&f.CompanyId!=null&&Access(f)!=null&&f.SalePrice+(decimal)(f.Position.Distance(b.Position)*.01)<=importedCost*1.12m).OrderBy(f=>f.SalePrice+(decimal)(f.Position.Distance(b.Position)*.01)).ToList():[];
            var local=farms.Where(f=>f.Quantity(ResourceKind.Food)-State.Shipments.Where(s=>s.SourceBuildingId==f.Id&&!s.Complete&&!s.PickedUp).Sum(s=>s.Cargo.GetValueOrDefault(ResourceKind.Food))>=amount).FirstOrDefault(f=>LocalDeliveryVehicle(f)!=null);
            // A competitive local supplier without enough stock receives this real
            // order as a production signal; the shop still imports meanwhile.
            if(local==null&&farms.Count>0)farms[0].FoodDemandEstimate=Math.Max(farms[0].FoodDemandEstimate,amount);
            if(local!=null&&OrderLocalFood(local,b,amount))continue;
            OrderImport(b,resource,amount,account);
        }
    }
    void AcquireFarmTruck(Building farm)
    {
        if(State.Vehicles.Any(v=>v.HomeBuildingId==farm.Id&&v.OwnerAccount=="company:"+farm.CompanyId)||State.Shipments.Any(s=>s.Purpose==ShipmentPurpose.Vehicle&&s.DestinationBuildingId==farm.Id&&!s.Complete)||Access(farm)==null||State.Vehicles.Count(v=>!v.External&&!v.PassengerCar)>=State.Tuning.ExternalCompanyCandidates)return;
        var account="company:"+farm.CompanyId;var fuel=State.Tuning.FuelTank;var price=State.Tuning.TruckPrice+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel]+State.Tuning.FreightBase;
        if(Spendable(account)<price+500||State.OutsideStock[ResourceKind.Fuel]<fuel||BusinessOpportunity(farm)<.2)return;var route=FindRoadRoute(EntryNode().Id,Access(farm)!.Id);if(route.Count==0)return;
        // One way only: the truck stays in the city and refuels at a station afterwards.
        if(RouteLength(route)*State.Tuning.FuelPerMetre+4>fuel){if(CompanyById(farm.CompanyId) is {} owner)owner.Decision="Caminhão não comprado: a entrega desde a entrada passa da autonomia do tanque";return;}
        if(!Transfer(account,"global",price,"Veículos","Aquisição/importação real de caminhão e combustível inicial",farm.Id))return;State.OutsideStock[ResourceKind.Fuel]-=fuel;
        var v=new Vehicle{Id=Id(),OwnerAccount=account,External=true,ExternalDriverId="entregador externo de veículo "+farm.Id,HomeBuildingId=farm.Id,Position=State.Entry,Fuel=fuel,Route=route,DockPosition=DockAt(Access(farm)!,farm,false),Status=VehicleStatus.Travelling};var s=new Shipment{Id=Id(),Purpose=ShipmentPurpose.Vehicle,DestinationBuildingId=farm.Id,VehicleId=v.Id,PickedUp=true,PricePaid=price,GoodsOwnerAccount=account};v.ShipmentId=s.Id;State.Vehicles.Add(v);State.Shipments.Add(s);
    }
    Vehicle? LocalDeliveryVehicle(Building farm)=>State.Vehicles.FirstOrDefault(v=>!v.External&&v.HomeBuildingId==farm.Id&&v.OwnerAccount=="company:"+farm.CompanyId&&v.Status==VehicleStatus.Parked&&v.DriverId==null&&v.Cargo.Values.Sum()<.001);
    bool OrderLocalFood(Building farm,Building shop,double amount)
    {
        var v=LocalDeliveryVehicle(farm);var driver=State.Citizens.FirstOrDefault(c=>c.Alive&&c.WorkplaceId==farm.Id&&c.VehicleId==null&&c.ConstructionProjectId==null&&!c.Moving&&c.Position.Distance(farm.Door)<3&&ShiftActive(c));if(v==null||driver==null||Access(shop)==null)return false;
        amount=Math.Min(amount,v.CargoCapacity);var price=farm.CompanyId==shop.CompanyId?0:Money((decimal)amount*farm.SalePrice+State.Tuning.FreightBase+(decimal)farm.Position.Distance(shop.Position)*State.Tuning.FreightPerMetre);
        if(Balance("company:"+shop.CompanyId)<price||!EnsureFuel(v,Access(shop)!.Id))return false;
        if(price>0&&!Transfer("company:"+shop.CompanyId,"company:"+farm.CompanyId,price,"Fornecedor local","Alimentos reservados e frete do fornecedor real",shop.Id))return false;
        var s=new Shipment{Id=Id(),Purpose=ShipmentPurpose.Food,SourceBuildingId=farm.Id,DestinationBuildingId=shop.Id,VehicleId=v.Id,Cargo=new(){{ResourceKind.Food,amount}},GoodsOwnerAccount="company:"+shop.CompanyId,PricePaid=price,PickedUp=false};State.Shipments.Add(s);v.ShipmentId=s.Id;v.DriverId=driver.Id;driver.BoardingVehicleId=v.Id;driver.Purpose=JourneyPurpose.Boarding;driver.Path=[driver.Position,v.Position];driver.PathIndex=0;driver.PathRoadRevision=State.RoadRevision;driver.Activity="Caminhando para embarcar no caminhão";v.Status=VehicleStatus.Unloading;v.LoadTimer=15;v.Cause="Carregando na fazenda; motorista real";shop.InventoryUnitCost=Money(price==0?farm.SalePrice:price/(decimal)amount);shop.SalePrice=Money(shop.InventoryUnitCost*1.4m);return true;
    }
    bool OrderImport(Building destination,ResourceKind resource,double requested,string account)
    {
        var n=Access(destination);var vehicle=AvailableTruck(resource);if(n==null||vehicle==null)return false;var route=FindRoadRoute(EntryNode().Id,n.Id);if(route.Count==0)return false;
        if(RouteLength(route)*2*FuelRate(vehicle)+4>State.Tuning.FuelTank){destination.Cause="Reposição inviável: ida e volta maiores que a autonomia do caminhão";return false;}
        var fuel=Math.Max(0,State.Tuning.FuelTank-vehicle.Fuel);var fee=State.Tuning.FreightBase+(decimal)(RouteLength(route)*2)*State.Tuning.FreightPerMetre+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel];var amount=Math.Min(requested,Math.Min(vehicle.CargoCapacity,Math.Min(State.OutsideStock[resource],(double)((Balance(account)-fee)/State.Tuning.ImportPrices[resource]))));amount=Math.Floor(amount*100)/100;
        if(amount<1||State.OutsideStock[ResourceKind.Fuel]<fuel){destination.Cause="Reposição inviável: saldo, carga ou combustível externos";return false;}var price=Money((decimal)amount*State.Tuning.ImportPrices[resource]+fee);
        if(!Transfer(account,"global",price,"Importação comercial","Produto, frete e combustível com entrega física",destination.Id))return false;State.OutsideStock[resource]-=amount;State.OutsideStock[ResourceKind.Fuel]-=fuel;
        var s=new Shipment{Id=Id(),Purpose=resource==ResourceKind.Food?ShipmentPurpose.Food:ShipmentPurpose.Fuel,DestinationBuildingId=destination.Id,Destination=n.Position,VehicleId=vehicle.Id,PickedUp=true,GoodsOwnerAccount=account,Cargo=new(){{resource,amount}},PricePaid=price};State.Shipments.Add(s);vehicle.Fuel+=fuel;vehicle.Cargo=new(s.Cargo);vehicle.ShipmentId=s.Id;vehicle.Route=route;vehicle.RouteIndex=0;vehicle.DockPosition=DockAt(n,destination,false);vehicle.Status=VehicleStatus.Travelling;vehicle.Position=State.Entry;vehicle.Cause="Importação comercial em trânsito";
        destination.InventoryUnitCost=Money(price/(decimal)amount);destination.SalePrice=Money(destination.InventoryUnitCost*1.4m);return true;
    }
    void UpdateNeeds(double dt)
    {
        // Pollution is unchanged during this routine, and residents in the same
        // home use exactly the same point. Retain every per-citizen calculation.
        var pollution=new Dictionary<Point,double>();
        double HomePollution(Point position){if(!pollution.TryGetValue(position,out var value)){value=SampledPollution(position);pollution.Add(position,value);}return value;}
        foreach(var c in State.Citizens.Where(c=>c.Alive&&c.Resident&&c.InsideMap)){
            c.HungerHours+=dt/3600;var family=FamilyById(c.FamilyId)!;var home=BuildingById(family.HomeId);
            if(home!=null&&!c.Moving&&c.VehicleId==null&&c.Position.Distance(home.Door)<3&&c.HungerHours>=12&&family.Pantry>=1){family.Pantry-=1;c.HungerHours=Math.Max(0,c.HungerHours-12);Personal(c,"Consumiu 1 alimento do estoque físico da residência");}
            var hungry=c.HungerHours>State.Tuning.HungerDamageAfterHours;var unmet=home==null?1:(1-home.WaterRatio)*.5+(1-home.ElectricityRatio)*.15+HomePollution(home.Position)*.1;
            if(hungry)c.Health-=dt/3600*(c.HungerHours/State.Tuning.HungerDamageAfterHours);else if(c.HungerHours<18)c.Health=Math.Min(100,c.Health+dt/3600*.3);
            c.Health=Math.Clamp(c.Health-unmet*dt/86400,0,100);c.Satisfaction=Math.Clamp(80-c.HungerHours*.7-unmet*15-(family.Members.Count>Catalog.Definitions[BuildingKind.House].Capacity?8:0),0,100);
            if(c.Health<=0)CitizenDied(c);
        }
    }
    void CitizenDied(Citizen c)
    {
        c.Alive=false;c.Resident=false;c.Path.Clear();c.WorkplaceId=null;Personal(c,"Morreu após deterioração real de saúde por necessidades não atendidas");
        var debts=State.Obligations.Where(o=>o.Debtor==c.Account&&o.Outstanding>0&&o.Creditor=="city").ToList();var total=debts.Sum(o=>o.Outstanding);var funds=Math.Min(total,Balance(c.Account));foreach(var debt in debts){var amount=total==0?0:Math.Min(Balance(c.Account),Money(funds*debt.Outstanding/total));Transfer(c.Account,"city",amount,"Dívidas pessoais","Quitação proporcional com saldo do falecido",debt.BuildingId);debt.Outstanding=0;}
        var heir=State.Citizens.FirstOrDefault(x=>x.Id!=c.Id&&x.FamilyId==c.FamilyId&&x.Alive);if(heir!=null)Transfer(c.Account,heir.Account,Balance(c.Account),"Herança","Dinheiro existente do falecido",c.Id);else Transfer(c.Account,"global",Balance(c.Account),"Patrimônio não reclamado","Saldo sem sucessor elegível",c.Id);
        foreach(var b in State.Buildings.Where(b=>b.OwnerAccount==c.Account))b.OwnerAccount=heir?.Account;
        foreach(var v in State.Vehicles.Where(v=>v.OwnerAccount==c.Account))v.OwnerAccount=heir?.Account??"unclaimed";
        foreach(var f in State.Families.Where(f=>f.Members.Contains(c.Id))){if(f.Members.All(id=>!CitizenWithId(id).Alive)){if(BuildingById(f.HomeId) is {} house)house.FamilyId=null;f.HomeId=null;}}
        Event("Falecimento registrado; patrimônio, dívidas e saldo conciliados");
    }
    void ReviewEmigration()
    {
        foreach(var family in State.Families.Where(f=>!f.Emigrating&&f.HomeId!=null)){
            var members=family.Members.Select(id=>CitizenWithId(id)).Where(c=>c.Alive&&c.Resident).ToList();if(members.Count==0)continue;
            if(members.All(c=>c.HungerHours>48&&c.WorkplaceId==null&&Balance(c.Account)<20)&&State.ElapsedSeconds-family.LastMigrationReview>3*86400){var home=BuildingById(family.HomeId);var start=home==null?null:Access(home);if(start==null)continue;var route=FindRoadRoute(start.Id,EntryNode().Id);if(route.Count==0)continue;var car=FamilyCar(family);
                // SPEC (populacao): a family with a car leaves with it, never on foot
                // abandoning it. It waits until the car is home with fuel for the exit,
                // refuelling first when the owner can pay for what the trip needs.
                if(car!=null){
                    void Wait(string reason){foreach(var m in members){m.Activity=reason;m.Decision=reason;}}
                    var carHome=home==null?(ParkingSpot?)null:ParkingSpots(home).FirstOrDefault(s=>s.Key==car.ParkingSpot);
                    if(car.Status!=VehicleStatus.Parked||car.DriverId!=null||carHome?.Key==null||!members.All(m=>m.Position.Distance(home!.Door)<3&&!m.Moving)){Wait("Família aguardando o carro em casa para emigrar");continue;}
                    if(FindRoadRoute((NearestRoad(car.Position,false)??start).Id,EntryNode().Id) is not {Count:>0} carRoute){Wait("Família aguardando rota de carro até a saída");continue;}
                    var missing=RouteLength(carRoute)*FuelRate(car)+1-car.Fuel;
                    if(missing>0){
                        var spot=car.DockPosition;
                        // Size the purchase on the trip actually driven: home → station → exit.
                        var planned=EnsureFuel(car,EntryNode().Id)&&BuildingById(car.RefuellingAt) is {} station&&(car.Fuel-RouteLength(car.Route)*FuelRate(car)) is var atStation&&(RouteLength(car.SavedRoute)*FuelRate(car)+1-atStation) is var purchase&&purchase<=State.Tuning.FuelTank-atStation&&station.Quantity(ResourceKind.Fuel)>=purchase&&Balance(car.OwnerAccount)>=Money((decimal)purchase*station.SalePrice);
                        if(!planned){car.RefuellingAt=null;car.SavedRoute=[];car.Route.Clear();car.RouteIndex=0;car.DockPosition=spot;Wait("Família aguardando dinheiro para o combustível da saída");continue;}
                    }else{car.Route=carRoute;car.RouteIndex=0;car.DockPosition=State.Entry;}
                    family.Emigrating=true;car.ParkingSpot=null;car.TripPurpose=JourneyPurpose.Leaving;car.DriverId=members[0].Id;car.Passengers=members.Skip(1).Select(m=>m.Id).ToList();car.Status=VehicleStatus.Travelling;car.Cause="Família emigrando de carro";foreach(var m in members){m.VehicleId=car.Id;m.Path.Clear();m.PathIndex=0;m.Purpose=JourneyPurpose.Leaving;m.Activity="Emigrando de carro";Personal(m,"Decidiu sair por falta persistente de renda e alimentação");}continue;
                }
                family.Emigrating=true;
                foreach(var c in members){c.Path=route.Select(id=>_roads[id].Position+new Point(0,State.Tuning.RoadWidth/2+1)).ToList();c.Path.Add(State.Entry);c.PathIndex=0;c.Purpose=JourneyPurpose.Leaving;c.PathRoadRevision=State.RoadRevision;c.Activity="Emigrando por condições reais";Personal(c,"Decidiu sair por falta persistente de renda e alimentação");}if(home!=null)home.FamilyId=null;family.HomeId=null;
            }
        }
    }
}
