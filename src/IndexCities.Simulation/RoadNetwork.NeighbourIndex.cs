namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // Same neighbours and list order as the original full scan, but only nearby
    // road nodes are considered. This is an implementation detail, not a new
    // traffic rule. A road's original list index remains the tie-break.
    static void RebuildRoadNeighbours(List<RoadNode> roads,double roadStep)
    {
        var radius=roadStep+.01;
        void FullScan()
        {
            foreach(var node in roads)
                node.Neighbours=roads.Where(other=>other.Id!=node.Id&&other.Position.Distance(node.Position)<=radius).Select(other=>other.Id).ToList();
        }

        if(!double.IsFinite(radius)||radius<=0){FullScan();return;}

        var cells=new Dictionary<(long X,long Z),List<int>>();
        var keys=new (long X,long Z)[roads.Count];
        for(var i=0;i<roads.Count;i++){
            var p=roads[i].Position;
            var x=Math.Floor(p.X/radius);var z=Math.Floor(p.Z/radius);
            // Preserve the reference behaviour for unusual/non-finite coordinates.
            if(!double.IsFinite(x)||!double.IsFinite(z)||x<=long.MinValue+4||x>=long.MaxValue-4||z<=long.MinValue+4||z>=long.MaxValue-4){FullScan();return;}
            var key=((long)x,(long)z);keys[i]=key;
            if(!cells.TryGetValue(key,out var bucket))cells[key]=bucket=[];
            bucket.Add(i);
        }

        for(var i=0;i<roads.Count;i++){
            var centre=keys[i];
            var candidates=new List<int>();
            // Two-cell margin also covers floating-point rounding at boundaries.
            for(var dx=-2;dx<=2;dx++)for(var dz=-2;dz<=2;dz++)
                if(cells.TryGetValue((centre.X+dx,centre.Z+dz),out var bucket))candidates.AddRange(bucket);
            candidates.Sort(); // preserve the exact pre-existing State.Roads order
            var node=roads[i];
            var neighbours=new List<int>();
            foreach(var candidate in candidates){
                var other=roads[candidate];
                if(other.Id!=node.Id&&other.Position.Distance(node.Position)<=radius)neighbours.Add(other.Id);
            }
            node.Neighbours=neighbours;
        }
    }
}
