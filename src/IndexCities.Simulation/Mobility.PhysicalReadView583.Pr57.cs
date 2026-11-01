namespace IndexCities.Simulation;

// PR57 #583: bounded, opt-in physical read view. Only the synchronous,
// read-only admission portion may reuse values; never survives a public
// Advance(2), an actor commit, a reservation write, or a list mutation.
// Uncovered paths use the original live Vehicle objects and predicates.
public sealed partial class CitySimulation
{
    static readonly bool Pr57583Enabled =
        Environment.GetEnvironmentVariable("PR57_583_PHYSICAL_VIEW")=="1";
    static readonly bool Pr57583Shadow =
        Environment.GetEnvironmentVariable("PR57_583_SHADOW")=="1";

    readonly record struct Pr57583Row(
        Vehicle Vehicle,int Id,Point Position,VehicleStatus Status,
        int? TowTruckId,int? Claim,int RouteIndex,double WaitingSince,double Heading);

    Pr57583Row[]? _pr57583Rows;
    long _pr57583Builds,_pr57583BodyReads,_pr57583TrafficReads,
        _pr57583SpaceReads,_pr57583Invalidations,_pr57583ShadowChecks;

    bool Pr57583Begin()
    {
        if(!Pr57583Enabled||_pr57583Rows is not null)return false;
        var vehicles=State.Vehicles;
        var rows=new Pr57583Row[vehicles.Count];
        for(var i=0;i<vehicles.Count;i++){
            var v=vehicles[i];
            rows[i]=new(v,v.Id,v.Position,v.Status,v.TowTruckId,
                v.IntersectionClaim,v.RouteIndex,v.WaitingSince,v.Heading);
        }
        _pr57583Rows=rows;
        _pr57583Builds++;
        return true;
    }

    void Pr57583Invalidate()
    {
        if(_pr57583Rows is null)return;
        _pr57583Rows=null;
        _pr57583Invalidations++;
    }

    void Pr57583End(bool owner)
    {
        if(owner)_pr57583Rows=null;
    }

    // Adversarial oracle only: exact original list order and every cached
    // field must still match before any dense consumer. Never on timed ABBA.
    void Pr57583VerifyRows(Pr57583Row[] rows)
    {
        if(!Pr57583Shadow)return;
        _pr57583ShadowChecks++;
        if(rows.Length!=State.Vehicles.Count)
            throw new InvalidOperationException("PR57_583_LIST_MUTATION");
        for(var i=0;i<rows.Length;i++){
            var row=rows[i];
            var v=State.Vehicles[i];
            if(!ReferenceEquals(row.Vehicle,v)||row.Id!=v.Id||
                BitConverter.DoubleToInt64Bits(row.Position.X)!=BitConverter.DoubleToInt64Bits(v.Position.X)||
                BitConverter.DoubleToInt64Bits(row.Position.Z)!=BitConverter.DoubleToInt64Bits(v.Position.Z)||
                row.Status!=v.Status||row.TowTruckId!=v.TowTruckId||
                row.Claim!=v.IntersectionClaim||row.RouteIndex!=v.RouteIndex||
                BitConverter.DoubleToInt64Bits(row.WaitingSince)!=BitConverter.DoubleToInt64Bits(v.WaitingSince)||
                BitConverter.DoubleToInt64Bits(row.Heading)!=BitConverter.DoubleToInt64Bits(v.Heading))
                throw new InvalidOperationException("PR57_583_ROW_STALE");
        }
    }

    public string Pr57583Stats()=>
        $"PR57_583_STATS enabled={Pr57583Enabled} shadow={Pr57583Shadow} builds={_pr57583Builds} body={_pr57583BodyReads} traffic={_pr57583TrafficReads} space={_pr57583SpaceReads} invalidations={_pr57583Invalidations} shadowChecks={_pr57583ShadowChecks}";
}
