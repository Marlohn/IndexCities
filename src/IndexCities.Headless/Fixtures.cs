using System.Reflection;
using IndexCities.Simulation;

namespace IndexCities.Headless;

// Measurement cities. Not game content: the grid completes its works instantly
// and scales utility capacities so a few plants serve a large synthetic city.
static class Fixtures
{
    static bool _scaled;
    static readonly Queue<(Point Position,double Rotation)> _pendingHouses=new();
    // Same layout as CityCycleTests.City(), with real construction until done.
    public static CitySimulation Cycle(int seed)
    {
        var sim=new CitySimulation(seed);Require(sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)));
        Require(sim.PlaceRoad(new(-192,0),new(240,0)));sim.Advance(14400);
        Require(sim.PlaceRoad(new(-184,0),new(116,44),true));sim.Advance(7200);
        foreach(var x in new[]{-128,-108,-88,-68,-48,-28,-8,12,32,52,72,92,112,132,152,172,192,212})Require(sim.PlaceBuilding(BuildingKind.House,new(x,14)));
        foreach(var x in new[]{-160,-140,-120,-100,-80,-60,-40,-20,0,20,40,60,80,100})Require(sim.PlaceBuilding(BuildingKind.House,new(x,58)));
        Require(sim.PlaceBuilding(BuildingKind.WorksDepot,new(-160,-18),Math.PI));Require(sim.PlaceBuilding(BuildingKind.Grocery,new(-124,-16),Math.PI));
        Require(sim.PlaceBuilding(BuildingKind.FuelStation,new(-88,-20),Math.PI));Require(sim.PlaceBuilding(BuildingKind.Farm,new(-44,-28),Math.PI));
        Require(sim.PlaceBuilding(BuildingKind.WindPower,new(0,-16),Math.PI));Require(sim.PlaceBuilding(BuildingKind.WindPower,new(120,-16),Math.PI));
        Require(sim.PlaceBuilding(BuildingKind.WaterTreatment,new(40,-20),Math.PI));Require(sim.PlaceBuilding(BuildingKind.SewageTreatment,new(80,-20),Math.PI));
        var lake=sim.State.LakeCentre;var z=Math.Round((lake.Z-sim.State.LakeRadius-30)/4)*4;
        // Seed 43 puts the original intake access through the first house row.
        // Keep the original fixture whenever valid; use the clear gap otherwise.
        if(!sim.QuoteRoad(new(240,0),new(132,z),true).PhysicallyValid)z=32;
        Require(sim.PlaceRoad(new(240,0),new(132,z),true));sim.Advance(14400);
        Require(sim.PlaceBuilding(BuildingKind.WaterIntake,new(144,z+14)));
        for(var i=0;i<240&&sim.State.Projects.Any(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress&&!p.IsMaintenance);i++)sim.Advance(3600);
        return sim;
    }

    // PR57-only representative-load probe. Requests ordinary house projects:
    // no private completion, money injection, capacity scaling or forced residents.
    // Failed quotes/orders remain observable rather than silently creating houses.
    public static (int Placed,int Attempted) QueueCycleHousingProjects(CitySimulation sim,int requested)
    {
        var placed=0;var attempted=0;
        foreach(var (z,rotation) in new[]{(30.0,Math.PI),(-14.0,Math.PI)})
        {
            for(var x=-176.0;x<=216&&placed<requested;x+=20)
            {
                var position=new Point(x,z);
                if(!sim.QuoteBuilding(BuildingKind.House,position,rotation).PhysicallyValid)continue;
                attempted++;
                if(sim.PlaceBuilding(BuildingKind.House,position,rotation).Success)placed++;
            }
            if(placed>=requested)break;
        }
        return (placed,attempted);
    }

    // Grid of horizontal streets (60 m apart) on a west spine, connectors every
    // 320 m, houses north of each street and workplaces alternating with houses
    // south of it; lake east of the main street for the intakes.
    public static (CitySimulation Sim,string Info) Grid(int seed,int rows,double length,bool signals,bool staged=false,double utilityScale=50)
    {
        // Mutates the static catalog of this process only; guarded against scaling twice.
        if(_scaled)throw new InvalidOperationException("Grid só pode ser criada uma vez por processo");_scaled=true;
        var defs=(Dictionary<BuildingKind,BuildingDefinition>)Catalog.Definitions;
        foreach(var k in new[]{BuildingKind.WaterIntake,BuildingKind.WaterTreatment,BuildingKind.SewageTreatment,BuildingKind.WindPower})defs[k]=defs[k] with {Capacity=defs[k].Capacity*utilityScale};
        const double rowGap=60;var half=(rows-1)/2;
        // Road nodes sit on a 4 m grid from the entry: keep the half-size a multiple
        // of 4 so the highway aligns with city streets (as in the default 256 m map).
        var H=Math.Ceiling(Math.Max(256,Math.Max((half+1)*rowGap+60,(72+length+260)/2+40))/4)*4;var x0=-H+72;
        var houses=(int)(rows*length/20*1.6)+50;
        var tuning=new SimulationTuning{MapHalfSize=H,TotalMoney=100_000_000_000m,CityInitialMoney=2_000_000_000m,ExternalFamilyCandidates=(int)(houses*1.5)+40,ExternalCompanyCandidates=houses,ExternalTruckCount=Math.Max(6,houses/12)};
        // Staged growth reaches maps larger than V1 (256 m half-size): scale the
        // tank so a round trip still fits. The unstaged grid keeps its old tuning.
        if(staged)tuning.FuelTank*=Math.Max(1,H/256);
        var sim=new CitySimulation(seed,tuning);var s=sim.State;
        var lakeX=x0+length+130;s.LakeCentre=new(lakeX,0);s.LakeRadius=36;s.LakeBoundary=LakeGeometry.GenerateBoundary(seed,s.LakeCentre,s.LakeRadius);
        var fails=0;
        void Road(Point a,Point b){if(!sim.PlaceRoad(a,b).Success)fails++;}
        bool Place(BuildingKind k,Point p,double rot){var ok=sim.PlaceBuilding(k,p,rot).Success;if(!ok)fails++;return ok;}
        // Staged: houses are only queued; AddHouses places and completes them later.
        bool House(Point p,double rot){if(!staged)return Place(BuildingKind.House,p,rot);if(!sim.QuoteBuilding(BuildingKind.House,p,rot).PhysicallyValid){fails++;return false;}_pendingHouses.Enqueue((p,rot));return true;}
        Place(BuildingKind.CityHall,new(x0+32,16),0);Road(new(-H+64,0),new(x0,0));
        var zMin=-half*rowGap;var zMax=(rows-1-half)*rowGap;
        if(rows>1){Road(new(x0,0),new(x0,zMax));Road(new(x0,0),new(x0,zMin));}
        var connectors=new List<double>();for(var x=x0+320;x<x0+length-40;x+=320)connectors.Add(Math.Round(x/4)*4);
        for(var r=0;r<rows;r++){var z=zMin+r*rowGap;Road(new(x0,z),new(x0+length,z));}
        for(var gap=8.0;gap<40;gap+=4)if(sim.PlaceRoad(new(x0+length,0),new(Math.Round((lakeX-36-gap)/4)*4,0)).Success)break;
        foreach(var cx in connectors)if(rows>1){Road(new(cx,0),new(cx,zMax));Road(new(cx,0),new(cx,zMin));}
        Place(BuildingKind.WorksDepot,new(x0+24,-18),Math.PI);
        var kinds=new[]{BuildingKind.Grocery,BuildingKind.Grocery,BuildingKind.FuelStation,BuildingKind.Grocery,BuildingKind.WindPower,BuildingKind.Grocery,BuildingKind.Grocery,BuildingKind.WaterTreatment,BuildingKind.Grocery,BuildingKind.SewageTreatment};
        int wi=0,nh=0,nw=0;
        bool NearConnector(double x,double w)=>connectors.Any(c=>Math.Abs(c-x)<w/2+8);
        for(var r=0;r<rows;r++){
            var z=zMin+r*rowGap;
            for(var x=x0+(z==0?60:24);x<x0+length-12;x+=20){if(!NearConnector(x,12)&&House(new(x,z+14),0))nh++;}
            var sx=x0+(z==0?56:24);var slot=0;
            while(sx<x0+length-16){
                if(slot%2==0){var k=kinds[wi%kinds.Length];var w=Catalog.Definitions[k].Width;var d=Catalog.Definitions[k].Depth;var cx=sx+w/2;if(!NearConnector(cx,w)&&cx+w/2<x0+length&&Place(k,new(cx,z-(d/2+6)),Math.PI)){nw++;wi++;}sx+=w+8;}
                else{var cx=sx+6;if(!NearConnector(cx,12)&&House(new(cx,z-14),Math.PI))nh++;sx+=20;}
                slot++;
            }
        }
        var intakes=0;
        for(var x=lakeX-40;x>lakeX-160&&intakes<2;x-=4)foreach(var (dz,rot) in new[]{(14.0,0.0),(-14.0,Math.PI)}){var p=new Point(x,dz);if(intakes<2&&sim.DistanceToLakeShore(p)<=CitySimulation.NaturalWaterSourceReach&&sim.QuoteBuilding(BuildingKind.WaterIntake,p,rot).PhysicallyValid&&Place(BuildingKind.WaterIntake,p,rot))intakes++;}
        CompleteAllWorks(sim);
        // Default right-hand priority at four-way junctions gridlocked these grids;
        // a signal is the control the player can choose there.
        if(signals)foreach(var n in s.Roads.Where(n=>!n.Highway&&n.Neighbours.Count>=4)){n.Control=JunctionControl.Signal;n.PlayerEdited=true;}
        var info=$"grid rows={rows} length={length} H={H} houses={nh} workplaces={nw} intakes={intakes} roadNodes={s.Roads.Count} jobs={s.Buildings.Where(b=>!b.Removed&&b.Kind!=BuildingKind.House).Sum(b=>b.Definition.Jobs)} placeFails={fails} signals={signals}";
        return (sim,info);
    }

    // Completes every ordered work at once through the simulation's own private
    // completion path (road nodes, graph, junctions, building completion).
    static void CompleteAllWorks(CitySimulation sim)
    {
        var addRoad=Private(sim,"AddRoad");var rebuild=Private(sim,"RebuildRoadGraph");var junctions=Private(sim,"SetDefaultJunctions");var complete=Private(sim,"CompleteConstructionProject");
        var open=sim.State.Projects.Where(p=>p.Status is WorkStatus.Waiting or WorkStatus.InProgress).ToList();
        foreach(var p in open.Where(p=>p.RoadPoints.Count>0))foreach(var pt in p.RoadPoints.Skip(p.BuiltRoadNodes.Count))p.BuiltRoadNodes.Add(((RoadNode)addRoad.Invoke(sim,[pt,false])!).Id);
        rebuild.Invoke(sim,[true]);junctions.Invoke(sim,null);
        foreach(var p in open.OrderBy(p=>p.RoadPoints.Count>0?0:1))complete.Invoke(sim,[p]);
        sim.CheckInvariants();
    }
    // Staged growth: houses appear over time, like a city that grows over days
    // instead of receiving every family at once. Returns houses still queued.
    public static int AddHouses(CitySimulation sim,int count)
    {
        var complete=Private(sim,"CompleteConstructionProject");
        for(var i=0;i<count&&_pendingHouses.Count>0;i++){var (p,rot)=_pendingHouses.Dequeue();var r=sim.PlaceBuilding(BuildingKind.House,p,rot);if(!r.Success)continue;complete.Invoke(sim,[sim.State.Projects.Single(w=>w.BuildingId==r.Id)]);}
        sim.CheckInvariants();return _pendingHouses.Count;
    }
    static MethodInfo Private(CitySimulation sim,string name)=>typeof(CitySimulation).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)??throw new InvalidOperationException($"Método {name} não encontrado; atualize Fixtures.cs");
    static void Require(CommandResult r){if(!r.Success)throw new InvalidOperationException("Fixture: "+r.Message);}
}
