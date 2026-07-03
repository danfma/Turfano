using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Scales a geometry by a factor, **geodesically** — `@turf/transform-scale`. Each
    /// point is repositioned via `rhumbDestination(origin, rhumbDistance*factor, rhumbBearing)`.
    /// Default origin = centroid. (The NTS version is Cartesian — this one follows @turf.)
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

    /// <summary>Moves each point by distance/bearing (rhumb line) — `@turf/transform-translate`.</summary>
    public static Geometry TransformTranslate(
        Geometry geometry,
        Units.Length distance,
        Units.Angle direction
    ) => MapPositions(geometry, p => RhumbDestination(p, distance, direction).Coordinates);

    /// <summary>
    /// Rotates a geometry by an angle around a pivot — `@turf/transform-rotate`.
    /// Default pivot = centroid.
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

    /// <summary>Deep copy of the geometry (new coordinate arrays) — `@turf/clone`.</summary>
    public static Geometry Clone(Geometry geometry) => MapPositions(geometry, p => p);
}
