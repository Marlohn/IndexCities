#!/usr/bin/env python3
"""PR57 #441 disposable, observational census. No decision or Step is skipped."""
from pathlib import Path

def patch(path, pairs):
    p=Path(path);s=p.read_text()
    for old,new in pairs:
        assert s.count(old)==1,(path,old[:90],s.count(old))
        s=s.replace(old,new,1)
    p.write_text(s)

patch("src/IndexCities.Simulation/Economy.cs",[
("readonly Dictionary<Point,double> _homePollution=new();readonly List<Point> _homePoints=[];",
 "readonly Dictionary<Point,double> _homePollution=new();readonly List<Point> _homePoints=[];\n    public long[] Pr57441PollutionCensus=new long[11];"),
("SnapshotPollution();var pollution=_homePollution;pollution.Clear();",
 """var pr57441SnapshotStart=System.Diagnostics.Stopwatch.GetTimestamp();
        SnapshotPollution();
        Pr57441PollutionCensus[0]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57441SnapshotStart;
        Pr57441PollutionCensus[3]++;
        Pr57441PollutionCensus[9]+=_cellCount;
        var pollution=_homePollution;pollution.Clear();"""),
("""foreach(var c in Pr57NeedsCandidates())if(c.Alive&&c.Resident&&c.InsideMap&&BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} h&&pollution.TryAdd(h.Position,0))_homePoints.Add(h.Position);
            SampledPollution(_homePoints,pollution);""",
 """var pr57441BuildStart=System.Diagnostics.Stopwatch.GetTimestamp();
            foreach(var c in Pr57NeedsCandidates())if(c.Alive&&c.Resident&&c.InsideMap&&BuildingById(FamilyById(c.FamilyId)?.HomeId) is {} h&&pollution.TryAdd(h.Position,0))_homePoints.Add(h.Position);
            Pr57441PollutionCensus[1]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57441BuildStart;
            Pr57441PollutionCensus[4]+=_homePoints.Count;
            Pr57441PollutionCensus[5]+=(long)_homePoints.Count*_cellCount;
            var pr57441DotStart=System.Diagnostics.Stopwatch.GetTimestamp();
            SampledPollution(_homePoints,pollution);
            Pr57441PollutionCensus[2]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57441DotStart;"""),
("double HomePollution(Point position){if(!pollution.TryGetValue(position,out var value)){value=SampledPollution(position);pollution.Add(position,value);}return value;}",
 "double HomePollution(Point position){if(!pollution.TryGetValue(position,out var value)){Pr57441PollutionCensus[10]++;value=SampledPollution(position);pollution.Add(position,value);}return value;}")
])
patch("src/IndexCities.Simulation/Pollution.cs",[
("if(changed)_cellLayout++;","if(changed){_cellLayout++;Pr57441PollutionCensus[8]++;}"),
("if(cache.Layout!=_cellLayout){","if(cache.Layout!=_cellLayout){Pr57441PollutionCensus[6]++;"),
("for(var j=0;j<_cellCount;j++)if(j>=cache.Count||cache.Keys[j]!=_cellKeys[j]){cache.Keys[j]=_cellKeys[j];cache.Factors[j]=CellFactor(point,_cellKeys[j]);}",
 "for(var j=0;j<_cellCount;j++)if(j>=cache.Count||cache.Keys[j]!=_cellKeys[j]){cache.Keys[j]=_cellKeys[j];cache.Factors[j]=CellFactor(point,_cellKeys[j]);Pr57441PollutionCensus[7]++;}")
])
patch("src/IndexCities.Simulation/Infrastructure.cs",[
("readonly List<RoadNode> _streets=[];readonly List<Point> _sources=[];readonly CapacityAllocation _allocation=new();",
 "readonly List<RoadNode> _streets=[];readonly List<Point> _sources=[];readonly CapacityAllocation _allocation=new();\n    public long[] Pr57441ElectricCensus=new long[7];"),
("allocation.Allocate(State.ElectricityCapacity,_sources);",
 """var pr57441ElectricStart=System.Diagnostics.Stopwatch.GetTimestamp();
        allocation.Allocate(State.ElectricityCapacity,_sources);
        Pr57441ElectricCensus[0]+=System.Diagnostics.Stopwatch.GetTimestamp()-pr57441ElectricStart;
        Pr57441ElectricCensus[1]++;
        Pr57441ElectricCensus[2+allocation.Pr57441LastBranch]++;
        Pr57441ElectricCensus[5]+=wind.Count;
        Pr57441ElectricCensus[6]+=active.Count+streets.Count;"""),
("public Point[] Positions=[];public double[] Demands=[];public double[] Result=[];",
 "public Point[] Positions=[];public double[] Demands=[];public double[] Result=[];\n    public int Pr57441LastBranch; // 0 zero, 1 sufficient, 2 constrained"),
("var total=0.0;for(var i=0;i<_count;i++){Result[i]=0;total+=Demands[i];}",
 "Pr57441LastBranch=0;var total=0.0;for(var i=0;i<_count;i++){Result[i]=0;total+=Demands[i];}"),
("if(total<=0||capacity<=0)return;",
 "if(total<=0||capacity<=0){Pr57441LastBranch=0;return;}"),
("if(capacity>=total){for(var i=0;i<_count;i++)Result[i]=Demands[i];return;}",
 "if(capacity>=total){Pr57441LastBranch=1;for(var i=0;i<_count;i++)Result[i]=Demands[i];return;}"),
("var pending=0;for(var i=0;i<_count;i++)if(Demands[i]>0)_pending[pending++]=i;var remaining=capacity;",
 "Pr57441LastBranch=2;var pending=0;for(var i=0;i<_count;i++)if(Demands[i]>0)_pending[pending++]=i;var remaining=capacity;")
])
patch("scripts/pr57-actor-regime-337/Program.cs",[
("    var before = sim.Pr57298VehicleCostSnapshot();",
 """    var before = sim.Pr57298VehicleCostSnapshot();
    var pr57441PollutionBefore=sim.Pr57441PollutionCensus.ToArray();
    var pr57441ElectricBefore=sim.Pr57441ElectricCensus.ToArray();"""),
("    var after=sim.Pr57298VehicleCostSnapshot();",
 """    var after=sim.Pr57298VehicleCostSnapshot();
    string[] pr57441PollutionNames={"needsSnapshotTicks","homeBuildTicks","homeSampleTicks","needsSnapshotCalls","homesSampled","homeCellProducts","factorLayoutRefreshes","factorRecomputes","cellLayoutChanges","needsSnapshotCells","fallbackHomeSamples"};
    for(var k=0;k<pr57441PollutionNames.Length;k++)
        Console.WriteLine($"PR57_441_POLLUTION hour={hour} metric={pr57441PollutionNames[k]} value={sim.Pr57441PollutionCensus[k]-pr57441PollutionBefore[k]}");
    string[] pr57441ElectricNames={"allocateTicks","allocateCalls","noCapacityOrDemand","sufficientCapacity","constrainedCapacity","sources","consumers"};
    for(var k=0;k<pr57441ElectricNames.Length;k++)
        Console.WriteLine($"PR57_441_ELECTRIC hour={hour} metric={pr57441ElectricNames[k]} value={sim.Pr57441ElectricCensus[k]-pr57441ElectricBefore[k]}");""")
])
print("PR57_441_INJECTED observational=True noDecisionReuse=True everyAdvance2=True")
