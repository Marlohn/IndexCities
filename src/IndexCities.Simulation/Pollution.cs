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
    readonly Dictionary<Point,(List<long> Keys,List<double> Factors)> _pollutionFactors=new();
    double SampledPollution(Point point)
    {
        if(!_pollutionFactors.TryGetValue(point,out var cache)){if(_pollutionFactors.Count>4096)_pollutionFactors.Clear();_pollutionFactors[point]=cache=([],[]);}
        var load=0.0;var i=0;
        foreach(var entry in State.PollutionCells){
            if(i==cache.Keys.Count){cache.Keys.Add(entry.Key);cache.Factors.Add(CellFactor(point,entry.Key));}
            else if(cache.Keys[i]!=entry.Key){cache.Keys[i]=entry.Key;cache.Factors[i]=CellFactor(point,entry.Key);}
            load+=entry.Value*cache.Factors[i];i++;
        }
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
        foreach(var key in State.PollutionCells.Keys.ToList()){State.PollutionCells[key]*=recovery;if(State.PollutionCells[key]<1e-10)State.PollutionCells.Remove(key);}
        foreach(var building in active)EmitPollution(building.Position,building.UntreatedSewage/100*dt/86400);
        foreach(var vehicle in State.Vehicles)EmitPollution(vehicle.Position,vehicle.FuelBurnedLastStep*.01);
        State.LakePollution=SampledPollution(State.LakeCentre);
    }
}
