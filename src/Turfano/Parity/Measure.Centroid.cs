namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Centroide (média aritmética dos vértices), idêntico ao `@turf/centroid`. **Conserto da
    /// Fase 2**: exclui o vértice de fechamento dos anéis, então
    /// `[[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]]` dá `[1,1]` (e não `[0.833,0.833]` do NTS).
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
