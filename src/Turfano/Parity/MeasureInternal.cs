using GeoJson = Turfano.GeoJson;

namespace Turfano;

// Onda A (paridade). Helpers internos compartilhados pelas funções de measurement sobre os
// novos tipos (Turfano.GeoJson). Reaproveita PiOver180/Factor/EarthRadius já definidos nos
// partials NTS-based (mesma classe Turf).
public static partial class Turf
{
    /// <summary>
    /// Enumera as posições de uma geometria GeoJSON. Quando <paramref name="excludeWrapCoord"/>
    /// é true, pula o vértice de fechamento dos anéis de Polygon/MultiPolygon — semântica do
    /// `coordEach(..., excludeWrapCoord = true)` do TurfJS (usada por `centroid`).
    /// </summary>
    private static void EachPosition(
        GeoJson.Geometry geometry,
        bool excludeWrapCoord,
        Action<GeoJson.Position> action
    )
    {
        switch (geometry)
        {
            case GeoJson.Point p:
                action(p.Coordinates);
                break;
            case GeoJson.MultiPoint mp:
                foreach (var c in mp.Coordinates)
                    action(c);
                break;
            case GeoJson.LineString ls:
                foreach (var c in ls.Coordinates)
                    action(c);
                break;
            case GeoJson.MultiLineString mls:
                foreach (var line in mls.Coordinates)
                    foreach (var c in line)
                        action(c);
                break;
            case GeoJson.Polygon poly:
                foreach (var ring in poly.Coordinates)
                    EachRing(ring, excludeWrapCoord, action);
                break;
            case GeoJson.MultiPolygon mpoly:
                foreach (var pol in mpoly.Coordinates)
                    foreach (var ring in pol)
                        EachRing(ring, excludeWrapCoord, action);
                break;
            case GeoJson.GeometryCollection gc:
                foreach (var g in gc.Geometries)
                    EachPosition(g, excludeWrapCoord, action);
                break;
        }
    }

    private static void EachRing(
        GeoJson.Position[] ring,
        bool excludeWrapCoord,
        Action<GeoJson.Position> action
    )
    {
        var len = excludeWrapCoord && ring.Length > 0 ? ring.Length - 1 : ring.Length;
        for (var i = 0; i < len; i++)
            action(ring[i]);
    }
}
