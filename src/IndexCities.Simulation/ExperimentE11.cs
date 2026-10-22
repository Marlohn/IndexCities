namespace IndexCities.Simulation;

// E11: sonda de localidade dos subpassos. Nao executa movimento alternativo.
// Medir mudancas finais nao prova ausencia de dependencias ou seguranca de paralelismo.
public sealed partial class CitySimulation
{
    public bool E11Enabled {get;set;}
    long _e11Substeps,_e11NoChanges,_e11NoMoving,_e11Sparse,_e11MultiTiles,_e11Affected,_e11Tiles;
    readonly record struct E11State(int Id,double X,double Z,double Heading,VehicleStatus Status,int RouteIndex,double Waiting,int? Claim,int SpaceCount,long Fuel)
    {
        public static E11State From(Vehicle v)=>new(v.Id,v.Position.X,v.Position.Z,v.Heading,v.Status,v.RouteIndex,v.WaitingSince,v.IntersectionClaim,v.ManoeuvreSpaces.Count,v.FuelUnits);
    }
    E11State[] E11Capture()=>State.Vehicles.Select(E11State.From).ToArray();
    void E11Completed(E11State[] before)
    {
        _e11Substeps++;
        var tiles=new HashSet<(int,int)>();var changes=0;var moved=0;
        var n=Math.Min(before.Length,State.Vehicles.Count);
        for(var i=0;i<n;i++)
        {
            var v=State.Vehicles[i];
            var next=E11State.From(v);
            if(next==before[i])continue;
            changes++;
            if(next.X!=before[i].X||next.Z!=before[i].Z||next.Heading!=before[i].Heading)moved++;
            tiles.Add(((int)Math.Floor(v.Position.X/64),(int)Math.Floor(v.Position.Z/64)));
        }
        changes+=Math.Abs(State.Vehicles.Count-before.Length);
        if(changes==0)_e11NoChanges++;
        if(moved==0)_e11NoMoving++;
        if(changes<=Math.Max(1,State.Vehicles.Count/10))_e11Sparse++;
        if(tiles.Count>1)_e11MultiTiles++;
        _e11Affected+=changes;_e11Tiles+=tiles.Count;
    }
    public string E11Summary()=>$"E11_RESULT substeps={_e11Substeps} noChanges={_e11NoChanges} noMoving={_e11NoMoving} sparse10pct={_e11Sparse} multi64mTiles={_e11MultiTiles} sumChangedVehicleStates={_e11Affected} sumChanged64mTiles={_e11Tiles} vehicles={State.Vehicles.Count} (retrospective only; no substeps eliminated)";
}
