using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    static Point SnapBuildingBounds(BuildingKind kind,Point pointer,double rotation,double step)
    {
        var definition=Catalog.Definitions[kind];
        var turned=Math.Abs(Math.Sin(rotation))>.5;
        var halfX=(turned?definition.Depth:definition.Width)/2;
        var halfZ=(turned?definition.Width:definition.Depth)/2;
        // The pointer still positions the centre. Derive it from a snapped
        // corner, so both even- and odd-cell footprints have aligned edges.
        return new(Math.Round((pointer.X-halfX)/step)*step+halfX,
            Math.Round((pointer.Z-halfZ)/step)*step+halfZ);
    }

    double FrontageOffset(double step)=>Math.Ceiling((_sim.State.Tuning.RoadWidth/2+.75)/step)*step;
}
