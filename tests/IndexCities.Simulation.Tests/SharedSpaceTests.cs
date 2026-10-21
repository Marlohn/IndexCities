using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// SPEC transporte-circulacao (curva simples um de cada vez, capacidade física de
// carga/descarga, cruzamentos) and ARCHITECTURE "Espaços compartilhados": new
// cities whose works need trucks to share a simple bend, a T junction and house
// bays, with one or two paving teams. Every step, no truck body enters another
// truck's, a machine's or a parked car's; and every work that has street access
// is completed: waiting is physical and finite, never a cycle.
public sealed class SharedSpaceTests
{
    // A street reached only through two L bends, three houses on it (the probe's "bend").
    static CitySimulation Bend()
    {
        var sim=new CitySimulation(42,new(){ExternalFamilyCandidates=0});
        Assert.True(sim.PlaceRoad(new(-192,0),new(-172,0)).Success&&sim.PlaceRoad(new(-172,0),new(-172,24)).Success&&sim.PlaceRoad(new(-172,24),new(-120,24)).Success,sim.State.LastMessage);
        Seat(sim);
        foreach(var x in new[]{-158.0,-144,-130})Assert.True(sim.PlaceBuilding(BuildingKind.House,new(x,38)).Success,sim.State.LastMessage);
        return sim;
    }
    // Two paving teams: a T at (-172,0) with a branch that bends at (-172,24),
    // and the main street bending south at (-120,0); houses on both new streets.
    static CitySimulation BendAndJunction()
    {
        var sim=new CitySimulation(42,new(){ExternalPavingTeams=2,ExternalFamilyCandidates=0});
        Assert.True(sim.PlaceRoad(new(-192,0),new(-120,0)).Success&&sim.PlaceRoad(new(-172,0),new(-172,24)).Success&&sim.PlaceRoad(new(-172,24),new(-128,24)).Success&&sim.PlaceRoad(new(-120,0),new(-120,-36)).Success,sim.State.LastMessage);
        Seat(sim);
        foreach(var x in new[]{-158.0,-144})Assert.True(sim.PlaceBuilding(BuildingKind.House,new(x,38)).Success,sim.State.LastMessage);
        foreach(var z in new[]{-14.0,-28})Assert.True(sim.PlaceBuilding(BuildingKind.House,new(-106,z),Math.PI/2).Success,sim.State.LastMessage);
        return sim;
    }

