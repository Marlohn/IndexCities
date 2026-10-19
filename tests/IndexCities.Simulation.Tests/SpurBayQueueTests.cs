using IndexCities.Simulation;
using Xunit;
using Xunit.Abstractions;

namespace IndexCities.Simulation.Tests;

// DD099 rebuilt from a valid city: houses placed beside a short dead-end street
// while it is still being built. When it opens, three deliveries to the first
// house's bay (8 m past the T) arrive together with deliveries further along.
// Before the fix the next delivery took the T and stopped inside the street on
// the leaving truck's only way out, and trucks waited at the T with their fronts
// in the crossing road: bodies overlapped and the street locked for good.
public sealed class SpurBayQueueTests
{
    readonly ITestOutputHelper output;
    public SpurBayQueueTests(ITestOutputHelper output)=>this.output=output;

    [Theory]
    [InlineData(.2)]
    [InlineData(.5)]
    [InlineData(2.0)]
    public void Deliveries_to_a_bay_just_past_a_junction_queue_before_it_and_every_truck_leaves(double dt)
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0});
        Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,-20)).Success);
        Assert.True(sim.PlaceRoad(new(-192,0),new(-100,0)).Success);
        Assert.True(sim.PlaceRoad(new(-156,0),new(-156,32)).Success);
        foreach(var z in new[]{12.0,26,40})sim.PlaceBuilding(BuildingKind.House,new(-142,z),Math.PI/2);
        double Fuel()=>sim.State.Vehicles.Sum(v=>v.Fuel)+sim.State.Buildings.Where(b=>!b.Removed).Sum(b=>b.Quantity(ResourceKind.Fuel))+sim.State.OutsideStock.GetValueOrDefault(ResourceKind.Fuel);
        var money=sim.MonetaryTotal;var fuel=Fuel();var burned=0.0;
        var held=false;var reloaded=false;var machineSamples=0;var time=0.0;
        bool Done()=>sim.State.Projects.Where(p=>BuildingReachable(sim,p)).All(p=>p.Status==WorkStatus.Completed)&&sim.State.Vehicles.Where(v=>v.External).All(v=>v.Status==VehicleStatus.Outside);
        for(;time<7200&&!(time>60&&Done());time+=dt){
            sim.Advance(dt);burned+=sim.State.Vehicles.Sum(v=>v.FuelBurnedLastStep);
            var present=sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).ToArray();
            foreach(var a in present)foreach(var b in present)
                if(a.Id<b.Id&&a.Position.Distance(b.Position)<14&&!(a.Position.Distance(sim.State.Entry)<8&&b.Position.Distance(sim.State.Entry)<8)&&MachineExitTests.Penetration(a,b) is var overlap&&overlap>0){
                    // A paving machine's own exit/dock interactions are a separate
                    // mechanism (present identically before this change); counted, not hidden.
                    if(a.Machine!=PavingMachine.None||b.Machine!=PavingMachine.None){machineSamples++;continue;}
                    Assert.Fail($"t={time+dt:F1}: V{a.Id} ({a.Status}, {a.Cause}) overlaps V{b.Id} ({b.Status}, {b.Cause}) by {overlap:F3}");
                }
            if(sim.State.Vehicles.Any(v=>v.Status==VehicleStatus.Travelling&&v.Cause=="Fila para acesso de carga"&&v.RouteIndex<v.Route.Count)){
                held=true;
                // Save and reload while a delivery is held before the T.
                if(!reloaded){var savedFuel=Fuel();var savedMoney=sim.MonetaryTotal;sim=CitySimulation.FromJson(sim.SaveToJson());Assert.Equal(savedMoney,sim.MonetaryTotal);Assert.Equal(savedFuel,Fuel(),6);reloaded=true;}
            }
        }
        Assert.True(held&&reloaded,"no delivery waited before the junction");
        Assert.True(Done(),PavingTests.Diagnostic(sim));
        var houses=sim.State.Projects.Where(p=>p.BuildingId!=null&&BuildingReachable(sim,p)).ToList();
        Assert.True(houses.Count>=2,PavingTests.Diagnostic(sim));
        Assert.All(houses,p=>Assert.All(p.Required,r=>Assert.True(p.Delivered.GetValueOrDefault(r.Key)>=r.Value-.001,$"P{p.Id} {r.Key}")));
        Assert.All(sim.State.Shipments.Where(s=>s.ProjectId is {} id&&houses.Any(p=>p.Id==id)),s=>Assert.True(s.Complete,$"shipment {s.Id}"));
        Assert.Equal(money,sim.MonetaryTotal);Assert.Equal(fuel,Fuel()+burned,6);
        sim.CheckInvariants();
        output.WriteLine($"dt={dt}; finishedAt={time:F1}; heldBeforeJunction={held}; reloaded={reloaded}; machineContactSamples={machineSamples}; houses={houses.Count}");
    }

    static bool BuildingReachable(CitySimulation sim,WorkProject p)=>p.BuildingId is not {} id||sim.State.Buildings.FirstOrDefault(b=>b.Id==id) is {} building&&sim.HasExteriorAccess(building);
}
