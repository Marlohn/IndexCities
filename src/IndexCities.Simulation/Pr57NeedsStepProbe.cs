namespace IndexCities.Simulation;

// PR57 #190 only: observational, non-serialized per-Step needs-input census.
// It never controls UpdateNeeds, does not cache a simulation decision and does
// not treat equal endpoint inputs as proof of causal independence.
public sealed partial class CitySimulation
{
    public Pr57NeedsStepProbe? Pr57NeedsStepProbe { get; set; }
}

public sealed class Pr57NeedsStepProbe
{
    public readonly record struct Input(
        int HomeId, double Water, double Electricity, double Unmet,
        double HungerHours, double Pantry, int Members, bool Moving, bool InVehicle);

    public readonly record struct StepRecord(
        int Step, int Citizens, int NewCitizens, int MissingCitizens,
        int SameHomeInputs, int ChangedHomeInputs, int ChangedWater,
        int ChangedElectricity, int ChangedUnmet, int ChangedHunger,
        int ChangedPantry, int ChangedMovement, int ChangedOtherNeedsInputs);

    readonly Dictionary<int, Input> _previous = new();
    readonly Dictionary<int, Input> _current = new();
    public List<StepRecord> Records { get; } = [];

    public void BeginStep() => _current.Clear();

    public void Observe(Citizen citizen, Building? home, double unmet, int members, double pantry)
    {
        var input = new Input(home?.Id ?? -1, home?.WaterRatio ?? 0,
            home?.ElectricityRatio ?? 0, unmet, citizen.HungerHours,
            pantry, members, citizen.Moving, citizen.VehicleId != null);
        if (!_current.TryAdd(citizen.Id, input))
            throw new InvalidOperationException("Needs probe observed one citizen twice in a Step.");
    }

    public void EndStep()
    {
        int fresh = 0, sameHome = 0, changedHome = 0, water = 0, electricity = 0;
        int unmet = 0, hunger = 0, pantry = 0, movement = 0, other = 0;
        foreach (var (id, input) in _current)
        {
            if (!_previous.TryGetValue(id, out var old)) { fresh++; continue; }
            var homeChanged = old.HomeId != input.HomeId ||
                old.Water != input.Water || old.Electricity != input.Electricity ||
                old.Unmet != input.Unmet;
            if (homeChanged) changedHome++; else sameHome++;
            if (old.Water != input.Water) water++;
            if (old.Electricity != input.Electricity) electricity++;
            if (old.Unmet != input.Unmet) unmet++;
            if (old.HungerHours != input.HungerHours) hunger++;
            if (old.Pantry != input.Pantry) pantry++;
            if (old.Moving != input.Moving || old.InVehicle != input.InVehicle) movement++;
            if (old.Members != input.Members || old.HomeId != input.HomeId) other++;
        }
        int missing = 0;
        foreach (var id in _previous.Keys) if (!_current.ContainsKey(id)) missing++;
        Records.Add(new StepRecord(Records.Count + 1, _current.Count, fresh, missing,
            sameHome, changedHome, water, electricity, unmet, hunger, pantry, movement, other));
        _previous.Clear();
        foreach (var (id, input) in _current) _previous.Add(id, input);
    }
}
