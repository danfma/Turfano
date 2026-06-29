namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Remove pontos redundantes (duplicados consecutivos e colineares) — `@turf/clean-coords`.
    /// </summary>
    public static Geometry CleanCoords(Geometry geometry) =>
        geometry switch
        {
            Point _ => geometry,
            MultiPoint mp => new MultiPoint(DistinctPositions(mp.Coordinates)),
            LineString ls => new LineString(CleanLine(ls.Coordinates, isPolygon: false)),
            MultiLineString mls => new MultiLineString(
                mls.Coordinates.Select(l => CleanLine(l, isPolygon: false)).ToArray()
            ),
            Polygon poly => new Polygon(
                poly.Coordinates.Select(r => CleanLine(r, isPolygon: true)).ToArray()
            ),
            MultiPolygon mpoly => new MultiPolygon(
                mpoly.Coordinates.Select(p => p.Select(r => CleanLine(r, isPolygon: true)).ToArray()).ToArray()
            ),
            GeometryCollection gc => new GeometryCollection(
                gc.Geometries.Select(CleanCoords).ToArray()
            ),
            _ => geometry,
        };

    private static Position[] DistinctPositions(Position[] coords)
    {
        var seen = new List<Position>();
        foreach (var c in coords)
            if (!seen.Contains(c))
                seen.Add(c);
        return seen.ToArray();
    }

    private static Position[] CleanLine(Position[] points, bool isPolygon)
    {
        if (points.Length == 2 && !points[0].Equals(points[1]))
            return points;

        var newPoints = new List<Position> { points[0] };
        int a = 0,
            b = 1,
            c = 2;
        while (c < points.Length)
        {
            if (BooleanPointOnLine(new Point(points[b]), new LineString(new[] { points[a], points[c] })))
            {
                b = c;
            }
            else
            {
                newPoints.Add(points[b]);
                a = b;
                b++;
                c = b;
            }
            c++;
        }
        newPoints.Add(points[b]);

        if (isPolygon && newPoints.Count >= 3)
        {
            if (BooleanPointOnLine(
                new Point(newPoints[0]),
                new LineString(new[] { newPoints[1], newPoints[^2] })
            ))
            {
                newPoints.RemoveAt(0);
                newPoints.RemoveAt(newPoints.Count - 1);
                newPoints.Add(newPoints[0]);
            }
        }

        return newPoints.ToArray();
    }
}
