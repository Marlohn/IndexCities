namespace IndexCities.Simulation;

// PR57 experimental broad phase for the shared-node geometry in Manoeuvre.
// Only a movement Step owns this grid. It is rebuilt from the authoritative
// shared-node list, never serialized, and discarded at the public Advance edge.
// Exact EntersArea predicates and original shared-node order remain authoritative.
public sealed partial class CitySimulation
{
    static readonly bool Pr57SharedNodeGridEnabled =
        Environment.GetEnvironmentVariable("PR57_SHARED_NODE_GRID")=="1";
    const double Pr57SharedNodeCellWidth=32;
    Dictionary<(int X,int Z),List<int>>? _pr57SharedNodeGrid;
    List<RoadNode>? _pr57SharedNodeGridNodes;
    int _pr57SharedNodeGridEpoch=-1;
    bool _pr57SharedNodeGridUsedAnnounced;

    void Pr57PrepareSharedNodeGrid()
    {
        _pr57SharedNodeGrid=null;
        _pr57SharedNodeGridNodes=null;
        if(!Pr57SharedNodeGridEnabled||State.Vehicles.Count==0)return;
        var nodes=SharedNodes();
        if(nodes.Count<8)return;
        var grid=new Dictionary<(int X,int Z),List<int>>();
        for(var i=0;i<nodes.Count;i++)
        {
            var position=nodes[i].Position;
            var cell=((int)Math.Floor(position.X/Pr57SharedNodeCellWidth),
                      (int)Math.Floor(position.Z/Pr57SharedNodeCellWidth));
            if(!grid.TryGetValue(cell,out var list))grid[cell]=list=[];
            list.Add(i);
        }
        _pr57SharedNodeGrid=grid;
        _pr57SharedNodeGridNodes=nodes;
        _pr57SharedNodeGridEpoch=_roadEpoch;
    }

    // Returns false if a stale or absent index requires the original full scan.
    // For every heading, EntersArea implies each coordinate is within
    // clearance+half-length+half-width. No physical overlap can be filtered out.
    bool Pr57GatherSharedNodeCandidates(Vehicle body,Point at,List<RoadNode> nodes,List<int> result)
    {
        result.Clear();
        if(_pr57SharedNodeGrid is not {} grid||
           !ReferenceEquals(_pr57SharedNodeGridNodes,nodes)||
           _pr57SharedNodeGridEpoch!=_roadEpoch)return false;
        var reach=CarriagewayHalfWidth+BodyHalfLength(body)+BodyHalfWidth(body);
        var firstX=(int)Math.Floor((at.X-reach)/Pr57SharedNodeCellWidth);
        var lastX=(int)Math.Floor((at.X+reach)/Pr57SharedNodeCellWidth);
        var firstZ=(int)Math.Floor((at.Z-reach)/Pr57SharedNodeCellWidth);
        var lastZ=(int)Math.Floor((at.Z+reach)/Pr57SharedNodeCellWidth);
        for(var x=firstX;x<=lastX;x++)
            for(var z=firstZ;z<=lastZ;z++)
                if(grid.TryGetValue((x,z),out var bucket))result.AddRange(bucket);
        if(!_pr57SharedNodeGridUsedAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine($"PR57_SHARED_NODE_GRID used=True nodes={nodes.Count} candidates={result.Count}");
            _pr57SharedNodeGridUsedAnnounced=true;
        }
        return true;
    }

    void Pr57ClearSharedNodeGrid()
    {
        _pr57SharedNodeGrid=null;
        _pr57SharedNodeGridNodes=null;
    }
}
