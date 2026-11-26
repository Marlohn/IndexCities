namespace IndexCities.Simulation;

// PR57-only POC: deadline-indexed public infrastructure maintenance.
// Every due asset still receives the exact per-quantum decrement in original list
// order. Only assets whose maintenance deadline is in the future avoid a scan.
// Rebuild at public Advance boundaries and after internal construction/repair
// transitions. No timers, state, events, or save fields are omitted.
public sealed partial class CitySimulation
{
    readonly SortedSet<(double At,int Index)> _pr57MaintenanceBuildingFuture=[];
    readonly SortedSet<(double At,int Index)> _pr57MaintenanceRoadFuture=[];
    readonly SortedSet<int> _pr57MaintenanceBuildingsDue=[];
    readonly SortedSet<int> _pr57MaintenanceRoadsDue=[];
    bool _pr57MaintenanceScheduleReady;
    int _pr57MaintenanceBuildingCount=-1,_pr57MaintenanceRoadCount=-1,_pr57MaintenanceRoadRevision=-1;

    void Pr57ScheduledMaintenanceCondition(double dt)
    {
        var now=State.ElapsedSeconds;
        if(!_pr57MaintenanceScheduleReady||
           _pr57MaintenanceBuildingCount!=State.Buildings.Count||
           _pr57MaintenanceRoadCount!=State.Roads.Count||
           _pr57MaintenanceRoadRevision!=State.RoadRevision)
        {
            _pr57MaintenanceBuildingFuture.Clear();
            _pr57MaintenanceRoadFuture.Clear();
            _pr57MaintenanceBuildingsDue.Clear();
            _pr57MaintenanceRoadsDue.Clear();
            _pr57MaintenanceBuildingCount=State.Buildings.Count;
            _pr57MaintenanceRoadCount=State.Roads.Count;
            _pr57MaintenanceRoadRevision=State.RoadRevision;
            for(var i=0;i<State.Buildings.Count;i++)
            {
                var b=State.Buildings[i];
                if(!b.Completed||b.Removed||b.Definition.Private)continue;
                if(b.MaintenanceDueAt<=0)b.MaintenanceDueAt=now+MaintenanceInterval;
                if(b.MaintenanceDueAt<=now)_pr57MaintenanceBuildingsDue.Add(i);
                else _pr57MaintenanceBuildingFuture.Add((b.MaintenanceDueAt,i));
            }
            for(var i=0;i<State.Roads.Count;i++)
            {
                var r=State.Roads[i];
                if(r.Highway)continue;
                if(r.MaintenanceDueAt<=0)r.MaintenanceDueAt=now+MaintenanceInterval;
                if(r.MaintenanceDueAt<=now)_pr57MaintenanceRoadsDue.Add(i);
                else _pr57MaintenanceRoadFuture.Add((r.MaintenanceDueAt,i));
            }
            _pr57MaintenanceScheduleReady=true;
        }
        while(_pr57MaintenanceBuildingFuture.Count>0&&_pr57MaintenanceBuildingFuture.Min.At<=now)
        {
            var item=_pr57MaintenanceBuildingFuture.Min;
            _pr57MaintenanceBuildingFuture.Remove(item);
            _pr57MaintenanceBuildingsDue.Add(item.Index);
        }
        while(_pr57MaintenanceRoadFuture.Count>0&&_pr57MaintenanceRoadFuture.Min.At<=now)
        {
            var item=_pr57MaintenanceRoadFuture.Min;
            _pr57MaintenanceRoadFuture.Remove(item);
            _pr57MaintenanceRoadsDue.Add(item.Index);
        }
        foreach(var i in _pr57MaintenanceBuildingsDue)
        {
            var b=State.Buildings[i];
            if(!b.Completed||b.Removed||b.Definition.Private||now<b.MaintenanceDueAt)continue;
            b.AddMaintenanceRatio(-.03*dt/86400);
            b.MaintenanceRatioUnits=Math.Max(0,b.MaintenanceRatioUnits);
        }
        var graphChanged=false;
        foreach(var i in _pr57MaintenanceRoadsDue)
        {
            var r=State.Roads[i];
            if(r.Highway||now<r.MaintenanceDueAt)continue;
            var before=r.MaintenanceRatio;
            r.AddMaintenanceRatio(-.03*dt/86400);
            r.MaintenanceRatioUnits=Math.Max(0,r.MaintenanceRatioUnits);
            if(before>.05&&r.MaintenanceRatio<=.05)graphChanged=true;
        }
        if(graphChanged)RebuildRoadGraph();
    }
}
