using System.Text.Json;
using System.Text.Json.Serialization;
namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    static readonly JsonSerializerOptions SaveOptions=new(){WriteIndented=false,Converters={new JsonStringEnumConverter()}};
    public string SaveToJson(){CheckInvariants();return JsonSerializer.Serialize(State,SaveOptions);}
    public static CitySimulation FromJson(string json)
    {
        var state=JsonSerializer.Deserialize<CityState>(json,SaveOptions)??throw new InvalidDataException("Save vazio");
        if(state.SchemaVersion!=1)throw new InvalidDataException("Versão de cidade não suportada");
        return new(state);
    }
}
