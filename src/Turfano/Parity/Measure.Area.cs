using GeoJson = Turfano.GeoJson;
using Units = Turfano.Units;

namespace Turfano;

public static partial class Turf
{
    /// <summary>
    /// Área esférica (m²) de uma geometria GeoJSON, idêntica ao `@turf/area`
    /// (mesma fórmula de anel e `earthRadius = 6371008.8`). Reusa `Factor`/`PiOver180`.
    /// </summary>
    public static Units.Area Area(GeoJson.Geometry geometry) =>
        geometry switch
        {
            GeoJson.Polygon p => Units.Area.FromSquareMeters(PolygonAreaSqm(p.Coordinates)),
            GeoJson.MultiPolygon mp => Units.Area.FromSquareMeters(
                mp.Coordinates.Sum(PolygonAreaSqm)
            ),
            GeoJson.GeometryCollection gc => gc.Geometries.Aggregate(
                Units.Area.Zero,
                (acc, g) => acc + Area(g)
            ),
            _ => Units.Area.Zero,
        };

    private static double PolygonAreaSqm(GeoJson.Position[][] rings)
    {
        if (rings.Length == 0)
            return 0;

        var total = Math.Abs(RingAreaSqm(rings[0]));
        for (var i = 1; i < rings.Length; i++)
            total -= Math.Abs(RingAreaSqm(rings[i]));
        return total;
    }

    private static double RingAreaSqm(GeoJson.Position[] coords)
    {
        var n = coords.Length - 1;
        if (n <= 2)
            return 0;

        var total = 0.0;
        for (var i = 0; i < n; i++)
        {
            var lowerX = coords[i].Lon * PiOver180;
            var middleY = coords[(i + 1) % n].Lat * PiOver180;
            var upperX = coords[(i + 2) % n].Lon * PiOver180;
            total += (upperX - lowerX) * Math.Sin(middleY);
        }
        return total * Factor;
    }
}
