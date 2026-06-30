using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Expande/contrai uma geometria por um raio — `@turf/buffer`. Reproduz o `@turf`:
    /// projeta para azimutal-equidistante centrada na geometria (o que equivale a
    /// `Distance`/`Bearing`/`Destination` great-circle), bufferiza no plano em metros com o
    /// NTS (o `buffer` do `@turf` **é** o JTS = NTS) e desprojeta. `null` se vazio.
    /// </summary>
    public static Geometry? Buffer(Geometry geometry, Units.Length radius, int steps = 8)
    {
        var center = Center(geometry).Coordinates;

        var projected = MapPositions(geometry, p => ProjectAzimuthalEquidistant(center, p));
        var buffered = Turfano.Interop.NtsBridge.ToNts(projected).Buffer(radius.Meters, steps);
        if (buffered is null || buffered.IsEmpty)
            return null;

        var unprojected = Turfano.Interop.NtsBridge.FromNts(buffered);
        return MapPositions(unprojected, p => UnprojectAzimuthalEquidistant(center, p));
    }

    // Projeção azimutal-equidistante (metros) centrada em `center`: x = leste, y = norte.
    private static Position ProjectAzimuthalEquidistant(Position center, Position p)
    {
        var distance = Distance(center, p).Meters;
        var azimuth = Bearing(center, p).Radians;
        return new Position(distance * Math.Sin(azimuth), distance * Math.Cos(azimuth));
    }

    private static Position UnprojectAzimuthalEquidistant(Position center, Position xy)
    {
        double x = xy.Lon,
            y = xy.Lat;
        var distance = Math.Sqrt(x * x + y * y);
        var azimuth = Math.Atan2(x, y);
        return Destination(center, Units.Length.FromMeters(distance), Units.Angle.FromRadians(azimuth)).Coordinates;
    }
}
