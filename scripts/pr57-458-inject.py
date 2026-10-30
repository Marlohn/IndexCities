#!/usr/bin/env python3
"""PR57 #458: opt-in per-invocation prepared SAT frames for BodyOnWay.

Only in disposable CI checkout. Preserve the canonical vehicle-first, pose-second
scan and its first blocker; never cache across an actor, slice or Advance(2).
No skipping of negative tests or mutation of traffic, economy or saves.
The fast path is disabled when the older penetration probes/memo/SAT flag are on.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
shared = root / "src/IndexCities.Simulation/Mobility.SharedSpace.cs"

def once(old, new):
    source = shared.read_text(encoding="utf-8")
    assert source.count(old) == 1, (old[:100], source.count(old))
    shared.write_text(source.replace(old, new, 1), encoding="utf-8")

once(
    "    Vehicle? BodyOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses,bool pr57305Onward=false)\n    {",
    """    // #458: frame geometry is valid ONLY within this pure BodyOnWay invocation.
    // Heading, body dimensions and ordered candidates are read at their canonical
    // points. There are no cross-invocation caches or global slice skips.
    static readonly bool Pr57458Enabled =
        Environment.GetEnvironmentVariable("PR57_458_PREPARED_FRAMES")=="1";
    readonly record struct Pr57458Frame(Point Forward,Point Right);
    static Pr57458Frame Pr57458Make(double heading)
    {
        var f=new Point(Math.Sin(heading),Math.Cos(heading));
        return new(f,new Point(f.Z,-f.X));
    }
    static double Pr57458Penetration(Vehicle a,Point aAt,Pr57458Frame aa,
        Vehicle b,Point bAt,Pr57458Frame bb)
    {
        // Same axes, axis order, arithmetic, Enumerable.Min and NaN behavior as
        // PenetrationCore with PR57_PENETRATION_STACK_AXES=0.
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        var af=aa.Forward;var ar=aa.Right;var bf=bb.Forward;var br=bb.Right;var d=bAt-aAt;
        return new[]{af,ar,bf,br}.Min(x=>BodyHalfLength(a)*Math.Abs(Dot(af,x))+BodyHalfWidth(a)*Math.Abs(Dot(ar,x))+BodyHalfLength(b)*Math.Abs(Dot(bf,x))+BodyHalfWidth(b)*Math.Abs(Dot(br,x))-Math.Abs(Dot(d,x)));
    }
    Vehicle? BodyOnWay(Vehicle v,List<(Vehicle Body,Point Position,double Heading)> poses,bool pr57305Onward=false)
    {"""
)
once(
    "        if(poses.Count==0)return null;var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;",
    """        if(poses.Count==0)return null;
        // Bounded stack scratch; long or instrumented paths use the canonical
        // implementation. Lazy initialization avoids computing unused pose axes.
        var pr57458Fast=Pr57458Enabled&&poses.Count<=128&&!Pr57271Enabled&&
            !Pr57272MemoEnabled&&!Pr57PenetrationStackAxes;
        Span<Pr57458Frame> pr57458Frames=pr57458Fast?
            stackalloc Pr57458Frame[poses.Count]:Span<Pr57458Frame>.Empty;
        Span<byte> pr57458Seen=pr57458Fast?
            stackalloc byte[poses.Count]:Span<byte>.Empty;
        if(pr57458Fast)pr57458Seen.Clear();
        var pr57458Self=pr57458Fast?Pr57458Make(v.Heading):default;
        var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;"""
)
once(
    "            if(census)eligible++;\n            var now=Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);var limit=Math.Max(-.1,now+.001);",
    """            if(census)eligible++;
            var pr57458Other=pr57458Fast?Pr57458Make(o.Heading):default;
            var now=pr57458Fast?
                Pr57458Penetration(v,v.Position,pr57458Self,o,o.Position,pr57458Other):
                Penetration(v,v.Position,v.Heading,o,o.Position,o.Heading);
            var limit=Math.Max(-.1,now+.001);"""
)
once(
    "            var tested=false;\n            foreach(var p in poses)if(p.Position.Distance(o.Position)<12){",
    """            var tested=false;
            var pr57458Index=-1;
            foreach(var p in poses){
                pr57458Index++;
                if(p.Position.Distance(o.Position)<12){"""
)
once(
    "                if(Penetration(v,p.Position,p.Heading,o,o.Position,o.Heading)>limit){",
    """                if(pr57458Fast&&pr57458Seen[pr57458Index]==0){
                    pr57458Frames[pr57458Index]=Pr57458Make(p.Heading);
                    pr57458Seen[pr57458Index]=1;
                }
                var pr57458Depth=pr57458Fast?
                    Pr57458Penetration(v,p.Position,pr57458Frames[pr57458Index],
                        o,o.Position,pr57458Other):
                    Penetration(v,p.Position,p.Heading,o,o.Position,o.Heading);
                if(pr57458Depth>limit){"""
)
once(
    "            }\n            if(census&&tested)nearNegative++;",
    "                }\n            }\n            if(census&&tested)nearNegative++;"
)
print("PR57_458_INJECTED perInvocation=True orderedFirstBlocker=True crossSliceCache=False")
