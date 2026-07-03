namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Geometries that cross — `@turf/boolean-crosses`. Line×polygon: the line intersects
    /// the polygon boundary. Covers Line/Polygon and Line/Line; MultiPoint is left for later.
    /// </summary>
    public static bool BooleanCrosses(Geometry a, Geometry b) =>
        (a, b) switch
        {
            (LineString line, Polygon poly) => LineCrossesPolygon(line, poly),
            (Polygon poly, LineString line) => LineCrossesPolygon(line, poly),
            (LineString l1, LineString l2) => LinesIntersect(l1.Coordinates, l2.Coordinates),
            _ => false,
        };

    private static bool LineCrossesPolygon(LineString line, Polygon poly)
    {
        foreach (var ring in poly.Coordinates)
            if (LinesIntersect(line.Coordinates, ring))
                return true;
        return false;
    }
}
