// PR57 #472: isolated .NET 8 byte-exact probe, NOT a replacement for CitySimulation.
// Baseline is raw UTF-8 event objects (not canonical typed PersonalEvent/MoneyMovement).
// Input: authentic #465 pre-midnight and midnight saves, unchanged engine.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

interface ILog
{
    int Count(string key);
    byte[] Get(string key, int index);
    void Append(Dictionary<string,List<byte[]>> delta);
    object Storage();
}

static class Data
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Dictionary<string,List<byte[]>> Load(string path)
    {
        using var doc=JsonDocument.Parse(File.ReadAllBytes(path));
        var root=doc.RootElement;
        var result=new Dictionary<string,List<byte[]>>(StringComparer.Ordinal);
        foreach(var c in root.GetProperty("Citizens").EnumerateArray())
            result.Add("h:"+c.GetProperty("Id").GetInt32().ToString(),
                c.GetProperty("History").EnumerateArray()
                 .Select(e=>Encoding.UTF8.GetBytes(e.GetRawText())).ToList());
        foreach(var name in new[]{"Ledger","CityEvents"})
            result.Add(name,root.GetProperty(name).EnumerateArray()
                .Select(e=>Encoding.UTF8.GetBytes(e.GetRawText())).ToList());
        return result;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Dictionary<string,List<byte[]>> Delta(string oldPath,string newPath)
    {
        var before=Load(oldPath); var after=Load(newPath);
        if(!before.Keys.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(after.Keys.OrderBy(x=>x,StringComparer.Ordinal)))
            throw new InvalidDataException("collection set changed");
        var result=new Dictionary<string,List<byte[]>>(StringComparer.Ordinal);
        foreach(var key in before.Keys)
        {
            var old=before[key]; var now=after[key];
            if(now.Count<old.Count) throw new InvalidDataException("history shrank: "+key);
            for(int i=0;i<old.Count;i++)
                if(!old[i].AsSpan().SequenceEqual(now[i]))
                    throw new InvalidDataException("non-append mutation: "+key+" "+i);
            result[key]=now.Skip(old.Count).ToList();
        }
        return result;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ILog Build(string mode,int block,string path)
    {
        ILog log=mode=="raw" ? new RawLog() : new PackedLog(block);
        log.Append(Load(path));
        return log;
    }
}

sealed class RawLog: ILog
{
    private readonly Dictionary<string,List<byte[]>> events=new(StringComparer.Ordinal);
    public int Count(string key)=>events[key].Count;
    public byte[] Get(string key,int i)=>events[key][i];
    public void Append(Dictionary<string,List<byte[]>> delta)
    {
        foreach(var kv in delta)
        {
            if(!events.TryGetValue(kv.Key,out var list)) events[kv.Key]=list=new();
            list.AddRange(kv.Value);
        }
    }
    public object Storage()=>new { kind="raw-event-byte-arrays", collections=events.Count,
        entries=events.Values.Sum(x=>x.Count), rawBytes=events.Values.Sum(x=>x.Sum(b=>b.Length)) };
}

sealed class PackedLog: ILog
{
    private sealed record Segment(int Begin,int Count,long[] Anchors);
    private readonly int blockSize;
    private readonly List<byte[]> sealedBlocks=new();
    private readonly List<byte> tail=new();
    private readonly Dictionary<string,List<Segment>> segments=new(StringComparer.Ordinal);
    private readonly Dictionary<string,int> counts=new(StringComparer.Ordinal);
    private long offset;
    public PackedLog(int blockSize){this.blockSize=blockSize;}
    public int Count(string key)=>counts[key];
    private static byte[] Compress(byte[] raw)
    {
        using var ms=new MemoryStream();
        using(var z=new ZLibStream(ms,CompressionLevel.Optimal,true)) z.Write(raw);
        return ms.ToArray();
    }
    private static byte[] Decompress(byte[] compressed)
    {
        using var input=new MemoryStream(compressed);
        using var z=new ZLibStream(input,CompressionMode.Decompress);
        using var output=new MemoryStream();
        z.CopyTo(output);
        return output.ToArray();
    }
    private long Write(byte[] value)
    {
        if(value.Length==0||value.Length>65535) throw new InvalidDataException("event outside 1..65535 bytes");
        long start=offset;
        tail.Add((byte)value.Length); tail.Add((byte)(value.Length>>8));
        tail.AddRange(value); offset+=2+value.Length;
        while(tail.Count>=blockSize)
        {
            sealedBlocks.Add(Compress(tail.GetRange(0,blockSize).ToArray()));
            tail.RemoveRange(0,blockSize);
        }
        return start;
    }
    public void Append(Dictionary<string,List<byte[]>> delta)
    {
        foreach(var kv in delta.OrderBy(x=>x.Key,StringComparer.Ordinal))
        {
            if(!counts.TryGetValue(kv.Key,out int old))
            {counts[kv.Key]=old=0;segments[kv.Key]=new();}
            if(kv.Value.Count==0)continue;
            var anchors=new List<long>();
            for(int i=0;i<kv.Value.Count;i++)
            {
                long pos=Write(kv.Value[i]);
                if(i%16==0) anchors.Add(pos);
            }
            segments[kv.Key].Add(new Segment(old,kv.Value.Count,anchors.ToArray()));
            counts[kv.Key]=old+kv.Value.Count;
        }
    }
    private byte[] Block(int id)=>id<sealedBlocks.Count?Decompress(sealedBlocks[id]):tail.ToArray();
    private byte[] Read(long start,int length)
    {
        if(start<0||start+length>offset)throw new InvalidDataException("invalid record offset");
        var result=new byte[length];int written=0;
        while(written<length)
        {
            int id=checked((int)(start/blockSize));int within=checked((int)(start%blockSize));
            var block=Block(id);int n=Math.Min(length-written,block.Length-within);
            if(n<=0)throw new InvalidDataException("invalid compressed block boundary");
            Buffer.BlockCopy(block,within,result,written,n);
            start+=n;written+=n;
        }
        return result;
    }
    public byte[] Get(string key,int index)
    {
        if(index<0||index>=counts[key])throw new ArgumentOutOfRangeException(nameof(index));
        var list=segments[key];int low=0,high=list.Count;
        while(low<high)
        {
            int mid=low+(high-low)/2;
            if(list[mid].Begin<=index)low=mid+1;else high=mid;
        }
        var seg=list[low-1];int rel=index-seg.Begin;
        if(rel<0||rel>=seg.Count)throw new InvalidDataException("segment index");
        long pos=seg.Anchors[rel/16];
        byte[] value=Array.Empty<byte>();
        for(int j=0;j<=rel%16;j++)
        {
            var prefix=Read(pos,2);int n=prefix[0]|(prefix[1]<<8);pos+=2;
            value=Read(pos,n);pos+=n;
        }
        return value;
    }
    public object Storage()
    {
        int count=segments.Values.Sum(x=>x.Count);
        int anchors=segments.Values.Sum(x=>x.Sum(s=>s.Anchors.Length));
        return new {kind="shared-zlib-sparse-index",collections=counts.Count,
            entries=counts.Values.Sum(),blocks=sealedBlocks.Count,
            compressedBytes=sealedBlocks.Sum(x=>x.Length),tailBytes=tail.Count,
            segments=count,anchors,
            hypotheticalPackedIndexBytes=16*counts.Count+24*count+8*anchors};
    }
}

static class Program
{
    static void Collect()
    {
        GC.Collect(2,GCCollectionMode.Forced,true,true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2,GCCollectionMode.Forced,true,true);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static object CheckAndMeasure(ILog log,string finalPath)
    {
        var expected=Data.Load(finalPath);
        var keys=expected.Keys.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        int total=keys.Sum(k=>expected[k].Count);
        var samples=new List<(string key,int index)>();
        var all=keys.SelectMany(k=>Enumerable.Range(0,expected[k].Count).Select(i=>(key:k,index:i))).ToArray();
        for(int i=0;i<250;i++)samples.Add(all[(i*7919)%all.Length]);
        var reads=new double[4];var readAlloc=new long[4];
        for(int pass=0;pass<4;pass++)
        {
            long alloc=GC.GetTotalAllocatedBytes(true);
            var sw=Stopwatch.StartNew();
            foreach(var (key,index) in samples)
                if(!log.Get(key,index).AsSpan().SequenceEqual(expected[key][index]))
                    throw new InvalidDataException("point read parity "+key+" "+index);
            reads[pass]=sw.Elapsed.TotalMilliseconds;
            readAlloc[pass]=GC.GetTotalAllocatedBytes(true)-alloc;
        }
        long exportAllocBefore=GC.GetTotalAllocatedBytes(true);
        var exportWatch=Stopwatch.StartNew();
        long exportedBytes=0;
        foreach(var key in keys)
        {
            if(log.Count(key)!=expected[key].Count)throw new InvalidDataException("count mismatch");
            var original=expected[key];
            var sb=new MemoryStream();
            sb.WriteByte((byte)'[');
            for(int i=0;i<original.Count;i++)
            {
                if(i>0)sb.WriteByte((byte)',');
                sb.Write(log.Get(key,i));
            }
            sb.WriteByte((byte)']');
            var exact=Encoding.UTF8.GetBytes("["+string.Join(",",original.Select(x=>Encoding.UTF8.GetString(x)))+"]");
            if(!sb.ToArray().AsSpan().SequenceEqual(exact))
                throw new InvalidDataException("full collection export mismatch "+key);
            exportedBytes+=sb.Length;
        }
        double exportMs=exportWatch.Elapsed.TotalMilliseconds;
        long exportAlloc=GC.GetTotalAllocatedBytes(true)-exportAllocBefore;
        return new {pointReads=250,events=total,
            readMedianMs=reads.Skip(1).Order().ElementAt(1),
            readMedianAllocatedBytes=readAlloc.Skip(1).Order().ElementAt(1),
            exportMs,exportAllocatedBytes=exportAlloc,exportedBytes,exact=true};
    }
    static int Main(string[] args)
    {
        if(args.Length!=5)throw new ArgumentException("mode blockSize repeat preJson midnightJson");
        string mode=args[0];int block=int.Parse(args[1]);int repeat=int.Parse(args[2]);
        if(mode!="raw"&&mode!="packed")throw new ArgumentException("mode");
        Collect();long heapBefore=GC.GetTotalMemory(true);
        ILog log=Data.Build(mode,block,args[3]);
        Collect();long heapInitial=GC.GetTotalMemory(true);
        var delta=Data.Delta(args[3],args[4]);
        int appended=delta.Values.Sum(x=>x.Count);
        long allocBefore=GC.GetTotalAllocatedBytes(true);
        var sw=Stopwatch.StartNew();log.Append(delta);
        double appendMs=sw.Elapsed.TotalMilliseconds;
        long appendAllocated=GC.GetTotalAllocatedBytes(true)-allocBefore;
        delta=null!;
        Collect();long heapAfter=GC.GetTotalMemory(true);
        var checks=CheckAndMeasure(log,args[4]);
        Console.WriteLine("PR57_472_NET "+JsonSerializer.Serialize(new {
            mode,block,repeat,heapBefore,heapInitial,heapAfter,
            retainedInitialBytes=heapInitial-heapBefore,
            retainedAfterBytes=heapAfter-heapBefore,
            appended,appendMs,appendAllocated,storage=log.Storage(),checks}));
        return 0;
    }
}
