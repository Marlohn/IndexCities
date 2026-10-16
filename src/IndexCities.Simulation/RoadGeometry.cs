namespace IndexCities.Simulation;

// The sampling interval is longitudinal; it is not the width of an occupied cell.
// Keep this query independent of Godot so quotes and presentation share the envelope.
public readonly record struct RoadFootprint(Point Centre, double Width, double Depth)
{
    public bool Contains(Point p) => Math.Abs(p.X-Centre.X)<=Width/2 && Math.Abs(p.Z-Centre.Z)<=Depth/2;
    public bool Touches(RoadFootprint other) => Math.Abs(Centre.X-other.Centre.X)<=(Width+other.Width)/2+.001 && Math.Abs(Centre.Z-other.Centre.Z)<=(Depth+other.Depth)/2+.001;
}

public static class RoadGeometry
{
    public static (bool Horizontal, bool Vertical) Directions(Point at, IReadOnlySet<Point> layout, double step)
        => (layout.Contains(at+new Point(step,0)) || layout.Contains(at-new Point(step,0)),
            layout.Contains(at+new Point(0,step)) || layout.Contains(at-new Point(0,step)));

    public static RoadFootprint Footprint(Point at, IReadOnlySet<Point> layout, double width, double step)
    {
        var (horizontal,vertical)=Directions(at,layout,step);
        if(!horizontal&&!vertical)horizontal=true;
        return new(at,vertical?width:step,horizontal?width:step);
    }
}
