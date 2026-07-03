namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Rebuilds a geometry by applying <paramref name="map"/> to each position (creates new
    /// arrays — the basis for flip/truncate/transform*/clone).
    /// </summary>
    private static Geometry MapPositions(Geometry g, Func<Position, Position> map) =>
        g switch
        {
            Point p => new Point(map(p.Coordinates)),
            MultiPoint mp => new MultiPoint(mp.Coordinates.Select(map).ToArray()),
            LineString ls => new LineString(ls.Coordinates.Select(map).ToArray()),
            MultiLineString mls => new MultiLineString(
                mls.Coordinates.Select(l => l.Select(map).ToArray()).ToArray()
            ),
            Polygon poly => new Polygon(
                poly.Coordinates.Select(r => r.Select(map).ToArray()).ToArray()
            ),
            MultiPolygon mpoly => new MultiPolygon(
                mpoly.Coordinates.Select(p => p.Select(r => r.Select(map).ToArray()).ToArray()).ToArray()
            ),
            GeometryCollection gc => new GeometryCollection(
                gc.Geometries.Select(sub => MapPositions(sub, map)).ToArray()
            ),
            _ => g,
        };

    /// <summary>Rounds like JS's `Math.round` (half toward +∞), same as `@turf/round`.</summary>
    private static double JsRound(double value, int precision)
    {
        var factor = Math.Pow(10, precision);
        return Math.Floor(value * factor + 0.5) / factor;
    }
}
