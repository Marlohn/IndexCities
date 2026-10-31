namespace IndexCities.Simulation;

// PR57-only opt-in POC: spatially index walkers at the start of each physical
// 2-second step, then update membership/position after EVERY citizen operation.
// This is an acceleration structure, never a source of authoritative state.
// The original PedestrianGap remains the default and is unchanged.
public sealed partial class CitySimulation
{
    const double Pr57PedestrianCellMetres=8.0;
    readonly bool _pr57PedestrianGridEnabled=Environment.GetEnvironmentVariable("PR57_PEDESTRIAN_GRID")=="1";
    bool _pr57PedestrianGridActive;
    bool _pr57PedestrianGridAnnounced;
    readonly Dictionary<(int X,int Z),List<Citizen>> _pr57PedestrianCells=new();
    readonly Dictionary<int,(int X,int Z)> _pr57PedestrianLocation=new();

    static int Pr57PedestrianCell(double value)=>(int)Math.Floor(value/Pr57PedestrianCellMetres);
    static (int X,int Z) Pr57PedestrianKey(Point p)=>(Pr57PedestrianCell(p.X),Pr57PedestrianCell(p.Z));

    void Pr57RebuildPedestrianGrid()
    {
        // Reuse lists across steps; all entries are revalidated from live state.
        foreach(var bucket in _pr57PedestrianCells.Values)bucket.Clear();
        _pr57PedestrianLocation.Clear();
        foreach(var citizen in State.Citizens)Pr57UpdatePedestrianGrid(citizen);
    }

    void Pr57UpdatePedestrianGrid(Citizen citizen)
    {
        var relevant=citizen.Alive&&citizen.Moving&&citizen.VehicleId==null;
        var key=relevant?Pr57PedestrianKey(citizen.Position):default;
        if(_pr57PedestrianLocation.TryGetValue(citizen.Id,out var previous)){
            if(relevant&&previous==key)return;
            var bucket=_pr57PedestrianCells[previous];
            for(var i=0;i<bucket.Count;i++)if(bucket[i].Id==citizen.Id){bucket.RemoveAt(i);break;}
            _pr57PedestrianLocation.Remove(citizen.Id);
        }
        if(!relevant)return;
        if(!_pr57PedestrianCells.TryGetValue(key,out var list))_pr57PedestrianCells[key]=list=new List<Citizen>();
        list.Add(citizen);
        _pr57PedestrianLocation.Add(citizen.Id,key);
    }

    double Pr57IndexedPedestrianGap(Citizen c,Point target,double d,Point heading,double reach)
    {
        // The cell rectangle covers the SAME exact Euclidean radius as the
        // baseline scan. Never approximate a gap, clearance, heading or tie.
        var gap=double.MaxValue;
        var radius=reach+1.3;
        var radiusSquared=radius*radius;
        var minX=Pr57PedestrianCell(c.Position.X-radius);
        var maxX=Pr57PedestrianCell(c.Position.X+radius);
        var minZ=Pr57PedestrianCell(c.Position.Z-radius);
        var maxZ=Pr57PedestrianCell(c.Position.Z+radius);
        for(var x=minX;x<=maxX;x++)for(var z=minZ;z<=maxZ;z++){
            if(!_pr57PedestrianCells.TryGetValue((x,z),out var bucket))continue;
            foreach(var other in bucket){
                if(other.Id==c.Id||!other.Alive||!other.Moving||other.VehicleId!=null)continue;
                var relative=other.Position-c.Position;
                if(relative.X*relative.X+relative.Z*relative.Z>radiusSquared)continue;
                var otherTarget=other.Path[other.PathIndex];
                var otherDelta=otherTarget-other.Position;
                var otherLength=otherDelta.Distance(new(0,0));
                var alignment=otherLength<.001?1:(heading.X*otherDelta.X+heading.Z*otherDelta.Z)/otherLength;
                var sharedTurn=otherTarget.Distance(target)<.05;
                var precedes=!sharedTurn||otherLength<d-.001||Math.Abs(otherLength-d)<=.001&&other.Id<c.Id;
                var ahead=relative.X*heading.X+relative.Z*heading.Z;
                var side=Math.Abs(relative.X*heading.Z-relative.Z*heading.X);
                if(precedes&&ahead>.001&&side<.55&&alignment>.5&&ahead<gap)gap=ahead;
            }
        }
        return gap;
    }
}
