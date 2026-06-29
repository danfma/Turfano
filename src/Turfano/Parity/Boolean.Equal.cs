namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Geometrias iguais — `@turf/boolean-equal`. Mesmo tipo e mesmas coordenadas (na ordem,
    /// dentro de precisão). NOTA: o `geojson-equality` do @turf também normaliza rotação/
    /// direção de anéis — isso fica como refinamento futuro; aqui é comparação ordenada.
    /// </summary>
    public static bool BooleanEqual(Geometry a, Geometry b, double tolerance = 1e-9)
    {
        if (a.Type != b.Type)
            return false;

        return (a, b) switch
        {
            (Point p1, Point p2) => Close(p1.Coordinates, p2.Coordinates, tolerance),
            (MultiPoint m1, MultiPoint m2) => CloseSeq(m1.Coordinates, m2.Coordinates, tolerance),
            (LineString l1, LineString l2) => CloseSeq(l1.Coordinates, l2.Coordinates, tolerance),
            (Polygon poly1, Polygon poly2) => CloseRings(poly1.Coordinates, poly2.Coordinates, tolerance),
            (MultiLineString ml1, MultiLineString ml2) => CloseRings(ml1.Coordinates, ml2.Coordinates, tolerance),
            (MultiPolygon mp1, MultiPolygon mp2) => CloseNested(mp1.Coordinates, mp2.Coordinates, tolerance),
            _ => false,
        };
    }

    private static bool Close(Position x, Position y, double t) =>
        Math.Abs(x.Lon - y.Lon) <= t && Math.Abs(x.Lat - y.Lat) <= t;

    private static bool CloseSeq(Position[] x, Position[] y, double t)
    {
        if (x.Length != y.Length)
            return false;
        for (var i = 0; i < x.Length; i++)
            if (!Close(x[i], y[i], t))
                return false;
        return true;
    }

    private static bool CloseRings(Position[][] x, Position[][] y, double t)
    {
        if (x.Length != y.Length)
            return false;
        for (var i = 0; i < x.Length; i++)
            if (!CloseSeq(x[i], y[i], t))
                return false;
        return true;
    }

    private static bool CloseNested(Position[][][] x, Position[][][] y, double t)
    {
        if (x.Length != y.Length)
            return false;
        for (var i = 0; i < x.Length; i++)
            if (!CloseRings(x[i], y[i], t))
                return false;
        return true;
    }
}
