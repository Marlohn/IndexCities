using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

// A paving machine leaving its site and the trucks/cars on the same lanes must
// never wait on each other forever, and never pass through one another.
public sealed class MachineExitTests
{
    static CitySimulation? Build(string shape,int seed)
    {
        var sim=shape=="two-teams"?new CitySimulation(seed,new(){ExternalPavingTeams=2}):new CitySimulation(seed);
        if(!sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success)return null;
        bool R(Point a,Point b,bool reverse=false)=>sim.PlaceRoad(a,b,reverse).Success;
        bool H(Point at,double rotation=0)=>sim.PlaceBuilding(BuildingKind.House,at,rotation).Success;
        var placed=shape switch{
            "straight"=>R(new(-192,0),new(-40,0))&&H(new(-100,14)),
            "L"=>R(new(-192,0),new(-100,64))&&H(new(-86,40),Math.PI/2),
            "T"=>R(new(-192,0),new(-80,0))&&R(new(-124,0),new(-124,64))&&H(new(-110,40),Math.PI/2),
            // CornerCity elbow beside a house, plus the trunk to the hall.
            "corner"=>R(new(-192,0),new(-128,132),true)&&H(new(-190,146))&&R(new(-192,0),new(-120,0)),
            "two-teams"=>R(new(-192,0),new(40,0))&&H(new(-100,14))&&R(new(-60,0),new(-20,48),true)&&R(new(-120,0),new(-120,-48))&&H(new(-106,-30),Math.PI/2),
            "busy"=>R(new(-192,0),new(40,0))&&new[]{-128,-108,-88,-20,0,20}.All(x=>H(new(x,14)))&&R(new(-60,0),new(-20,48),true)&&R(new(-120,0),new(-120,-48))&&H(new(-106,-30),Math.PI/2),
            "lake"=>Lake(sim),
            _=>throw new ArgumentException(shape)};
        return placed?sim:null;
    }

    // Same layout as the rendered --lake-recovery-smoke build sequence.
    static bool Lake(CitySimulation sim)
    {
        var west=sim.State.LakeBoundary!.MinBy(p=>p.X);var north=sim.State.LakeBoundary!.MinBy(p=>p.Z);
        var house=new Point(Math.Floor((west.X-8-.1)/2)*2,Math.Round(west.Z/2)*2);
        var roadX=Math.Floor((house.X-14)/4)*4;var roadZ=Math.Round(house.Z/4)*4;
        var shoreZ=Math.Floor((north.Z-sim.State.Tuning.RoadWidth/2-.05)/4)*4;var shoreX=Math.Round((north.X+16)/4)*4;
        return sim.PlaceRoad(new(-192,0),new(roadX,roadZ)).Success&&sim.PlaceRoad(new(roadX,0),new(shoreX,shoreZ),true).Success&&sim.PlaceBuilding(BuildingKind.House,house,Math.PI/2).Success;
    }

    static double HalfLength(Vehicle v)=>v.PassengerCar?2.1:v.Machine==PavingMachine.Paver?2.31:v.Machine==PavingMachine.Roller?1.99:3.6;
    static double HalfWidth(Vehicle v)=>v.PassengerCar?.9:v.Machine==PavingMachine.Paver?1.27:v.Machine==PavingMachine.Roller?1.16:1.45;

    // Physical penetration of two rendered bodies (no safety margin).
    static double Penetration(Vehicle a,Vehicle b)
    {
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        var af=new Point(Math.Sin(a.Heading),Math.Cos(a.Heading));var ar=new Point(af.Z,-af.X);
        var bf=new Point(Math.Sin(b.Heading),Math.Cos(b.Heading));var br=new Point(bf.Z,-bf.X);var delta=b.Position-a.Position;
        return new[]{af,ar,bf,br}.Min(axis=>HalfLength(a)*Math.Abs(Dot(af,axis))+HalfWidth(a)*Math.Abs(Dot(ar,axis))+HalfLength(b)*Math.Abs(Dot(bf,axis))+HalfWidth(b)*Math.Abs(Dot(br,axis))-Math.Abs(Dot(delta,axis)));
    }

