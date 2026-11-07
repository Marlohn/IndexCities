namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 experiment only: exact conservative BVH broad phase, opt-in.
    // The tree lives for one public MoveVehicles call, not across Advance(2).
    // Every movement slice refits all bounds (including towed/entry changes);
    // each processed vehicle also refits its own leaf before the next vehicle.
    static readonly bool Pr57DynamicBvhEnabled =
        Environment.GetEnvironmentVariable("PR57_DYNAMIC_BVH") == "1";
    Pr57VehicleBvh? _pr57VehicleBvh;
    bool _pr57BvhAnnounced;

    sealed class Pr57VehicleBvh
    {
        sealed class Node
        {
            public int Left = -1, Right = -1, Parent = -1, VehicleIndex = -1;
            public double MinX, MaxX, MinZ, MaxZ;
        }

        readonly List<Vehicle> _vehicles;
        readonly List<Node> _nodes = [];
        readonly Dictionary<int, int> _indexById = [];
        readonly int[] _leaves;
        readonly List<int> _hits = [];
        readonly List<Vehicle> _results = [];

        public Pr57VehicleBvh(List<Vehicle> vehicles)
        {
            _vehicles = vehicles;
            _leaves = new int[vehicles.Count];
            var order = Enumerable.Range(0, vehicles.Count).ToArray();
            for (var i = 0; i < vehicles.Count; i++)
                _indexById.Add(vehicles[i].Id, i);
            if (order.Length > 0) Build(order, 0, order.Length, -1, 0);
        }

        public bool Matches(List<Vehicle> vehicles)
        {
            if (!ReferenceEquals(_vehicles, vehicles) || _leaves.Length != vehicles.Count)
                return false;
            for (var i = 0; i < vehicles.Count; i++)
                if (!_indexById.TryGetValue(vehicles[i].Id, out var index) || index != i)
                    return false;
            return true;
        }

        int Build(int[] order, int start, int length, int parent, int depth)
        {
            var index = _nodes.Count;
            var node = new Node { Parent = parent };
            _nodes.Add(node);
            if (length == 1)
            {
                node.VehicleIndex = order[start];
                _leaves[node.VehicleIndex] = index;
                RefitLeaf(node);
            }
            else
            {
                Array.Sort(order, start, length, Comparer<int>.Create((a, b) =>
                {
                    var pa = _vehicles[a].Position;
                    var pb = _vehicles[b].Position;
                    var cmp = depth % 2 == 0 ? pa.X.CompareTo(pb.X) : pa.Z.CompareTo(pb.Z);
                    return cmp != 0 ? cmp : a.CompareTo(b);
                }));
                var half = length / 2;
                node.Left = Build(order, start, half, index, depth + 1);
                node.Right = Build(order, start + half, length - half, index, depth + 1);
                RefitBranch(node);
            }
            return index;
        }

        void RefitLeaf(Node node)
        {
            var p = _vehicles[node.VehicleIndex].Position;
            node.MinX = node.MaxX = p.X;
            node.MinZ = node.MaxZ = p.Z;
        }

        void RefitBranch(Node node)
        {
            var a = _nodes[node.Left];
            var b = _nodes[node.Right];
            node.MinX = Math.Min(a.MinX, b.MinX);
            node.MaxX = Math.Max(a.MaxX, b.MaxX);
            node.MinZ = Math.Min(a.MinZ, b.MinZ);
            node.MaxZ = Math.Max(a.MaxZ, b.MaxZ);
        }

        // Exact invalidation within a public Advance: catches positions changed
        // by towing, operations and lane-entry frame transitions between slices.
        public void Refresh()
        {
            for (var i = _nodes.Count - 1; i >= 0; i--)
            {
                var node = _nodes[i];
                if (node.VehicleIndex >= 0) RefitLeaf(node);
                else RefitBranch(node);
            }
        }

        public void Update(Vehicle vehicle)
        {
            if (!_indexById.TryGetValue(vehicle.Id, out var index)) return;
            var leaf = _leaves[index];
            RefitLeaf(_nodes[leaf]);
            for (var parent = _nodes[leaf].Parent; parent >= 0; parent = _nodes[parent].Parent)
                RefitBranch(_nodes[parent]);
        }

        static double MinimumSquaredDistance(Node node, Point point)
        {
            var dx = Math.Max(0, Math.Max(node.MinX - point.X, point.X - node.MaxX));
            var dz = Math.Max(0, Math.Max(node.MinZ - point.Z, point.Z - node.MaxZ));
            return dx * dx + dz * dz;
        }

        static bool MayHoldLane(Node node, Point point, double sin, double cos)
        {
            // Dock-body exception and coincident vehicles can affect the
            // winner even when they are behind or outside the lane strip.
            const double dockRadius = 20.0000001;
            if (MinimumSquaredDistance(node, point) <= dockRadius * dockRadius)
                return true;
            // Project all four AABB corners conservatively onto the infinite
            // forward lane ray. No maximum leader distance is assumed.
            var x0 = node.MinX - point.X; var x1 = node.MaxX - point.X;
            var z0 = node.MinZ - point.Z; var z1 = node.MaxZ - point.Z;
            var fMax = Math.Max(x0 * sin, x1 * sin) + Math.Max(z0 * cos, z1 * cos);
            if (fMax < -1e-7) return false;
            var sideMin = Math.Min(x0 * cos, x1 * cos) + Math.Min(-z0 * sin, -z1 * sin);
            var sideMax = Math.Max(x0 * cos, x1 * cos) + Math.Max(-z0 * sin, -z1 * sin);
            return sideMin <= 2.6000001 && sideMax >= -2.6000001;
        }

        void Visit(int index, Func<Node, bool> overlaps)
        {
            var node = _nodes[index];
            if (!overlaps(node)) return;
            if (node.VehicleIndex >= 0) { _hits.Add(node.VehicleIndex); return; }
            Visit(node.Left, overlaps);
            Visit(node.Right, overlaps);
        }

        List<Vehicle> Finish()
        {
            _hits.Sort(); // exact original State.Vehicles iteration order
            _results.Clear();
            foreach (var index in _hits) _results.Add(_vehicles[index]);
            return _results;
        }

        public List<Vehicle> Lane(Vehicle vehicle)
        {
            _hits.Clear();
            if (_nodes.Count == 0) return Finish();
            var point = vehicle.Position;
            var sin = Math.Sin(vehicle.Heading);
            var cos = Math.Cos(vehicle.Heading);
            Visit(0, node => MayHoldLane(node, point, sin, cos));
            return Finish();
        }

        public List<Vehicle> Bend(Vehicle vehicle, double step)
        {
            _hits.Clear();
            if (_nodes.Count == 0) return Finish();
            var point = vehicle.Position;
            var radius = step + 12 + 1e-7;
            Visit(0, node => MinimumSquaredDistance(node, point) <= radius * radius);
            return Finish();
        }
    }
}
