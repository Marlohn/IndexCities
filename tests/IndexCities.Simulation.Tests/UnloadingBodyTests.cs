using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// The asphalt truck V19 unloads at the paver bay on the street lane. A lane car (V18)
// driving across the bay must wait while the unload is physically in progress: the
// unloading body keeps its place, nothing passes through it, and once the unload ends
// the car and the rest of the traffic resume with fuel and money conserved.
public sealed class UnloadingBodyTests
{
    [Fact]
    public void A_lane_car_crossing_the_bay_waits_for_the_asphalt_truck_unloading_on_the_street()
    {
        var sim=CitySimulation.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","stuck-asphalt-approach.json")));
        var truck=sim.State.Vehicles.Single(v=>v.Id==19);var car=sim.State.Vehicles.Single(v=>v.Id==18);
        var shipment=sim.State.Shipments.Single(s=>s.Id==121);
        // The car is an ordinary lane car 22 m east of its fixture pose, on the same lane.
        // Its next node ahead there is the second of its route (x=-160); the saved index
        // (past the bend) would send it diagonally across the street.
        car.Position=new Point(-150,car.Position.Z);car.RouteIndex=1;
        double Fuel()=>sim.State.Vehicles.Sum(v=>v.Fuel)+sim.State.Buildings.Where(b=>!b.Removed).Sum(b=>b.Quantity(ResourceKind.Fuel))+sim.State.OutsideStock.GetValueOrDefault(ResourceKind.Fuel);
        var money=sim.MonetaryTotal;var fuel=Fuel();var burned=0.0;
        var unloadingSteps=0;
        for(var step=0;step<600;step++){
            sim.Advance(0.2);
            burned+=sim.State.Vehicles.Sum(v=>v.FuelBurnedLastStep);
            // Only the unloading at the paver bay is asserted here. The exterior gateway is the
            // staging origin (as in StuckAsphaltApproachTests), and the truck leaving the bay
            // afterwards is an ordinary lane vehicle, whose lane-body rules this test does not change.
            if(truck.Status==VehicleStatus.Unloading&&truck.Position.Distance(sim.State.Entry)>8){
                unloadingSteps++;
                var overlap=MachineExitTests.Penetration(car,truck);
                Assert.True(overlap<=0,$"step {step}: car V18 overlaps the unloading truck V19 (penetration {overlap:F3}) | {PavingTests.Diagnostic(sim)}");
            }
        }
        // The case is exercised: the truck really unloaded, then the delivery completed.
        Assert.True(unloadingSteps>0,PavingTests.Diagnostic(sim));
        Assert.True(shipment.Complete,PavingTests.Diagnostic(sim));
        // Traffic resumes: the car gets past the bay and leaves the street.
        Assert.True(car.Status==VehicleStatus.Outside||car.Position.X<-180,$"car V18 still held: {car.Status} at ({car.Position.X:F1},{car.Position.Z:F1}) | {PavingTests.Diagnostic(sim)}");
        Assert.Equal(money,sim.MonetaryTotal);Assert.Equal(fuel,Fuel()+burned,6);
        sim.CheckInvariants();
    }
}