    // Runs the ordinary 2 s quantum until every work is finished and the
    // external machines are back outside; returns why it did not get there.
    static string? Finish(CitySimulation sim,double limit=14400,double stall=900)
    {
        var still=new Dictionary<int,(Point At,double Since)>();
        while(sim.State.ElapsedSeconds<limit){
            sim.Advance(2);
            foreach(var machine in sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.Status==VehicleStatus.Parked))
                foreach(var other in sim.State.Vehicles.Where(o=>o.Id!=machine.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(machine.Position)<12))
                    if(Penetration(machine,other)>0)return $"t={sim.State.ElapsedSeconds}: V{machine.Id} and V{other.Id} overlap | {PavingTests.Diagnostic(sim)}";
            foreach(var v in sim.State.Vehicles){
                if(v.Status!=VehicleStatus.Travelling&&v.MachinePhase!=MachinePhase.LeavingSite){still.Remove(v.Id);continue;}
                if(!still.TryGetValue(v.Id,out var s)||s.At!=v.Position){still[v.Id]=(v.Position,sim.State.ElapsedSeconds);continue;}
                if(sim.State.ElapsedSeconds-s.Since>stall)return $"t={sim.State.ElapsedSeconds}: V{v.Id} stopped since {s.Since} | {PavingTests.Diagnostic(sim)}";
            }
            if(sim.State.Projects.All(p=>p.Status==WorkStatus.Completed)&&sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).All(v=>v.Status==VehicleStatus.Outside)){sim.CheckInvariants();return null;}
        }
        return "unfinished | "+PavingTests.Diagnostic(sim);
    }

    [Fact]
    public void A_machine_stranded_without_fuel_while_leaving_holds_no_path()
    {
        // Cancelling a street whose paver ran dry sends it to LeavingSite; with
        // no fuel it can never move, so it must not hold its whole exit path.
        var sim=Build("corner",42)!;Vehicle? Paver()=>sim.State.Vehicles.FirstOrDefault(v=>v.Machine==PavingMachine.Paver&&v.MachinePhase==MachinePhase.OnSite);
        for(var i=0;i<4000&&Paver()==null;i++)sim.Advance(1);
        var paver=Paver();Assert.True(paver!=null,PavingTests.Diagnostic(sim));
        paver.Fuel=0;Assert.True(sim.CancelProject(paver.MachineProjectId!.Value).Success);sim.Advance(2);
        Assert.Equal(MachinePhase.LeavingSite,paver.MachinePhase);Assert.True(paver.SitePathIndex<paver.SitePath.Count,PavingTests.Diagnostic(sim));
        var reservation=typeof(CitySimulation).GetMethod("MachineReservation",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        Assert.Empty((System.Collections.ICollection)reservation.Invoke(sim,[paver])!);
    }

    [Fact]
    public void A_truck_following_the_leaving_roller_waits_clear_of_its_turn_and_the_lake_streets_finish()
    {
        // Seed 2877 of --lake-recovery-smoke: the dump truck closed up behind
        // the roller turning out of the site; the roller's turn then touched it.
        var sim=Build("lake",2877)!;
        Assert.Null(Finish(sim));
        Assert.All(sim.State.Buildings,b=>Assert.True(b.Completed&&sim.HasExteriorAccess(b),PavingTests.Diagnostic(sim)));
    }

    public static TheoryData<string,int[]> Layouts=>new(){
        // Lake seeds include every geometry that froze or overlapped before:
        // follower behind the turn, oncoming truck at the frontage crossing,
        // left/right turns meeting at the corner, a truck turning into a roller.
        {"lake",[1,408,593,1074,1111,1148,1296,1518,1925,2036,2147,2184,2221,2332,2628,3294,4182,4219,4404,5551,6069,6550,6883,7697]},
        {"straight",[1,38,75]},{"L",[1,38,75]},{"T",[1,38,75]},{"corner",[1,38,75]},{"two-teams",[1,38,75]},{"busy",[1,38,75]},
    };

    [Theory]
    [MemberData(nameof(Layouts))]
    public void Leaving_machines_and_street_traffic_never_wait_on_each_other_forever(string shape,int[] seeds)
    {
        foreach(var seed in seeds){
            var sim=Build(shape,seed);Assert.True(sim!=null,$"{shape} seed {seed} could not be placed");
            var failure=Finish(sim!);Assert.True(failure==null,$"{shape} seed {seed}: {failure}");
        }
    }
}
