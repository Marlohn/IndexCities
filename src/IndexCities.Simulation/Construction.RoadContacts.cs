namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // A remote route between two streets does not turn a local surface overlap
    // into a junction. Require an actual graph path inside the contact's bounds.
    string? RoadContactProblem(IReadOnlyList<Point> shape, int? ignoredProject)
    {
        var existing=State.Roads.Select(r=>r.Position).Concat(State.Projects
            .Where(p=>p.Id!=ignoredProject&&!p.IsMaintenance&&p.Status is WorkStatus.Waiting or WorkStatus.InProgress)
            .SelectMany(p=>p.RoadPoints)).ToHashSet();
        var layout=existing.Concat(shape).ToHashSet();var step=State.Tuning.RoadStep;
        var directions=new[]{new Point(step,0),new Point(-step,0),new Point(0,step),new Point(0,-step)};
        bool NewArm(Point at)=>!existing.Contains(at)||directions.Any(d=>layout.Contains(at+d)&&!existing.Contains(at+d));
        // A straight order can introduce a tiny S at an existing endpoint.
        // Inspect the resulting turns, including the already built/planned arm.
        // Inspect newly formed bends only: adding a T near an unchanged bend
        // must not introduce an unrelated minimum distance between junctions.
        foreach(var at in layout){
            var axes=RoadGeometry.Directions(at,layout,step);
            if(!axes.Horizontal||!axes.Vertical||directions.Count(d=>layout.Contains(at+d))!=2||!NewArm(at))continue;
            foreach(var direction in directions)for(var distance=step;distance<State.Tuning.RoadWidth;distance+=step){
                var next=at+direction*(distance/step);if(!layout.Contains(next))break;
                var nextAxes=RoadGeometry.Directions(next,layout,step);
                if(nextAxes.Horizontal&&nextAxes.Vertical)
                    return "Curva muito curta junto à rua existente. Afaste a esquina ou encaixe no eixo.";
            }
        }
        // Adjacent parallel rows connect at every node, so connectivity alone
        // cannot detect this folded strip. A new four-node square narrower
        // than the street folds its occupied surface back over itself.
        // Keep pre-existing contacts unchanged when merely following an axis.
        if(step<State.Tuning.RoadWidth)foreach(var point in shape)
            foreach(var dx in new[]{-step,step})foreach(var dz in new[]{-step,step}){
                var square=new[]{point,point+new Point(dx,0),point+new Point(0,dz),point+new Point(dx,dz)};
                if(square.All(layout.Contains)&&square.Any(p=>!existing.Contains(p)))
                    return "Ruas muito próximas: as faixas se sobrepõem. Afaste o trecho ou encaixe no eixo existente.";
            }
        var footprints=layout.ToDictionary(p=>p,p=>RoadGeometry.Footprint(p,layout,State.Tuning.RoadWidth,step));
        foreach(var point in shape)foreach(var other in layout){
            if(point.Distance(other)<=step+.01 || !footprints[point].Touches(footprints[other]))continue;
            // An old save can already contain an ambiguous contact. A new order
            // must not be blamed for an unchanged overlap elsewhere on its axis.
            if(existing.Contains(point)&&existing.Contains(other)&&RoadGeometry.Footprint(point,existing,State.Tuning.RoadWidth,step)
                .Touches(RoadGeometry.Footprint(other,existing,State.Tuning.RoadWidth,step)))continue;
            var minX=Math.Min(point.X,other.X)-.01;var maxX=Math.Max(point.X,other.X)+.01;
            var minZ=Math.Min(point.Z,other.Z)-.01;var maxZ=Math.Max(point.Z,other.Z)+.01;
            var local=layout.Where(p=>p.X>=minX&&p.X<=maxX&&p.Z>=minZ&&p.Z<=maxZ).ToArray();
            var visited=new HashSet<Point>{point};var queue=new Queue<Point>();queue.Enqueue(point);
            while(queue.TryDequeue(out var at))foreach(var next in local)
                if(!visited.Contains(next)&&at.Distance(next)<=step+.01){visited.Add(next);queue.Enqueue(next);}
            if(!visited.Contains(other))return "As ruas se sobrepõem sem encontro viário. Encaixe no eixo da rua ou afaste o traçado.";
        }
        return null;
    }
}
