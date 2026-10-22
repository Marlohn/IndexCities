namespace IndexCities.Simulation;

// E12: medicao de repeticao de parametros fisicos de Manoeuvre.
// O codigo original continua calculando SEM cache: nao representa speedup.
public sealed partial class CitySimulation
{
    public bool E12Enabled {get;set;}
    long _e12Calls,_e12Eligible,_e12ConsecutiveRepeat,_e12Different,_e12Uncacheable;
    readonly Dictionary<int,E12Input> _e12Last=new();
    sealed record E12Input(
        int Epoch,int Body,int From,Point At,double Heading,bool Approach,
        int Minimum,int? Through,double? SiteHeading,int[] Route,Point[] Site);
    void E12Observe(Vehicle o,List<int> route,int from,Point at,double facing,IReadOnlyList<Point> site,bool approach,int minimum,Func<Point,double,bool>? extraClear,int? through)
    {
        _e12Calls++;
        if(extraClear!=null){_e12Uncacheable++;return;}
        _e12Eligible++;
        if(_e12Last.TryGetValue(o.Id,out var last)&&last.Epoch==_roadEpoch&&last.Body==(int)o.Body&&last.From==from&&last.At==at&&last.Heading==facing&&last.Approach==approach&&last.Minimum==minimum&&last.Through==through&&last.SiteHeading==o.SiteDockHeading&&last.Route.AsSpan().SequenceEqual(route.ToArray())&&last.Site.AsSpan().SequenceEqual(site.ToArray()))
        {
            _e12ConsecutiveRepeat++;
            return;
        }
        _e12Different++;
        _e12Last[o.Id]=new E12Input(_roadEpoch,(int)o.Body,from,at,facing,approach,minimum,through,o.SiteDockHeading,route.ToArray(),site.ToArray());
    }
    public string E12Summary()=>$"E12_RESULT calls={_e12Calls} eligibleNoClosure={_e12Eligible} consecutiveSameInputs={_e12ConsecutiveRepeat} changedInputs={_e12Different} closureBased={_e12Uncacheable} uniqueLastVehicleKeys={_e12Last.Count} (conservative upper bound; no cache applied)";
}
