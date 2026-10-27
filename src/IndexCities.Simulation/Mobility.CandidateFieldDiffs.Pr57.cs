namespace IndexCities.Simulation;

// PR57 #215: opt-in, bounded field attribution over #214's already-captured
// candidate JSON. Never participates in a physics decision or save. A changed
// persisted field is NOT proof that the physical predicate read that field;
// clock, signals, pedestrians, roads, transient writes and rejected candidates
// remain outside this observational sample.
public sealed partial class CitySimulation
{
    static readonly bool Pr57FieldDiffsEnabled =
        Environment.GetEnvironmentVariable("PR57_215_FIELD_DIFFS") == "1";

    readonly Dictionary<string, long> _pr57FieldCounts = new(StringComparer.Ordinal);
    long _pr57FieldCompared, _pr57FieldChanged, _pr57FieldIdMismatch;
    long _pr57FieldCauseOnly, _pr57FieldPose, _pr57FieldReservation, _pr57FieldRoute;
    long _pr57FieldOther;
    string _pr57FieldExample = "";

    void Pr57FieldBeginSlice()
    {
        if (!Pr57FieldDiffsEnabled) return;
        _pr57FieldCounts.Clear();
        _pr57FieldCompared = _pr57FieldChanged = _pr57FieldIdMismatch = 0;
        _pr57FieldCauseOnly = _pr57FieldPose = _pr57FieldReservation = 0;
        _pr57FieldRoute = _pr57FieldOther = 0;
        _pr57FieldExample = "";
    }

    void Pr57FieldCompare(
        List<(int Id, string State)> previous,
        List<(int Id, string State)> current, int tier)
    {
        if (!Pr57FieldDiffsEnabled) return;
        // Only same-position candidate IDs are compared; order changes are
        // already counted separately by #214 and cannot imply reuse.
        for (var i = 0; i < Math.Min(previous.Count, current.Count); i++)
        {
            if (previous[i].Id != current[i].Id)
            {
                _pr57FieldIdMismatch++;
                continue;
            }
            _pr57FieldCompared++;
            if (string.Equals(previous[i].State, current[i].State, StringComparison.Ordinal))
                continue;
            _pr57FieldChanged++;
            using var oldJson = System.Text.Json.JsonDocument.Parse(previous[i].State);
            using var newJson = System.Text.Json.JsonDocument.Parse(current[i].State);
            var changedFields = 0;
            var cause = false;
            var pose = false;
            var reservation = false;
            var route = false;
            var other = false;
            foreach (var field in oldJson.RootElement.EnumerateObject())
            {
                if (newJson.RootElement.TryGetProperty(field.Name, out var newValue) &&
                    string.Equals(field.Value.GetRawText(), newValue.GetRawText(),
                        StringComparison.Ordinal))
                    continue;
                changedFields++;
                _pr57FieldCounts.TryGetValue(field.Name, out var hits);
                _pr57FieldCounts[field.Name] = hits + 1;
                switch (field.Name)
                {
                    case "Cause": cause = true; break;
                    case "Position":
                    case "Heading":
                    case "Status": pose = true; break;
                    case "IntersectionClaim":
                    case "IntersectionEnteredAt":
                    case "ManoeuvreSpaces":
                    case "ManoeuvreDock":
                    case "DockCorridor":
                    case "ManoeuvreDeparture":
                    case "MovementAdmission": reservation = true; break;
                    case "Route":
                    case "RouteIndex":
                    case "Detour":
                    case "DetourResumeNode":
                    case "TowPhase":
                    case "TowTargetId":
                    case "TowTruckId": route = true; break;
                    default: other = true; break;
                }
                if (_pr57FieldExample.Length == 0 &&
                    field.Name is "Position" or "Heading" or "Status" or
                        "IntersectionClaim" or "ManoeuvreSpaces" or "Detour")
                {
                    var before = field.Value.GetRawText();
                    var after = newJson.RootElement.TryGetProperty(field.Name, out var value)
                        ? value.GetRawText() : "missing";
                    _pr57FieldExample = $"tier{tier}-id{previous[i].Id}-{field.Name}:" +
                        before[..Math.Min(before.Length, 60)] + "->" +
                        after[..Math.Min(after.Length, 60)];
                }
            }
            if (changedFields == 1 && cause) _pr57FieldCauseOnly++;
            if (pose) _pr57FieldPose++;
            if (reservation) _pr57FieldReservation++;
            if (route) _pr57FieldRoute++;
            if (other) _pr57FieldOther++;
        }
        _pr57FieldIdMismatch += Math.Abs(previous.Count - current.Count);
    }

    void Pr57FieldEndSlice()
    {
        if (!Pr57FieldDiffsEnabled) return;
        var fields = string.Join(",", _pr57FieldCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}:{pair.Value}"));
        Console.WriteLine(
            $"PR57_215_FIELDS sample={_pr57ExclusiveSamples} " +
            $"comparedRows={_pr57FieldCompared} changedRows={_pr57FieldChanged} " +
            $"idOrLengthMismatch={_pr57FieldIdMismatch} causeOnlyRows={_pr57FieldCauseOnly} " +
            $"poseStatusRows={_pr57FieldPose} reservationRelatedRows={_pr57FieldReservation} " +
            $"routeTowRows={_pr57FieldRoute} otherFieldRows={_pr57FieldOther} " +
            $"fieldHits={fields} firstPhysicalExample={_pr57FieldExample} " +
            "scope=selectedTier3AndTier9Occurrences observational=True intrusive=True " +
            "physicalReadClosureProven=False transientWritesCovered=False");
    }
}
