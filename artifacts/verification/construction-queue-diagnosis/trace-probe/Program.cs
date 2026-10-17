using System.Reflection;
using IndexCities.Simulation;
// Diagnostic only: traces the corner deadlock without altering product code.
var sim=new CitySimulation(42);
sim.PlaceBuilding(BuildingKind.CityHall,new(-152,16));
sim.PlaceRoad(new(-192,0),new(-128,132),true);
sim.PlaceBuilding(BuildingKind.House,new(-190,146));
var overlap=typeof(CitySimulation).GetMethod("VehicleBodiesOverlap",BindingFlags.NonPublic|BindingFlags.Static)!;
string last="";
var stepArg=args.Length>0?double.Parse(args[0],System.Globalization.CultureInfo.InvariantCulture):2;
for(var t=0.0;t<7200;t+=stepArg){
  sim.Advance(stepArg);
  var vs=sim.State.Vehicles.Where(v=>v.Status!=VehicleStatus.Outside).ToList();
  var line=string.Join(" | ",vs.Select(v=>$"V{v.Id} {v.Body}/{v.Machine} {v.MachinePhase} {v.Status} ({v.Position.X:F2},{v.Position.Z:F2}) h={v.Heading:F2} sp={v.SitePathIndex}/{v.SitePath.Count} ri={v.RouteIndex}/{v.Route.Count} {v.Cause}"));
  var key=string.Join(";",vs.Select(v=>$"{v.Id}{v.Status}{v.MachinePhase}{v.Cause}"));
  if(key!=last){Console.WriteLine($"t={sim.State.ElapsedSeconds:F0} P26={sim.State.Projects.First(p=>p.Id==26).Cause}\n  "+line.Replace(" | ","\n  "));last=key;}
}
var r=sim.State.Vehicles.First(v=>v.Id==30);var k=sim.State.Vehicles.First(v=>v.Id==22);
Console.WriteLine($"overlap now roller-vs-truck={overlap.Invoke(null,new object[]{r,r.Position,k})} truck-vs-roller={overlap.Invoke(null,new object[]{k,k.Position,r})}");
if(r.SitePathIndex>=r.SitePath.Count)return;
var tgt=r.SitePath[r.SitePathIndex];
for(var s=0.5;s<=6;s+=.5){var at=r.Position.MoveTowards(tgt,s);Console.WriteLine($"roller step {s}: at ({at.X:F2},{at.Z:F2}) overlap={overlap.Invoke(null,new object[]{r,at,k})}");}
var kt=new Point(-193.65,116);for(var s=0.5;s<=3;s+=.5){var at=k.Position.MoveTowards(new Point(-193.65,0),s);Console.WriteLine($"truck step {s}: overlap={overlap.Invoke(null,new object[]{k,at,r})}");}
