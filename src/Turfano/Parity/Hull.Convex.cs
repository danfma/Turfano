namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Convex hull — `@turf/convex`. Port of the code path @turf actually executes: the
    /// concaveman `fastConvexHull` (culling by the extreme quadrilateral + monotone chain),
    /// since with `concavity = Infinity` (the @turf default) concaveman never "carves"
    /// (research decision R2). `null` if degenerate.
    /// </summary>
    public static Polygon? Convex(Geometry geojson)
    {
        var points = new List<double[]>();
        EachPosition(geojson, false, p => points.Add(new[] { p.Lon, p.Lat }));
        return ConvexFromPoints(points);
    }

    /// <inheritdoc cref="Convex(Geometry)"/>
    public static Polygon? Convex(FeatureCollection features)
    {
        var points = new List<double[]>();
        foreach (var feature in features.Features)
        {
            if (feature.Geometry is { } geometry)
                EachPosition(geometry, false, p => points.Add(new[] { p.Lon, p.Lat }));
        }
        return ConvexFromPoints(points);
    }

    private static Polygon? ConvexFromPoints(List<double[]> points)
    {
        if (points.Count == 0)
            return null;

        var hull = FastConvexHull(points);
        // o concaveman percorre a lista circular a partir do ÚLTIMO nó inserido: o anel
        // devolvido começa em hull[^1] e fecha nele
        var ring = new List<double[]> { hull[^1] };
        ring.AddRange(hull.Take(hull.Count - 1));
        ring.Add(hull[^1]);

        if (ring.Count > 3)
            return new Polygon(new[] { ring.Select(p => new Position(p[0], p[1])).ToArray() });
        return null;
    }

    // concaveman.fastConvexHull: descarta os pontos dentro do quadrilátero extremo e roda
    // o monotone chain sobre o resto.
    private static List<double[]> FastConvexHull(List<double[]> points)
    {
        var left = points[0];
        var top = points[0];
        var right = points[0];
        var bottom = points[0];

        foreach (var p in points)
        {
            if (p[0] < left[0])
                left = p;
            if (p[0] > right[0])
                right = p;
            if (p[1] < top[1])
                top = p;
            if (p[1] > bottom[1])
                bottom = p;
        }

        var cull = new List<double[]> { left, top, right, bottom };
        var filtered = new List<double[]>(cull);
        foreach (var p in points)
        {
            if (!RayCastPointInRing(p, cull))
                filtered.Add(p);
        }

        return MonotoneChainHull(filtered);
    }

    // ray casting da lib point-in-polygon (anel aberto)
    private static bool RayCastPointInRing(double[] point, List<double[]> ring)
    {
        var x = point[0];
        var y = point[1];
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            var xi = ring[i][0];
            var yi = ring[i][1];
            var xj = ring[j][0];
            var yj = ring[j][1];
            var intersects = yi > y != yj > y && x < (xj - xi) * (y - yi) / (yj - yi) + xi;
            if (intersects)
                inside = !inside;
        }
        return inside;
    }

    private static List<double[]> MonotoneChainHull(List<double[]> points)
    {
        // sort por x, depois y (compareByX); estável
        points = points.OrderBy(p => p[0]).ThenBy(p => p[1]).ToList();

        static double CrossProduct(double[] p1, double[] p2, double[] p3) =>
            (p2[1] - p1[1]) * (p3[0] - p2[0]) - (p2[0] - p1[0]) * (p3[1] - p2[1]);

        var lower = new List<double[]>();
        foreach (var p in points)
        {
            while (lower.Count >= 2 && CrossProduct(lower[^2], lower[^1], p) <= 0)
                lower.RemoveAt(lower.Count - 1);
            lower.Add(p);
        }

        var upper = new List<double[]>();
        for (var i = points.Count - 1; i >= 0; i--)
        {
            while (upper.Count >= 2 && CrossProduct(upper[^2], upper[^1], points[i]) <= 0)
                upper.RemoveAt(upper.Count - 1);
            upper.Add(points[i]);
        }

        upper.RemoveAt(upper.Count - 1);
        lower.RemoveAt(lower.Count - 1);
        lower.AddRange(upper);
        return lower;
    }
}
