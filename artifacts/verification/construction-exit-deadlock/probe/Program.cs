using IndexCities.Simulation;
// Usage: ExitProbe <seed> [traceFrom] [traceTo] [step]   (lake layout trace)
//        ExitProbe sweep <seeds> <shape>...               (stall/overlap sweep)
//        ExitProbe trace <shape> <seed> <from> <to>
if(args.Length>0&&args[0]=="trace"){Trace.Run(args[1],int.Parse(args[2]),double.Parse(args[3]),double.Parse(args[4]));return;}
if(args.Length>0&&args[0]=="sweep"){var n=int.Parse(args[1]);var fails=0;var ran=0;var watch=System.Diagnostics.Stopwatch.StartNew();foreach(var shape in args.Skip(2)){var skipped=0;for(var s=0;s<n;s++){var c=Sweep.Build(shape,s*37+1);if(c==null){skipped++;continue;}ran++;var r=Sweep.Run(c);if(r!=null){fails++;Console.WriteLine($"FAIL {shape} seed={s*37+1} {r}");}}Console.WriteLine($"{shape}: skipped {skipped}");}Console.WriteLine($"ran={ran} fails={fails} ms={watch.ElapsedMilliseconds}");return;}
var seed=args.Length>0?int.Parse(args[0]):2877;var from=args.Length>1?double.Parse(args[1]):1e9;var to=args.Length>2?double.Parse(args[2]):0;var step=args.Length>3?double.Parse(args[3]):2;
var sim=new CitySimulation(seed);
if(!sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16)).Success)throw new("hall");
var west=sim.State.LakeBoundary!.MinBy(p=>p.X);var north=sim.State.LakeBoundary!.MinBy(p=>p.Z);
var house=new Point(Math.Floor((west.X-8-.1)/2)*2,Math.Round(west.Z/2)*2);
var roadX=Math.Floor((house.X-14)/4)*4;var roadZ=Math.Round(house.Z/4)*4;
var shoreZ=Math.Floor((north.Z-sim.State.Tuning.RoadWidth/2-.05)/4)*4;var shoreX=Math.Round((north.X+16)/4)*4;
Console.WriteLine($"road1 (-192,0)->({roadX},{roadZ}) road2 ({roadX},0)->({shoreX},{shoreZ}) house {house}");
if(!sim.PlaceRoad(new(-192,0),new(roadX,roadZ)).Success)throw new("r1");
if(!sim.PlaceRoad(new(roadX,0),new(shoreX,shoreZ),true).Success)throw new("r2");
var h=sim.PlaceBuilding(BuildingKind.House,house,Math.PI/2);if(!h.Success)throw new("house "+h.Message);
string V(Vehicle v)=>$"V{v.Id} {v.Machine}/{v.Body} {v.MachinePhase} {v.Status} ({v.Position.X:F2},{v.Position.Z:F2}) h={v.Heading:F2} r{v.RouteIndex}/{v.Route.Count} sp{v.SitePathIndex}/{v.SitePath.Count} ws={v.WaitingSince:F0}: {v.Cause}";
for(var t=0.0;t<14400;t+=step){sim.Advance(step);
  if(sim.State.ElapsedSeconds>=from&&sim.State.ElapsedSeconds<=to){Console.WriteLine($"--- t={sim.State.ElapsedSeconds}");foreach(var v in sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside&&!v.PassengerCar))Console.WriteLine("  "+V(v));}
  if(sim.State.Projects.All(p=>p.Status==WorkStatus.Completed)){Console.WriteLine($"ALL COMPLETED t={sim.State.ElapsedSeconds}");break;}}
foreach(var p in sim.State.Projects)Console.WriteLine($"P{p.Id} {p.Status} {p.Progress:F2} {p.AppliedRoadPoints.Count}/{p.RolledRoadPoints.Count}/{p.RoadPoints.Count}: {p.Cause}");
foreach(var v in sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside))Console.WriteLine(V(v));
