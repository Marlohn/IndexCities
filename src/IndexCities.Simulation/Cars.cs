namespace IndexCities.Simulation;

public readonly record struct ParkingSpot(string Key,int BuildingId,Point Position,double Heading);

public sealed partial class CitySimulation
{
    // Real lot spaces per building type: local offset (x to the right of the
    // facade, z towards the back; the facade faces local -Z) and whether the
    // car lies parallel to the facade. Counts and offsets are calibration.
    static readonly IReadOnlyDictionary<BuildingKind,(double X,double Z,bool AlongX)[]> LotSpots=new Dictionary<BuildingKind,(double,double,bool)[]>
    {
        [BuildingKind.House]=[(-3.4,-6.3,true)],
        [BuildingKind.Grocery]=[(-6.6,-3.5,false),(-6.6,1.2,false)],
        [BuildingKind.Farm]=[(-14,-17.6,true),(-9.4,-17.6,true)],
        [BuildingKind.FuelStation]=[(8.5,7,false),(10.7,7,false)],
        [BuildingKind.WorksDepot]=[(-9,-8,false),(-6.6,-8,false),(-4.2,-8,false)],
        [BuildingKind.WaterIntake]=[(-5,-6,false)],
        [BuildingKind.WaterTreatment]=[(-10,-10,false),(-7.6,-10,false)],
        [BuildingKind.SewageTreatment]=[(-11,-11,false),(-8.6,-11,false)],
        [BuildingKind.WindPower]=[(-7,-6,false)],
        [BuildingKind.CityHall]=[],
    };
    static Point LocalPoint(Building b,double x,double z)=>b.Position+new Point(x*Math.Cos(b.Rotation)+z*Math.Sin(b.Rotation),-x*Math.Sin(b.Rotation)+z*Math.Cos(b.Rotation));
    public IEnumerable<ParkingSpot> ParkingSpots(Building b)
    {
        if(!b.Completed||b.Removed)yield break;var spots=LotSpots[b.Kind];
        for(var i=0;i<spots.Length;i++){var s=spots[i];var p=LocalPoint(b,s.X,s.Z);var forward=s.AlongX?new Point(Math.Cos(b.Rotation),-Math.Sin(b.Rotation)):new Point(-Math.Sin(b.Rotation),-Math.Cos(b.Rotation));
            if(State.Roads.Any(n=>n.Position.Distance(p)<State.Tuning.RoadWidth/2+1.2))continue;yield return new("b:"+b.Id+":"+i,b.Id,p,Math.Atan2(forward.X,forward.Z));}
    }
    ParkingSpot? SpotByKey(string? key)
    {
        if(key==null||!key.StartsWith("b:",StringComparison.Ordinal))return null;var parts=key.Split(':');if(parts.Length!=3||!int.TryParse(parts[1],out var id))return null;
        var b=BuildingById(id);if(b==null)return null;foreach(var spot in ParkingSpots(b))if(spot.Key==key)return spot;return null;
    }
    // A car still physically in the space (its driver already chose another
    // destination) keeps occupying it until it actually leaves.
    bool SpotFree(ParkingSpot spot,int exceptVehicle)=>!State.Vehicles.Any(v=>v.Id!=exceptVehicle&&v.PassengerCar&&v.Status!=VehicleStatus.Outside&&(v.ParkingSpot==spot.Key||v.Position.Distance(spot.Position)<2));
    public Vehicle? FamilyCar(Family? family)=>family==null?null:State.Vehicles.FirstOrDefault(v=>v.PassengerCar&&v.Status!=VehicleStatus.Outside&&family.Members.Any(id=>v.OwnerAccount=="sim:"+id));
    // Strangers do not use private house garages; other lots serve trips to that building.
    ParkingSpot? FreeSpot(Building destination,Family family,Vehicle car)
    {
        if(destination.Kind==BuildingKind.House&&destination.FamilyId!=family.Id)return null;
        foreach(var spot in ParkingSpots(destination))if(SpotFree(spot,car.Id))return spot;return null;
    }
    // Reaching the car must not mean walking across a carriageway outside a crossing.
    bool WalkCrossesRoad(Point a,Point b)=>State.Roads.Any(n=>{var ab=b-a;var len2=ab.X*ab.X+ab.Z*ab.Z;var t=len2<1e-9?0:Math.Clamp(((n.Position.X-a.X)*ab.X+(n.Position.Z-a.Z)*ab.Z)/len2,0,1);return n.Position.Distance(a+ab*t)<State.Tuning.RoadWidth/2-.6;});
    double FuelRate(Vehicle v)=>v.PassengerCar?State.Tuning.CarFuelPerMetre:State.Tuning.FuelPerMetre;
    bool TryDrive(Citizen c,Building destination,JourneyPurpose purpose)
    {
        if(c.Age<18||purpose is not (JourneyPurpose.Work or JourneyPurpose.Home or JourneyPurpose.Shopping or JourneyPurpose.Visit))return false;
        var family=FamilyById(c.FamilyId);var car=FamilyCar(family);if(family==null||car==null||car.Status!=VehicleStatus.Parked||car.DriverId!=null||car.Position.Distance(c.Position)>40||WalkCrossesRoad(c.Position,car.Position))return false;
        var start=NearestRoad(car.Position,false);var target=Access(destination);if(start==null||target==null||start.Position.Distance(car.Position)>State.Tuning.RoadWidth/2+16)return false;
        var route=FindRoadRoute(start.Id,target.Id);if(route.Count==0)return false;
        var home=BuildingById(family.HomeId);var atHome=home!=null&&ParkingSpots(home).Any(s=>s.Key==car.ParkingSpot);
        // A car left at another lot is driven back; from home, short trips stay on foot.
        if(atHome&&RouteLength(route)<State.Tuning.CarMinimumTripMetres)return false;
        var spot=FreeSpot(destination,family,car);if(spot==null)return false;
        var previousSpot=car.ParkingSpot;if(!EnsureFuel(car,target.Id)){car.Route.Clear();return false;}
        car.ParkingSpot=spot.Value.Key;if(car.RefuellingAt==null)car.DockPosition=spot.Value.Position;
        car.DestinationBuildingId=destination.Id;car.TripPurpose=purpose;car.DriverId=c.Id;car.Passengers.Clear();car.Cause="Aguardando motorista embarcar";
        c.BoardingVehicleId=car.Id;c.Purpose=JourneyPurpose.Boarding;c.Path=[c.Position,car.Position];c.PathIndex=0;c.PathRoadRevision=State.RoadRevision;c.DestinationId=destination.Id;c.Activity="Indo ao carro";
        Personal(c,"Vai de carro a "+destination.Definition.Name+(previousSpot!=null&&!atHome?" (trazendo o carro de volta)":""));return true;
    }
    void CarReachedSpot(Vehicle v)
    {
        v.Status=VehicleStatus.Parked;v.Route.Clear();v.RouteIndex=0;v.IntersectionClaim=null;v.WaitingSince=0;
        var occupants=new List<int>();if(v.DriverId is {} driver)occupants.Add(driver);occupants.AddRange(v.Passengers);v.DriverId=null;v.Passengers.Clear();
        if(v.TripPurpose==JourneyPurpose.Leaving){
            // The family leaves its home only now that the car actually left the city.
            foreach(var family in occupants.Select(id=>FamilyById(CitizenWithId(id).FamilyId)).OfType<Family>().Distinct()){if(BuildingById(family.HomeId) is {} house&&house.FamilyId==family.Id)house.FamilyId=null;family.HomeId=null;}
            v.Status=VehicleStatus.Outside;v.ParkingSpot=null;v.Position=State.Entry;v.Cause="No exterior com a família emigrada";
            foreach(var c in occupants.Select(id=>CitizenWithId(id))){c.VehicleId=null;c.Position=State.Entry;c.Path.Clear();c.PathIndex=0;c.Resident=false;c.InsideMap=false;c.Activity="Residindo no exterior";c.WorkplaceId=null;Personal(c,"Saiu de carro da cidade; dinheiro, patrimônio e dívidas persistem");}return;}
        if(SpotByKey(v.ParkingSpot) is {} spot){v.Position=spot.Position;v.Heading=spot.Heading;}v.Cause="Estacionado";
        var destination=BuildingById(v.DestinationBuildingId);
        foreach(var c in occupants.Select(id=>CitizenWithId(id))){c.VehicleId=null;c.Position=v.Position;c.InsideMap=InMap(c.Position);
            if(destination==null){c.Path.Clear();c.PathIndex=0;c.Activity="Destino indisponível";continue;}
            c.Path=[v.Position,destination.Door];c.PathIndex=0;c.PathRoadRevision=State.RoadRevision;c.Purpose=v.TripPurpose;c.DestinationId=destination.Id;c.Activity="Estacionou; caminhando até "+destination.Definition.Name;}
    }
    // A car that lost its route or fuel stays where it physically is; people continue on foot.
    void LeaveCar(Vehicle v,string cause)
    {
        var occupants=new List<int>();if(v.DriverId is {} driver)occupants.Add(driver);occupants.AddRange(v.Passengers);v.DriverId=null;v.Passengers.Clear();v.ParkingSpot=null;v.Route.Clear();v.RouteIndex=0;v.Cause=cause;
        // A stranded car with a tow on the way still stands where it stopped: it keeps
        // holding the junction it is in until the tow loads it.
        if(v.Status!=VehicleStatus.Stranded||v.TowTruckId==null)v.IntersectionClaim=null;
        if(v.Status!=VehicleStatus.Stranded)v.Status=VehicleStatus.Parked;var destination=BuildingById(v.DestinationBuildingId);
        foreach(var c in occupants.Select(id=>CitizenWithId(id))){c.VehicleId=null;c.BoardingVehicleId=null;c.Position=v.Position;c.Path.Clear();c.PathIndex=0;c.NextDecisionAt=State.ElapsedSeconds;Personal(c,cause+"; segue a pé");if(destination!=null&&v.TripPurpose!=JourneyPurpose.Leaving){var path=CitizenRoute(c,destination);if(path.Count>0){c.Path=path;c.PathRoadRevision=State.RoadRevision;c.Purpose=v.TripPurpose;c.DestinationId=destination.Id;}}}
    }
    void ReplanCar(Vehicle v)
    {
        if(v.DriverId==null&&v.Passengers.Count==0){v.Status=VehicleStatus.Parked;v.Route.Clear();return;}
        var destination=BuildingById(v.DestinationBuildingId);var start=NearestRoad(v.Position);
        var target=v.TripPurpose==JourneyPurpose.Leaving?EntryNode():destination==null?null:Access(destination);
        var route=start==null||target==null?new List<int>():FindRoadRoute(start.Id,target.Id);
        if(route.Count==0||v.TripPurpose!=JourneyPurpose.Leaving&&SpotByKey(v.ParkingSpot)==null){LeaveCar(v,"Carro parado: rota ou vaga indisponível");return;}
        v.Route=route;v.RouteIndex=0;v.Status=VehicleStatus.Travelling;v.Cause="Rota refeita";
    }
    void BuyCarOnMigration(Family family,Building house,ExternalFamilyCandidate candidate)
    {
        var members=family.Members.Select(id=>CitizenWithId(id)).ToList();var buyer=members[0];
        var fuel=State.Tuning.FuelTank;var price=State.Tuning.CarPrice+(decimal)fuel*State.Tuning.ImportPrices[ResourceKind.Fuel];
        var funds=members.Sum(c=>Balance(c.Account));var access=Access(house);var spot=ParkingSpots(house).FirstOrDefault();
        if(funds<price+State.Tuning.CarPurchaseReserve||access==null||spot.Key==null||State.OutsideStock[ResourceKind.Fuel]<fuel||!SpotFree(spot,-1))return;
        var route=FindRoadRoute(EntryNode().Id,access.Id);if(route.Count==0)return;
        foreach(var other in members.Skip(1))if(Balance(buyer.Account)<price)Transfer(other.Account,buyer.Account,Math.Min(Balance(other.Account),price-Balance(buyer.Account)),"Cooperação familiar","Contribuição para compra do carro com titular único",null);
        if(!Transfer(buyer.Account,"global",price,"Veículos","Compra/importação real de carro particular e combustível inicial ao migrar",house.Id))return;State.OutsideStock.AddAmount(ResourceKind.Fuel,-(fuel));
        var car=new Vehicle{Id=Id(),OwnerAccount=buyer.Account,PassengerCar=true,Position=State.Entry,Fuel=fuel,CargoCapacity=0,Route=route,DockPosition=spot.Position,ParkingSpot=spot.Key,DriverId=buyer.Id,Passengers=members.Skip(1).Select(c=>c.Id).ToList(),DestinationBuildingId=house.Id,TripPurpose=JourneyPurpose.Arriving,Status=VehicleStatus.Travelling,Cause="Família chegando de carro"};
        State.Vehicles.Add(car);candidate.Cause+="; comprou carro ao migrar";
        foreach(var c in members){c.VehicleId=car.Id;c.Path.Clear();c.PathIndex=0;c.Purpose=JourneyPurpose.Arriving;c.DestinationId=house.Id;c.Activity="Chegando de carro";Personal(c,c.Id==buyer.Id?"Comprou carro importado ao migrar; preço pago à Reserva Global":"Chegando no carro da família");}
    }
    bool FreeTime(Citizen c)
    {
        if(ShiftActive(c)||State.ElapsedSeconds<c.OutingUntil+2*3600)return false;
        var day=(int)State.Now.DayOfWeek;var hour=State.Now.TimeOfDay.TotalHours;var dayOff=day==c.WeeklyDayOff||day==(c.WeeklyDayOff+1)%7;
        return dayOff?hour>=9&&hour<20:hour>=17&&hour<21;
    }
    bool TryOuting(Citizen c,Building home)
    {
        if(NextRandom()<.55){
            var hosts=State.Buildings.Where(b=>b.Kind==BuildingKind.House&&b.Completed&&!b.Removed&&b.FamilyId!=null&&b.FamilyId!=c.FamilyId&&b.Position.Distance(home.Position)<350&&FamilyById(b.FamilyId) is {} f&&f.Members.Any(id=>CitizenWithId(id) is {Alive:true,Moving:false,VehicleId:null} host&&host.Position.Distance(b.Door)<3)).OrderBy(b=>b.Position.Distance(home.Position)).Take(8).ToList();
            if(hosts.Count>0){var host=hosts[(int)(NextRandom()*hosts.Count)];if(StartJourney(c,host,JourneyPurpose.Visit)){c.OutingUntil=State.ElapsedSeconds+(30+NextRandom()*45)*60;return true;}}
            return false;
        }
        var places=State.Buildings.Where(b=>b.Completed&&!b.Removed&&b.Id!=home.Id&&b.Position.Distance(home.Position) is >120 and <320).OrderBy(b=>b.Id).ToList();
        if(places.Count==0)return false;var place=places[(int)(NextRandom()*places.Count)];if(!StartJourney(c,place,JourneyPurpose.Stroll))return false;c.OutingUntil=State.ElapsedSeconds;return true;
    }
}
