namespace IndexCities.Simulation;

// Geography belongs to the saved simulation. Rendering consumes these exact
// vertices; it never regenerates a contour or decides what counts as water.
public static class LakeGeometry
{
    public static List<Point> GenerateBoundary(int seed,Point centre,double radius)
    {
        // A local stream derived from all 32 seed bits. It does not advance
        // State.RandomState, and saved contours are never regenerated.
        var bits=(uint)seed;
        double Sample(){unchecked{bits+=0x9e3779b9;var x=bits;x=(x^(x>>16))*0x85ebca6b;x=(x^(x>>13))*0xc2b2ae35;return (x^(x>>16))/4294967296.0;}}
        var phase2=Sample()*Math.Tau;var phase3=Sample()*Math.Tau;var phase5=Sample()*Math.Tau;
        var amplitude2=.085+.035*Sample();var amplitude3=.10+.04*Sample();var amplitude5=.035+.02*Sample();
        return Enumerable.Range(0,128).Select(i=>{
            var a=Math.Tau*i/128;
            var r=radius*(1+amplitude2*Math.Cos(2*a+phase2)+amplitude3*Math.Sin(3*a+phase3)+amplitude5*Math.Cos(5*a+phase5));
            // Store the same precision the Godot mesh receives, including after load.
            return new Point((float)(centre.X+Math.Cos(a)*r),(float)(centre.Z+Math.Sin(a)*r));
        }).ToList();
    }

    public static void Validate(CityState state)
    {
        if(state.SchemaVersion==1){if(state.LakeBoundary!=null)throw new InvalidDataException("Mapa antigo com contorno incompatível");return;}
        if(state.SchemaVersion!=2||state.LakeBoundary is not {Count:>=8 and <=512} polygon)throw new InvalidDataException("Contorno do lago ausente ou inválido");
        var total=0.0;
        for(var i=0;i<polygon.Count;i++){
            var point=polygon[i];var a=point-state.LakeCentre;var b=polygon[(i+1)%polygon.Count]-state.LakeCentre;
            if(!double.IsFinite(point.X)||!double.IsFinite(point.Z)||Math.Abs(point.X)>=state.Tuning.MapHalfSize||Math.Abs(point.Z)>=state.Tuning.MapHalfSize||a.Distance(default)<.5)throw new InvalidDataException("Vértice do lago inválido");
            var cross=a.X*b.Z-a.Z*b.X;
            if(cross<=1e-6)throw new InvalidDataException("Contorno do lago fora de ordem");
            total+=Math.Atan2(cross,a.X*b.X+a.Z*b.Z);
        }
        if(!double.IsFinite(total)||Math.Abs(total-Math.Tau)>1e-5)throw new InvalidDataException("Contorno do lago não é simples");
    }

    // Positive on dry land, negative in water. The legacy circle is intentional:
    // loading a schema-1 city must not change either geography or intake reach.
    public static double SignedDistance(CityState state,Point point)
    {
        if(state.SchemaVersion==1)return point.Distance(state.LakeCentre)-state.LakeRadius;
        var polygon=state.LakeBoundary!;var inside=false;var nearest=double.PositiveInfinity;
        for(var i=0;i<polygon.Count;i++){
            var a=polygon[i];var b=polygon[(i+1)%polygon.Count];var d=b-a;var length2=d.X*d.X+d.Z*d.Z;
            var t=Math.Clamp(((point.X-a.X)*d.X+(point.Z-a.Z)*d.Z)/length2,0,1);
            nearest=Math.Min(nearest,point.Distance(a+d*t));
            if((a.Z>point.Z)!=(b.Z>point.Z)&&point.X<(b.X-a.X)*(point.Z-a.Z)/(b.Z-a.Z)+a.X)inside=!inside;
        }
        return inside?-nearest:nearest;
    }

    public static bool OverlapsRectangle(CityState state,Point centre,double width,double depth,double rotation=0)
    {
        var c=Math.Cos(rotation);var s=Math.Sin(rotation);
        var polygon=state.LakeBoundary!.Select(p=>{var d=p-centre;return new Point(d.X*c-d.Z*s,d.X*s+d.Z*c);}).ToList();
        // Clip the water polygon to the footprint. This also catches water
        // crossing an edge while all four corners remain on dry land.
        Clip(p=>p.X+width/2);Clip(p=>width/2-p.X);Clip(p=>p.Z+depth/2);Clip(p=>depth/2-p.Z);
        var area=0.0;for(var i=0;i<polygon.Count;i++){var a=polygon[i];var b=polygon[(i+1)%polygon.Count];area+=a.X*b.Z-a.Z*b.X;}
        return Math.Abs(area)>.000002;
        void Clip(Func<Point,double> distance){
            var result=new List<Point>();if(polygon.Count==0)return;
            var prior=polygon[^1];var dp=distance(prior);
            foreach(var point in polygon){var d=distance(point);if((d>=0)!=(dp>=0))result.Add(prior+(point-prior)*(dp/(dp-d)));if(d>=0)result.Add(point);prior=point;dp=d;}
            polygon=result;
        }
    }
}
