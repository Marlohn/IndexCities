using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class Main
{
    async void PresentationSmoke()
    {
        try {
            Require(DisplayServer.GetName() != "headless", "Apresentação exige Godot renderizado");
            _speed = 0; _autosaveMinutes = 0; _clockBudget = 0;
            Require(_sim.PlaceBuilding(BuildingKind.CityHall, new(-152, 16)).Success, "Fundação real");
            Require(_sim.PlaceRoad(new(-192, 0), new(-80, 0)).Success, "Rua real");
            _sim.Advance(7200);
            Require(_sim.PlaceBuilding(BuildingKind.House, new(-120, 14)).Success, "Casa real");
            _sim.Advance(7200);
            var home = _sim.State.Buildings.Single(b => b.Kind == BuildingKind.House);
            var resident = _sim.State.Citizens.First(c => c.Alive && c.Resident && _sim.State.Families.Any(f => f.Id == c.FamilyId && f.HomeId == home.Id));
            _world.Reset(_sim.State); BuildCatalog(); ClearTool(); _focus = ToV(home.Position); _zoom = 70; UpdateCamera();

            // Controlled presentation fixtures: no simulation step runs between
            // these states. Rendering must never change authoritative state.
            resident.VehicleId = null; resident.ConstructionProjectId = null;
            resident.Path = []; resident.PathIndex = 0; resident.Position = home.Door;
            resident.Activity = "Em casa";
            var atHome = _sim.SaveToJson(); _world.Sync(_sim.State);
            Require(!_world.IsCitizenVisible(resident.Id), "Interior real permanece interior");
            Require(!_world.ActivityIndicator(home.Id).Visible, "Sem balão de presença: edifício sem caminhão real não mostra indicador");
            Require(_sim.SaveToJson() == atHome, "Indicador não altera pessoas, recursos ou dinheiro");

            resident.Activity = "Sem rota";
            _world.Sync(_sim.State);
            Require(_world.IsCitizenVisible(resident.Id), "Pessoa parada junto a porta não desaparece por proximidade");
            SelectAt(home.Door);
            Require(_selectedCitizen == resident.Id, "Pessoa visível pode ser inspecionada");
            ClearSelection(); RefreshHud();

            resident.Position = home.Door + new Point(0, -2);
            resident.Path = [home.Door]; resident.PathIndex = 0; resident.Activity = "Voltando para casa";
            _world.CaptureMotionBeforeStep(_sim.State);
            resident.Position = home.Door; resident.Path = []; resident.PathIndex = 0; resident.Activity = "Em casa";
            _sim.State.AddElapsedSeconds(2);
            _world.Sync(_sim.State, .5);
            Require(_world.IsCitizenVisible(resident.Id), "Chegada interpolada continua visível até alcançar interior");
            _world.Sync(_sim.State);
            Require(!_world.IsCitizenVisible(resident.Id), "Ao terminar chegada o interior continua representado pela pessoa oculta");
            var directory = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath("res://../../artifacts/verification/presentation-visual"));
            System.IO.Directory.CreateDirectory(directory);
            await RecoveryCapture(System.IO.Path.Combine(directory, "people-inside.png"), GetViewport());

            // A maintenance order creates actual trucks/cargo. The indicator is
            // absent while travelling and appears only in a real unloading step.
            var hall = _sim.State.Buildings.Single(b => b.Kind == BuildingKind.CityHall);
            hall.MaintenanceDueAt = _sim.State.ElapsedSeconds - 1;
            _sim.State.EconomyNextAt = _sim.State.ElapsedSeconds;
            var travellingChecked = false; var unloadingChecked = false;
            for (var step = 0; step < 2000 && !unloadingChecked; step++) {
                _sim.Advance(2); _world.Sync(_sim.State);
                var shipment = _sim.State.Shipments.FirstOrDefault(s => !s.Complete && !s.Cancelled && s.DestinationBuildingId == hall.Id && s.Cargo.Values.Sum() > 0);
                if (shipment == null) continue;
                var truck = _sim.State.Vehicles.Single(v => v.Id == shipment.VehicleId);
                var shown = _world.ActivityIndicator(hall.Id);
                if (truck.Status == VehicleStatus.Travelling) { Require(shown.Cargo == 0, "Pedido em trânsito não parece descarga"); travellingChecked = true; }
                if (truck.Status == VehicleStatus.Unloading) { Require(shown.Visible && shown.Cargo > 0, "Descarga física é perceptível no destino"); unloadingChecked = true; }
            }
            Require(travellingChecked && unloadingChecked, "Cenário percorreu viagem e descarga reais");
            _focus = ToV(hall.Position); UpdateCamera();
            await RecoveryCapture(System.IO.Path.Combine(directory, "real-unloading.png"), GetViewport());
            _sim.CheckInvariants();
            GD.Print("INDEXCITIES_PRESENTATION_OK scope=controlled-visual-states real-maintenance=travel-and-unload picking=visible-person state=preserved");
            GetTree().Quit();
        } catch (Exception exception) { GD.PushError(exception.ToString()); GetTree().Quit(1); }
    }
}
