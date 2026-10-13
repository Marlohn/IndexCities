using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    sealed record PlacementRoadGuide(Point Position, bool Horizontal, bool Vertical);
    CityState? _guideState;
    (int Revision, int NextId, int? Moving, int Works) _guideKey;
    readonly List<PlacementRoadGuide> _placementRoadGuides = new();

    IReadOnlyList<PlacementRoadGuide> PlacementRoadGuides()
    {
        var state = _sim.State;
        var workHash = state.Projects.Where(p => p.RoadPoints.Count > 0 && p.Status is WorkStatus.Waiting or WorkStatus.InProgress).Aggregate(0, (hash, p) => HashCode.Combine(hash, p.Id, p.Status));
        var key = (state.RoadRevision, state.NextId, _movingProject, workHash);
        if (ReferenceEquals(state, _guideState) && key == _guideKey) return _placementRoadGuides;
        _guideState = state; _guideKey = key; _placementRoadGuides.Clear();
        var points = new Dictionary<Point, (bool Horizontal, bool Vertical)>();
        var roads = state.Roads.ToDictionary(r => r.Id);
        void Add(Point position, bool horizontal, bool vertical)
        {
            var previous = points.GetValueOrDefault(position);
            points[position] = (previous.Horizontal || horizontal, previous.Vertical || vertical);
        }
        foreach (var road in state.Roads.Where(r => !r.Highway))
            Add(road.Position, road.Neighbours.Any(id => Math.Abs(roads[id].Position.X - road.Position.X) > .1), road.Neighbours.Any(id => Math.Abs(roads[id].Position.Z - road.Position.Z) > .1));
        // Confirmed work is only a spatial guide. Quotes and Access remain the
        // authoritative checks; no planned point enters the traffic graph.
        foreach (var project in state.Projects.Where(p => p.Id != _movingProject && p.RoadPoints.Count > 0 && p.Status is WorkStatus.Waiting or WorkStatus.InProgress)) {
            for (var i = 0; i < project.RoadPoints.Count; i++) {
                var position = project.RoadPoints[i]; var horizontal = false; var vertical = false;
                foreach (var neighbour in new[] { i - 1, i + 1 }.Where(n => n >= 0 && n < project.RoadPoints.Count)) {
                    var difference = project.RoadPoints[neighbour] - position;
                    horizontal |= Math.Abs(difference.X) > .1; vertical |= Math.Abs(difference.Z) > .1;
                }
                Add(position, horizontal, vertical);
            }
        }
        _placementRoadGuides.AddRange(points.Select(p => new PlacementRoadGuide(p.Key, p.Value.Horizontal, p.Value.Vertical)));
        return _placementRoadGuides;
    }

    static double FacePlacementGuide(PlacementRoadGuide guide, Point cursor)
    {
        var dx = cursor.X - guide.Position.X; var dz = cursor.Z - guide.Position.Z;
        var sideOnZ = guide.Horizontal && !guide.Vertical || guide.Horizontal == guide.Vertical && Math.Abs(dz) >= Math.Abs(dx);
        return sideOnZ ? dz >= 0 ? 0 : Math.PI : dx >= 0 ? Math.PI / 2 : Math.PI * 1.5;
    }
}
