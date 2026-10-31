using System.Text.Json;
using IndexCities.Simulation;
using Xunit;

namespace IndexCities.Simulation.Tests;

public sealed class Pr57PersonalHistoryChunksTests
{
    static List<PersonalEvent> Events(int count)
        =>Enumerable.Range(0,count).Select(i=>
            new PersonalEvent(new DateTime(2026,1,1,6,0,0).AddSeconds(i),
                "Pagamento real: ação "+(i%7)+" / UTF-8 çã \" "+i)).ToList();

    [Fact]
    public void Chunking_keeps_every_event_order_index_and_exact_public_json()
    {
        var expected=Events(259);
        var history=new Pr57PersonalHistory(expected);
        var canonical=JsonSerializer.Serialize(history);
        history.EnableChunks();
        Assert.Equal(4,history.SealedBlocks);
        Assert.Equal(256,history.SealedEvents);
        Assert.True(history.SealedBytes>0);
        Assert.Equal(expected.Count,history.Count);
        Assert.Equal(expected[65],history[65]);
        Assert.Equal(expected,history.ToArray());
        Assert.Equal(canonical,JsonSerializer.Serialize(history));
        var last=new PersonalEvent(new DateTime(2026,1,2),"Último registro");
        history.Add(last);expected.Add(last);
        Assert.Equal(expected,history.ToArray());
        Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(history));
        var loaded=JsonSerializer.Deserialize<Pr57PersonalHistory>(JsonSerializer.Serialize(history));
        Assert.NotNull(loaded);
        Assert.Equal(expected,loaded.ToArray());
    }

    [Fact]
    public void External_insert_replace_remove_and_clear_keep_list_semantics()
    {
        var expected=Events(140);
        var history=new Pr57PersonalHistory(expected);
        history.EnableChunks();
        var inserted=new PersonalEvent(new DateTime(2026,2,1),"inserido");
        history.Insert(3,inserted);expected.Insert(3,inserted);
        var replacement=new PersonalEvent(new DateTime(2026,2,2),"substituído");
        history[70]=replacement;expected[70]=replacement;
        history.RemoveAt(4);expected.RemoveAt(4);
        Assert.Equal(expected,history.ToArray());
        Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(history));
        history.EnableChunks();
        Assert.Equal(expected,history.ToArray());
        history.Clear();
        Assert.Empty(history);
        Assert.Equal("[]",JsonSerializer.Serialize(history));
    }

    [Fact]
    public void City_state_json_is_byte_identical_with_chunked_citizen_history()
    {
        var events=Events(190);
        var state=new CityState();
        state.Citizens.Add(new Citizen{Id=123,History=new Pr57PersonalHistory(events)});
        var baseline=JsonSerializer.Serialize(state);
        state.Citizens[0].History.EnableChunks();
        Assert.Equal(baseline,JsonSerializer.Serialize(state));
        var restored=JsonSerializer.Deserialize<CityState>(baseline);
        Assert.NotNull(restored);
        Assert.Equal(events,restored.Citizens[0].History.ToArray());
        restored.Citizens[0].History.EnableChunks();
        Assert.Equal(baseline,JsonSerializer.Serialize(restored));
    }
}