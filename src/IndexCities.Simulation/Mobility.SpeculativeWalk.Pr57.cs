namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 #599: isolated opt-in speculative physical read for two consecutive walkers.
    // This is NOT a general independent-actor scheduler. Commit remains serial.
    static readonly bool Pr57599SpecWalkEnabled=Environment.GetEnvironmentVariable("PR57_599_SPEC_WALK")=="1";
    static readonly bool Pr57599SpecWalkShadow=Environment.GetEnvironmentVariable("PR57_599_SHADOW")=="1";
    readonly Dictionary<int,Pr57599WalkPrediction> _pr57599WalkPredictions=new();
    long _pr57599WalkPairs,_pr57599WalkPrepared,_pr57599WalkUsed,_pr57599WalkFallback,_pr57599WalkSteps;
    readonly record struct Pr57599WalkPrediction(
        Point Position,Point Target,Point Heading,int PathIndex,int CitizenCount,
        double Distance,double Reach,double Gap);
    static bool Pr57599Bits(double a,double b)=>
        BitConverter.DoubleToInt64Bits(a)==BitConverter.DoubleToInt64Bits(b);
    static bool Pr57599Bits(Point a,Point b)=>
        Pr57599Bits(a.X,b.X)&&Pr57599Bits(a.Z,b.Z);

    bool Pr57599WalkEligible(Citizen c,double travel)
    {
        if(!c.Alive||!c.Moving||c.VehicleId!=null||
           c.PathRoadRevision!=State.RoadRevision||
           c.Purpose==JourneyPurpose.Construction||
           c.PathIndex<0||c.PathIndex>=c.Path.Count)return false;
        // A replan near the destination door can change the path before the gap.
        if(BuildingById(c.DestinationId) is {} destination&&
           c.Position.Distance(destination.Door)<3)return false;
        // The first actor must not arrive or enter another path segment; it
        // therefore cannot trigger a shop, boarding, route or arrival writer.
        return c.Position.Distance(c.Path[c.PathIndex])>travel+1;
    }

    void Pr57599PrepareWalkPair(Citizen first,Citizen second,double dt)
    {
        if(!Pr57599SpecWalkEnabled||_pr57PedestrianGridActive)return;
        var travel=State.Tuning.WalkingSpeed*dt;
        if(!(travel>0)||first.Id==second.Id||
           !Pr57599WalkEligible(first,travel)||!Pr57599WalkEligible(second,travel))return;
        var aTarget=first.Path[first.PathIndex];
        var bTarget=second.Path[second.PathIndex];
        var ad=first.Position.Distance(aTarget);
        var bd=second.Position.Distance(bTarget);
        var aReach=Math.Min(travel,ad)+.7;
        var bReach=Math.Min(travel,bd)+.7;
        // The only intervening walker writer is 'first'. Even if it travels
        // its full budget, it cannot enter the second actor's COMPLETE
        // PedestrianGap radius. This covers negative as well as positive
        // dependencies on the first walker (position, heading and membership).
        if(first.Position.Distance(second.Position)<=bReach+1.3+travel+1e-7)return;
        var aHeading=(aTarget-first.Position)*(1/ad);
        var bHeading=(bTarget-second.Position)*(1/bd);
        double aGap=0,bGap=0;
        System.Threading.Tasks.Parallel.Invoke(
            ()=>aGap=PedestrianGap(first,aTarget,ad,aHeading,aReach),
            ()=>bGap=PedestrianGap(second,bTarget,bd,bHeading,bReach));
        var count=State.Citizens.Count;
        _pr57599WalkPredictions[first.Id]=new(first.Position,aTarget,aHeading,first.PathIndex,count,ad,aReach,aGap);
        _pr57599WalkPredictions[second.Id]=new(second.Position,bTarget,bHeading,second.PathIndex,count,bd,bReach,bGap);
        _pr57599WalkPairs++;
        _pr57599WalkPrepared+=2;
    }

    bool Pr57599TryWalkGap(Citizen c,Point target,double d,Point heading,double reach,out double gap)
    {
        gap=0;
        if(!Pr57599SpecWalkEnabled||!_pr57599WalkPredictions.Remove(c.Id,out var p))return false;
        if(p.CitizenCount!=State.Citizens.Count||p.PathIndex!=c.PathIndex||
           !Pr57599Bits(p.Position,c.Position)||!Pr57599Bits(p.Target,target)||
           !Pr57599Bits(p.Heading,heading)||!Pr57599Bits(p.Distance,d)||
           !Pr57599Bits(p.Reach,reach))
        {
            _pr57599WalkFallback++;
            return false;
        }
        if(Pr57599SpecWalkShadow)
        {
            var canonical=PedestrianGap(c,target,d,heading,reach);
            if(!Pr57599Bits(canonical,p.Gap))
                throw new InvalidOperationException(
                    $"PR57_599_SPEC_WALK causal mismatch citizen={c.Id} predicted={p.Gap:R} actual={canonical:R}");
        }
        gap=p.Gap;
        _pr57599WalkUsed++;
        return true;
    }

    void Pr57599WalkEndStep()
    {
        if(!Pr57599SpecWalkEnabled)return;
        _pr57599WalkPredictions.Clear();
        if(++_pr57599WalkSteps%100==0)
            Console.WriteLine($"PR57_599_SPEC_WALK steps={_pr57599WalkSteps} pairs={_pr57599WalkPairs} prepared={_pr57599WalkPrepared} reused={_pr57599WalkUsed} fallback={_pr57599WalkFallback} shadow={Pr57599SpecWalkShadow}");
    }
}
