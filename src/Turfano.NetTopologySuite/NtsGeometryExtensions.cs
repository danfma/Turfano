using GeoJson = Turfano.GeoJson;
using Units = Turfano.Units;

namespace Turfano.NetTopologySuite;

/// <summary>
/// Extensões NTS-backed sobre os tipos do Turfano. O `Buffer` reproduz o `@turf/buffer`:
/// projeta para azimutal-equidistante centrada na geometria (equivalente great-circle via
/// `Geo.Distance`/`Bearing`/`Destination`), roda o buffer do NTS no plano em metros (o
/// buffer do `@turf` é o JTS = NTS) e desprojeta. Vive no satélite porque `partial class
/// Geo` não atravessa assemblies — e é a única função NTS-bound do Turfano.
/// </summary>
public static class NtsGeometryExtensions
{
    /// <summary>Expande/contrai a geometria por um raio — `@turf/buffer`. `null` se vazio.</summary>
    public static GeoJson.Geometry? Buffer(this GeoJson.Geometry geometry, Units.Length radius, int steps = 8)
    {
        var center = GeoJson.Geo.Center(geometry).Coordinates;

        var projected = MapPositions(geometry, p => ProjectAzimuthalEquidistant(center, p));
        var buffered = NtsConvert.ToNts(projected).Buffer(radius.Meters, steps);
        if (buffered is null || buffered.IsEmpty)
            return null;

        var unprojected = NtsConvert.FromNts(buffered);
        return MapPositions(unprojected, p => UnprojectAzimuthalEquidistant(center, p));
    }

    // Projeção azimutal-equidistante (metros) centrada em `center`: x = leste, y = norte.
    private static GeoJson.Position ProjectAzimuthalEquidistant(GeoJson.Position center, GeoJson.Position p)
    {
        var distance = GeoJson.Geo.Distance(center, p).Meters;
        var azimuth = GeoJson.Geo.Bearing(center, p).Radians;
        return new GeoJson.Position(distance * Math.Sin(azimuth), distance * Math.Cos(azimuth));
    }

    private static GeoJson.Position UnprojectAzimuthalEquidistant(GeoJson.Position center, GeoJson.Position xy)
    {
        double x = xy.Lon,
            y = xy.Lat;
        var distance = Math.Sqrt(x * x + y * y);
        var azimuth = Math.Atan2(x, y);
        return GeoJson
            .Geo.Destination(center, Units.Length.FromMeters(distance), Units.Angle.FromRadians(azimuth))
            .Coordinates;
    }

    // Helper local (decisão do plano: o satélite não abre InternalsVisibleTo do core).
    private static GeoJson.Geometry MapPositions(GeoJson.Geometry g, Func<GeoJson.Position, GeoJson.Position> map) =>
        g switch
        {
            GeoJson.Point p => new GeoJson.Point(map(p.Coordinates)),
            GeoJson.MultiPoint mp => new GeoJson.MultiPoint(mp.Coordinates.Select(map).ToArray()),
            GeoJson.LineString ls => new GeoJson.LineString(ls.Coordinates.Select(map).ToArray()),
            GeoJson.MultiLineString mls => new GeoJson.MultiLineString(
                mls.Coordinates.Select(line => line.Select(map).ToArray()).ToArray()
            ),
            GeoJson.Polygon poly => new GeoJson.Polygon(
                poly.Coordinates.Select(ring => ring.Select(map).ToArray()).ToArray()
            ),
            GeoJson.MultiPolygon mpoly => new GeoJson.MultiPolygon(
                mpoly.Coordinates.Select(p => p.Select(ring => ring.Select(map).ToArray()).ToArray()).ToArray()
            ),
            GeoJson.GeometryCollection gc => new GeoJson.GeometryCollection(
                gc.Geometries.Select(sub => MapPositions(sub, map)).ToArray()
            ),
            _ => g,
        };
}
