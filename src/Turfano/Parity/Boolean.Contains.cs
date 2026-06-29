namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// `a` contém `b` — `@turf/boolean-contains`. Cobre os combos núcleo (Point/Point,
    /// Polygon/Point, Polygon/Polygon); combinações com MultiPoint/LineString-em-polígono
    /// ficam para um incremento futuro.
    /// </summary>
    public static bool BooleanContains(Geometry a, Geometry b) =>
        (a, b) switch
        {
            (Point p1, Point p2) => p1.Coordinates.Equals(p2.Coordinates),
            (Polygon poly, Point pt) => BooleanPointInPolygon(pt, poly, ignoreBoundary: true),
            (Polygon container, Polygon contained) => PolyContainsPoly(container, contained),
            _ => false,
        };

    /// <summary>
    /// `a` está dentro de `b` — `@turf/boolean-within` (dual de contains). Cobre os combos
    /// núcleo (Point/Polygon, Polygon/Polygon).
    /// </summary>
    public static bool BooleanWithin(Geometry a, Geometry b) =>
        (a, b) switch
        {
            (Point pt, Polygon poly) => BooleanPointInPolygon(pt, poly, ignoreBoundary: true),
            (Polygon contained, Polygon container) => PolyContainsPoly(container, contained),
            _ => false,
        };

    // container contém contained: bbox do container contém o bbox do contained E todos os
    // vértices do contained estão no container (PIP default, inclui borda).
    private static bool PolyContainsPoly(Polygon container, Polygon contained)
    {
        var cb = Bbox(container).Values;
        var db = Bbox(contained).Values;
        if (cb[0] > db[0] || cb[1] > db[1] || cb[2] < db[2] || cb[3] < db[3])
            return false;

        foreach (var ring in contained.Coordinates)
            foreach (var coord in ring)
                if (!BooleanPointInPolygon(new Point(coord), container))
                    return false;
        return true;
    }
}
