namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Centroid (arithmetic mean of the vertices), identical to `@turf/centroid`. **Phase 2
    /// fix**: excludes the closing vertex of rings, so
    /// `[[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]]` gives `[1,1]` (rather than NTS's
    /// `[0.833,0.833]`).
    /// </summary>
    public static Point Centroid(Geometry geometry)
    {
        double xSum = 0,
            ySum = 0;
        var count = 0;

        EachPosition(
            geometry,
            excludeWrapCoord: true,
            p =>
            {
                xSum += p.Lon;
                ySum += p.Lat;
                count++;
            }
        );

        return count == 0
            ? new Point(new Position(0, 0))
            : new Point(new Position(xSum / count, ySum / count));
    }
}
