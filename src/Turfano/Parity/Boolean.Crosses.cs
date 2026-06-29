namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Geometrias que se cruzam — `@turf/boolean-crosses`. Linha×polígono: a linha intersecta
    /// a borda do polígono. Cobre Line/Polygon e Line/Line; MultiPoint fica para depois.
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
