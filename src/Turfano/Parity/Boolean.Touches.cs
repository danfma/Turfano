namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Geometries that touch (boundaries meet, interiors do not) —
    /// `@turf/boolean-touches`. Covers Polygon/Polygon and Point/Polygon; the full type
    /// matrix (lines, multi*) is left for a future increment.
    /// </summary>
    public static bool BooleanTouches(Geometry a, Geometry b) =>
        (a, b) switch
        {
            (Point pt, Polygon poly) => PointTouchesPolygon(pt, poly),
            (Polygon poly, Point pt) => PointTouchesPolygon(pt, poly),
            (Polygon poly1, Polygon poly2) => PolygonsTouch(poly1, poly2),
            _ => false,
        };

    private static bool PointTouchesPolygon(Point pt, Polygon poly) =>
        BooleanPointOnLine(pt, new LineString(poly.Coordinates[0]))
        && !BooleanPointInPolygon(pt, poly, ignoreBoundary: true);

    // Algum vértice de `a` está na borda de `b`, e nenhum está estritamente dentro de `b`.
    private static bool PolygonsTouch(Polygon a, Polygon b)
    {
        var foundTouchingPoint = false;
        foreach (var vertex in a.Coordinates[0])
        {
            if (BooleanPointOnLine(new Point(vertex), new LineString(b.Coordinates[0])))
                foundTouchingPoint = true;
            if (BooleanPointInPolygon(new Point(vertex), b, ignoreBoundary: true))
                return false;
        }
        return foundTouchingPoint;
    }
}
