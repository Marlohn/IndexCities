using System.Buffers;

namespace IndexCities.Simulation;

// PR57 #546: experimental contact representation scoped to one BodyOnWay call.
// A moving vehicle's sampled pose axes are built once, rather than once for
// every candidate/pose penetration. Every candidate is read LIVE in original
// vehicle order. No cached collision result, global contact graph, writer
// assumption, skipped actor, or skipped movement slice. Off by default.
public sealed partial class CitySimulation
{
    static readonly bool Pr57ContactFramesEnabled =
        Environment.GetEnvironmentVariable("PR57_CONTACT_FRAMES") == "1";
    static readonly bool Pr57ContactFramesVerify =
        Environment.GetEnvironmentVariable("PR57_CONTACT_FRAMES_VERIFY") == "1";
    static readonly bool Pr57ContactFramesDiag =
        Environment.GetEnvironmentVariable("PR57_CONTACT_FRAMES_DIAG") == "1";

    readonly record struct Pr57ContactFrame(
        Point Position, Point Forward, Point Right, double HalfLength, double HalfWidth);

    long _pr57ContactCalls, _pr57ContactPoseFrames, _pr57ContactCandidateFrames,
         _pr57ContactPenetrations, _pr57ContactPositive;

    static Pr57ContactFrame Pr57ContactAt(Vehicle v,Point position,double heading)
    {
        var forward=new Point(Math.Sin(heading),Math.Cos(heading));
        return new(position,forward,new Point(forward.Z,-forward.X),
            BodyHalfLength(v),BodyHalfWidth(v));
    }

    static double Pr57ContactDepth(in Pr57ContactFrame a,in Pr57ContactFrame b,
        Point delta,Point axis)
    {
        static double Dot(Point x,Point y)=>x.X*y.X+x.Z*y.Z;
        // Preserve the four-axis SAT arithmetic and axis order of PenetrationCore.
        return a.HalfLength*Math.Abs(Dot(a.Forward,axis))
             +a.HalfWidth*Math.Abs(Dot(a.Right,axis))
             +b.HalfLength*Math.Abs(Dot(b.Forward,axis))
             +b.HalfWidth*Math.Abs(Dot(b.Right,axis))
             -Math.Abs(Dot(delta,axis));
    }

    static double Pr57ContactPenetration(in Pr57ContactFrame a,in Pr57ContactFrame b)
    {
        var delta=b.Position-a.Position;
        var min=Pr57ContactDepth(in a,in b,delta,a.Forward);
        var next=Pr57ContactDepth(in a,in b,delta,a.Right);
        if(double.IsNaN(next)||next<min)min=next;
        next=Pr57ContactDepth(in a,in b,delta,b.Forward);
        if(double.IsNaN(next)||next<min)min=next;
        next=Pr57ContactDepth(in a,in b,delta,b.Right);
        if(double.IsNaN(next)||next<min)min=next;
        return min;
    }

    static void Pr57ContactCheck(double actual,Vehicle v,Point vAt,double vHeading,
        Vehicle o,Point oAt,double oHeading)
    {
        var original=Penetration(v,vAt,vHeading,o,oAt,oHeading);
        if(BitConverter.DoubleToInt64Bits(actual)!=BitConverter.DoubleToInt64Bits(original))
            throw new InvalidOperationException("PR57_546_SAT_BITWISE_MISMATCH");
    }

    Vehicle? Pr57BodyOnWayFrames(Vehicle v,
        List<(Vehicle Body,Point Position,double Heading)> poses)
    {
        _pr57ContactCalls++;
        if(poses.Count==0)return null;
        // The canonical coarse reach and all candidate predicates remain live.
        var reach=poses.Max(p=>p.Position.Distance(v.Position))+12;
        Pr57ContactFrame[]? rented=null;
        Span<Pr57ContactFrame> frames=default;
        var ready=false;
        var current=default(Pr57ContactFrame);
        try
        {
            foreach(var o in State.Vehicles)
            {
                if(o.Id==v.Id||o.Id==v.TowTargetId||o.TowTruckId==v.Id
                   ||o.Status is VehicleStatus.Outside or VehicleStatus.Towed
                   ||o.Position.Distance(v.Position)>reach
                   ||AtGateway(o)&&AtGateway(v))continue;

                if(!ready)
                {
                    // Pool once per invocation. A stackalloc inside this actor loop
                    // would violate the ref-escape/CA2014 constraints.
                    rented=ArrayPool<Pr57ContactFrame>.Shared.Rent(poses.Count);
                    frames=rented.AsSpan(0,poses.Count);
                    for(var i=0;i<poses.Count;i++)
                        frames[i]=Pr57ContactAt(v,poses[i].Position,poses[i].Heading);
                    current=Pr57ContactAt(v,v.Position,v.Heading);
                    _pr57ContactPoseFrames+=poses.Count+1;
                    ready=true;
                }

                var other=Pr57ContactAt(o,o.Position,o.Heading);
                _pr57ContactCandidateFrames++;
                var now=Pr57ContactPenetration(in current,in other);
                _pr57ContactPenetrations++;
                if(Pr57ContactFramesVerify)
                    Pr57ContactCheck(now,v,v.Position,v.Heading,o,o.Position,o.Heading);
                var limit=Math.Max(-.1,now+.001);
                for(var i=0;i<poses.Count;i++)
                {
                    var p=poses[i];
                    if(p.Position.Distance(o.Position)>=12)continue;
                    var depth=Pr57ContactPenetration(in frames[i],in other);
                    _pr57ContactPenetrations++;
                    if(Pr57ContactFramesVerify)
                        Pr57ContactCheck(depth,v,p.Position,p.Heading,o,o.Position,o.Heading);
                    if(depth>limit)
                    {
                        _pr57ContactPositive++;
                        return o; // first blocker, in original State.Vehicles order
                    }
                }
            }
            return null;
        }
        finally
        {
            if(rented is not null)ArrayPool<Pr57ContactFrame>.Shared.Return(rented);
            if(Pr57ContactFramesDiag&&_pr57ContactCalls%1000==0)
                Console.WriteLine($"PR57_546_CONTACT calls={_pr57ContactCalls} poseFrames={_pr57ContactPoseFrames} candidateFrames={_pr57ContactCandidateFrames} penetration={_pr57ContactPenetrations} positives={_pr57ContactPositive}");
        }
    }
}
