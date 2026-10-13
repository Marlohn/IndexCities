namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    const double PollutionCellSize=32;
    public double LocalPollution(Point point)
    {
        var load=0.0;
        foreach(var entry in State.PollutionCells){var x=(int)(entry.Key>>32);var z=unchecked((int)entry.Key);var centre=new Point((x+.5)*PollutionCellSize,(z+.5)*PollutionCellSize);load+=entry.Value*Math.Exp(-point.Distance(centre)/64);}
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
        State.LakePollution=LocalPollution(State.LakeCentre);
    }
}
