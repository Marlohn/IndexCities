using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    Node3D? _roadSurface;
    readonly List<Rect2> _sidewalkSurface = new();
    readonly Dictionary<int, RoadNode> _roadVisualNodes = new();
    internal double RoadSurfaceBuildMilliseconds { get; private set; }

    // Partition the union into disjoint X slabs. Subtract carriageways before
    // raising sidewalks: an adjoining tile must never close a junction arm.
    internal static List<Rect2> RoadSurfacePartition(IEnumerable<Rect2> source, IEnumerable<Rect2>? subtract = null)
    {
        var rectangles = source.ToArray(); var cuts = subtract?.ToArray() ?? [];
        var xs = rectangles.Concat(cuts).SelectMany(r => new[] { r.Position.X, r.End.X }).Distinct().Order().ToArray();
        var result = new List<Rect2>();
        for (var i = 1; i < xs.Length; i++) {
            var x = xs[i - 1]; var end = xs[i]; if (end - x < .001f) continue;
            var middle = (x + end) / 2;
            var spans = Merge(rectangles.Where(r => r.Position.X < middle && r.End.X > middle));
            foreach (var cut in Merge(cuts.Where(r => r.Position.X < middle && r.End.X > middle))) {
                var remaining = new List<(float Start, float End)>();
                foreach (var span in spans) {
                    if (cut.End <= span.Start || cut.Start >= span.End) { remaining.Add(span); continue; }
                    if (cut.Start > span.Start) remaining.Add((span.Start, cut.Start));
                    if (cut.End < span.End) remaining.Add((cut.End, span.End));
                }
                spans = remaining;
            }
            foreach (var span in spans.Where(s => s.End - s.Start > .001f)) result.Add(new(x, span.Start, end - x, span.End - span.Start));
        }
        return result;

        static List<(float Start, float End)> Merge(IEnumerable<Rect2> input)
        {
            var merged = new List<(float Start, float End)>();
            foreach (var rect in input.OrderBy(r => r.Position.Y)) {
                var start = rect.Position.Y; var end = rect.End.Y;
                if (merged.Count == 0 || start > merged[^1].End + .001f) merged.Add((start, end));
                else merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, end));
            }
            return merged;
        }
    }

    static void SurfaceMesh(Node3D parent, IEnumerable<Rect2> rectangles, float top, Material material, float? bottom = null)
    {
        var cells = rectangles.ToArray(); if (cells.Length == 0) return;
        var mesh = new ImmediateMesh(); mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        foreach (var cell in cells) {
            var a = new Vector3(cell.Position.X, top, cell.Position.Y); var b = new Vector3(cell.End.X, top, cell.Position.Y);
            var c = new Vector3(cell.End.X, top, cell.End.Y); var d = new Vector3(cell.Position.X, top, cell.End.Y);
            Quad(a, d, c, b, Vector3.Up);
            if (bottom is not {} low) continue;
            Quad(a, b, new(b.X, low, b.Z), new(a.X, low, a.Z), Vector3.Forward);
            Quad(b, c, new(c.X, low, c.Z), new(b.X, low, b.Z), Vector3.Right);
            Quad(c, d, new(d.X, low, d.Z), new(c.X, low, c.Z), Vector3.Back);
            Quad(d, a, new(a.X, low, a.Z), new(d.X, low, d.Z), Vector3.Left);
        }
        mesh.SurfaceEnd(); parent.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            // Godot uses clockwise front faces, independent of the supplied normal.
            mesh.SurfaceSetNormal(normal); foreach (var point in new[] { a, c, b, a, d, c }) mesh.SurfaceAddVertex(point);
        }
    }

    void DrawRoadSurface(CityState state)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        if (_roadSurface != null) { RemoveChild(_roadSurface); _roadSurface.QueueFree(); }
        _roadSurface = new Node3D { Name = "ContinuousRoadSurface" }; AddChild(_roadSurface);
        var asphalt = RoadSurfacePartition(_roadCarriageways);
        _sidewalkSurface.Clear(); _sidewalkSurface.AddRange(RoadSurfacePartition(_roadFootprints, _roadCarriageways));
        var banks=RoadSurfacePartition(_roadFootprints).Where(r=>OnBank(new(r.GetCenter().X,r.GetCenter().Y),r.Size.X,r.Size.Y));
        SurfaceMesh(_roadSurface,banks,-.055f,Material("a6a08b"),-.825f);
        SurfaceMesh(_roadSurface, asphalt, RoadVisualLift + RoadAsphaltTop, GD.Load<Material>("res://Assets/V1/Materials/road_asphalt.tres"));
        SurfaceMesh(_roadSurface, _sidewalkSurface, RoadVisualLift + RoadSidewalkTop, GD.Load<Material>("res://Assets/V1/Materials/road_sidewalk.tres"), -.06f);
        var junctions = state.Roads.Where(r => r.Neighbours.Count > 2).ToArray();
        foreach (var road in state.Roads) {
            if (road.Crosswalk || road.Neighbours.Count > 2) continue;
            var horizontal = road.Highway || road.Neighbours.Any(id => Math.Abs(_roadVisualNodes[id].Position.X - road.Position.X) > .1);
            var along = horizontal ? road.Position.X : road.Position.Z;
            if (Math.Abs(along / (state.Tuning.RoadStep * 3) - Math.Round(along / (state.Tuning.RoadStep * 3))) > .001) continue;
            // Do not paint the centre line across another arm of the junction.
            if (junctions.Any(r => r.Position.Distance(road.Position) < state.Tuning.RoadWidth / 2 + state.Tuning.RoadStep / 2)) continue;
            var dash = new Rect2((float)road.Position.X - (horizontal ? 1 : .07f), (float)road.Position.Z - (horizontal ? .07f : 1), horizontal ? 2 : .14f, horizontal ? .14f : 2);
            if (_crosswalkStripes.Any(r => dash.Intersects(new Rect2(r.MinX, r.MinZ, r.MaxX-r.MinX, r.MaxZ-r.MinZ)))) continue;
            Box(_roadSurface, horizontal ? new(2, .008f, .14f) : new(.14f, .008f, 2), P(road.Position, RoadVisualLift + RoadAsphaltTop + .008f), "e0b23d");
        }
        RoadSurfaceBuildMilliseconds = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
    }

    internal bool RaisedSidewalkAt(Point p) => _sidewalkSurface.Any(r => r.HasPoint(new((float)p.X, (float)p.Z)));

    void PreviewRoadPoint(Point point, string name, float radius, string color)
    {
        _preview.AddChild(new MeshInstance3D { Name = name, Position = P(point, .43f), Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = .07f, RadialSegments = 24 }, MaterialOverride = PreviewMaterial(color), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
    }

    public void PreviewDemolition(RoadNode road)
    {
        var key="demolish:"+road.Id+":"+_state.RoadRevision;if(_previewKey==key)return;ClearPreview();_previewKey=key;
        var horizontal=road.Neighbours.Any(id=>Math.Abs(_roadVisualNodes[id].Position.X-road.Position.X)>.1);
        var vertical=road.Neighbours.Any(id=>Math.Abs(_roadVisualNodes[id].Position.Z-road.Position.Z)>.1);
        var width=(float)_state.Tuning.RoadWidth;var step=(float)_state.Tuning.RoadStep;
        var size=horizontal&&vertical?new Vector3(width,.07f,width):vertical?new(width,.07f,step):new Vector3(step,.07f,width);
        var preview=PreviewBox(_preview,size,P(road.Position,.3f),PreviewMaterial("df8577",.48f));preview.Name="DemolitionPreview";PreviewValidity(_preview,P(road.Position,1.5f),false);
    }
}
