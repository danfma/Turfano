namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Geometrias do mesmo tipo que se sobrepõem — `@turf/boolean-overlap`. Idênticas → false.
    /// Polígonos: algum par de segmentos se intersecta (inclui tocar-se num canto). Linhas:
    /// algum par de segmentos é colinear e se sobrepõe. MultiPoint: compartilham algum ponto.
    /// </summary>
    public static bool BooleanOverlap(Geometry a, Geometry b)
    {
        if (a is Point || b is Point)
            throw new ArgumentException("Point geometry não é suportado.");
        if (!SameTypeFamily(a, b))
            throw new ArgumentException("As geometrias devem ser do mesmo tipo.");
        if (BooleanEqual(a, b, 1e-6))
            return false;

        if (a is MultiPoint m1 && b is MultiPoint m2)
        {
            foreach (var c1 in m1.Coordinates)
                foreach (var c2 in m2.Coordinates)
                    if (c1.Equals(c2))
                        return true;
            return false;
        }

        var lineFamily = a is LineString || a is MultiLineString;
        foreach (var (a1, a2) in EachSegment(a))
            foreach (var (b1, b2) in EachSegment(b))
                if (lineFamily ? SegmentsCollinearOverlap(a1, a2, b1, b2) : SegmentsIntersect(a1, a2, b1, b2))
                    return true;
        return false;
    }

    private static bool SameTypeFamily(Geometry a, Geometry b)
    {
        static bool Line(Geometry g) => g is LineString || g is MultiLineString;
        static bool Poly(Geometry g) => g is Polygon || g is MultiPolygon;
        if (a is MultiPoint)
            return b is MultiPoint;
        if (Line(a))
            return Line(b);
        if (Poly(a))
            return Poly(b);
        return false;
    }

    private static IEnumerable<(Position, Position)> EachSegment(Geometry g)
    {
        IEnumerable<Position[]> lines = g switch
        {
            LineString ls => new[] { ls.Coordinates },
            MultiLineString mls => mls.Coordinates,
            Polygon poly => poly.Coordinates,
            MultiPolygon mpoly => mpoly.Coordinates.SelectMany(p => p),
            _ => Enumerable.Empty<Position[]>(),
        };
        foreach (var line in lines)
            for (var i = 0; i < line.Length - 1; i++)
                yield return (line[i], line[i + 1]);
    }

    private static bool SegmentsCollinearOverlap(Position a1, Position a2, Position b1, Position b2)
    {
        var cross1 = (a2.Lon - a1.Lon) * (b1.Lat - a1.Lat) - (a2.Lat - a1.Lat) * (b1.Lon - a1.Lon);
        var cross2 = (a2.Lon - a1.Lon) * (b2.Lat - a1.Lat) - (a2.Lat - a1.Lat) * (b2.Lon - a1.Lon);
        if (Math.Abs(cross1) > 1e-12 || Math.Abs(cross2) > 1e-12)
            return false;

        var useX = Math.Abs(a2.Lon - a1.Lon) >= Math.Abs(a2.Lat - a1.Lat);
        double a1p = useX ? a1.Lon : a1.Lat,
            a2p = useX ? a2.Lon : a2.Lat,
            b1p = useX ? b1.Lon : b1.Lat,
            b2p = useX ? b2.Lon : b2.Lat;
        var low = Math.Max(Math.Min(a1p, a2p), Math.Min(b1p, b2p));
        var high = Math.Min(Math.Max(a1p, a2p), Math.Max(b1p, b2p));
        return high - low > 1e-12;
    }
}
