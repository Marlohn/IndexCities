using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// Raw save of a city where a dump truck stopped at a bend beside the paver's
// bay (V18), the asphalt truck for that paver (V19, Shipment 121, P86) waited
// on its site path behind it, and the paver waited for the asphalt: nobody
// moved for an hour. Loaded unchanged, the trucks must steer clear, the
// asphalt must reach the paver present at its bay and the street must finish.
public sealed class StuckAsphaltApproachTests
{
    [Fact]
    public void The_saved_city_with_trucks_stuck_at_the_paver_bay_delivers_the_asphalt_and_finishes_the_street()
    {
        var sim=CitySimulation.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","stuck-asphalt-approach.json")));
        var shipment=sim.State.Shipments.Single(s=>s.Id==121);var street=sim.State.Projects.Single(p=>p.Id==86);
        Assert.False(shipment.Complete);Assert.Equal(27,shipment.ReceivingMachineId);
        double Fuel()=>sim.State.Vehicles.Sum(v=>v.Fuel)+sim.State.Buildings.Where(b=>!b.Removed).Sum(b=>b.Quantity(ResourceKind.Fuel))+sim.State.OutsideStock.GetValueOrDefault(ResourceKind.Fuel);
        var money=sim.MonetaryTotal;var fuel=Fuel();var burned=0.0;
        for(var t=0;t<900&&street.Status!=WorkStatus.Completed;t+=2){
            sim.Advance(2);burned+=sim.State.Vehicles.Sum(v=>v.FuelBurnedLastStep);
            // Trucks still stacked at the exterior gateway are staging, not on the map.
            bool Staging(Vehicle v)=>v.Position.Distance(sim.State.Entry)<8;var bodies=sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).ToList();
            foreach(var a in bodies)foreach(var b in bodies.Where(b=>b.Id>a.Id&&b.Position.Distance(a.Position)<12&&!(Staging(a)&&Staging(b))))
                Assert.True(MachineExitTests.Penetration(a,b)<=0,$"t={t}: V{a.Id} and V{b.Id} overlap | {PavingTests.Diagnostic(sim)}");
        }
        Assert.True(shipment.Complete,PavingTests.Diagnostic(sim));
        Assert.True(street.Delivered.GetValueOrDefault(ResourceKind.Asphalt)>=48.7,PavingTests.Diagnostic(sim));
        Assert.True(street.Status==WorkStatus.Completed,PavingTests.Diagnostic(sim));
        Assert.Equal(money,sim.MonetaryTotal);Assert.Equal(fuel,Fuel()+burned,6);
        sim.CheckInvariants();
    }
}
