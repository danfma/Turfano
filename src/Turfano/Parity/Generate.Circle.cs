using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Circular polygon with `steps` sides around a center — `@turf/circle`. Each vertex
    /// is `Destination(center, radius, bearing = i·-360/steps)`.
    /// </summary>
    public static Polygon Circle(Point center, Units.Length radius, int steps = 64)
    {
        var coordinates = new Position[steps + 1];
        for (var i = 0; i < steps; i++)
            coordinates[i] = Destination(
                center.Coordinates,
                radius,
                Units.Angle.FromDegrees(i * -360.0 / steps)
            ).Coordinates;
        coordinates[steps] = coordinates[0];
        return new Polygon(new[] { coordinates });
    }
}
