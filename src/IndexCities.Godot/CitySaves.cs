using System.Text.Json;
using System.Text.Json.Nodes;
using IndexCities.Simulation;
using Godot;
namespace IndexCities.Game;
public sealed class CitySave
{
    public string CityJson {get;set;}="";
    public float CameraX {get;set;}=-80;
    public float CameraZ {get;set;}=0;
    public float CameraYaw {get;set;}=45;
    public float CameraZoom {get;set;}=180;
    public BuildingKind? DraftBuilding {get;set;}
    public Point? DraftAnchor {get;set;}
    public Point? DraftCursor {get;set;}
    public Point? DraftRoadStart {get;set;}
    public Point? DraftRoadEnd {get;set;}
    public int? DraftMoveProject {get;set;}
    public double DraftRotation {get;set;}
    public bool DraftReverse {get;set;}
    public bool? DraftRoadFixed {get;set;}
    public bool RoadTool {get;set;}
    public bool DemolitionTool {get;set;}
    public int Speed {get;set;}=1;
    public double AutosaveMinutes {get;set;}=5;
}
public static class CitySaves
{
    internal static string? VerificationDirectory {get;set;}
    public static string DirectoryPath=>VerificationDirectory??ProjectSettings.GlobalizePath("user://cities");
    static void ValidateId(string id){if(id.Length!=32||id.Any(c=>!char.IsAsciiHexDigit(c)))throw new InvalidOperationException("Identificador de cidade inválido");}
    public static string NewId()=>Guid.NewGuid().ToString("N");
    public static IEnumerable<(string Id,string Name,DateTime At)> List()=>ListAll().Where(saved=>!saved.Outdated).Select(saved=>(saved.Id,saved.Name,saved.At));
    // Outdated: the file is a readable city envelope that this build refuses to
    // load (no conversion until a stable version); listed so opening it warns.
    public static IEnumerable<(string Id,string Name,DateTime At,bool Outdated)> ListAll()
    {
        if(!System.IO.Directory.Exists(DirectoryPath))yield break;
        foreach(var path in System.IO.Directory.EnumerateFiles(DirectoryPath,"*.json").OrderByDescending(System.IO.File.GetLastWriteTimeUtc)){
            CitySave? save=null;CitySimulation? sim=null;var id=System.IO.Path.GetFileNameWithoutExtension(path);
            try{save=JsonSerializer.Deserialize<CitySave>(System.IO.File.ReadAllText(path));if(save!=null)sim=CitySimulation.FromJson(save.CityJson);}catch(Exception){ }
            if(sim!=null)yield return(id,sim.State.Name,sim.State.Now,false);
            else if(save!=null&&OutdatedSummary(save.CityJson) is {} old)yield return(id,old.Name,old.At,true);
        }
    }
    static (string Name,DateTime At)? OutdatedSummary(string cityJson)
    {
        try{var city=System.Text.Json.Nodes.JsonNode.Parse(cityJson)!.AsObject();
            return (city["Name"]?.GetValue<string>() is {Length:>0} name?name:"Cidade sem nome",city["Now"]?.GetValue<DateTime>()??DateTime.MinValue);}
        catch(Exception){return null;}
    }
    public static void Save(string id,CitySave save)
        =>WriteJson(id,JsonSerializer.Serialize(save));
    static void WriteJson(string id,string json)
    {
        ValidateId(id);
        System.IO.Directory.CreateDirectory(DirectoryPath);var target=System.IO.Path.Combine(DirectoryPath,id+".json");var temp=target+".tmp";
        System.IO.File.WriteAllText(temp,json);System.IO.File.Move(temp,target,true);
    }
    public static string NormalizeName(string name)
    {
        var value=name.Trim();
        if(value.Any(char.IsControl))throw new ArgumentException("Use um nome em uma única linha");
        return value;
    }
    public static string Rename(string id,string name)
    {
        ValidateId(id);var value=NormalizeName(name);
        if(value.Length==0)throw new ArgumentException("Digite o nome da cidade");
        var path=System.IO.Path.Combine(DirectoryPath,id+".json");
        var save=JsonNode.Parse(System.IO.File.ReadAllText(path))?.AsObject()??throw new InvalidOperationException("Save vazio");
        var city=JsonNode.Parse(save[nameof(CitySave.CityJson)]?.GetValue<string>()??"")?.AsObject()??throw new InvalidOperationException("Cidade vazia");
        // Change only the name, preserving legacy/unknown fields and all host
        // preferences. Deserializing the simulation here would migrate its state.
        city[nameof(CityState.Name)]=value;save[nameof(CitySave.CityJson)]=city.ToJsonString();
        WriteJson(id,save.ToJsonString());return value;
    }
    public static CitySave Load(string id){ValidateId(id);return JsonSerializer.Deserialize<CitySave>(System.IO.File.ReadAllText(System.IO.Path.Combine(DirectoryPath,id+".json")))??throw new InvalidOperationException("Save vazio");}
}
