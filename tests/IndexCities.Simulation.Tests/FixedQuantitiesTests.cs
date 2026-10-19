using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class FixedQuantitiesTests
{
    static readonly (object Owner,string Name,long Scale)[] Accumulators=[
        (new Citizen(),"Health",FixedPoint.PointScale),(new Citizen(),"BagFood",FixedPoint.ResourceScale),(new Citizen(),"WorkSecondsToday",FixedPoint.TimeScale),
        (new Family(),"Pantry",FixedPoint.ResourceScale),
        (new Vehicle(),"Fuel",FixedPoint.ResourceScale),(new Vehicle(),"FuelBurnedLastStep",FixedPoint.ResourceScale),(new Vehicle(),"LoadTimer",FixedPoint.TimeScale),
        (new Building(),"FoodProduced",FixedPoint.ResourceScale),(new Building(),"WaterConsumedToday",FixedPoint.ResourceScale),(new Building(),"ElectricityConsumedToday",FixedPoint.ResourceScale),
        (new Building(),"FoodSoldToday",FixedPoint.ResourceScale),(new Building(),"WaterOutageSeconds",FixedPoint.TimeScale),
        (new Building(),"MaintenanceRatio",FixedPoint.PointScale),(new RoadNode(),"MaintenanceRatio",FixedPoint.PointScale),(new WorkProject(),"Progress",FixedPoint.PointScale),
        (new CityState(),"ElapsedSeconds",FixedPoint.TimeScale),(new CityState(),"StreetElectricityConsumedToday",FixedPoint.ResourceScale),
        (new CityState(),"WaterAvailableToday",FixedPoint.ResourceScale),(new CityState(),"ElectricityAvailableToday",FixedPoint.ResourceScale)
    ];
    [Fact]
    public void EveryScalarAccumulatorAddsQuantizedDeltasExactly()
    {
        var random=new Random(42);
        foreach(var (owner,name,scale) in Accumulators){
            var type=owner.GetType();var units=type.GetProperty(name+"Units")!;var add=type.GetMethod("Add"+name)!;
            // Above double's exact integer range: a lost low bit exposes an
            // accidental read through the presentation property before adding.
            for(var trial=0;trial<30;trial++){
                var initial=9_007_199_254_740_993L;var n=random.Next(1,400);var delta=(random.Next(-1000,1000)+.5)/scale;
                units.SetValue(owner,initial);for(var i=0;i<n;i++)add.Invoke(owner,[delta]);
                var perStep=checked((long)Math.Round(delta*scale,MidpointRounding.AwayFromZero));
                Assert.Equal(checked(initial+n*perStep),(long)units.GetValue(owner)!);
            }
        }
        var citizen=new Citizen{HungerMicroseconds=9_007_199_254_740_993};
        for(var i=0;i<1000;i++)citizen.AddHungerSeconds(2);
        Assert.Equal(9_007_201_254_740_993,citizen.HungerMicroseconds);
    }
    [Fact]
    public void EveryResourceStoreAddsAndCopiesExactIntegerAmounts()
    {
        ResourceStock[] stores=[new Building().Stock,new WorkProject().Delivered,new WorkProject().LocalReserved,new Vehicle().Cargo,new Shipment().Cargo,new CityState().OutsideStock];
        foreach(var store in stores)foreach(var kind in Enum.GetValues<ResourceKind>()){
            store.Units[kind]=1_000_000*FixedPoint.ResourceScale+1;
            for(var i=0;i<300;i++)store.AddAmount(kind,-.0000000015);
            Assert.Equal(1_000_000*FixedPoint.ResourceScale+1-300*2,store.Units[kind]);
            Assert.Equal(store.Units[kind],new ResourceStock(store).Units[kind]);
        }
        var building=new Building();building.Stock.Units[ResourceKind.Food]=9_007_199_254_740_993;
        for(var i=0;i<30;i++)building.AddStock(ResourceKind.Food,.0000000015);
        Assert.Equal(9_007_199_254_741_053,building.Stock.Units[ResourceKind.Food]);
        var shipment=new ResourceStock(building.Stock);var destination=new Building();
        foreach(var pair in shipment.Units)destination.AddStockUnits(pair.Key,pair.Value);
        Assert.Equal(building.Stock.Units[ResourceKind.Food],destination.Stock.Units[ResourceKind.Food]);
    }
    static void Invoke(CitySimulation sim,string method,params object[] args)=>typeof(CitySimulation).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(sim,args);
    static (CitySimulation Sim,Citizen Citizen,Building Home,Family Family) Needs(double hunger,double health)
    {
        var sim=new CitySimulation(42);var home=new Building{Id=500,Kind=BuildingKind.House,Position=new(100,100),WaterRatio=1,ElectricityRatio=1};
        var family=new Family{Id=501,HomeId=home.Id,Members=[502]};var citizen=new Citizen{Id=502,FamilyId=family.Id,HungerHours=hunger,Health=health,Resident=true,InsideMap=true};
        sim.State.Buildings.Add(home);sim.State.Families.Add(family);sim.State.Citizens.Add(citizen);return(sim,citizen,home,family);
    }
    [Fact]
    public void HungerAndNonlinearHealthEqualSumOfIndividuallyRoundedTerms()
    {
        var (sim,citizen,_,_)=Needs(35,80);const int n=300;
        for(var i=0;i<n;i++)Invoke(sim,"UpdateNeeds",2.0);
        var damage=Enumerable.Range(1,n).Sum(i=>(long)decimal.Round((2m/3600)*(35m+i*2m/3600)/30*FixedPoint.PointScale,0,MidpointRounding.AwayFromZero));
        Assert.Equal(80*FixedPoint.PointScale-damage,citizen.HealthUnits);
        Assert.Equal((35*3600L+2*n)*FixedPoint.TimeScale,citizen.HungerMicroseconds);
        var (recovering,healthy,_,_)=Needs(1,80);
        for(var i=0;i<n;i++)Invoke(recovering,"UpdateNeeds",2.0);
        Assert.Equal(80*FixedPoint.PointScale+n*166_666_667L,healthy.HealthUnits);
    }
    [Fact]
    public void MealAndHealthLimitsKeepTheirOrderInsideTheStep()
    {
        var (sim,citizen,home,family)=Needs(30,80);family.Pantry=1;citizen.Position=home.Door;
        Invoke(sim,"UpdateNeeds",2.0);
        Assert.Equal((18*3600L+2)*FixedPoint.TimeScale,citizen.HungerMicroseconds);
        Assert.Equal(80*FixedPoint.PointScale,citizen.HealthUnits);Assert.Equal(0,family.PantryUnits);
        citizen.HungerHours=1;citizen.Health=100;home.WaterRatio=0;home.ElectricityRatio=0;
        Invoke(sim,"UpdateNeeds",2.0);
        Assert.Equal(100*FixedPoint.PointScale-FixedPoint.Quantize(.65*2/86400,FixedPoint.PointScale),citizen.HealthUnits);
    }
    [Fact]
    public void PollutionAndFarmDemandComposeRoundedIntegerRecurrences()
    {
        var sim=new CitySimulation(42);var point=new Point(0,0);const int n=300;const double emission=.0000001234567;
        sim.State.PollutionCells[0]=123_456_789_123;long expected=123_456_789_123;
        var factor=(decimal)Math.Exp(-.15*2/86400);var delta=(long)Math.Round(emission*FixedPoint.PointScale,MidpointRounding.AwayFromZero);
        for(var i=0;i<n;i++){
            Invoke(sim,"UpdatePollution",2.0,new List<Building>());Invoke(sim,"EmitPollution",point,emission);
            expected=checked((long)decimal.Truncate(expected*factor)+delta);
        }
        Assert.Equal(expected,sim.State.PollutionCells[0]);
        var farm=new Building{FoodDemandEstimateUnits=17_123_456_789,FoodSoldTodayUnits=9_123_456_789};expected=farm.FoodDemandEstimateUnits;
        for(var i=0;i<n;i++){Invoke(sim,"ReviewFarmDemand",farm);expected=(long)decimal.Round(expected*.7m+farm.FoodSoldTodayUnits*.3m,0,MidpointRounding.AwayFromZero);}
        Assert.Equal(expected,farm.FoodDemandEstimateUnits);
        sim.State.PollutionCells[0]=99;Invoke(sim,"UpdatePollution",2.0,new List<Building>());Assert.Empty(sim.State.PollutionCells);
    }
    [Theory]
    [InlineData(100L)]
    [InlineData(1000L)]
    [InlineData(100000L)]
    [InlineData(143000L)]
    [InlineData(144000L)]
    public void TinyPollutionResiduesKeepDecayingUntilRemoved(long initial)
    {
        var sim=new CitySimulation(42);sim.State.PollutionCells.Clear();sim.State.PollutionCells[0]=initial;
        // At least one integer unit must disappear per 2 s step, even where
        // nearest rounding used to leave the residue unchanged forever.
        var previous=initial;
        for(var step=0;step<=initial-100&&sim.State.PollutionCells.Count>0;step++){
            Invoke(sim,"UpdatePollution",2.0,new List<Building>());
            if(sim.State.PollutionCells.TryGetValue(0,out var current)){Assert.True(current<previous);Assert.True(current>=100);previous=current;}
        }
        Assert.Empty(sim.State.PollutionCells);
    }
    [Theory]
    [InlineData("\"1\"")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    [InlineData("1e100")]
    public void InvalidQuantityVersionReportsUnsupportedSave(string version)
    {
        Assert.Throws<InvalidDataException>(()=>CitySimulation.FromJson("{\"QuantityStateVersion\":"+version+"}"));
    }
    [Fact]
    public void PersistedDoublesAreOnlyExplicitNonAccumulators()
    {
        // Review every persisted double (including nullable/collection values)
        // reachable from CityState. New state cannot silently add an accumulator.
        (Type Type,string Names)[] allowed=[
            (typeof(Point),"X,Z"),
            (typeof(Building),"Rotation,WaterRatio,ElectricityRatio,WaterDemand,ElectricityDemand,SewageProduced,UntreatedSewage,OperationalCapacity,MaintenanceDueAt,LastCustomerAt"),
            (typeof(RoadNode),"MaintenanceDueAt,ElectricityRatio"),
            (typeof(WorkProject),"Required,WorkSeconds"),
            (typeof(Citizen),"Satisfaction,LastPaidAt,NextDecisionAt,OutingUntil"),
            (typeof(Family),"LastMigrationReview"),(typeof(Company),"LossStartedAt"),
            (typeof(Vehicle),"SiteDockHeading,Heading,CargoCapacity,WaitingSince,IntersectionEnteredAt,TowCalledAt,TowRetryAt,TowExitWaitSince"),
            (typeof(ExternalFamilyCandidate),"RiskTolerance"),(typeof(ExternalCompanyCandidate),"RiskTolerance"),
            (typeof(CityState),"EconomyNextAt,LakeRadius,LakePollution,PotableCapacity,WaterDemand,ElectricityCapacity,ElectricityDemand,SewageCapacity,SewageDemand,UntreatedSewage,WindFactor,RawWaterCapacity,MaintenanceNextAt"),
            (typeof(SimulationTuning),"MapHalfSize,RoadStep,RoadWidth,CrosswalkSpacing,WalkingSpeed,VehicleSpeed,HighwaySpeed,FuelPerMetre,FuelTank,SimulationSecondsPerRealSecond,SpeedMultipliers,EconomyInterval,PurchaseFoodPerPerson,HungerDamageAfterHours,WaterClosureHours,UtilityRecoveryPerHour,PublicMaintenanceIntervalHours,CarFuelPerMetre,CarMinimumTripMetres,OutingChancePerMinute,BypassWaitSeconds,UTurnWaitSeconds,TowMaxWaitSeconds,TowLoadSeconds,TowUnloadSeconds,TowRetrySeconds")
        ];
        var doubles=new SortedSet<string>();var visited=new HashSet<Type>();
        bool ContainsDouble(Type type)=>type==typeof(double)||(type.IsArray&&ContainsDouble(type.GetElementType()!))||(type.IsGenericType&&type.GetGenericArguments().Any(ContainsDouble));
        void Visit(Type type){
            if(!visited.Add(type))return;
            if(type==typeof(ResourceStock)){
                // The converter serializes Units, not IDictionary<...,double>.
                Assert.Equal(typeof(ResourceStockConverter),type.GetCustomAttribute<JsonConverterAttribute>()!.ConverterType);
                Visit(type.GetProperty(nameof(ResourceStock.Units))!.PropertyType);return;
            }
            if(type.IsArray)Visit(type.GetElementType()!);
            if(type.IsGenericType)foreach(var argument in type.GetGenericArguments())Visit(argument);
            if(type.Assembly!=typeof(CityState).Assembly)return;
            foreach(var property in type.GetProperties(BindingFlags.Instance|BindingFlags.Public)){
                if(property.GetCustomAttribute<JsonIgnoreAttribute>() is {Condition:JsonIgnoreCondition.Always})continue;
                if(ContainsDouble(property.PropertyType))doubles.Add(type.Name+"."+property.Name);
                Visit(property.PropertyType);
            }
        }
        Visit(typeof(CityState));
        var expected=allowed.SelectMany(entry=>entry.Names.Split(',').Select(name=>entry.Type.Name+"."+name)).Order().ToArray();
        Assert.Equal(expected,doubles.ToArray());
    }
    [Fact]
    public void SaveRoundTripPreservesLowBitsOfEveryAccumulator()
    {
        var (sim,citizen,home,family)=Needs(35,80);var project=new WorkProject{Id=503};sim.State.Projects.Add(project);
        var shipment=new Shipment{Id=504};sim.State.Shipments.Add(shipment);
        object[] owners=[sim.State,citizen,home,family,project,shipment,sim.State.Vehicles[0],sim.State.Roads[0]];
        foreach(var owner in owners)foreach(var property in owner.GetType().GetProperties().Where(p=>p.PropertyType==typeof(long)&&p.CanWrite))property.SetValue(owner,9_007_199_254_740_993L);
        // Health/progress have no invariant limit at save time; cargo/resources
        // need positive stock, and delivered must not exceed required materials.
        foreach(var store in new[]{home.Stock,project.LocalReserved,sim.State.OutsideStock,shipment.Cargo,sim.State.Vehicles[0].Cargo})store.Units[ResourceKind.Wood]=9_007_199_254_740_993;
        project.Delivered.Units[ResourceKind.Wood]=123_456_789;project.Required[ResourceKind.Wood]=1;
        sim.State.PollutionCells[0]=9_007_199_254_740_993;
        var json=sim.SaveToJson();Assert.Equal(json,CitySimulation.FromJson(json).SaveToJson());
        var old=JsonNode.Parse(json)!.AsObject();old.Remove("QuantityStateVersion");Assert.Throws<InvalidDataException>(()=>CitySimulation.FromJson(old.ToJsonString()));
    }
    [Fact]
    public void ScalesCoverOneHundredYearsAndLargestCurrentStockAndRejectOverflow()
    {
        var state=new CityState();var seconds=(new DateTime(2126,1,1)-new DateTime(2026,1,1)).TotalSeconds;
        state.AddElapsedSeconds(seconds);Assert.Equal(checked((long)seconds*FixedPoint.TimeScale),state.ElapsedSecondsUnits);
        state.AddElapsedSeconds(.000001);Assert.Equal(checked((long)seconds*FixedPoint.TimeScale+1),state.ElapsedSecondsUnits);
        var stock=new ResourceStock();stock[ResourceKind.Aggregate]=1_000_000;stock.AddAmount(ResourceKind.Aggregate,.000000001);
        Assert.Equal(1_000_000*FixedPoint.ResourceScale+1,stock.Units[ResourceKind.Aggregate]);
        foreach(var scale in new[]{FixedPoint.TimeScale,FixedPoint.ResourceScale,FixedPoint.PointScale}){
            Assert.Throws<OverflowException>(()=>FixedPoint.Add(long.MaxValue,1,scale));
            Assert.Throws<OverflowException>(()=>FixedPoint.Quantize(long.MaxValue/(double)scale*2,scale));
            Assert.Equal(2,FixedPoint.Quantize(1.5/scale,scale));Assert.Equal(-2,FixedPoint.Quantize(-1.5/scale,scale));
        }
        Assert.Throws<OverflowException>(()=>FixedPoint.Multiply(long.MaxValue,2));
    }
}
