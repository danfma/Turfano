using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Escala uma geometria por um fator, **geodesicamente** — `@turf/transform-scale`. Cada
    /// ponto é reposicionado por `rhumbDestination(origem, rhumbDistance*fator, rhumbBearing)`.
    /// Origem default = centroide. (A versão NTS é cartesiana — esta segue o @turf.)
    /// </summary>
    public static Geometry TransformScale(Geometry geometry, double factor, Position? origin = null)
    {
        if (factor == 1)
            return Clone(geometry);

        var originCoord = origin ?? Centroid(geometry).Coordinates;
        return MapPositions(
            geometry,
            p =>
            {
                if (p.Equals(originCoord))
                    return p;
                var distance = RhumbDistance(originCoord, p);
                var bearing = RhumbBearing(originCoord, p);
                var scaled = RhumbDestination(originCoord, distance * factor, bearing).Coordinates;
                return p.Alt is { } z ? new Position(scaled.Lon, scaled.Lat, z * factor) : scaled;
            }
        );
    }

    /// <summary>Move cada ponto por distância/rumo (linha de rumo) — `@turf/transform-translate`.</summary>
    public static Geometry TransformTranslate(
        Geometry geometry,
        Units.Length distance,
        Units.Angle direction
    ) => MapPositions(geometry, p => RhumbDestination(p, distance, direction).Coordinates);

    /// <summary>
    /// Rotaciona uma geometria por um ângulo em torno de um pivô — `@turf/transform-rotate`.
    /// Pivô default = centroide.
    /// </summary>
    public static Geometry TransformRotate(Geometry geometry, Units.Angle angle, Position? pivot = null)
    {
        if (angle.Degrees == 0)
            return Clone(geometry);

        var pivotCoord = pivot ?? Centroid(geometry).Coordinates;
        return MapPositions(
            geometry,
            p =>
            {
                if (p.Equals(pivotCoord))
                    return p;
                var initial = RhumbBearing(pivotCoord, p);
                var distance = RhumbDistance(pivotCoord, p);
                var finalAngle = Units.Angle.FromDegrees(initial.Degrees + angle.Degrees);
                return RhumbDestination(pivotCoord, distance, finalAngle).Coordinates;
            }
        );
    }

    /// <summary>Cópia profunda da geometria (arrays de coordenadas novos) — `@turf/clone`.</summary>
    public static Geometry Clone(Geometry geometry) => MapPositions(geometry, p => p);
}
