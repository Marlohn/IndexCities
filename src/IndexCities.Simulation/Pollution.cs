using System.Runtime.InteropServices;

namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    const double PollutionCellSize=32;
    public double LocalPollution(Point point)
    {
        var load=0.0;
        foreach(var entry in State.PollutionCells)load+=entry.Value*CellFactor(point,entry.Key);
        return Math.Clamp(load,0,2);
    }
    static double CellFactor(Point point,long key){var x=(int)(key>>32);var z=unchecked((int)key);var centre=new Point((x+.5)*PollutionCellSize,(z+.5)*PollutionCellSize);return Math.Exp(-point.Distance(centre)/64);}
    // The factor between a fixed point and a cell never changes. For the points
    // the simulation itself samples every step (homes, lake), keep the factors
    // aligned with the cells' enumeration order and recompute only where that
    // order changed: same factors, same summation order, same result.
    sealed class CellFactors{public int Layout=-1,Count;public long[] Keys=[];public double[] Factors=[];}
    readonly Dictionary<Point,CellFactors> _pollutionFactors=new();
    readonly List<long> _decayKeys=[];
    long[] _cellKeys=[];double[] _cellValues=[];int _cellCount,_cellLayout;
    // Copies the cells in enumeration order; the layout changes whenever that
    // key sequence differs from the previous snapshot.
    void SnapshotPollution()
    {
        var count=State.PollutionCells.Count;var changed=count!=_cellCount;
        if(_cellKeys.Length<count){Array.Resize(ref _cellKeys,Math.Max(count,_cellKeys.Length*2));Array.Resize(ref _cellValues,_cellKeys.Length);}
        var i=0;foreach(var entry in State.PollutionCells){if(!changed&&_cellKeys[i]!=entry.Key)changed=true;_cellKeys[i]=entry.Key;_cellValues[i]=entry.Value;i++;}
        _cellCount=count;if(changed)_cellLayout++;
    }
    CellFactors FactorsFor(Point point)
    {
        if(!_pollutionFactors.TryGetValue(point,out var cache)){if(_pollutionFactors.Count>4096)_pollutionFactors.Clear();_pollutionFactors[point]=cache=new();}
        if(cache.Layout!=_cellLayout){
            if(cache.Keys.Length<_cellCount){Array.Resize(ref cache.Keys,_cellKeys.Length);Array.Resize(ref cache.Factors,_cellKeys.Length);}
            for(var j=0;j<_cellCount;j++)if(j>=cache.Count||cache.Keys[j]!=_cellKeys[j]){cache.Keys[j]=_cellKeys[j];cache.Factors[j]=CellFactor(point,_cellKeys[j]);}
            cache.Count=_cellCount;cache.Layout=_cellLayout;
        }
        return cache;
    }
    // LocalPollution over the last snapshot.
    double SampledPollution(Point point)
    {
        var load=0.0;var factors=FactorsFor(point).Factors;for(var i=0;i<_cellCount;i++)load+=_cellValues[i]*factors[i];
        return Math.Clamp(load,0,2);
    }
    // SampledPollution of each point into `loads`. Four points share one pass
    // over the cells; each keeps its own sum in cell order, so every result is
    // the one SampledPollution(point) gives.
    void SampledPollution(List<Point> points,Dictionary<Point,double> loads)
    {
        var i=0;
        for(;i+4<=points.Count;i+=4){
            var a=FactorsFor(points[i]).Factors;var b=FactorsFor(points[i+1]).Factors;var c=FactorsFor(points[i+2]).Factors;var d=FactorsFor(points[i+3]).Factors;
            double la=0,lb=0,lc=0,ld=0;
            for(var j=0;j<_cellCount;j++){var value=_cellValues[j];la+=value*a[j];lb+=value*b[j];lc+=value*c[j];ld+=value*d[j];}
            loads[points[i]]=Math.Clamp(la,0,2);loads[points[i+1]]=Math.Clamp(lb,0,2);loads[points[i+2]]=Math.Clamp(lc,0,2);loads[points[i+3]]=Math.Clamp(ld,0,2);
        }
        for(;i<points.Count;i++)loads[points[i]]=SampledPollution(points[i]);
    }
    void EmitPollution(Point point,double amount)
    {
        if(amount<=0||!InMap(point))return;var x=(int)Math.Floor(point.X/PollutionCellSize);var z=(int)Math.Floor(point.Z/PollutionCellSize);var key=((long)x<<32)|unchecked((uint)z);
        State.PollutionCells[key]=State.PollutionCells.GetValueOrDefault(key)+amount;
    }
    void UpdatePollution(double dt,List<Building> active)
    {
        var recovery=Math.Exp(-.15*dt/86400);
        // Same multiplications and removals, in the same key order.
        _decayKeys.Clear();_decayKeys.AddRange(State.PollutionCells.Keys);
        foreach(var key in _decayKeys){ref var value=ref CollectionsMarshal.GetValueRefOrNullRef(State.PollutionCells,key);value*=recovery;if(value<1e-10)State.PollutionCells.Remove(key);}
        foreach(var building in active)EmitPollution(building.Position,building.UntreatedSewage/100*dt/86400);
        foreach(var vehicle in State.Vehicles)EmitPollution(vehicle.Position,vehicle.FuelBurnedLastStep*.01);
        SnapshotPollution();State.LakePollution=SampledPollution(State.LakeCentre);
    }
}
