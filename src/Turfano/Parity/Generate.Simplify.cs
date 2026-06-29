namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Simplifica uma geometria (Douglas-Peucker planar) — `@turf/simplify` (`simplify-js`).
    /// Sem `highQuality`, aplica antes um pré-passo de distância radial.
    /// </summary>
    public static Geometry Simplify(Geometry geometry, double tolerance = 1, bool highQuality = false) =>
        geometry switch
        {
            LineString ls => new LineString(SimplifyLine(ls.Coordinates, tolerance, highQuality)),
            MultiLineString mls => new MultiLineString(
                mls.Coordinates.Select(l => SimplifyLine(l, tolerance, highQuality)).ToArray()
            ),
            Polygon poly => new Polygon(
                poly.Coordinates.Select(r => SimplifyRing(r, tolerance, highQuality)).ToArray()
            ),
            MultiPolygon mpoly => new MultiPolygon(
                mpoly.Coordinates.Select(p => p.Select(r => SimplifyRing(r, tolerance, highQuality)).ToArray()).ToArray()
            ),
            GeometryCollection gc => new GeometryCollection(
                gc.Geometries.Select(sub => Simplify(sub, tolerance, highQuality)).ToArray()
            ),
            _ => geometry,
        };

    private static Position[] SimplifyRing(Position[] ring, double tolerance, bool highQuality)
    {
        var simplified = SimplifyLine(ring, tolerance, highQuality);
        // mantém um anel válido (>= 4 pontos, fechado)
        return simplified.Length >= 4 ? simplified : ring;
    }

    private static Position[] SimplifyLine(Position[] points, double tolerance, bool highQuality)
    {
        if (points.Length <= 2)
            return points;
        var sqTolerance = tolerance * tolerance;
        var pts = highQuality ? points : SimplifyRadialDistance(points, sqTolerance);
        return SimplifyDouglasPeucker(pts, sqTolerance);
    }

    private static Position[] SimplifyRadialDistance(Position[] points, double sqTolerance)
    {
        var prev = points[0];
        var newPoints = new List<Position> { prev };
        var point = prev;
        for (var i = 1; i < points.Length; i++)
        {
            point = points[i];
            if (SquaredDistance(point, prev) > sqTolerance)
            {
                newPoints.Add(point);
                prev = point;
            }
        }
        if (!prev.Equals(point))
            newPoints.Add(point);
        return newPoints.ToArray();
    }

    private static Position[] SimplifyDouglasPeucker(Position[] points, double sqTolerance)
    {
        var last = points.Length - 1;
        var simplified = new List<Position> { points[0] };
        DouglasPeuckerStep(points, 0, last, sqTolerance, simplified);
        simplified.Add(points[last]);
        return simplified.ToArray();
    }

    private static void DouglasPeuckerStep(
        Position[] points,
        int first,
        int last,
        double sqTolerance,
        List<Position> simplified
    )
    {
        var maxSquaredDistance = sqTolerance;
        var index = -1;
        for (var i = first + 1; i < last; i++)
        {
            var squaredDistance = SquaredSegmentDistance(points[i], points[first], points[last]);
            if (squaredDistance > maxSquaredDistance)
            {
                index = i;
                maxSquaredDistance = squaredDistance;
            }
        }

        if (maxSquaredDistance > sqTolerance && index != -1)
        {
            if (index - first > 1)
                DouglasPeuckerStep(points, first, index, sqTolerance, simplified);
            simplified.Add(points[index]);
            if (last - index > 1)
                DouglasPeuckerStep(points, index, last, sqTolerance, simplified);
        }
    }

    private static double SquaredDistance(Position a, Position b)
    {
        var dx = a.Lon - b.Lon;
        var dy = a.Lat - b.Lat;
        return dx * dx + dy * dy;
    }

    private static double SquaredSegmentDistance(Position p, Position a, Position b)
    {
        double x = a.Lon,
            y = a.Lat,
            dx = b.Lon - x,
            dy = b.Lat - y;
        if (dx != 0 || dy != 0)
        {
            var t = ((p.Lon - x) * dx + (p.Lat - y) * dy) / (dx * dx + dy * dy);
            if (t > 1)
            {
                x = b.Lon;
                y = b.Lat;
            }
            else if (t > 0)
            {
                x += dx * t;
                y += dy * t;
            }
        }
        dx = p.Lon - x;
        dy = p.Lat - y;
        return dx * dx + dy * dy;
    }
}
