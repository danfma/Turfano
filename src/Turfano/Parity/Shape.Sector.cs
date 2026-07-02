namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Setor circular entre dois rumos — `@turf/sector` (centro + arco + centro). Rumos
    /// iguais (mod 360) viram o círculo completo.
    /// </summary>
    public static Polygon Sector(
        Point center,
        Units.Length radius,
        Units.Angle bearing1,
        Units.Angle bearing2,
        int steps = 64
    )
    {
        if (ConvertAngleTo360(bearing1.Degrees) == ConvertAngleTo360(bearing2.Degrees))
            return Circle(center, radius, steps);

        var coords = center.Coordinates;
        var arc = LineArc(center, radius, bearing1, bearing2, steps);
        var ring = new List<Position> { coords };
        ring.AddRange(arc.Coordinates);
        ring.Add(coords);
        return new Polygon(new[] { ring.ToArray() });
    }
}
