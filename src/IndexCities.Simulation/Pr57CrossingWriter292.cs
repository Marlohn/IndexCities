using System.Diagnostics;

namespace IndexCities.Simulation;

// PR57 #292: one canonical crossing-acquisition writer -> one real vehicle
// reader (MayEnterJunction's pedestrian gate). NOT a complete invalidation
// journal: crossing release, position, life, vehicle, route, roads and external
// writes are intentionally uninstrumented. No decisions are reused.
public sealed partial class CitySimulation
{
    static readonly bool Pr57Crossing292Enabled =
        Environment.GetEnvironmentVariable("PR57_292_CROSSING") == "1";
    readonly Dictionary<int,long> _pr57Crossing292Versions = new();
    readonly Dictionary<int,long> _pr57Crossing292Seen = new();
    readonly Dictionary<int,long> _pr57Crossing292ReadsByNode = new();
    long _pr57Crossing292Acquires, _pr57Crossing292Reads;
    long _pr57Crossing292Hits, _pr57Crossing292Visits;
    long _pr57Crossing292ReadsAtWrittenNode, _pr57Crossing292FirstReadsAfterWrite;
    long _pr57Crossing292WriteTicks, _pr57Crossing292ReadTicks;
    long _pr57Crossing292WriteBytes, _pr57Crossing292ReadBytes;
    int _pr57Crossing292LastCitizen, _pr57Crossing292LastNode;

    void Pr57Crossing292Acquire(Citizen c,int node)
    {
        if (!Pr57Crossing292Enabled) return;
        var started=Stopwatch.GetTimestamp();
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        _pr57Crossing292Acquires++;
        _pr57Crossing292Versions[node]=_pr57Crossing292Versions.GetValueOrDefault(node)+1;
        _pr57Crossing292LastCitizen=c.Id;
        _pr57Crossing292LastNode=node;
        _pr57Crossing292WriteTicks+=Stopwatch.GetTimestamp()-started;
        _pr57Crossing292WriteBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
    }

    void Pr57Crossing292Read(int node,bool blocked,int visits,long predicateStarted)
    {
        if (!Pr57Crossing292Enabled) return;
        var bytes=GC.GetAllocatedBytesForCurrentThread();
        _pr57Crossing292ReadTicks+=Stopwatch.GetTimestamp()-predicateStarted;
        _pr57Crossing292Reads++;
        _pr57Crossing292Visits+=visits;
        if(blocked)_pr57Crossing292Hits++;
        _pr57Crossing292ReadsByNode[node]=_pr57Crossing292ReadsByNode.GetValueOrDefault(node)+1;
        var version=_pr57Crossing292Versions.GetValueOrDefault(node);
        if(version>0)
        {
            _pr57Crossing292ReadsAtWrittenNode++;
            if(version>_pr57Crossing292Seen.GetValueOrDefault(node))
                _pr57Crossing292FirstReadsAfterWrite++;
            _pr57Crossing292Seen[node]=version;
        }
        _pr57Crossing292ReadBytes+=GC.GetAllocatedBytesForCurrentThread()-bytes;
    }

    public IEnumerable<string> Pr57Crossing292Lines(int calls)
    {
        if(!Pr57Crossing292Enabled)yield break;
        yield return FormattableString.Invariant(
            $"PR57_292_READER calls={calls} acquisitions={_pr57Crossing292Acquires} " +
            $"distinctAcquiredNodes={_pr57Crossing292Versions.Count} lastCitizen={_pr57Crossing292LastCitizen} " +
            $"lastNode={_pr57Crossing292LastNode} readerCalls={_pr57Crossing292Reads} " +
            $"readerPositive={_pr57Crossing292Hits} citizenVisits={_pr57Crossing292Visits} " +
            $"readerDistinctNodes={_pr57Crossing292ReadsByNode.Count} " +
            $"readerCallsAtEverAcquiredNode={_pr57Crossing292ReadsAtWrittenNode} " +
            $"firstReaderAfterAcquisition={_pr57Crossing292FirstReadsAfterWrite} " +
            $"readerIntrusiveMs={_pr57Crossing292ReadTicks*1000.0/Stopwatch.Frequency:F5} " +
            $"writerObserverMs={_pr57Crossing292WriteTicks*1000.0/Stopwatch.Frequency:F5} " +
            $"readerObserverAllocatedBytes={_pr57Crossing292ReadBytes} " +
            $"writerObserverAllocatedBytes={_pr57Crossing292WriteBytes} " +
            "scope=ONE_ACQUISITION_WRITER_ONE_GATE_READER " +
            "causalClosure=False noCache=True noSkippedSlices=True");
        foreach(var pair in _pr57Crossing292ReadsByNode.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key).Take(5))
            yield return $"PR57_292_NODE calls={calls} node={pair.Key} reads={pair.Value} " +
                $"acquisitions={_pr57Crossing292Versions.GetValueOrDefault(pair.Key)}";
    }
}
