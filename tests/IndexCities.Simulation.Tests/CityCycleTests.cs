using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;
public sealed class CityCycleTests
{
    internal static CitySimulation City()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(240,0)).Success);sim.Advance(14400);
        Assert.True(sim.PlaceRoad(new(-184,0),new(116,44),true).Success);sim.Advance(7200);
        foreach(var x in new[]{-128,-108,-88,-68,-48,-28,-8,12,32,52,72,92,112,132,152,172,192,212})Assert.True(sim.PlaceBuilding(BuildingKind.House,new(x,14)).Success,"house "+x);
        foreach(var x in new[]{-160,-140,-120,-100,-80,-60,-40,-20,0,20,40,60,80,100})Assert.True(sim.PlaceBuilding(BuildingKind.House,new(x,58)).Success,"house north "+x);
        Assert.True(sim.PlaceBuilding(BuildingKind.WorksDepot,new(-160,-18),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.Grocery,new(-124,-16),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.FuelStation,new(-88,-20),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.Farm,new(-44,-28),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.WindPower,new(0,-16),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.WindPower,new(120,-16),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.WaterTreatment,new(40,-20),Math.PI).Success);
        Assert.True(sim.PlaceBuilding(BuildingKind.SewageTreatment,new(80,-20),Math.PI).Success);
        var lake=sim.State.LakeCentre;var z=Math.Round((lake.Z-sim.State.LakeRadius-30)/4)*4;
        Assert.True(sim.PlaceRoad(new(240,0),new(132,z),true).Success);sim.Advance(14400);
        Assert.True(sim.PlaceBuilding(BuildingKind.WaterIntake,new(144,z+14)).Success);
        return sim;
    }
    static void Dump(CitySimulation sim){var folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory,"../../../../../artifacts/verification"));System.IO.Directory.CreateDirectory(folder);System.IO.File.WriteAllText(System.IO.Path.Combine(folder,"city-cycle-snapshot.json"),sim.SaveToJson());}
    static string Details(CitySimulation sim)=>string.Join(" | ",sim.State.Buildings.Where(b=>!b.Removed).Select(b=>$"{b.Id}:{b.Kind} built={b.Completed} owner={b.OwnerAccount} workers={sim.State.Citizens.Count(c=>c.WorkplaceId==b.Id)} op={b.OperationalCapacity:F1} stock={string.Join(',',b.Stock.Select(k=>$"{k.Key}={k.Value:F1}"))} cause={b.Cause}"))+" | "+string.Join(" | ",sim.State.Projects.Where(p=>p.Status!=WorkStatus.Completed).Select(p=>$"work{p.Id}:{p.Cause} {p.Progress:P0} budget={p.BudgetRemaining}"));
    [Fact] public void Entire_v1_catalog_can_be_built_and_operate_with_real_people_and_payments()
    {
        var sim=City();var snapshots=new List<string>();bool farmProduced=false,foodBought=false,waterRan=false,powerRan=false,maintenanceCapacityChecked=false;
        for(var hour=0;hour<48;hour++){sim.Advance(3600);sim.CheckInvariants();farmProduced|=sim.State.Buildings.Any(b=>b.FoodProduced>0);foodBought|=sim.State.Ledger.Any(l=>l.Category=="Alimentos");waterRan|=sim.State.PotableCapacity>0;powerRan|=sim.State.ElectricityCapacity>0;if(!maintenanceCapacityChecked&&sim.State.PotableCapacity>.001){var treatment=sim.State.Buildings.First(b=>b.Kind==BuildingKind.WaterTreatment&&b.OperationalCapacity>0);var condition=treatment.MaintenanceRatio;treatment.MaintenanceRatio=.5;sim.Advance(2);Assert.Equal(sim.State.PotableCapacity,sim.State.Buildings.Where(b=>b.Kind==BuildingKind.WaterTreatment&&!b.Removed).Sum(b=>b.OperationalCapacity),6);treatment.MaintenanceRatio=condition;sim.Advance(2);maintenanceCapacityChecked=true;}if(hour%12==0){snapshots.Add(Details(sim));Console.WriteLine($"CYCLE hour={hour} complete={sim.State.Buildings.Count(b=>b.Completed)} population={sim.Population} power={sim.State.ElectricityCapacity} water={sim.State.PotableCapacity}");Dump(sim);}}
        Dump(sim);
        var detail=string.Join("\n",snapshots)+"\nFINAL "+Details(sim)+"\nVEHICLES "+string.Join(" | ",sim.State.Vehicles.Select(v=>$"{v.Id}: {v.Status} p={v.Position} heading={v.Heading} routeIndex={v.RouteIndex}/{v.Route.Count} fuel={v.Fuel} shipment={v.ShipmentId} cause={v.Cause}"));
        Assert.True(sim.State.Buildings.Where(b=>!b.Removed).All(b=>b.Completed),detail);
        Assert.True(sim.State.Citizens.Count>0,detail);Assert.True(sim.State.Companies.Count>0,detail);
        Assert.True(maintenanceCapacityChecked,detail);Assert.True(powerRan,detail);Assert.True(waterRan,detail);Assert.True(farmProduced,detail);Assert.True(foodBought,detail);
        Assert.Contains(sim.State.Ledger,l=>l.Category.StartsWith("Salários"));Assert.Equal(sim.State.Tuning.TotalMoney,sim.MonetaryTotal);
    }
    [Fact] public void Inaccessible_private_building_never_gets_fake_occupancy_or_stock()
    {
        var sim=new CitySimulation(42);sim.PlaceBuilding(BuildingKind.CityHall,new(-100,40));sim.PlaceBuilding(BuildingKind.Grocery,new(-70,40));sim.Advance(172800);
        Assert.Empty(sim.State.Citizens);Assert.Empty(sim.State.Companies);Assert.All(sim.State.Buildings,b=>Assert.Equal(0,b.Stock.Values.Sum()));sim.CheckInvariants();
    }
}
