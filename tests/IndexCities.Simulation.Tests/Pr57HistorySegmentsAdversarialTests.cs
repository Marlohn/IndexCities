using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class Pr57HistorySegmentsAdversarialTests
{
    [Fact] public void Append_only_segments_keep_exact_json_and_order()
    {
        var canonical=new Citizen{Id=7,Name="Teste"};
        var candidate=new Citizen{Id=7,Name="Teste"};
        var start=new DateTime(2026,10,31,12,0,0,DateTimeKind.Utc);
        for(var i=0;i<193;i++)
        {
            var entry=new PersonalEvent(start.AddSeconds(i),"evento "+i);
            canonical.History.Add(entry);
            candidate.Pr57AppendPersonalEvent(entry);
        }
        Assert.Equal(193,candidate.Pr57HistoryEventCount);
        if(Environment.GetEnvironmentVariable("PR57_HISTORY_SEGMENTS")=="1")
            Assert.True(candidate.Pr57HistorySegmented);
        var expected=JsonSerializer.Serialize(canonical);
        Assert.Equal(expected,JsonSerializer.Serialize(candidate));
        Assert.Contains("\"History\":[",expected);
        Assert.DoesNotContain("Pr57HistoryJson",expected);
        Assert.Equal(193,candidate.History.Count);
        Assert.False(candidate.Pr57HistorySegmented);
    }

    [Fact] public void Retained_external_list_alias_and_edits_force_canonical_fallback()
    {
        var candidate=new Citizen{Id=8};
        var alias=candidate.History; // Must remain live after later Personal appends.
        candidate.Pr57AppendPersonalEvent(new PersonalEvent(DateTime.UnixEpoch,"first"));
        Assert.Same(alias,candidate.History);
        Assert.Single(alias);
        alias[0]=new PersonalEvent(DateTime.UnixEpoch,"changed");
        alias.Add(new PersonalEvent(DateTime.UnixEpoch,"external"));
        candidate.Pr57AppendPersonalEvent(new PersonalEvent(DateTime.UnixEpoch,"last"));
        Assert.Equal(new[]{"changed","external","last"},alias.Select(e=>e.Description));
        Assert.False(candidate.Pr57HistorySegmented);
        var replacement=new List<PersonalEvent>{new(DateTime.UnixEpoch,"replacement")};
        candidate.History=replacement;
        candidate.Pr57AppendPersonalEvent(new PersonalEvent(DateTime.UnixEpoch,"after setter"));
        Assert.Same(replacement,candidate.History);
        Assert.Equal(2,replacement.Count);
    }

    [Fact] public void Save_load_preserves_history_and_external_mutation()
    {
        var original=new Citizen{Id=9};
        for(var i=0;i<73;i++)
            original.Pr57AppendPersonalEvent(new PersonalEvent(DateTime.UnixEpoch.AddSeconds(i),"event "+i));
        var json=JsonSerializer.Serialize(original);
        var loaded=JsonSerializer.Deserialize<Citizen>(json)!;
        Assert.Equal(json,JsonSerializer.Serialize(loaded));
        var alias=loaded.History;
        alias.RemoveAt(0);
        loaded.Pr57AppendPersonalEvent(new PersonalEvent(DateTime.UnixEpoch,"later"));
        Assert.Equal(73,alias.Count);
        Assert.Equal("event 1",alias[0].Description);
        Assert.Equal("later",alias[^1].Description);
    }
}
