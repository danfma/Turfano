namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Value interpolated on the plane of a triangle — `@turf/planepoint`. Vertex values come
    /// from the feature's `a`/`b`/`c` properties or, when absent, from the 3rd coordinate of
    /// each vertex.
    /// </summary>
    public static double Planepoint(Point point, Feature triangle)
    {
        if (triangle.Geometry is not Polygon polygon)
            throw new ArgumentException("triangle deve ser uma Feature de Polygon", nameof(triangle));

        var outer = polygon.Coordinates[0];
        if (outer.Length < 4)
            throw new ArgumentException("OuterRing of a Polygon must have 4 or more Positions.");

        var properties = triangle.Properties;
        var x = point.Coordinates.Lon;
        var y = point.Coordinates.Lat;

        var x1 = outer[0].Lon;
        var y1 = outer[0].Lat;
        var z1 = NumberOrNull(properties?["a"]) ?? outer[0].Alt ?? double.NaN;
        var x2 = outer[1].Lon;
        var y2 = outer[1].Lat;
        var z2 = NumberOrNull(properties?["b"]) ?? outer[1].Alt ?? double.NaN;
        var x3 = outer[2].Lon;
        var y3 = outer[2].Lat;
        var z3 = NumberOrNull(properties?["c"]) ?? outer[2].Alt ?? double.NaN;

        return (
                z3 * (x - x1) * (y - y2)
                + z1 * (x - x2) * (y - y3)
                + z2 * (x - x3) * (y - y1)
                - z2 * (x - x1) * (y - y3)
                - z3 * (x - x2) * (y - y1)
                - z1 * (x - x3) * (y - y2)
            )
            / (
                (x - x1) * (y - y2)
                + (x - x2) * (y - y3)
                + (x - x3) * (y - y1)
                - (x - x1) * (y - y3)
                - (x - x2) * (y - y1)
                - (x - x3) * (y - y2)
            );
    }
}
