namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 only: a slice-local ordered occupancy sweep. It is never persisted,
    // never reused across Advance(2), and is disabled unless explicitly opted in.
    bool _pr57AxisSweepDirty;
    bool _pr57AxisSweepAnnounced;
    bool _pr57AxisSweepUsedAnnounced;

    sealed class Pr57AxisSweepIndex
    {
        readonly IReadOnlyList<Vehicle> _vehicles;
        readonly SortedSet<(double Coordinate,int Index)> _byX=[];
        readonly SortedSet<(double Coordinate,int Index)> _byZ=[];
        readonly Dictionary<int,int> _indices=[];
        readonly List<int> _scratchIndices=[];
        readonly List<Vehicle> _scratchVehicles=[];
        readonly bool _verify;
        double _maxAbsX,_maxAbsZ;
        public int Count { get; }
        public bool Valid { get; }

        public Pr57AxisSweepIndex(IReadOnlyList<Vehicle> vehicles,bool verify)
        {
            _vehicles=vehicles;Count=vehicles.Count;_verify=verify;
            var valid=true;
            for(var i=0;i<vehicles.Count;i++)
            {
                var p=vehicles[i].Position;
                if(!double.IsFinite(p.X)||!double.IsFinite(p.Z)){valid=false;break;}
                _indices.Add(vehicles[i].Id,i);
                _byX.Add((p.X,i));_byZ.Add((p.Z,i));
                _maxAbsX=Math.Max(_maxAbsX,Math.Abs(p.X));
                _maxAbsZ=Math.Max(_maxAbsZ,Math.Abs(p.Z));
            }
            Valid=valid;
        }

        // Reorder into the original State.Vehicles order, not spatial order.
        // All downstream collision, priority and tie-break predicates remain intact.
        List<Vehicle> Query(SortedSet<(double Coordinate,int Index)> sorted,double low,double high)
        {
            _scratchIndices.Clear();_scratchVehicles.Clear();
            if(!double.IsFinite(low)||!double.IsFinite(high)||low>high)
                throw new InvalidOperationException("PR57 axis sweep invalid query bounds");
            foreach(var item in sorted.GetViewBetween((low,int.MinValue),(high,int.MaxValue)))
                _scratchIndices.Add(item.Index);
            _scratchIndices.Sort();
            foreach(var i in _scratchIndices)_scratchVehicles.Add(_vehicles[i]);
            return _scratchVehicles;
        }

        public List<Vehicle>? Lane(Vehicle v,bool entering)
        {
            if(entering||!Valid)return null; // lateral entry requires full physical admission.
            var sin=Math.Sin(v.Heading);var cos=Math.Cos(v.Heading);
            if(!double.IsFinite(sin)||!double.IsFinite(cos))return null;
            List<Vehicle> result;
            if(Math.Abs(sin)<=1e-9&&Math.Abs(cos)>.999999)
            {
                // Every normal leader has lateral <2.6m. The exact 20m
                // BodyAcrossLane exception is included independently.
                // A tiny nonzero heading tilt is bounded by the entire world extent.
                var band=Math.Max(20,(2.6+Math.Abs(sin)*(_maxAbsZ+Math.Abs(v.Position.Z)))/Math.Abs(cos))+1e-7;
                result=Query(_byX,v.Position.X-band,v.Position.X+band);
            }
            else if(Math.Abs(cos)<=1e-9&&Math.Abs(sin)>.999999)
            {
                var band=Math.Max(20,(2.6+Math.Abs(cos)*(_maxAbsX+Math.Abs(v.Position.X)))/Math.Abs(sin))+1e-7;
                result=Query(_byZ,v.Position.Z-band,v.Position.Z+band);
            }
            else return null;
            if(_verify)
            {
                var included=result.Select(o=>o.Id).ToHashSet();
                foreach(var o in _vehicles)
                {
                    if(included.Contains(o.Id))continue;
                    var delta=o.Position-v.Position;
                    var lateral=Math.Abs(delta.X*cos-delta.Z*sin);
                    if(lateral<2.6||o.Position.Distance(v.Position)<=20)
                        throw new InvalidOperationException("PR57 axis sweep omitted a possible lane body "+o.Id);
                }
            }
            return result;
        }

        public List<Vehicle> Bend(Vehicle v,double step)
        {
            // The existing bend predicate requires Euclidean distance <=step+12.
            // A sorted X interval is an exact superset; no candidate is truncated.
            var reach=step+12+1e-7;
            var result=Query(_byX,v.Position.X-reach,v.Position.X+reach);
            if(_verify)
            {
                var included=result.Select(o=>o.Id).ToHashSet();
                foreach(var o in _vehicles)
                    if(!included.Contains(o.Id)&&o.Position.Distance(v.Position)<=step+12)
                        throw new InvalidOperationException("PR57 axis sweep omitted a possible bend body "+o.Id);
            }
            return result;
        }

        public void Update(Vehicle v,Point before)
        {
            var index=_indices[v.Id];
            var after=v.Position;
            if(before==after)return;
            _byX.Remove((before.X,index));_byZ.Remove((before.Z,index));
            _byX.Add((after.X,index));_byZ.Add((after.Z,index));
            _maxAbsX=Math.Max(_maxAbsX,Math.Abs(after.X));
            _maxAbsZ=Math.Max(_maxAbsZ,Math.Abs(after.Z));
        }
    }
}
