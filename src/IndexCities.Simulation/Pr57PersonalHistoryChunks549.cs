using System.Collections;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndexCities.Simulation;

// PR57 #549: experimental, opt-in representation. The public JSON remains a
// PersonalEvent array; the canonical event order and every record are retained.
// Arbitrary IList mutations materialize and fall back to the ordinary list.
[JsonConverter(typeof(Pr57PersonalHistoryJsonConverter))]
public sealed class Pr57PersonalHistory : Collection<PersonalEvent>
{
    readonly Pr57PersonalHistoryStorage _storage;

    public Pr57PersonalHistory() : this(new Pr57PersonalHistoryStorage()) { }
    Pr57PersonalHistory(Pr57PersonalHistoryStorage storage) : base(storage) => _storage=storage;
    public Pr57PersonalHistory(IEnumerable<PersonalEvent> entries) : this()
    {
        foreach(var entry in entries)Add(entry);
    }

    public static implicit operator Pr57PersonalHistory(List<PersonalEvent> entries)=>new(entries);
    public static implicit operator List<PersonalEvent>(Pr57PersonalHistory entries)=>entries.ToList();

    public void EnableChunks()=>_storage.EnableChunks();
    [JsonIgnore] public int SealedEvents=>_storage.SealedEvents;
    [JsonIgnore] public int SealedBytes=>_storage.SealedBytes;
    [JsonIgnore] public int SealedBlocks=>_storage.SealedBlocks;

    public void AddRange(IEnumerable<PersonalEvent> entries)
    {
        foreach(var entry in entries)Add(entry);
    }
}

public sealed class Pr57PersonalHistoryJsonConverter : JsonConverter<Pr57PersonalHistory>
{
    public override Pr57PersonalHistory Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)
        =>new(JsonSerializer.Deserialize<List<PersonalEvent>>(ref reader,options)??new());

    public override void Write(Utf8JsonWriter writer,Pr57PersonalHistory value,JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach(var entry in value)JsonSerializer.Serialize(writer,entry,options);
        writer.WriteEndArray();
    }
}

internal sealed class Pr57PersonalHistoryStorage : IList<PersonalEvent>
{
    const int BlockSize=64;
    List<PersonalEvent>? _raw=new();
    readonly List<byte[]> _sealed=new();
    readonly List<PersonalEvent> _tail=new();
    int _sealedEvents;

    public int Count=>_raw?.Count??_sealedEvents+_tail.Count;
    public bool IsReadOnly=>false;
    public int SealedEvents=>_sealedEvents;
    public int SealedBlocks=>_sealed.Count;
    public int SealedBytes=>_sealed.Sum(b=>b.Length);

    public void EnableChunks()
    {
        if(_raw is null)return;
        var entries=_raw;
        _raw=null;
        foreach(var entry in entries)AppendChunked(entry);
    }

    static byte[] Pack(List<PersonalEvent> entries)
    {
        var json=JsonSerializer.SerializeToUtf8Bytes(entries);
        using var output=new MemoryStream();
        using(var stream=new BrotliStream(output,CompressionLevel.Fastest,leaveOpen:true))
            stream.Write(json);
        return output.ToArray();
    }

    static PersonalEvent[] Unpack(byte[] bytes)
    {
        using var input=new MemoryStream(bytes,writable:false);
        using var stream=new BrotliStream(input,CompressionMode.Decompress);
        return JsonSerializer.Deserialize<PersonalEvent[]>(stream)
            ??throw new InvalidDataException("Bloco de histórico pessoal vazio");
    }

    void AppendChunked(PersonalEvent entry)
    {
        _tail.Add(entry);
        if(_tail.Count!=BlockSize)return;
        _sealed.Add(Pack(_tail));
        _sealedEvents+=BlockSize;
        _tail.Clear();
    }

    void EnsureRaw()
    {
        if(_raw is not null)return;
        var entries=new List<PersonalEvent>(Count);
        foreach(var block in _sealed)entries.AddRange(Unpack(block));
        entries.AddRange(_tail);
        _raw=entries;
        _sealed.Clear();
        _tail.Clear();
        _sealedEvents=0;
    }

    public PersonalEvent this[int index]
    {
        get
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            if(_raw is not null)return _raw[index];
            return index<_sealedEvents
                ?Unpack(_sealed[index/BlockSize])[index%BlockSize]
                :_tail[index-_sealedEvents];
        }
        set
        {
            EnsureRaw();
            _raw![index]=value;
        }
    }

    public void Add(PersonalEvent item)
    {
        if(_raw is not null)_raw.Add(item);
        else AppendChunked(item);
    }

    public void Clear()
    {
        _raw=new();
        _sealed.Clear();
        _tail.Clear();
        _sealedEvents=0;
    }

    public bool Contains(PersonalEvent item)=>IndexOf(item)>=0;
    public int IndexOf(PersonalEvent item)
    {
        if(_raw is not null)return _raw.IndexOf(item);
        var index=0;
        foreach(var entry in this)
        {
            if(EqualityComparer<PersonalEvent>.Default.Equals(entry,item))return index;
            index++;
        }
        return -1;
    }

    public void Insert(int index,PersonalEvent item)
    {
        if(index==Count){Add(item);return;}
        EnsureRaw();
        _raw!.Insert(index,item);
    }

    public bool Remove(PersonalEvent item)
    {
        EnsureRaw();
        return _raw!.Remove(item);
    }

    public void RemoveAt(int index)
    {
        EnsureRaw();
        _raw!.RemoveAt(index);
    }

    public void CopyTo(PersonalEvent[] array,int arrayIndex)
    {
        foreach(var entry in this)array[arrayIndex++]=entry;
    }

    public IEnumerator<PersonalEvent> GetEnumerator()
    {
        if(_raw is not null)
        {
            foreach(var entry in _raw)yield return entry;
            yield break;
        }
        foreach(var block in _sealed)
            foreach(var entry in Unpack(block))yield return entry;
        foreach(var entry in _tail)yield return entry;
    }

    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}