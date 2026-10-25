namespace IndexCities.Simulation;

// PR57 opt-in activity agenda: no changes to State, physics, time or saves.
// Citizens due in the same quantum retain State.Citizens order.
public sealed partial class CitySimulation
{
    static readonly bool Pr57CitizenAgendaEnabled=Environment.GetEnvironmentVariable("PR57_CITIZEN_AGENDA")=="1";
    static readonly bool Pr57CitizenAgendaVerify=Environment.GetEnvironmentVariable("PR57_CITIZEN_AGENDA_VERIFY")=="1";
    readonly SortedSet<int> _pr57AgendaActive=[];
    readonly SortedSet<int> _pr57AgendaCurrent=[];
    readonly SortedSet<(double At,int Index)> _pr57AgendaSleeping=[];
    readonly Dictionary<int,double> _pr57AgendaDeadlines=[];
    readonly Dictionary<int,int> _pr57AgendaIndices=[];
    bool _pr57AgendaReady,_pr57AgendaInMove,_pr57AgendaAnnounced;
    int _pr57AgendaCount=-1,_pr57AgendaRoadRevision=-1,_pr57AgendaCursor=-1;

    void Pr57CitizenAgendaWake(Citizen citizen)
    {
        if(!Pr57CitizenAgendaEnabled||!_pr57AgendaReady||!_pr57AgendaIndices.TryGetValue(citizen.Id,out var index))return;
        if(_pr57AgendaDeadlines.Remove(index,out var deadline))_pr57AgendaSleeping.Remove((deadline,index));
        if(_pr57AgendaInMove&&index>_pr57AgendaCursor)_pr57AgendaCurrent.Add(index);
        else _pr57AgendaActive.Add(index);
    }

    void Pr57CitizenAgendaSchedule(Citizen citizen)
    {
        if(!Pr57CitizenAgendaEnabled||!_pr57AgendaReady||!_pr57AgendaIndices.TryGetValue(citizen.Id,out var index))return;
        if(_pr57AgendaDeadlines.Remove(index,out var old))_pr57AgendaSleeping.Remove((old,index));
        _pr57AgendaActive.Remove(index);
        if(!citizen.Alive)return;
        if(citizen.Moving||citizen.VehicleId!=null||citizen.ConstructionProjectId!=null)
            _pr57AgendaActive.Add(index);
        else
        {
            _pr57AgendaDeadlines[index]=citizen.NextDecisionAt;
            _pr57AgendaSleeping.Add((citizen.NextDecisionAt,index));
        }
    }

    void Pr57CitizenAgendaBegin()
    {
        if(!_pr57AgendaAnnounced&&Environment.GetEnvironmentVariable("PR57_GRID_DIAG")=="1")
        {
            Console.WriteLine("PR57_CITIZEN_AGENDA active=True verify="+Pr57CitizenAgendaVerify);
            _pr57AgendaAnnounced=true;
        }
        if(!_pr57AgendaReady||_pr57AgendaCount!=State.Citizens.Count||_pr57AgendaRoadRevision!=State.RoadRevision)
        {
            _pr57AgendaActive.Clear();_pr57AgendaCurrent.Clear();_pr57AgendaSleeping.Clear();
            _pr57AgendaDeadlines.Clear();_pr57AgendaIndices.Clear();
            _pr57AgendaCount=State.Citizens.Count;_pr57AgendaRoadRevision=State.RoadRevision;
            for(var i=0;i<State.Citizens.Count;i++)
            {
                var citizen=State.Citizens[i];
                _pr57AgendaIndices.Add(citizen.Id,i);
                if(!citizen.Alive)continue;
                if(citizen.Moving||citizen.VehicleId!=null||citizen.ConstructionProjectId!=null)
                    _pr57AgendaActive.Add(i);
                else
                {
                    _pr57AgendaDeadlines.Add(i,citizen.NextDecisionAt);
                    _pr57AgendaSleeping.Add((citizen.NextDecisionAt,i));
                }
            }
            _pr57AgendaReady=true;
        }
        if(Pr57CitizenAgendaVerify)
        {
            for(var i=0;i<State.Citizens.Count;i++)
            {
                var citizen=State.Citizens[i];if(!citizen.Alive)continue;
                if(_pr57AgendaActive.Contains(i))continue;
                if(!_pr57AgendaDeadlines.TryGetValue(i,out var at)||at!=citizen.NextDecisionAt||
                    citizen.Moving||citizen.VehicleId!=null||citizen.ConstructionProjectId!=null)
                    throw new InvalidOperationException("PR57 citizen agenda missed invalidation for citizen "+citizen.Id);
            }
        }
        while(_pr57AgendaSleeping.Count>0&&_pr57AgendaSleeping.Min.At<=State.ElapsedSeconds)
        {
            var next=_pr57AgendaSleeping.Min;_pr57AgendaSleeping.Remove(next);
            _pr57AgendaDeadlines.Remove(next.Index);
            _pr57AgendaActive.Add(next.Index);
        }
        _pr57AgendaCurrent.UnionWith(_pr57AgendaActive);
        _pr57AgendaActive.Clear();
        _pr57AgendaCursor=-1;_pr57AgendaInMove=true;
    }

    IEnumerable<Citizen> Pr57CitizenAgendaCandidates()
    {
        Pr57CitizenAgendaBegin();
        try
        {
            while(_pr57AgendaCurrent.Count>0)
            {
                var index=_pr57AgendaCurrent.Min;
                _pr57AgendaCurrent.Remove(index);
                if(index<=_pr57AgendaCursor)continue;
                _pr57AgendaCursor=index;
                var citizen=State.Citizens[index];
                if(citizen.Alive)yield return citizen;
            }
        }
        finally{_pr57AgendaInMove=false;_pr57AgendaCurrent.Clear();}
    }
}
