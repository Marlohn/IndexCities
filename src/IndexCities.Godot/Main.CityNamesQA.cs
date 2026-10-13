using Godot;
using IndexCities.Simulation;
using System.Text.Json.Nodes;
namespace IndexCities.Game;

public partial class Main
{
    async Task TypeCityName(LineEdit field,string value)
    {
        field.GrabFocus();field.SelectAll();
        foreach(var rune in value.EnumerateRunes()) {
            RecoveryInput(new InputEventKey { Pressed=true, Unicode=(uint)rune.Value },_modal);
            await RecoveryFrame();
        }
        Require(field.Text==value,"Campo aceita nome digitado, inclusive acentos");
    }

    async void CityNamesSmoke()
    {
        var directory=System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/recovery-v2/names-"+Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(directory);
        try {
            Require(DisplayServer.GetName()!="headless","Nomes exigem GUI renderizada");
            _speed=0;_autosaveMinutes=0;CitySaves.VerificationDirectory=System.IO.Path.Combine(directory,"saves");
            _building=BuildingKind.House;_cursor=new(-120,14);_hoverPreview=true;_holdLoadedPreview=true;_rotation=Math.PI/2;
            var id=_cityId;var initial=_sim.SaveToJson();Require(TrySaveCity(),"Partida real salva em diretório isolado");
            var path=System.IO.Path.Combine(CitySaves.DirectoryPath,id+".json");var envelope=JsonNode.Parse(System.IO.File.ReadAllText(path))!;
            await RecoveryButton(RecoveryHudButton("Cidades e autosave"));
            await RecoveryCapture(System.IO.Path.Combine(directory,"cities-before-rename.png"),_modal!);
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="Renomear"));
            await TypeCityName((LineEdit)_modal!.FindChild("RenameCityName",true,false),"São Bento");
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="Cancelar"));
            Require(_sim.SaveToJson()==initial&&JsonNode.DeepEquals(envelope,JsonNode.Parse(System.IO.File.ReadAllText(path))),"Cancelar nome mantém arquivo e cidade");
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="Renomear"));
            await TypeCityName((LineEdit)_modal!.FindChild("RenameCityName",true,false),"São Bento");
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="Confirmar nome"));
            var expected=JsonNode.Parse(initial)!;expected[nameof(CityState.Name)]="São Bento";
            Require(_cityId==id&&JsonNode.DeepEquals(expected,JsonNode.Parse(_sim.SaveToJson())),"Renomear altera somente nome da partida aberta");
            envelope[nameof(CitySave.CityJson)]=CitySaves.Load(id).CityJson;
            Require(JsonNode.DeepEquals(envelope,JsonNode.Parse(System.IO.File.ReadAllText(path)))&&CitySaves.List().Single().Id==id,"Mesmo arquivo, câmera, preferências e esboços");
            Require(JsonNode.DeepEquals(expected,JsonNode.Parse(CitySaves.Load(id).CityJson)),"Arquivo renomeado conserva simulação integral");
            await RecoveryCapture(System.IO.Path.Combine(directory,"cities-after-rename.png"),_modal!);

            // Forward/legacy fields are controlled persistence fixtures, not
            // actors, resources or activity added to the running city.
            var other=CitySaves.NewId();var otherPath=System.IO.Path.Combine(CitySaves.DirectoryPath,other+".json");
            var legacy=envelope.DeepClone();legacy["UnknownHostPreference"]=17;
            var legacyCity=expected.DeepClone();legacyCity["UnknownSimulationField"]=new JsonObject { ["keep"]=true };
            legacy[nameof(CitySave.CityJson)]=legacyCity.ToJsonString();System.IO.File.WriteAllText(otherPath,legacy.ToJsonString());
            CitySaves.Rename(other,"Outra cidade");legacyCity[nameof(CityState.Name)]="Outra cidade";legacy[nameof(CitySave.CityJson)]=legacyCity.ToJsonString();
            Require(JsonNode.DeepEquals(legacy,JsonNode.Parse(System.IO.File.ReadAllText(otherPath))),"Renomear outra partida preserva campos desconhecidos sem migrar save");
            var beforeInvalid=System.IO.File.ReadAllText(otherPath);var refused=false;
            try{CitySaves.Rename(other," ");}catch(ArgumentException){refused=true;}
            Require(refused&&System.IO.File.ReadAllText(otherPath)==beforeInvalid,"Nome vazio recusa sem sobrescrever arquivo");
            using(var lockedOther=System.IO.File.Open(otherPath,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.Read)) {
                var blocked=false;try{CitySaves.Rename(other,"Nome bloqueado");}catch(Exception exception)when(exception is System.IO.IOException or UnauthorizedAccessException){blocked=true;}
                Require(blocked&&System.IO.File.ReadAllText(otherPath)==beforeInvalid,"Falha na troca atômica de cidade não aberta preserva arquivo integral");
            }
            using(var locked=System.IO.File.Open(path,System.IO.FileMode.Open,System.IO.FileAccess.Read,System.IO.FileShare.Read)) {
                await RecoveryButton(RecoveryNodes<Button>(_modal!).First(b=>b.Text=="Renomear"));
                await TypeCityName((LineEdit)_modal!.FindChild("RenameCityName",true,false),"Nome recusado");
                await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="Confirmar nome"));
                Require(_sim.State.Name=="São Bento"&&_modal!.Title=="Renomear cidade","Falha de escrita mantém nome aberto e exibe erro");
            }
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="Cancelar"));
            // Isolate the saved-current row; the other fixture was written later.
            await RecoveryCloseModal();await RecoveryButton(RecoveryHudButton("Cidades e autosave"));
            var createName=(LineEdit)_modal!.FindChild("NewCityName",true,false);
            await TypeCityName(createName,"Vila do Sol");
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text=="+ Criar"));
            Require(_sim.State.Name=="Vila do Sol"&&_sim.State.Seed==42&&_sim.State.Buildings.Count==0&&_cityId!=id,"Nome opcional não substitui código nem carrega outra partida");
            Require(JsonNode.DeepEquals(expected,JsonNode.Parse(CitySaves.Load(id).CityJson)),"Criar preserva a partida anterior renomeada");
            Require(TrySaveCity(),"Nova cidade nomeada é persistida");
            await RecoveryButton(RecoveryHudButton("Cidades e autosave"));
            await RecoveryButton(RecoveryNodes<Button>(_modal!).Single(b=>b.Text.StartsWith("São Bento •")));
            Require(_cityId==id&&JsonNode.DeepEquals(expected,JsonNode.Parse(_sim.SaveToJson()))&&_building==BuildingKind.House&&_rotation==Math.PI/2,"Retomar cidade com nome conserva estado e ferramenta");
            NewCity(7);Require(_sim.State.Name=="Cidade 7","Nome opcional vazio mantém rótulo padrão");
            _sim.CheckInvariants();
            GD.Print("INDEXCITIES_CITY_NAMES_OK rename=same-file state=preserved unknown-fields=preserved cancel=preserved write-failure=preserved create=optional load=preserved directory="+directory);
            GetTree().Quit();
        }catch(Exception exception){GD.PushError(exception.ToString());GetTree().Quit(1);}
        finally{CitySaves.VerificationDirectory=null;}
    }
}
