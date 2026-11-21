#!/usr/bin/env python3
"""PR57 #358: shadow pedestrian/signal gates at actual queue-reader scans.

Checkout-only instrumentation; the canonical scan, junction admission, writers,
all global movement slices, economy, history and persistence remain untouched.
The shadow gate is NOT a causal certificate or a proposed cache.
"""
from pathlib import Path

root=Path(__file__).resolve().parents[1]
base=root/"src/IndexCities.Simulation"

def patch(path,old,new):
    source=path.read_text()
    assert source.count(old)==1,(str(path),old,source.count(old))
    path.write_text(source.replace(old,new,1))

mob=base/"Mobility.cs"
patch(mob,
      "Pr57351Observe(v,pr57LaneList,blocker,leaderDistance,step,enteringLane);",
      "Pr57351Observe(v,pr57LaneList,blocker,leaderDistance,step,enteringLane);\n"
      "                Pr57358Observe(v,blocker,leaderDistance,step,enteringLane);")
patch(mob,
      "Pr57ExclusiveBeginSlice(dt,operationsDt,scanProfile is not null);",
      "Pr57ExclusiveBeginSlice(dt,operationsDt,scanProfile is not null);\n"
      "        Pr57358BeginSlice(dt,operationsDt,scanProfile is not null);")
patch(mob,
      "Pr57Crossing259EndSlice();",
      "Pr57Crossing259EndSlice();\n        Pr57358EndSlice();")

returns=base/"Mobility.JunctionReturns.Pr57.cs"
patch(returns,
      "if (reason == Pr57JunctionExit.Pedestrian) Pr57304Walk(reader,junction);",
      "if (reason == Pr57JunctionExit.Pedestrian) Pr57304Walk(reader,junction);\n"
      "        Pr57358ActualReturn(reader,junction,reason,allowed);")

