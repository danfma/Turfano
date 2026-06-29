namespace Turfano.GeoJson;

// Onda A (paridade) — a fachada `Geo` ganha as funções de measurement sobre os tipos
// próprios. Aqui, no namespace Turfano.GeoJson, os nomes Point/Polygon/Position/... resolvem
// para os tipos próprios (o namespace local vence o global using do NTS), então não há
// colisão — nem com `Length` (que fica em Turfano.Units). Constantes self-contained (sem
// depender dos privados NTS de `Turf`).
public static partial class Geo
{
    private const double EarthRadiusMeters = 6371008.8; // = @turf earthRadius
    private const double D2R = Math.PI / 180.0;
    private const double AreaFactor = EarthRadiusMeters * EarthRadiusMeters / 2.0;

    /// <summary>
    /// Enumera as posições de uma geometria. Com <paramref name="excludeWrapCoord"/> = true,
    /// pula o vértice de fechamento dos anéis de Polygon/MultiPolygon (semântica do
    /// `coordEach(..., excludeWrapCoord = true)` do TurfJS, usada por `centroid`).
    /// </summary>
    private static void EachPosition(
        Geometry geometry,
        bool excludeWrapCoord,
        Action<Position> action
    )
    {
        switch (geometry)
        {
            case Point p:
                action(p.Coordinates);
                break;
            case MultiPoint mp:
                foreach (var c in mp.Coordinates)
                    action(c);
                break;
            case LineString ls:
                foreach (var c in ls.Coordinates)
                    action(c);
                break;
            case MultiLineString mls:
                foreach (var line in mls.Coordinates)
                    foreach (var c in line)
                        action(c);
                break;
            case Polygon poly:
                foreach (var ring in poly.Coordinates)
                    EachRing(ring, excludeWrapCoord, action);
                break;
            case MultiPolygon mpoly:
                foreach (var pol in mpoly.Coordinates)
                    foreach (var ring in pol)
                        EachRing(ring, excludeWrapCoord, action);
                break;
            case GeometryCollection gc:
                foreach (var g in gc.Geometries)
                    EachPosition(g, excludeWrapCoord, action);
                break;
        }
    }

    private static void EachRing(Position[] ring, bool excludeWrapCoord, Action<Position> action)
    {
        var len = excludeWrapCoord && ring.Length > 0 ? ring.Length - 1 : ring.Length;
        for (var i = 0; i < len; i++)
            action(ring[i]);
    }
}
