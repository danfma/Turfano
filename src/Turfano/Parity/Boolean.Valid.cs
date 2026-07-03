namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Valid geometry — `@turf/boolean-valid`. Checks the main conditions: lines with ≥2
    /// points; polygon rings with ≥4 points and closed. NOTE: `@turf` does **not** detect
    /// self-intersection of the outer ring (a "loop" returns `true`); spikes/punctures and
    /// hole×outer intersection are left as future refinements.
    /// </summary>
    public static bool BooleanValid(Geometry geometry) =>
        geometry switch
        {
            Point _ => true,
            MultiPoint _ => true,
            LineString ls => ls.Coordinates.Length >= 2,
            MultiLineString mls => mls.Coordinates.Length >= 2 && mls.Coordinates.All(l => l.Length >= 2),
            Polygon poly => poly.Coordinates.All(RingValid),
            MultiPolygon mpoly => mpoly.Coordinates.All(p => p.All(RingValid)),
            GeometryCollection gc => gc.Geometries.All(BooleanValid),
            _ => false,
        };

    private static bool RingValid(Position[] ring) => ring.Length >= 4 && ring[0].Equals(ring[^1]);
}
