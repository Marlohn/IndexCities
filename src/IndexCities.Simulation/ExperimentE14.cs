using System.Security.Cryptography;
using System.Text;

namespace IndexCities.Simulation;

// Experimento E14 isolado: mede eventos que fracionam o passo e tenta falsificar
// a retirada de previsoes de liberaçao de entradas. Nao e algoritmo de produto.
public sealed partial class CitySimulation
{
    public bool E14Observe {get;set;}
    // Ablacao deliberadamente nao comprovada: somente para encontrar divergencias.
    public bool E14DropEntryEvents {get;set;}
    long _e14Calls,_e14NoEntries,_e14Full,_e14Route,_e14Entry,_e14Clamped;
    long _e14Substeps,_e14NoPoseChanges,_e14NoTrackedChanges,_e14RouteNoChanges,
        _e14EntryNoChanges,_e14FullNoChanges,_e14DroppedEntry,_e14Tiny,_e14Short,
        _e14EligibleVehicles,_e14ChangedVehicles;
    double _e14SimSeconds,_e14MinSlice=double.MaxValue,_e14MaxSlice;
    string _e14Reason="none";
    readonly record struct E14Stamp(
        double X,double Z,double Heading,VehicleStatus Status,int RouteIndex,
        double Wait,int? Claim,int ManoeuvreCount,long Fuel,string Cause,int DetourCount)
    {
        public static E14Stamp Of(Vehicle v)=>new(v.Position.X,v.Position.Z,v.Heading,v.Status,
            v.RouteIndex,v.WaitingSince,v.IntersectionClaim,v.ManoeuvreSpaces.Count,v.FuelUnits,
            v.Cause,v.Detour.Count);
        public bool SamePose(E14Stamp b)=>X==b.X&&Z==b.Z&&Heading==b.Heading;
    }
    E14Stamp[] E14Capture()=>State.Vehicles.Select(E14Stamp.Of).ToArray();

    void E14NoEntryForecast(double full)
    {
        if(!E14Observe)return;
        _e14Calls++;_e14NoEntries++;
        _e14Reason="no-entry";
    }

    void E14Forecast(double full,double routeLimit,double entryLimit,double actual)
    {
        if(!E14Observe&&!E14DropEntryEvents)return;
        if(E14Observe)
        {
            _e14Calls++;
            _e14Reason=entryLimit<routeLimit-1e-9?"entry":routeLimit<full-1e-9?"route":"full";
            switch(_e14Reason)
            {
                case "entry":_e14Entry++;break;
                case "route":_e14Route++;break;
                default:_e14Full++;break;
            }
            if(actual<0.0001)_e14Clamped++;
        }
    }

    void E14Slice(E14Stamp[] before,double seconds)
    {
        if(!E14Observe)return;
        _e14Substeps++;_e14SimSeconds+=seconds;
        _e14MinSlice=Math.Min(_e14MinSlice,seconds);
        _e14MaxSlice=Math.Max(_e14MaxSlice,seconds);
        if(seconds<=.0001)_e14Tiny++;
        if(seconds<.05)_e14Short++;
        var tracked=0,pose=0,n=Math.Min(before.Length,State.Vehicles.Count);
        for(var i=0;i<n;i++)
        {
            var next=E14Stamp.Of(State.Vehicles[i]);
            if(next!=before[i])tracked++;
            if(!next.SamePose(before[i]))pose++;
        }
        tracked+=Math.Abs(before.Length-State.Vehicles.Count);
        _e14EligibleVehicles+=before.Length;
        _e14ChangedVehicles+=tracked;
        if(pose==0)_e14NoPoseChanges++;
        if(tracked!=0)return;
        _e14NoTrackedChanges++;
        switch(_e14Reason)
        {
            case "entry":_e14EntryNoChanges++;break;
            case "route":_e14RouteNoChanges++;break;
            case "full":_e14FullNoChanges++;break;
        }
    }

    public string E14Summary()
      =>$"E14_RESULT forecasts={_e14Calls} noEntries={_e14NoEntries} fullHorizon={_e14Full} routeBound={_e14Route} entryBound={_e14Entry} epsilonClamps={_e14Clamped} substeps={_e14Substeps} noPose={_e14NoPoseChanges} noTracked={_e14NoTrackedChanges} noTrackedEntry={_e14EntryNoChanges} noTrackedRoute={_e14RouteNoChanges} noTrackedFull={_e14FullNoChanges} tiny={_e14Tiny} shortUnder50ms={_e14Short} changedVehicleStates={_e14ChangedVehicles} observedVehicleStates={_e14EligibleVehicles} simSeconds={_e14SimSeconds:F6} minSlice={(_e14Substeps==0?0:_e14MinSlice):F8} maxSlice={_e14MaxSlice:F6} droppedEntryForecasts={_e14DroppedEntry} (noTracked means selected fields only; no causal proof)";
    public string E14SaveHash()=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SaveToJson())))[..16];
}
