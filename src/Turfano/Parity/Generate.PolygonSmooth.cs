namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Smooths a polygon using corner cutting (Chaikin's algorithm) — `@turf/polygon-smooth`. Each edge
    /// (p0→p1) produces the points `0.75·p0 + 0.25·p1` and `0.25·p0 + 0.75·p1`.
    /// </summary>
    public static Polygon PolygonSmooth(Polygon polygon, int iterations = 1)
    {
        var rings = polygon.Coordinates;
        for (var i = 0; i < iterations; i++)
            rings = rings.Select(SmoothRing).ToArray();
        return new Polygon(rings);
    }

    private static Position[] SmoothRing(Position[] ring)
    {
        var uniqueCount = ring.Length - 1; // exclui o vértice de fechamento
        var output = new List<Position>(uniqueCount * 2 + 1);
        for (var i = 0; i < uniqueCount; i++)
        {
            var p0 = ring[i];
            var p1 = ring[(i + 1) % uniqueCount];
            output.Add(new Position(0.75 * p0.Lon + 0.25 * p1.Lon, 0.75 * p0.Lat + 0.25 * p1.Lat));
            output.Add(new Position(0.25 * p0.Lon + 0.75 * p1.Lon, 0.25 * p0.Lat + 0.75 * p1.Lat));
        }
        output.Add(output[0]); // fecha o anel
        return output.ToArray();
    }
}
