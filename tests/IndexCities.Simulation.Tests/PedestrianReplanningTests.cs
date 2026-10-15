using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class PedestrianReplanningTests
{
    static (CitySimulation Sim,Building Home,Point Origin) Straight(double rotation,bool painted=true,bool connected=true)
    {
        Point Turn(Point p)=>new(p.X*Math.Cos(rotation)+p.Z*Math.Sin(rotation),40-p.X*Math.Sin(rotation)+p.Z*Math.Cos(rotation));
        var initial=new CitySimulation(42);
        for(var step=0;step<=15;step++)if(connected||step!=8)initial.State.Roads.Add(new(){Id=1000+step,Position=Turn(new(0,step*4)),Crosswalk=painted&&step==1});
        var home=new Building{Id=1100,Kind=BuildingKind.House,Position=Turn(new(-7,60)),Rotation=rotation,Completed=true};
        initial.State.Buildings.Add(home);
        return(new(initial.State),home,Turn(new(-.60088323307795,4.6)));
    }

    [Theory]
    [InlineData(0)] [InlineData(Math.PI/2)] [InlineData(Math.PI)] [InlineData(Math.PI*1.5)]
    public void Diagonal_origin_in_a_marked_straight_road_uses_the_real_crossing_axis(double rotation)
    {
        var (sim,home,origin)=Straight(rotation);var before=sim.SaveToJson();
        var route=sim.PedestrianRoute(origin,home);
        Assert.NotEmpty(route);Assert.Equal(origin,route[0]);Assert.Equal(home.Door,route[^1]);
        // Any sideways movement inside the carriageway must stay in the paint.
        Point Local(Point p){var delta=p-new Point(0,40);return new(delta.X*Math.Cos(rotation)-delta.Z*Math.Sin(rotation),delta.X*Math.Sin(rotation)+delta.Z*Math.Cos(rotation));}
        foreach(var pair in route.Zip(route.Skip(1))) {
            var a=Local(pair.First);var b=Local(pair.Second);
            if(Math.Abs(a.X-b.X)>.05&&Math.Min(a.X,b.X)<3.3&&Math.Max(a.X,b.X)>-3.3){Assert.InRange(a.Z,4-1.95,4+1.95);Assert.InRange(b.Z,4-1.95,4+1.95);}
        }
        Assert.Equal(before,sim.SaveToJson());sim.CheckInvariants();
    }

    [Fact]
    public void Marked_entrance_near_highway_approaches_the_shifted_paint_before_leaving_the_lane()
    {
        var initial=new CitySimulation(42);
        for(var step=1;step<=12;step++)initial.State.Roads.Add(new(){Id=1000+step,Position=new(-228,-step*4),Crosswalk=step==1});
        var home=new Building{Id=1100,Kind=BuildingKind.House,Position=new(-235,-30),Completed=true};initial.State.Buildings.Add(home);
        var sim=new CitySimulation(initial.State);var origin=new Point(-228.60088323307795,-4.6);
        var crossing=sim.State.Roads.Single(r=>r.Id==1001);var band=Assert.Single(CitySimulation.CrosswalkLocations(crossing,sim.State));
        Assert.Equal(-6.25,band.Centre.Z);var route=sim.PedestrianRoute(origin,home);Assert.NotEmpty(route);
        Assert.Equal(origin,route[0]);Assert.Equal(home.Door,route[^1]);
        Assert.Equal(origin.X,route[1].X);Assert.Equal(band.Centre.Z,route[1].Z);
        foreach(var pair in route.Zip(route.Skip(1)).Where(p=>Math.Abs(p.First.X-p.Second.X)>.05&&Math.Max(p.First.X,p.Second.X)>-231.3&&Math.Min(p.First.X,p.Second.X)<-224.7)) {
            Assert.InRange(pair.First.Z,band.Centre.Z-1.95,band.Centre.Z+1.95);Assert.InRange(pair.Second.Z,band.Centre.Z-1.95,band.Centre.Z+1.95);
        }
        sim.CheckInvariants();
    }

    [Fact]
    public void Replanning_does_not_invent_paint_or_bridge_a_disconnected_road()
    {
        var unmarked=Straight(0,painted:false);Assert.Empty(unmarked.Sim.PedestrianRoute(unmarked.Origin,unmarked.Home));
        var disconnected=Straight(0,connected:false);Assert.Empty(disconnected.Sim.PedestrianRoute(disconnected.Origin,disconnected.Home));
    }

    [Fact]
    public void Lane_recovery_does_not_approach_paint_on_a_disconnected_collinear_fragment()
    {
        var (sim,home,_)=Straight(0,painted:false,connected:false);
        sim.State.Roads.Single(r=>r.Id==1010).Crosswalk=true;
        Assert.Empty(sim.PedestrianRoute(new(-.6,68.6),home));
    }

    [Fact]
    public void Opposite_curbs_at_the_same_access_node_walk_to_paint_before_crossing()
    {
        var (sim,home,_)=Straight(0);
        var access=sim.Access(home)!;var origin=access.Position+new Point(4.55,0);
        var route=sim.PedestrianRoute(origin,home);Assert.NotEmpty(route);
        foreach(var pair in route.Zip(route.Skip(1))) {
            if(Math.Abs(pair.First.X-pair.Second.X)>.05&&Math.Min(pair.First.X,pair.Second.X)<3.3&&Math.Max(pair.First.X,pair.Second.X)>-3.3){
                Assert.InRange(pair.First.Z,44-1.95,44+1.95);Assert.InRange(pair.Second.Z,44-1.95,44+1.95);
            }
        }
        var sameCurb=sim.PedestrianRoute(access.Position-new Point(4.55,0),home);
        Assert.Equal(2,sameCurb.Count);Assert.Equal(home.Door,sameCurb[^1]);
    }
}
