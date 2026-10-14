using System.Text.Json.Nodes;
using IndexCities.Simulation;
using Xunit;
namespace IndexCities.Simulation.Tests;

public sealed class LakeGeometryTests
{
    [Fact] public void New_lake_is_irregular_deterministic_and_does_not_consume_simulation_randomness()
    {
        foreach(var seed in new[]{int.MinValue,-1,0,42,1412,2877,int.MaxValue}){
            var a=new CitySimulation(seed);var b=new CitySimulation(seed);var polygon=a.State.LakeBoundary!;
            Assert.Equal(2,a.State.SchemaVersion);Assert.Equal(128,polygon.Count);Assert.Equal(a.SaveToJson(),b.SaveToJson());
            Assert.True(polygon.Max(p=>p.Distance(a.State.LakeCentre))-polygon.Min(p=>p.Distance(a.State.LakeCentre))>5);
            var random=a.State.RandomState;Assert.Equal(polygon,LakeGeometry.GenerateBoundary(seed,a.State.LakeCentre,a.State.LakeRadius));Assert.Equal(random,a.State.RandomState);
            Assert.All(polygon,p=>{Assert.Equal(p.X,(double)(float)p.X);Assert.Equal(p.Z,(double)(float)p.Z);});
        }
    }

    [Fact] public void Polygon_is_the_saved_authority_and_roundtrip_preserves_following_simulation()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        sim.Advance(120);var json=sim.SaveToJson();var loaded=CitySimulation.FromJson(json);
        Assert.Equal(json,loaded.SaveToJson());Assert.Equal(sim.State.LakeBoundary,loaded.State.LakeBoundary);
        sim.Advance(120);loaded.Advance(120);Assert.Equal(sim.SaveToJson(),loaded.SaveToJson());
        var before=loaded.State.LakeBoundary!.ToList();loaded.State.Seed=1412;
        var renamed=CitySimulation.FromJson(loaded.SaveToJson());Assert.Equal(before,renamed.State.LakeBoundary);
    }

    [Fact] public void Legacy_circle_save_remains_schema_one_and_keeps_placement_and_intake_rules()
    {
        var sim=new CitySimulation(42);sim.State.SchemaVersion=1;sim.State.LakeBoundary=null;
        var before=sim.SaveToJson();Assert.DoesNotContain("LakeBoundary",before);var loaded=CitySimulation.FromJson(before);
        Assert.Equal(before,loaded.SaveToJson());Assert.Equal(1,loaded.State.SchemaVersion);Assert.Null(loaded.State.LakeBoundary);
        var p=sim.State.LakeCentre+new Point(sim.State.LakeRadius+21.999,0);
        Assert.True(loaded.HasNaturalWaterSource(new(){Kind=BuildingKind.WaterIntake,Position=p}));
        Assert.False(loaded.HasNaturalWaterSource(new(){Kind=BuildingKind.WaterIntake,Position=p+new Point(.002,0)}));
        Assert.False(loaded.QuoteBuilding(BuildingKind.CityHall,sim.State.LakeCentre).PhysicallyValid);
    }

    [Fact] public void New_water_queries_follow_the_polygon_instead_of_the_nominal_circle()
    {
        var sim=new CitySimulation(42);var centre=sim.State.LakeCentre;
        var inward=sim.State.LakeBoundary!.MinBy(p=>p.Distance(centre));var outward=sim.State.LakeBoundary!.MaxBy(p=>p.Distance(centre));
        var dry=inward.MoveTowards(centre,-1); // one metre outward from the indentation
        Assert.True(dry.Distance(centre)<sim.State.LakeRadius);Assert.True(LakeGeometry.SignedDistance(sim.State,dry)>0);
        var wet=outward.MoveTowards(centre,1);Assert.True(wet.Distance(centre)>sim.State.LakeRadius);Assert.True(LakeGeometry.SignedDistance(sim.State,wet)<0);
        Assert.False(sim.QuoteBuilding(BuildingKind.CityHall,wet).PhysicallyValid);
    }

    [Fact] public void Intake_uses_actual_shore_and_queries_do_not_grant_capacity_or_access()
    {
        var sim=new CitySimulation(42);var north=sim.State.LakeBoundary!.MinBy(p=>p.Z);
        var near=north+new Point(0,-21.9);var far=north+new Point(0,-22.1);
        var intake=new Building{Id=10000,Kind=BuildingKind.WaterIntake,Position=near};
        Assert.True(sim.QuoteBuilding(BuildingKind.CityHall,near).PhysicallyValid);Assert.True(sim.HasNaturalWaterSource(intake));
        Assert.False(sim.HasExteriorAccess(intake));Assert.Equal(0,intake.OperationalCapacity);var json=sim.SaveToJson();
        Assert.True(CitySimulation.FromJson(json).HasNaturalWaterSource(intake));Assert.Equal(json,sim.SaveToJson());
        intake.Position=far;Assert.False(sim.HasNaturalWaterSource(intake));
    }

    [Fact] public void Footprint_detects_water_crossing_edges_with_dry_corners_and_honours_rotation()
    {
        var state=new CitySimulation(42).State;state.LakeCentre=new(0,0);
        state.LakeBoundary=[new(-10,-1),new(0,-1),new(10,-1),new(10,0),new(10,1),new(0,1),new(-10,1),new(-10,0)];
        LakeGeometry.Validate(state);
        foreach(var x in new[]{-2,2})foreach(var z in new[]{-2,2})Assert.True(LakeGeometry.SignedDistance(state,new(x,z))>0);
        Assert.True(LakeGeometry.OverlapsRectangle(state,new(0,0),4,4));
        Assert.True(LakeGeometry.OverlapsRectangle(state,new(0,2),6,1,Math.PI/4));
        Assert.False(LakeGeometry.OverlapsRectangle(state,new(0,2),6,1));
        Assert.False(LakeGeometry.OverlapsRectangle(state,new(0,1.5),6,1)); // edge contact only
    }

    [Fact] public void Roads_and_buildings_reject_water_without_reserving_money_or_materials()
    {
        var sim=new CitySimulation(42);Assert.True(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success);
        var lake=sim.State.LakeCentre;var before=sim.SaveToJson();
        Assert.False(sim.PlaceBuilding(BuildingKind.House,lake).Success);
        Assert.False(sim.PlaceRoad(sim.SnapRoad(lake+new Point(-64,0)),sim.SnapRoad(lake+new Point(64,0))).Success);
        Assert.Equal(before,sim.SaveToJson());
    }

    [Fact] public void Unsupported_or_incomplete_geography_is_rejected_instead_of_regenerated()
    {
        var json=JsonNode.Parse(new CitySimulation(42).SaveToJson())!.AsObject();
        json.Remove("LakeBoundary");Assert.Throws<InvalidDataException>(()=>CitySimulation.FromJson(json.ToJsonString()));
        json["SchemaVersion"]=3;Assert.Throws<InvalidDataException>(()=>CitySimulation.FromJson(json.ToJsonString()));
        var bad=new CitySimulation(42);bad.State.LakeBoundary!.Reverse();Assert.Throws<InvalidDataException>(()=>bad.SaveToJson());
    }
}
