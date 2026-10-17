using System.Text.Json;
using System.Text.Json.Serialization;
namespace IndexCities.Simulation;

public sealed partial class CitySimulation
{
    static readonly JsonSerializerOptions SaveOptions=new(){WriteIndented=false,Converters={new JsonStringEnumConverter()}};
    public string SaveToJson(){CheckInvariants();LakeGeometry.Validate(State);return JsonSerializer.Serialize(State,SaveOptions);}
    public static CitySimulation FromJson(string json)
    {
        var state=JsonSerializer.Deserialize<CityState>(json,SaveOptions)??throw new InvalidDataException("Save vazio");
        if(state.SchemaVersion is not (1 or 2))throw new InvalidDataException("Versão de cidade não suportada");
        if(state.ConstructionStateVersion<1)throw new InvalidDataException("Cidade salva numa versão antiga do jogo; até a versão estável, saves antigos não podem ser abertos");
        return new(state);
    }
}
