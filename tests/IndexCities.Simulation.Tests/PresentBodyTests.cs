using IndexCities.Simulation;
using Xunit;
using Xunit.Abstractions;
namespace IndexCities.Simulation.Tests;

// The saved city with asphalt trucks stuck at the paver bay. The yielding truck
// V19 stands across the street centre while V18 and V20 turn into the street
// past it. Lane traffic must neither drive nor turn in place into its present
// body, at the step sizes the game actually uses, and the asphalt must still be
// delivered physically with fuel and money conserved. The second case moves V18
// 22 m east on its own lane, so it reaches the bay while V19 unloads.
public sealed class PresentBodyTests
{
    readonly ITestOutputHelper output;
    public PresentBodyTests(ITestOutputHelper output)=>this.output=output;

    [Theory]
    [InlineData(.2,false)]
    [InlineData(.5,false)]
    [InlineData(.2,true)]
    [InlineData(.5,true)]
    public void Lane_traffic_never_overlaps_the_asphalt_truck_and_the_street_is_finished(double dt,bool v18Moved)
    {
        var sim=CitySimulation.FromJson(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","stuck-asphalt-approach.json")));
        if(v18Moved){var car=sim.State.Vehicles.Single(v=>v.Id==18);car.Position=new Point(-150,car.Position.Z);}
        double Fuel()=>sim.State.Vehicles.Sum(v=>v.Fuel)+sim.State.Buildings.Where(b=>!b.Removed).Sum(b=>b.Quantity(ResourceKind.Fuel))+sim.State.OutsideStock.GetValueOrDefault(ResourceKind.Fuel);
        var money=sim.MonetaryTotal;var fuel=Fuel();var burned=0.0;
        Shipment Shipment()=>sim.State.Shipments.Single(s=>s.Id==121);
        WorkProject Project()=>sim.State.Projects.Single(p=>p.Id==86);
        var reloaded=false;var samples=0;var finishedAt=0.0;
        for(var t=0.0;t<900;t+=dt){
            sim.Advance(dt);burned+=sim.State.Vehicles.Sum(v=>v.FuelBurnedLastStep);
            // The exterior gateway is the staging origin, as in StuckAsphaltApproachTests.
            var present=sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).ToList();
            foreach(var a in present)foreach(var b in present)
                if(a.Id<b.Id&&a.Position.Distance(b.Position)<14&&!(a.Position.Distance(sim.State.Entry)<8&&b.Position.Distance(sim.State.Entry)<8)&&MachineExitTests.Penetration(a,b) is var overlap&&overlap>0)
                    Assert.Fail($"t={t+dt:F1}: V{a.Id} ({a.Status}, {a.Cause}) overlaps V{b.Id} ({b.Status}, {b.Cause}) by {overlap:F3} | {PavingTests.Diagnostic(sim)}");
            samples++;
            // Reload during the physical recovery, rather than only testing an
            // already finished city. Capture all resources before switching instances.
            if(!reloaded&&t+dt>=5){var savedFuel=Fuel();sim=CitySimulation.FromJson(sim.SaveToJson());Assert.Equal(money,sim.MonetaryTotal);Assert.Equal(savedFuel,Fuel(),6);reloaded=true;}
            if(Project().Status==WorkStatus.Completed&&sim.State.Vehicles.Where(v=>v.Id is 18 or 19 or 20).All(v=>v.Status==VehicleStatus.Outside)){finishedAt=t+dt;break;}
        }
        Assert.True(Shipment().Complete&&Project().Status==WorkStatus.Completed,PavingTests.Diagnostic(sim));
        Assert.True(Project().Delivered.GetValueOrDefault(ResourceKind.Asphalt)>=48.7,PavingTests.Diagnostic(sim));
        Assert.All(sim.State.Vehicles.Where(v=>v.Id is 18 or 19 or 20),v=>Assert.Equal(VehicleStatus.Outside,v.Status));
        Assert.Equal(money,sim.MonetaryTotal);Assert.Equal(fuel,Fuel()+burned,6);
        sim.CheckInvariants();
        output.WriteLine($"dt={dt}; V18+22m={v18Moved}; samples={samples}; contacts=0; finishedAt={finishedAt:F1}; shipment121={Shipment().Complete}; P86={Project().Status}; money={money}; fuel+burn={Fuel()+burned:F6}; initialFuel={fuel:F6}; reload={reloaded}");
    }
}
