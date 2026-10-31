namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    // PR57 #300: observational per-public-Advance census. No cache, skip,
    // scheduler input, persisted state, or cross-Advance reuse.
    static readonly bool Pr57300Enabled=Environment.GetEnvironmentVariable("PR57_300_BOUNDARY")=="1";
    long _pr57300Calls;
    Pr57300Step? _pr57300Step;

    sealed class Pr57300Step
    {
        readonly record struct Pose(Point Position,double Heading,int RouteIndex,VehicleStatus Status);
        readonly Dictionary<int,Pose> _before=new();
        readonly Dictionary<int,long> _entries=new();
        readonly Dictionary<int,long> _winners=new();
        readonly long[] _reasons=new long[4]; // no live entry, horizon, motion event, projected recheck
        readonly long[] _causes=new long[5];  // fuel, junction, speed, turn, route end
        public long ScheduleTicks,MoveTicks,ObserverTicks;
        long _entrySum,_entryMin=long.MaxValue,_entryMax,_blockedChecks,_projectionCandidates,_projectionRechecks;
        long _changedSlices,_unchangedSlices,_changedTouches,_movedTouches,_routeTouches,_statusTouches;
        public void Entries(List<Vehicle> entries)
        {
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            _entrySum+=entries.Count;
            _entryMin=Math.Min(_entryMin,entries.Count);
            _entryMax=Math.Max(_entryMax,entries.Count);
            foreach(var v in entries){_entries.TryGetValue(v.Id,out var n);_entries[v.Id]=n+1;}
            ObserverTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        }
        public void Decision(int reason,int movementCause,Vehicle? winner,long blockedChecks,long candidates,long rechecks)
        {
            _reasons[reason]++;
            if(reason==2&&movementCause>=0&&movementCause<_causes.Length)_causes[movementCause]++;
            if(reason==2&&winner is not null){_winners.TryGetValue(winner.Id,out var n);_winners[winner.Id]=n+1;}
            _blockedChecks+=blockedChecks;
            _projectionCandidates+=candidates;
            _projectionRechecks+=rechecks;
        }
        public void Capture(IEnumerable<Vehicle> vehicles)
        {
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            _before.Clear();
            foreach(var v in vehicles)_before[v.Id]=new(v.Position,v.Heading,v.RouteIndex,v.Status);
            ObserverTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        }
        public void After(IEnumerable<Vehicle> vehicles)
        {
            var started=System.Diagnostics.Stopwatch.GetTimestamp();
            long changed=0;
            foreach(var v in vehicles)
            {
                if(!_before.TryGetValue(v.Id,out var p)){changed++;_movedTouches++;continue;}
                var moved=!v.Position.Equals(p.Position);
                var route=v.RouteIndex!=p.RouteIndex;
                var status=v.Status!=p.Status;
                if(moved)_movedTouches++;
                if(route)_routeTouches++;
                if(status)_statusTouches++;
                if(moved||route||status||v.Heading!=p.Heading)changed++;
            }
            _changedTouches+=changed;
            if(changed==0)_unchangedSlices++;else _changedSlices++;
            ObserverTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;
        }
        public void Print(long call,bool batched)
        {
            var entriesTop=_entries.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).FirstOrDefault();
            var winnerTop=_winners.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).FirstOrDefault();
            var hz=System.Diagnostics.Stopwatch.Frequency;
            Console.WriteLine($"PR57_300_STEP call={call} batched={(batched?1:0)} boundaries={_reasons.Sum()} noEntry={_reasons[0]} full={_reasons[1]} movement={_reasons[2]} projection={_reasons[3]} fuel={_causes[0]} junction={_causes[1]} speed={_causes[2]} turn={_causes[3]} routeEnd={_causes[4]} entriesSum={_entrySum} entriesMin={(_entryMin==long.MaxValue?0:_entryMin)} entriesMax={_entryMax} uniqueEntries={_entries.Count} topEntry={entriesTop.Key} topEntryCount={entriesTop.Value} uniqueWinners={_winners.Count} topWinner={winnerTop.Key} topWinnerCount={winnerTop.Value} blockedChecks={_blockedChecks} projectedCandidates={_projectionCandidates} projectedRechecks={_projectionRechecks} changedSlices={_changedSlices} unchangedSlices={_unchangedSlices} changedTouches={_changedTouches} movedTouches={_movedTouches} routeTouches={_routeTouches} statusTouches={_statusTouches} schedulerMs={ScheduleTicks*1000.0/hz:F6} movementMs={MoveTicks*1000.0/hz:F6} observerMs={ObserverTicks*1000.0/hz:F6} scope=INTRUSIVE_NET_POSE_NOT_CAUSAL_INDEPENDENCE");
        }
    }
}
