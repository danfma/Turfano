using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Área esférica de uma geometria, idêntica ao `@turf/area` (mesma fórmula de anel e
    /// `earthRadius = 6371008.8`).
    /// </summary>
    public static Units.Area Area(Geometry geometry) =>
        geometry switch
        {
            Polygon p => Units.Area.FromSquareMeters(PolygonAreaSqm(p.Coordinates)),
            MultiPolygon mp => Units.Area.FromSquareMeters(mp.Coordinates.Sum(PolygonAreaSqm)),
            GeometryCollection gc => gc.Geometries.Aggregate(
                Units.Area.Zero,
                (acc, g) => acc + Area(g)
            ),
            _ => Units.Area.Zero,
        };

    private static double PolygonAreaSqm(Position[][] rings)
    {
        if (rings.Length == 0)
            return 0;

        var total = Math.Abs(RingAreaSqm(rings[0]));
        for (var i = 1; i < rings.Length; i++)
            total -= Math.Abs(RingAreaSqm(rings[i]));
        return total;
    }

    private static double RingAreaSqm(Position[] coords)
    {
        var n = coords.Length - 1;
        if (n <= 2)
            return 0;

        var total = 0.0;
        for (var i = 0; i < n; i++)
        {
            var lowerX = coords[i].Lon * D2R;
            var middleY = coords[(i + 1) % n].Lat * D2R;
            var upperX = coords[(i + 2) % n].Lon * D2R;
            total += (upperX - lowerX) * Math.Sin(middleY);
        }
        return total * AreaFactor;
    }
}
