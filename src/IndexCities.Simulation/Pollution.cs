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
    // LocalPollution over the last snapshot.
    double SampledPollution(Point point)
    {
        if(!_pollutionFactors.TryGetValue(point,out var cache)){if(_pollutionFactors.Count>4096)_pollutionFactors.Clear();_pollutionFactors[point]=cache=new();}
        if(cache.Layout!=_cellLayout){
            if(cache.Keys.Length<_cellCount){Array.Resize(ref cache.Keys,_cellKeys.Length);Array.Resize(ref cache.Factors,_cellKeys.Length);}
            for(var j=0;j<_cellCount;j++)if(j>=cache.Count||cache.Keys[j]!=_cellKeys[j]){cache.Keys[j]=_cellKeys[j];cache.Factors[j]=CellFactor(point,_cellKeys[j]);}
            cache.Count=_cellCount;cache.Layout=_cellLayout;
        }
        var load=0.0;var factors=cache.Factors;for(var i=0;i<_cellCount;i++)load+=_cellValues[i]*factors[i];
        return Math.Clamp(load,0,2);
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