    // The seat south of the main street, its door facing it.
    // At x=-152 the bend fixture's hall door was 21 m from its nearest street:
    // it could never complete even with perfect traffic. This door is 7 m
    // south of the first 20 m street in both layouts (access limit is 8.25 m).
    static void Seat(CitySimulation sim)=>Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-184,-16),Math.PI).Success,sim.State.LastMessage);

    static CitySimulation City(string layout)=>layout=="bend"?Bend():BendAndJunction();

    static List<WorkProject> Reachable(CitySimulation sim)=>sim.State.Projects.Where(p=>p.Status!=WorkStatus.Cancelled).ToList();

    // Bodies that may legitimately coincide: two trucks staged together at the
    // exterior gateway, or a towed vehicle carried by its tow.
    // A body still at the exterior staging origin or its exterior dock (every
    // external vehicle starts and unloads there, not yet in the city) and one
    // just leaving it; any other pair of bodies counts.
    static bool AtOrigin(CitySimulation sim,Vehicle v)=>v.Position.Distance(sim.State.Entry)<2;
    static bool Exempt(CitySimulation sim,Vehicle a,Vehicle b)=>(AtOrigin(sim,a)||AtOrigin(sim,b))&&a.Position.Distance(sim.State.Entry)<8&&b.Position.Distance(sim.State.Entry)<8||a.TowTruckId==b.Id||b.TowTruckId==a.Id;

    static string Contact(CitySimulation sim)
    {
        var live=sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).ToArray();
        foreach(var a in live)foreach(var b in live)
            if(a.Id<b.Id&&!(a.PassengerCar&&b.PassengerCar)&&a.Position.Distance(b.Position)<14&&!Exempt(sim,a,b)&&MachineExitTests.Penetration(a,b)>0)
                return $"V{a.Id} {a.Status} ({a.Position.X:F2},{a.Position.Z:F2}) '{a.Cause}' x V{b.Id} {b.Status} ({b.Position.X:F2},{b.Position.Z:F2}) '{b.Cause}' pen={MachineExitTests.Penetration(a,b):F3}";
        return "";
    }

    [Theory]
    [InlineData("bend",.2)][InlineData("bend",.5)][InlineData("bend",2)]
    [InlineData("junction",.2)][InlineData("junction",.5)][InlineData("junction",2)]
    public void Trucks_share_bends_bays_and_junctions_without_contact_and_finish_every_reachable_work(string layout,double dt)
    {
        var sim=City(layout);var money=sim.MonetaryTotal;
        for(var t=0.0;t<6*3600&&Reachable(sim).Any(p=>p.Status!=WorkStatus.Completed);t+=dt){
            sim.Advance(dt);
            var contact=Contact(sim);Assert.True(contact.Length==0,$"t={t+dt:F1}: {contact} | {PavingTests.Diagnostic(sim)}");
        }
        Assert.All(Reachable(sim),p=>Assert.True(p.Status==WorkStatus.Completed,$"work {p.Id} {p.Status} '{p.Cause}' | {PavingTests.Diagnostic(sim)}"));
        Assert.Equal(money,sim.MonetaryTotal);sim.CheckInvariants();
    }

    // A save taken while a truck really waits for a shared space continues exactly
    // as the uninterrupted city: the waits and owners are persisted state.
    [Fact]
    public void A_save_during_the_waits_continues_identically()
    {
        var a=BendAndJunction();
        static bool Waits(Vehicle v)=>!v.PassengerCar&&v.Status==VehicleStatus.Travelling&&v.WaitingSince>0&&(v.Cause.StartsWith("Fila para acesso de carga")||v.Cause.StartsWith("Aguardando liberação da curva")||v.Cause.StartsWith("Aguardando espaço para acessar a faixa"));
        static bool Admitted(Vehicle v)=>v.ManoeuvreDock!=null||v.ManoeuvreSpaces.Count>0;
        var t=0.0;for(;t<6*3600&&!(a.State.Vehicles.Any(Waits)&&a.State.Vehicles.Any(Admitted));t+=.5)a.Advance(.5);
        Assert.True(a.State.Vehicles.Any(Waits)&&a.State.Vehicles.Any(Admitted),"no wait for a shared space happened | "+PavingTests.Diagnostic(a));
        var b=CitySimulation.FromJson(a.SaveToJson());
        Assert.Equal(a.State.Vehicles.Select(v=>(v.Id,v.ManoeuvreDock,v.ManoeuvreDeparture,string.Join(",",v.ManoeuvreSpaces))),b.State.Vehicles.Select(v=>(v.Id,v.ManoeuvreDock,v.ManoeuvreDeparture,string.Join(",",v.ManoeuvreSpaces))));
        for(var s=0.0;s<1800;s+=.5){a.Advance(.5);b.Advance(.5);}
        Assert.Equal(a.SaveToJson(),b.SaveToJson());
        b.CheckInvariants();
    }

    [Fact]
    public void Save_during_unloading_requires_a_new_exit_admission_and_preserves_a_stranded_owner()
    {
        var a=BendAndJunction();
        for(var t=0;t<3600&&!a.State.Vehicles.Any(v=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Unloading&&v.ManoeuvreDock!=null);t++)a.Advance(.5);
        var truck=a.State.Vehicles.First(v=>v.Machine==PavingMachine.None&&v.Status==VehicleStatus.Unloading&&v.ManoeuvreDock!=null);
        Assert.False(truck.ManoeuvreDeparture);
        var b=CitySimulation.FromJson(a.SaveToJson());var sawExit=false;
        for(var t=0;t<3600;t++){
            a.Advance(.5);b.Advance(.5);
            if(truck.ManoeuvreDock!=null&&truck.ManoeuvreDeparture==true){
                sawExit=true;
                // An interrupted admitted manoeuvre remains occupied on load.
                var interrupted=CitySimulation.FromJson(a.SaveToJson());var owner=interrupted.State.Vehicles.Single(v=>v.Id==truck.Id);
                owner.Status=VehicleStatus.Stranded;owner.BrokenDown=true;
                var loaded=CitySimulation.FromJson(interrupted.SaveToJson());
                Assert.Equal(owner.ManoeuvreDock,loaded.State.Vehicles.Single(v=>v.Id==owner.Id).ManoeuvreDock);
                Assert.Equal(owner.ManoeuvreSpaces,loaded.State.Vehicles.Single(v=>v.Id==owner.Id).ManoeuvreSpaces);
                interrupted.Advance(.5);loaded.Advance(.5);Assert.Equal(interrupted.SaveToJson(),loaded.SaveToJson());
                break;
            }
        }
        Assert.True(sawExit,"the return never obtained its own admission");
        for(var t=0;t<3600;t++){a.Advance(.5);b.Advance(.5);}
        Assert.Equal(a.SaveToJson(),b.SaveToJson());
        Assert.All(a.State.Projects,p=>Assert.Equal(WorkStatus.Completed,p.Status));a.CheckInvariants();
    }
}
