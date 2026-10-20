using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndexCities.Simulation;

// Scales are part of the save format, not gameplay tuning. Formula inputs and
// presentation keep their usual units; only each delta crosses this boundary.
public static class FixedPoint
{
    public const long TimeScale=1_000_000;
    public const long ResourceScale=1_000_000_000;
    public const long PointScale=1_000_000_000_000;
    // Pollution cells: exact decimal points kept to 20 places, so multiplicative
    // decay never stalls above the 1e-10 removal threshold.
    public const int PollutionDecimals=20;public const decimal PollutionRemovalThreshold=0.0000000001m;
    public static decimal Pollution(decimal value)=>decimal.Round(value,PollutionDecimals,MidpointRounding.AwayFromZero);
    public static long Quantize(double value,long scale)=>checked((long)Math.Round(value*scale,MidpointRounding.AwayFromZero));
    public static double Value(long units,long scale)=>units/(double)scale;
    public static long Add(long units,double delta,long scale)=>checked(units+Quantize(delta,scale));
    // Round the product once, without converting the accumulated value to double.
    public static long Multiply(long units,double factor,MidpointRounding rounding=MidpointRounding.AwayFromZero)=>checked((long)decimal.Round(units*(decimal)factor,0,rounding));
}

// The public dictionary boundary uses catalogue units. All persistent quantities
// and AddAmount operations use integer nanos of those units. Copies stay exact.
[JsonConverter(typeof(ResourceStockConverter))]
public sealed class ResourceStock : IDictionary<ResourceKind,double>
{
    public Dictionary<ResourceKind,long> Units {get;}=new();
    public ResourceStock(){}
    public ResourceStock(IEnumerable<KeyValuePair<ResourceKind,double>> source)
    {
        if(source is ResourceStock stock){foreach(var pair in stock.Units)Units.Add(pair.Key,pair.Value);}
        else foreach(var pair in source)Add(pair.Key,pair.Value);
    }
    public double this[ResourceKind key]{get=>FixedPoint.Value(Units[key],FixedPoint.ResourceScale);set=>Units[key]=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddAmount(ResourceKind key,double delta)=>Units[key]=FixedPoint.Add(Units.GetValueOrDefault(key),delta,FixedPoint.ResourceScale);
    public void AddUnits(ResourceKind key,long delta)=>Units[key]=checked(Units.GetValueOrDefault(key)+delta);
    public ICollection<ResourceKind> Keys=>Units.Keys;
    public ICollection<double> Values=>Units.Values.Select(x=>FixedPoint.Value(x,FixedPoint.ResourceScale)).ToArray();
    public int Count=>Units.Count;
    public bool IsReadOnly=>false;
    public void Add(ResourceKind key,double value)=>Units.Add(key,FixedPoint.Quantize(value,FixedPoint.ResourceScale));
    public bool ContainsKey(ResourceKind key)=>Units.ContainsKey(key);
    public bool Remove(ResourceKind key)=>Units.Remove(key);
    public bool TryGetValue(ResourceKind key,out double value){var found=Units.TryGetValue(key,out var units);value=FixedPoint.Value(units,FixedPoint.ResourceScale);return found;}
    public void Add(KeyValuePair<ResourceKind,double> item)=>Add(item.Key,item.Value);
    public void Clear()=>Units.Clear();
    public bool Contains(KeyValuePair<ResourceKind,double> item)=>TryGetValue(item.Key,out var value)&&value==item.Value;
    public void CopyTo(KeyValuePair<ResourceKind,double>[] array,int arrayIndex){foreach(var pair in this)array[arrayIndex++]=pair;}
    public bool Remove(KeyValuePair<ResourceKind,double> item)=>Contains(item)&&Remove(item.Key);
    public IEnumerator<KeyValuePair<ResourceKind,double>> GetEnumerator(){foreach(var pair in Units)yield return new(pair.Key,FixedPoint.Value(pair.Value,FixedPoint.ResourceScale));}
    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
    public double GetValueOrDefault(ResourceKind key)=>FixedPoint.Value(Units.GetValueOrDefault(key),FixedPoint.ResourceScale);
}

public sealed class ResourceStockConverter : JsonConverter<ResourceStock>
{
    public override ResourceStock Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)
    {
        var stock=new ResourceStock();var units=JsonSerializer.Deserialize<Dictionary<ResourceKind,long>>(ref reader,options)??throw new JsonException("Estoque vazio");
        foreach(var pair in units)stock.Units.Add(pair.Key,pair.Value);return stock;
    }
    public override void Write(Utf8JsonWriter writer,ResourceStock value,JsonSerializerOptions options)=>JsonSerializer.Serialize(writer,value.Units,options);
}
