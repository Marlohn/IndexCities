using System.Text.Json;
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
    public int Speed {get;set;}=1;
    public double AutosaveMinutes {get;set;}=5;
}
public static class CitySaves
{
    internal static string? VerificationDirectory {get;set;}
    public static string DirectoryPath=>VerificationDirectory??ProjectSettings.GlobalizePath("user://cities");
    static void ValidateId(string id){if(id.Length!=32||id.Any(c=>!char.IsAsciiHexDigit(c)))throw new InvalidOperationException("Identificador de cidade inválido");}
    public static string NewId()=>Guid.NewGuid().ToString("N");
    public static IEnumerable<(string Id,string Name,DateTime At)> List()
    {
        if(!System.IO.Directory.Exists(DirectoryPath))yield break;
        foreach(var path in System.IO.Directory.EnumerateFiles(DirectoryPath,"*.json").OrderByDescending(System.IO.File.GetLastWriteTimeUtc)){
            CitySave? save=null;CitySimulation? sim=null;
            try{save=JsonSerializer.Deserialize<CitySave>(System.IO.File.ReadAllText(path));if(save!=null)sim=CitySimulation.FromJson(save.CityJson);}catch(Exception){ }
            if(sim!=null)yield return(System.IO.Path.GetFileNameWithoutExtension(path),sim.State.Name,sim.State.Now);
        }
    }
    public static void Save(string id,CitySave save)
    {
        ValidateId(id);
        System.IO.Directory.CreateDirectory(DirectoryPath);var target=System.IO.Path.Combine(DirectoryPath,id+".json");var temp=target+".tmp";
        System.IO.File.WriteAllText(temp,JsonSerializer.Serialize(save));System.IO.File.Move(temp,target,true);
    }
    public static CitySave Load(string id){ValidateId(id);return JsonSerializer.Deserialize<CitySave>(System.IO.File.ReadAllText(System.IO.Path.Combine(DirectoryPath,id+".json")))??throw new InvalidOperationException("Save vazio");}
}
