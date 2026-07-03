namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Point inside a polygon — `@turf/boolean-point-in-polygon` (Hao's algorithm, which
    /// detects the **boundary**). With `ignoreBoundary`, a point on the boundary counts as outside.
    /// </summary>
    public static bool BooleanPointInPolygon(Point point, Geometry polygon, bool ignoreBoundary = false)
    {
        var pt = point.Coordinates;
        var polys = polygon switch
        {
            Polygon p => new[] { p.Coordinates },
            MultiPolygon mp => mp.Coordinates,
            _ => throw new ArgumentException("polygon deve ser Polygon ou MultiPolygon", nameof(polygon)),
        };

        var result = false;
        foreach (var poly in polys)
        {
            var r = ClassifyPointInPolygon(pt, poly);
            if (r == 0)
                return !ignoreBoundary; // na borda
            if (r == 1)
                result = true; // dentro
        }
        return result;
    }

    // point-in-polygon-hao: 0 = borda, 1 = dentro, 2 = fora.
    private static int ClassifyPointInPolygon(Position p, Position[][] polygon)
    {
        double x = p.Lon,
            y = p.Lat;
        var k = 0;

        foreach (var contour in polygon)
        {
            var contourLen = contour.Length - 1;
            var u1 = contour[0].Lon - x;
            var v1 = contour[0].Lat - y;

            for (var ii = 0; ii < contourLen; ii++)
            {
                var nextP = contour[ii + 1];
                var u2 = nextP.Lon - x;
                var v2 = nextP.Lat - y;

                if (v1 == 0 && v2 == 0)
                {
                    if ((u2 <= 0 && u1 >= 0) || (u1 <= 0 && u2 >= 0))
                        return 0;
                }
                else if ((v2 >= 0 && v1 <= 0) || (v2 <= 0 && v1 >= 0))
                {
                    var f = u1 * v2 - u2 * v1;
                    if (f == 0)
                        return 0;
                    if ((f > 0 && v2 > 0 && v1 <= 0) || (f < 0 && v2 <= 0 && v1 > 0))
                        k++;
                }

                u1 = u2;
                v1 = v2;
            }
        }

        return k % 2 == 0 ? 2 : 1;
    }
}
