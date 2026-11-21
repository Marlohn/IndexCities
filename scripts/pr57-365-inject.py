#!/usr/bin/env python3
"""PR57 #365: exact nested-list contents at #364 phase/public boundaries.

Checkout-only, opt-in; applied after #364. Detects net differences in six
selected nested physical inputs even when list counts stay identical.
Does NOT detect transient changes between observations or prove causal closure.
"""
from pathlib import Path
p=Path(__file__).resolve().parents[1]/"src/IndexCities.Simulation/Mobility.CrossPhaseWriters364.Pr57.cs"
assert p.exists(), "apply pr57-364-inject.py first"
s=p.read_text(encoding="utf-8")
def patch(old,new):
    global s
    count=s.count(old)
    assert count==1,(old,count)
    s=s.replace(old,new,1)
patch("        bool Admission,bool? Departure);",
"""        bool Admission,bool? Departure,
        string RouteContents,string SitePathContents,string DockContents,
        string SpaceContents,string AdmissionContents,string DetourContents);""")
patch("    static long Pr57364Bits(double x)=>BitConverter.DoubleToInt64Bits(x);",
"""    static long Pr57364Bits(double x)=>BitConverter.DoubleToInt64Bits(x);
    // Ordered elements and exact IEEE-754 bits. NOT a reusable cache key.
    static readonly bool Pr57365Enabled =
        Environment.GetEnvironmentVariable("PR57_365_NESTED_SHADOW")=="1";
    static string Pr57365Ints(IEnumerable<int> xs)=>string.Join(",",xs);
    static string Pr57365Points(IEnumerable<Point> xs)=>string.Join(";",
        xs.Select(p=>$"{Pr57364Bits(p.X)}:{Pr57364Bits(p.Z)}"));
    static string Pr57365Poses(IEnumerable<MovementPose> xs)=>string.Join(";",
        xs.Select(p=>$"{Pr57364Bits(p.Position.X)}:{Pr57364Bits(p.Position.Z)}:{Pr57364Bits(p.Heading)}"));
    static string Pr57365Admission(LocalMovementAdmission? a)=>a is null?"null":
        $"{Pr57365Ints(a.Route)}|{Pr57365Points(a.SitePath)}|{a.EndRouteIndex}|{a.ReachesDock}";
    readonly long[] _pr57365Route=new long[Pr57364Rows];
    readonly long[] _pr57365Site=new long[Pr57364Rows];
    readonly long[] _pr57365Dock=new long[Pr57364Rows];
    readonly long[] _pr57365Spaces=new long[Pr57364Rows];
    readonly long[] _pr57365Admission=new long[Pr57364Rows];
    readonly long[] _pr57365Detour=new long[Pr57364Rows];""")
patch("                v.ManoeuvreDeparture));",
"""                v.ManoeuvreDeparture,
                Pr57365Enabled?Pr57365Ints(v.Route):"",
                Pr57365Enabled?Pr57365Points(v.SitePath):"",
                Pr57365Enabled?Pr57365Poses(v.DockCorridor):"",
                Pr57365Enabled?Pr57365Ints(v.ManoeuvreSpaces):"",
                Pr57365Enabled?Pr57365Admission(v.MovementAdmission):"",
                Pr57365Enabled?Pr57365Points(v.Detour):""));""")
patch("            if(current==old)continue;",
"""            if(current.RouteContents!=old.RouteContents)_pr57365Route[row]++;
            if(current.SitePathContents!=old.SitePathContents)_pr57365Site[row]++;
            if(current.DockContents!=old.DockContents)_pr57365Dock[row]++;
            if(current.SpaceContents!=old.SpaceContents)_pr57365Spaces[row]++;
            if(current.AdmissionContents!=old.AdmissionContents)_pr57365Admission[row]++;
            if(current.DetourContents!=old.DetourContents)_pr57365Detour[row]++;
            if(current==old)continue;""")
patch("            Array.Clear(_pr57364Assign);Array.Clear(_pr57364Effective);",
"""            Array.Clear(_pr57364Assign);Array.Clear(_pr57364Effective);
            foreach(var arr in new[]{_pr57365Route,_pr57365Site,_pr57365Dock,
                _pr57365Spaces,_pr57365Admission,_pr57365Detour})Array.Clear(arr);""")
patch("        for(var row=0;row<Pr57364Rows;row++)\n        {\n            long a=0,e=0;",
"""        for(var row=0;row<Pr57364Rows;row++)
        {
            if(Pr57365Enabled)Console.WriteLine(
                $"PR57_365_PHASE window={window} phase={labels[row]} "+
                $"route={_pr57365Route[row]} site={_pr57365Site[row]} "+
                $"dock={_pr57365Dock[row]} spaces={_pr57365Spaces[row]} "+
                $"admission={_pr57365Admission[row]} detour={_pr57365Detour[row]} "+
                "netOnly=True transientUnseen=True observational=True");
            long a=0,e=0;""")
patch('            "noSkippedSlices=True notCausalClosure=True observational=True");',
"""            "noSkippedSlices=True notCausalClosure=True observational=True");
        if(Pr57365Enabled)Console.WriteLine(
            $"PR57_365_WINDOW window={window} calls={_pr57364AdvanceCount} "+
            $"route={_pr57365Route.Sum()} site={_pr57365Site.Sum()} "+
            $"dock={_pr57365Dock.Sum()} spaces={_pr57365Spaces.Sum()} "+
            $"admission={_pr57365Admission.Sum()} detour={_pr57365Detour.Sum()} "+
            "scope=SIX_NESTED_CONTENTS_PHASE_BOUNDARIES "+
            "netOnly=True transientUnseen=True notCausalClosure=True observational=True");""")
p.write_text(s,encoding="utf-8")
print("PR57_365_INJECTED exact nested content phase shadow")
