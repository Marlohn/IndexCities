using IndexCities.Simulation;
static class Sweep
{
    // Returns null when the layout cannot be placed for this seed.
    public static CitySimulation? Build(string shape,int seed)
    {
        var sim=shape.StartsWith("two")?new CitySimulation(seed,new(){ExternalPavingTeams=2}):new CitySimulation(seed);
        if(!sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success)return null;
        bool R(Point a,Point b,bool rev=false)=>sim.PlaceRoad(a,b,rev).Success;
        bool H(Point at,double rot=0)=>sim.PlaceBuilding(BuildingKind.House,at,rot).Success;
        switch(shape){
            case "straight": if(!R(new(-192,0),new(-40,0))||!H(new(-100,14)))return null;break;
            case "L": if(!R(new(-192,0),new(-100,64))||!H(new(-86,40),Math.PI/2))return null;break;
            case "cornercity": if(!R(new(-192,0),new(-128,132),true)||!H(new(-190,146)))return null;break;
            case "corner": if(!R(new(-192,0),new(-128,132),true)||!H(new(-190,146))||!R(new(-192,0),new(-120,0)))return null;break;
            case "T": if(!R(new(-192,0),new(-80,0))||!R(new(-124,0),new(-124,64))||!H(new(-110,40),Math.PI/2))return null;break;
            case "lake":{
                var west=sim.State.LakeBoundary!.MinBy(p=>p.X);var north=sim.State.LakeBoundary!.MinBy(p=>p.Z);
                var house=new Point(Math.Floor((west.X-8-.1)/2)*2,Math.Round(west.Z/2)*2);
                var roadX=Math.Floor((house.X-14)/4)*4;var roadZ=Math.Round(house.Z/4)*4;
                var shoreZ=Math.Floor((north.Z-sim.State.Tuning.RoadWidth/2-.05)/4)*4;var shoreX=Math.Round((north.X+16)/4)*4;
                if(!R(new(-192,0),new(roadX,roadZ))||!R(new(roadX,0),new(shoreX,shoreZ),true)||!H(house,Math.PI/2))return null;break;}
            // Several streets and houses at once: queues at the trunk and corners.
            case "busy": if(!R(new(-192,0),new(40,0)))return null;foreach(var x in new[]{-128,-108,-88,-20,0,20})if(!H(new(x,14)))return null;if(!R(new(-60,0),new(-20,48),true)||!R(new(-120,0),new(-120,-48))||!H(new(-106,-30),Math.PI/2))return null;break;
            case "twoteams": if(!R(new(-192,0),new(40,0))||!H(new(-100,14))||!R(new(-60,0),new(-20,48),true)||!R(new(-120,0),new(-120,-48))||!H(new(-106,-30),Math.PI/2))return null;break;
            default: throw new ArgumentException(shape);
        }
        return sim;
    }

    // Runs until every work completes; reports the first indefinite stop.
    public static string? Run(CitySimulation sim,double limit=14400,double stall=900)
    {
        var still=new Dictionary<int,(Point At,double Since)>();
        while(sim.State.ElapsedSeconds<limit){
            sim.Advance(2);
            foreach(var m in sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None&&v.Status==VehicleStatus.Parked))foreach(var o in sim.State.Vehicles.Where(o=>o.Id!=m.Id&&o.Status!=VehicleStatus.Outside&&o.Position.Distance(m.Position)<12))if(Bodies.Penetration(m,o)>0)return $"t={sim.State.ElapsedSeconds}: OVERLAP V{m.Id} {m.Machine} x V{o.Id} {o.Status} depth={Bodies.Penetration(m,o):F2} at {m.Position}/{o.Position}";
            foreach(var v in sim.State.Vehicles){
                var moving=v.Status==VehicleStatus.Travelling||v.Machine!=PavingMachine.None&&v.MachinePhase==MachinePhase.LeavingSite;
                if(!moving){still.Remove(v.Id);continue;}
                if(!still.TryGetValue(v.Id,out var s)||s.At!=v.Position){still[v.Id]=(v.Position,sim.State.ElapsedSeconds);continue;}
                if(sim.State.ElapsedSeconds-s.Since>stall)return $"t={sim.State.ElapsedSeconds}: V{v.Id} {v.Machine} {v.MachinePhase} {v.Status} stopped since {s.Since} at {v.Position}: {v.Cause}";
            }
            if(sim.State.Projects.All(p=>p.Status==WorkStatus.Completed)&&sim.State.Vehicles.Where(v=>v.Machine!=PavingMachine.None).All(v=>v.Status==VehicleStatus.Outside))return null;
        }
        return $"t={sim.State.ElapsedSeconds}: incomplete "+string.Join("; ",sim.State.Projects.Where(p=>p.Status!=WorkStatus.Completed).Select(p=>$"P{p.Id} {p.Status} {p.Cause}"));
    }
}
static class Bodies
{
    static double HalfLength(Vehicle v)=>v.PassengerCar?2.1:v.Machine==PavingMachine.Paver?2.31:v.Machine==PavingMachine.Roller?1.99:3.6;
    static double HalfWidth(Vehicle v)=>v.PassengerCar?.9:v.Machine==PavingMachine.Paver?1.27:v.Machine==PavingMachine.Roller?1.16:1.45;
    static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
    // Physical penetration depth (no safety margin); <=0 means apart.
    public static double Penetration(Vehicle a,Vehicle b)
    {
        var af=new Point(Math.Sin(a.Heading),Math.Cos(a.Heading));var ar=new Point(af.Z,-af.X);
        var bf=new Point(Math.Sin(b.Heading),Math.Cos(b.Heading));var br=new Point(bf.Z,-bf.X);var delta=b.Position-a.Position;var min=double.MaxValue;
        foreach(var axis in new[]{af,ar,bf,br})min=Math.Min(min,HalfLength(a)*Math.Abs(Dot(af,axis))+HalfWidth(a)*Math.Abs(Dot(ar,axis))+HalfLength(b)*Math.Abs(Dot(bf,axis))+HalfWidth(b)*Math.Abs(Dot(br,axis))-Math.Abs(Dot(delta,axis)));
        return min;
    }
}
