namespace IndexCities.Simulation;

// PR57 opt-in: a single admission-local read plan for two ordered readers.
// No persistence, cross-actor reuse, altered actor order or skipped physical slice.
public sealed partial class CitySimulation
{
    static readonly bool Pr57FusedAdmissionEnabled =
        Environment.GetEnvironmentVariable("PR57_FUSED_ADMISSION") == "1";
    static readonly bool Pr57FusedAdmissionShadow =
        Environment.GetEnvironmentVariable("PR57_FUSED_ADMISSION_SHADOW") == "1";
    bool _pr57FusedAdmissionAnnounced;

    (Vehicle? Manoeuvre, Vehicle? Owner) Pr57PlanAdmissionReaders(
        Vehicle requester, RoadNode requested, HashSet<int> area)
    {
        bool FollowingBehind(Vehicle other)
        {
            if (requester.IntersectionClaim != requested.Id ||
                other.IntersectionClaim == requested.Id ||
                requester.Position.Distance(requested.Position) > JunctionClearance ||
                Math.Cos(other.Heading - requester.Heading) < .999999)
                return false;
            var relative = other.Position - requester.Position;
            var forward = relative.X * Math.Sin(requester.Heading) +
                          relative.Z * Math.Cos(requester.Heading);
            var lateral = Math.Abs(relative.X * Math.Cos(requester.Heading) -
                                   relative.Z * Math.Sin(requester.Heading));
            return forward < -.1 && lateral < 2.6;
        }

        Vehicle? manoeuvre = null;
        Vehicle? owner = null;
        double ownerTime = 0;
        foreach (var other in State.Vehicles)
        {
            // Preserve FirstOrDefault order, including tow and shipment exclusions.
            if (manoeuvre is null && other.Id != requester.Id &&
                other.Id != requester.TowTargetId && other.TowTruckId != requester.Id &&
                !Receives(other, requester) &&
                (other.ManoeuvreSpaces.Contains(requested.Id) ||
                 (other.ManoeuvreSpaces.Any(area.Contains) &&
                  other.Position.Distance(requested.Position) < 9)))
                manoeuvre = other;

            // Preserve NearbyAreaOwner order by timestamp then ID, including self.
            if (other.IntersectionClaim is not {} claim || !area.Contains(claim) ||
                other.Position.Distance(requested.Position) >= 9 ||
                FollowingBehind(other))
                continue;
            var time = other.IntersectionEnteredAt ??
                       (other.WaitingSince > 0 ? other.WaitingSince : VehicleMovementTime);
            var compare = owner is null ? -1 : Comparer<double>.Default.Compare(time, ownerTime);
            if (compare < 0 || (compare == 0 && other.Id < owner!.Id))
            {
                owner = other;
                ownerTime = time;
            }
        }

        if (Pr57FusedAdmissionShadow)
        {
            var expectedManoeuvre = State.Vehicles.FirstOrDefault(o =>
                o.Id != requester.Id && o.Id != requester.TowTargetId &&
                o.TowTruckId != requester.Id && !Receives(o, requester) &&
                (o.ManoeuvreSpaces.Contains(requested.Id) ||
                 (o.ManoeuvreSpaces.Any(area.Contains) &&
                  o.Position.Distance(requested.Position) < 9)));
            var expectedOwner = NearbyAreaOwner(area, requested, requester);
            if (!ReferenceEquals(manoeuvre, expectedManoeuvre) ||
                !ReferenceEquals(owner, expectedOwner))
                throw new InvalidOperationException(
                    $"PR57 admission plan mismatch at junction {requested.Id}");
        }
        if (!_pr57FusedAdmissionAnnounced &&
            Environment.GetEnvironmentVariable("PR57_GRID_DIAG") == "1")
        {
            _pr57FusedAdmissionAnnounced = true;
            Console.WriteLine("PR57_FUSED_ADMISSION used=True scope=one-admission");
        }
        return (manoeuvre, owner);
    }
}
