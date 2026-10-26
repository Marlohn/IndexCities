using System.Text.Json;

namespace IndexCities.Simulation;

// PR57-only, intrusive observation of NET changes in serialized CityState at
// boundaries of the 13 existing phases. No writes, no cache and no Step skipped.
// Identical serialized snapshots imply no persisted net change at that boundary,
// NOT that reads, temporary mutations or events were causally unnecessary.
public sealed class Pr57PhaseEffects
{
    static readonly string[] Collections = { "Vehicles", "Citizens", "Projects", "Buildings", "Shipments" };
    readonly Dictionary<string,long>[] _properties =
        Enumerable.Range(0,StepCostProfile.Names.Length).Select(_=>new Dictionary<string,long>(StringComparer.Ordinal)).ToArray();
    readonly long[,] _changedRecords = new long[StepCostProfile.Names.Length,Collections.Length];
    // Nested persisted field census for the two causal bottlenecks: citizen effects
    // in vehicle processing and project effects in construction/dispatch.
    // This is an intrusive observation only; never used by simulation decisions.
    readonly Dictionary<string,long>[] _nestedFields =
        Enumerable.Range(0,StepCostProfile.Names.Length).Select(_=>new Dictionary<string,long>(StringComparer.Ordinal)).ToArray();
    readonly long[] _changedSteps = new long[StepCostProfile.Names.Length];
    readonly int _every;
    long _steps;
    string? _previous;

    public long Samples { get; private set; }
    public Pr57PhaseEffects(int every)
    {
        if(every<1)throw new ArgumentOutOfRangeException(nameof(every));
        _every=every;
    }

    internal bool ShouldSample()
    {
        var selected=_steps%_every==0;
        _steps++;
        return selected;
    }

    internal void Begin(string snapshot)
    {
        if(_previous is not null)throw new InvalidOperationException("Unfinished PR57 phase sample");
        _previous=snapshot;
        Samples++;
    }

    internal void Observe(int phase,string snapshot)
    {
        if(_previous is null)throw new InvalidOperationException("Missing PR57 phase baseline");
        var previous=_previous;
        if(previous!=snapshot)
        {
            _changedSteps[phase]++;
            using var before=JsonDocument.Parse(previous);
            using var after=JsonDocument.Parse(snapshot);
            foreach(var property in after.RootElement.EnumerateObject())
            {
                if(before.RootElement.TryGetProperty(property.Name,out var old)
                   && old.GetRawText()==property.Value.GetRawText())continue;
                CountProperty(phase,property.Name);
                for(var k=0;k<Collections.Length;k++)
                {
                    if(property.Name!=Collections[k])continue;
                    if(old.ValueKind==JsonValueKind.Array&&property.Value.ValueKind==JsonValueKind.Array)
                    {
                        // Count net changed fields, not writes, reads or transient effects.
                        Action<string>? countField=property.Name is "Citizens" or "Projects"
                            ?name=>CountNestedField(phase,property.Name+"."+name):null;
                        _changedRecords[phase,k]+=ChangedById(old,property.Value,countField);
                    }
                    else _changedRecords[phase,k]+=property.Value.ValueKind==JsonValueKind.Array
                        ?property.Value.GetArrayLength():1;
                }
            }
            foreach(var property in before.RootElement.EnumerateObject())
                if(!after.RootElement.TryGetProperty(property.Name,out _))CountProperty(phase,property.Name);
        }
        // Drop the retained full snapshot after the last phase of the sampled Step.
        _previous=phase==StepCostProfile.Names.Length-1?null:snapshot;
    }

    void CountProperty(int phase,string name)
    {
        var counts=_properties[phase];
        counts[name]=counts.GetValueOrDefault(name)+1;
    }

    void CountNestedField(int phase,string name)
    {
        var counts=_nestedFields[phase];
        counts[name]=counts.GetValueOrDefault(name)+1;
    }

    static void CountChangedFields(JsonElement before,JsonElement after,Action<string> count)
    {
        foreach(var field in after.EnumerateObject())
            if(!before.TryGetProperty(field.Name,out var old)||old.GetRawText()!=field.Value.GetRawText())
                count(field.Name);
        foreach(var field in before.EnumerateObject())
            if(!after.TryGetProperty(field.Name,out _))count(field.Name);
    }

    static long ChangedById(JsonElement before,JsonElement after,Action<string>? countField=null)
    {
        // JsonElements retain the live parent JsonDocument for this Observe call.
        var old=new Dictionary<int,JsonElement>();
        foreach(var item in before.EnumerateArray())
        {
            if(!item.TryGetProperty("Id",out var id)||!id.TryGetInt32(out var number))
                throw new InvalidOperationException("PR57 tracked entity lacks integer Id");
            old[number]=item;
        }
        long changes=0;
        foreach(var item in after.EnumerateArray())
        {
            if(!item.TryGetProperty("Id",out var id)||!id.TryGetInt32(out var number))
                throw new InvalidOperationException("PR57 tracked entity lacks integer Id");
            if(!old.Remove(number,out var original))
            {
                changes++;
                countField?.Invoke("<added>");
            }
            else if(original.GetRawText()!=item.GetRawText())
            {
                changes++;
                if(countField is not null)CountChangedFields(original,item,countField);
            }
        }
        if(countField is not null)
            foreach(var _ in old)countField("<removed>");
        return changes+old.Count;
    }

    public IEnumerable<string> ReportLines()
    {
        yield return $"PR57_PHASE_EFFECT_SUMMARY samples={Samples} intervalSteps={_every} observedSteps={_steps} " +
                     "classification=NET_SERIALIZED_STATE_ONLY";
        for(var i=0;i<StepCostProfile.Names.Length;i++)
        {
            var top=string.Join(",",_properties[i].OrderByDescending(x=>x.Value)
                .ThenBy(x=>x.Key,StringComparer.Ordinal).Take(8).Select(x=>$"{x.Key}:{x.Value}"));
            var records=string.Join(",",Enumerable.Range(0,Collections.Length)
                .Select(k=>$"{Collections[k]}:{_changedRecords[i,k]}"));
            var nested=string.Join(",",_nestedFields[i].OrderByDescending(x=>x.Value)
                .ThenBy(x=>x.Key,StringComparer.Ordinal).Take(20).Select(x=>$"{x.Key}:{x.Value}"));
            yield return $"PR57_PHASE_EFFECT name={StepCostProfile.Names[i]} samples={Samples} changed={_changedSteps[i]} " +
                         $"unchanged={Samples-_changedSteps[i]} topFields={top} netEntityChanges={records} nestedFields={nested}";
        }
    }
}