out=base/"Mobility.GateShadow358.Pr57.cs"
assert not out.exists()
out.write_text(r'''namespace IndexCities.Simulation;

// #358: observational shadow of only the pedestrian/signal gates at the NEXT
// route node of selected blocked lane readers. This is NOT the whole admission
// readset, and a shadow gate result is NEVER used to decide physical movement.
public sealed partial class CitySimulation
{
    static readonly bool Pr57358Enabled =
        Environment.GetEnvironmentVariable("PR57_358_GATE_SHADOW") == "1";
    readonly record struct Pr57358Mark(long Slice,int Node,bool Pedestrian,
        bool SignalRed,bool SignalPhase,long HeadingBits);
    readonly Dictionary<int,Pr57358Mark> _pr57358Previous=new();
    readonly HashSet<int> _pr57358Seen=new();
    readonly HashSet<int> _pr57358Readers=new();
    readonly HashSet<int> _pr57358Nodes=new();
    readonly long[] _pr57358Reasons=new long[18];
    bool _pr57358Active;
    long _pr57358Window,_pr57358Slice,_pr57358Eligible,_pr57358NoNode;
    long _pr57358Compared,_pr57358NodeChanged,_pr57358PedChanged;
    long _pr57358RedChanged,_pr57358PhaseChanged,_pr57358HeadingChanged;
    long _pr57358ClockChanged,_pr57358PedPositive,_pr57358RedPositive;
    long _pr57358SignalReaders,_pr57358Actual,_pr57358ActualAllowed;
    long _pr57358PredicateVisits,_pr57358PredicateTicks,_pr57358PredicateBytes;
    double _pr57358FirstTime,_pr57358LastTime;

    void Pr57358BeginSlice(double dt,double? operationsDt,bool profiled)
    {
        _pr57358Active=false;
        if(!Pr57358Enabled||!profiled||dt<=0||operationsDt is not null)return;
        var slice=_pr57ExclusiveEligibleSlices;
        var start=slice is >=40001 and <=40600?40001:
                  slice is >=50001 and <=50600?50001:0;
        if(start==0)return;
        _pr57358Active=true;
        if(slice!=start)return;
        _pr57358Window=start;_pr57358Slice=-1;
        _pr57358Previous.Clear();_pr57358Seen.Clear();
        _pr57358Readers.Clear();_pr57358Nodes.Clear();
        Array.Clear(_pr57358Reasons);
        _pr57358Eligible=_pr57358NoNode=_pr57358Compared=0;
        _pr57358NodeChanged=_pr57358PedChanged=_pr57358RedChanged=0;
        _pr57358PhaseChanged=_pr57358HeadingChanged=_pr57358ClockChanged=0;
        _pr57358PedPositive=_pr57358RedPositive=_pr57358SignalReaders=0;
        _pr57358Actual=_pr57358ActualAllowed=0;
        _pr57358PredicateVisits=_pr57358PredicateTicks=_pr57358PredicateBytes=0;
        _pr57358FirstTime=VehicleMovementTime;
    }

    void Pr57358Observe(Vehicle reader,Vehicle? blocker,double distance,
        double step,bool entering)
    {
        if(!_pr57358Active||entering||blocker is null||
            blocker.Status==VehicleStatus.Stranded||distance>8||step>.00001)
            return;
        var slice=_pr57ExclusiveEligibleSlices;
        if(_pr57358Slice!=slice){_pr57358Slice=slice;_pr57358Seen.Clear();}
        if(!_pr57358Seen.Add(reader.Id))return;
        _pr57358Eligible++;
        _pr57358Readers.Add(reader.Id);
        _pr57358LastTime=VehicleMovementTime;
        if(reader.RouteIndex<0||reader.RouteIndex>=reader.Route.Count||
           !_roads.TryGetValue(reader.Route[reader.RouteIndex],out var node))
        {_pr57358NoNode++;return;}
        _pr57358Nodes.Add(node.Id);
        var start=System.Diagnostics.Stopwatch.GetTimestamp();
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        var visits=0;
        var pedestrian=State.Citizens.Any(c=>{
            visits++;
            return c.Alive&&c.VehicleId==null&&c.Moving&&
                c.CrossingNodeId==node.Id&&
                c.Position.Distance(node.Position)<State.Tuning.RoadWidth/2+1.5;
        });
        _pr57358PredicateTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;
        _pr57358PredicateBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
        _pr57358PredicateVisits+=visits;
        var phase=VehicleMovementTime%60<30;
        var signal=node.Control==JunctionControl.Signal&&node.Neighbours.Count>=3;
        var horizontal=Math.Abs(Math.Sin(reader.Heading))>.7;
        var red=signal&&(phase!=horizontal);
        if(signal)_pr57358SignalReaders++;
        if(red)_pr57358RedPositive++;
        if(pedestrian)_pr57358PedPositive++;
        var now=new Pr57358Mark(slice,node.Id,pedestrian,red,phase,
            BitConverter.DoubleToInt64Bits(reader.Heading));
        if(_pr57358Previous.TryGetValue(reader.Id,out var old)&&old.Slice+1==slice)
        {
            if(old.Node!=now.Node)_pr57358NodeChanged++;
            else
            {
                _pr57358Compared++;
                if(old.Pedestrian!=now.Pedestrian)_pr57358PedChanged++;
                if(old.SignalRed!=now.SignalRed)_pr57358RedChanged++;
                if(old.SignalPhase!=now.SignalPhase)_pr57358PhaseChanged++;
                if(old.HeadingBits!=now.HeadingBits)_pr57358HeadingChanged++;
                _pr57358ClockChanged++;
            }
        }
        _pr57358Previous[reader.Id]=now;
    }

    void Pr57358ActualReturn(Vehicle reader,RoadNode junction,
        Pr57JunctionExit reason,bool allowed)
    {
        if(!_pr57358Active||!_pr57358Readers.Contains(reader.Id)||
           !_pr57358Previous.TryGetValue(reader.Id,out var last)||
           last.Node!=junction.Id)return;
        _pr57358Actual++;
        if(allowed)_pr57358ActualAllowed++;
        _pr57358Reasons[(int)reason]++;
    }

    void Pr57358EndSlice()
    {
        if(!_pr57358Active||_pr57ExclusiveEligibleSlices!=_pr57358Window+599)return;
        _pr57358Active=false;
        var ms=_pr57358PredicateTicks*1000.0/
            System.Diagnostics.Stopwatch.Frequency;
        Console.WriteLine("PR57_358_GATE window="+_pr57358Window+
            " eligible="+_pr57358Eligible+" noNode="+_pr57358NoNode+
            " readers="+_pr57358Readers.Count+" nodes="+_pr57358Nodes.Count+
            " compared="+_pr57358Compared+" nodeChanged="+_pr57358NodeChanged+
            " pedChanged="+_pr57358PedChanged+" redChanged="+_pr57358RedChanged+
            " phaseChanged="+_pr57358PhaseChanged+
            " headingChanged="+_pr57358HeadingChanged+
            " clockChanged="+_pr57358ClockChanged+
            " pedestrianPositive="+_pr57358PedPositive+
            " redPositive="+_pr57358RedPositive+
            " signalReads="+_pr57358SignalReaders+
            " actualReturns="+_pr57358Actual+
            " actualAllowed="+_pr57358ActualAllowed+
            " actualPedestrian="+_pr57358Reasons[(int)Pr57JunctionExit.Pedestrian]+
            " actualSignal="+_pr57358Reasons[(int)Pr57JunctionExit.Signal]+
            " actualOnwardBody="+_pr57358Reasons[(int)Pr57JunctionExit.OnwardBody]+
            " predicateVisits="+_pr57358PredicateVisits+
            " predicateMs="+ms.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+
            " predicateAllocatedBytes="+_pr57358PredicateBytes+
            " firstTime="+_pr57358FirstTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+
            " lastTime="+_pr57358LastTime.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+
            " scope=SELECTED_QUEUE_NEXT_NODE_SHADOW notExactReadset=True"+
            " noSkippedSlices=True noDecisionReuse=True intrusive=True");
        _pr57358Previous.Clear();_pr57358Seen.Clear();
        _pr57358Readers.Clear();_pr57358Nodes.Clear();
    }
}
''')
print("PR57_358_INJECTED read-only pedestrian/signal shadow; canonical motor unchanged")
