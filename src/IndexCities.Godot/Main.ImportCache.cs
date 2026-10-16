using Godot;
using System.Text.RegularExpressions;
namespace IndexCities.Game;
public partial class Main
{
    const string ImportCommand="godot --headless --path src/IndexCities.Godot --editor --import --quit";
    static readonly Regex ImportedResource=new(@"""res://\.godot/imported/[^""]+""",RegexOptions.Compiled);

    bool EnsureImportCachePresent()
    {
        var projectDir=ProjectSettings.GlobalizePath("res://");
        if(projectDir.Length==0||!System.IO.File.Exists(System.IO.Path.Combine(projectDir,"project.godot")))return true;
        if(ImportCacheComplete(projectDir))return true;
        GD.PushError("INDEXCITIES_IMPORT_CACHE_MISSING: cache de import do Godot ausente ou incompleto em '"+System.IO.Path.Combine(projectDir,".godot","imported")+"'. Importe os recursos antes de executar o jogo: "+ImportCommand+" (ou use scripts/Play.ps1, que importa por padrão).");
        SetProcess(false);SetProcessInput(false);SetProcessUnhandledInput(false);
        GetTree().Quit(1);
        return false;
    }

    static bool ImportCacheComplete(string projectDir)
    {
        var found=false;
        try
        {
            foreach(var importFile in System.IO.Directory.EnumerateFiles(projectDir,"*.import",System.IO.SearchOption.AllDirectories))
                foreach(Match match in ImportedResource.Matches(System.IO.File.ReadAllText(importFile)))
                {
                    found=true;
                    if(!System.IO.File.Exists(ProjectSettings.GlobalizePath(match.Value[1..^1])))return false;
                }
        }
        catch(System.Exception){return true;}
        return found;
    }
}
