using GeoJson = Turfano.GeoJson;

namespace Turfano;

public static partial class Turf
{
    /// <summary>
    /// Centroide (média aritmética dos vértices) de uma geometria GeoJSON, idêntico ao
    /// `@turf/centroid`. **Conserto da Fase 2**: exclui o vértice de fechamento dos anéis
    /// (`excludeWrapCoord`), então o polígono `[[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]]` dá
    /// `[1,1]` (e não `[0.833,0.833]` do código NTS-based).
    /// </summary>
    public static GeoJson.Point Centroid(GeoJson.Geometry geometry)
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
            ? new GeoJson.Point(new GeoJson.Position(0, 0))
            : new GeoJson.Point(new GeoJson.Position(xSum / count, ySum / count));
    }
}
