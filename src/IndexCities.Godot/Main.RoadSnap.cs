using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    Point SnapRoadTool(Point pointer, Point rounded)
    {
        var state=_sim.State;var step=state.Tuning.RoadStep;
        var layout=state.Roads.Select(r=>r.Position).Concat(PlacementRoadGuides().Select(r=>r.Position)).ToHashSet();
        // Clicking the visible street selects its axis, at any zoom. This only
        // selects a position; it never adds a planned street to the real graph.
        var target=layout.Where(at=>RoadGeometry.Footprint(at,layout,state.Tuning.RoadWidth,step).Contains(pointer))
            .OrderBy(at=>at.Distance(pointer)).ThenBy(at=>at.X).ThenBy(at=>at.Z).Select(at=>(Point?)at).FirstOrDefault();
        if(target is {} road)return road;
        if(_roadStart is {} start){
            var dx=Math.Abs(rounded.X-start.X);var dz=Math.Abs(rounded.Z-start.Z);
            // A bend shorter than the street's own width folds into its corner.
            // Only the free end is aligned: explicit road targets win.
            if(Math.Min(dx,dz)<state.Tuning.RoadWidth){
                if(dx<dz)rounded=new(start.X,rounded.Z);else rounded=new(rounded.X,start.Z);
            }
            if(SnapApproachingRoad(start,rounded,layout) is {} junction)return junction;
        }
        return rounded;
    }

    Point? SnapApproachingRoad(Point start,Point end,IReadOnlySet<Point> layout)
    {
        var step=_sim.State.Tuning.RoadStep;var width=_sim.State.Tuning.RoadWidth;
        var path=_sim.RoadShape(start,end,_reverse);if(path.Count<2)return null;
        var direction=(end-path[^2])*(1/step);var horizontal=Math.Abs(direction.X)>.5;
        // A short visible gap at an approaching end is an intended junction.
        // Extend only along the last leg; never attract a parallel street.
        foreach(var at in layout.OrderBy(p=>p.Distance(end))){
            var delta=at-end;var ahead=delta.X*direction.X+delta.Z*direction.Z;
            if(ahead<=0||ahead>width/2+step)continue;
            if(Math.Abs(horizontal?delta.Z:delta.X)>.01)continue;
            var axes=RoadGeometry.Directions(at,layout,step);
            if(horizontal?!axes.Vertical:!axes.Horizontal)continue;
            var gap=ahead-step/2-width/2;if(gap>=step/2)continue;
            if(_sim.QuoteRoad(start,at,_reverse,_movingProject).PhysicallyValid)return at;
        }
        return null;
    }
}
