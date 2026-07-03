namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// True if the geometries **do not** touch/intersect — `@turf/boolean-disjoint`.
    /// Flattens multi-geometries and requires that every pair be disjoint.
    /// </summary>
    public static bool BooleanDisjoint(Geometry a, Geometry b)
    {
        foreach (var g1 in FlattenGeometry(a))
            foreach (var g2 in FlattenGeometry(b))
                if (!Disjoint(g1, g2))
                    return false;
        return true;
    }

    /// <summary>Negation of <see cref="BooleanDisjoint"/> — `@turf/boolean-intersects`.</summary>
    public static bool BooleanIntersects(Geometry a, Geometry b) => !BooleanDisjoint(a, b);

    private static bool Disjoint(Geometry g1, Geometry g2) =>
        g1 switch
        {
            Point p1 => g2 switch
            {
                Point p2 => !p1.Coordinates.Equals(p2.Coordinates),
                LineString l2 => !IsPointOnLineGeom(l2, p1.Coordinates),
                Polygon poly2 => !BooleanPointInPolygon(p1, poly2),
                _ => true,
            },
            LineString l1 => g2 switch
            {
                Point p2 => !IsPointOnLineGeom(l1, p2.Coordinates),
                LineString l2 => !LinesIntersect(l1.Coordinates, l2.Coordinates),
                Polygon poly2 => !IsLineInPoly(poly2, l1.Coordinates),
                _ => true,
            },
            Polygon poly1 => g2 switch
            {
                Point p2 => !BooleanPointInPolygon(p2, poly1),
                LineString l2 => !IsLineInPoly(poly1, l2.Coordinates),
                Polygon poly2 => !IsPolyInPoly(poly1, poly2),
                _ => true,
            },
            _ => true,
        };

    private static bool IsPointOnLineGeom(LineString line, Position pt)
    {
        var coords = line.Coordinates;
        for (var i = 0; i < coords.Length - 1; i++)
            if (IsPointOnLineSegment(coords[i], coords[i + 1], pt, null, null))
                return true;
        return false;
    }

    private static bool IsLineInPoly(Polygon poly, Position[] line)
    {
        foreach (var c in line)
            if (BooleanPointInPolygon(new Point(c), poly))
                return true;
        foreach (var ring in poly.Coordinates)
            if (LinesIntersect(line, ring))
                return true;
        return false;
    }

    private static bool IsPolyInPoly(Polygon a, Polygon b)
    {
        foreach (var c in a.Coordinates[0])
            if (BooleanPointInPolygon(new Point(c), b))
                return true;
        foreach (var c in b.Coordinates[0])
            if (BooleanPointInPolygon(new Point(c), a))
                return true;
        foreach (var ringA in a.Coordinates)
            foreach (var ringB in b.Coordinates)
                if (LinesIntersect(ringA, ringB))
                    return true;
        return false;
    }

    // --- primitivas compartilhadas pelas relações ---

    /// <summary>Some pair of segments from two polylines crosses (`@turf/line-intersect`).</summary>
    private static bool LinesIntersect(Position[] a, Position[] b)
    {
        for (var i = 0; i < a.Length - 1; i++)
            for (var j = 0; j < b.Length - 1; j++)
                if (SegmentsIntersect(a[i], a[i + 1], b[j], b[j + 1]))
                    return true;
        return false;
    }

    private static bool SegmentsIntersect(Position p1, Position p2, Position p3, Position p4)
    {
        var denominator = (p4.Lat - p3.Lat) * (p2.Lon - p1.Lon) - (p4.Lon - p3.Lon) * (p2.Lat - p1.Lat);
        if (denominator == 0)
            return false; // paralelos/colineares (limitação herdada do @turf)
        var uA = ((p4.Lon - p3.Lon) * (p1.Lat - p3.Lat) - (p4.Lat - p3.Lat) * (p1.Lon - p3.Lon)) / denominator;
        var uB = ((p2.Lon - p1.Lon) * (p1.Lat - p3.Lat) - (p2.Lat - p1.Lat) * (p1.Lon - p3.Lon)) / denominator;
        return uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;
    }

    /// <summary>Flattens a geometry into its primitive parts (Point/LineString/Polygon).</summary>
    private static IEnumerable<Geometry> FlattenGeometry(Geometry g)
    {
        switch (g)
        {
            case MultiPoint mp:
                foreach (var c in mp.Coordinates)
                    yield return new Point(c);
                break;
            case MultiLineString mls:
                foreach (var l in mls.Coordinates)
                    yield return new LineString(l);
                break;
            case MultiPolygon mpoly:
                foreach (var p in mpoly.Coordinates)
                    yield return new Polygon(p);
                break;
            case GeometryCollection gc:
                foreach (var sub in gc.Geometries)
                    foreach (var f in FlattenGeometry(sub))
                        yield return f;
                break;
            default: // Point / LineString / Polygon → a própria geometria
                yield return g;
                break;
        }
    }
}
