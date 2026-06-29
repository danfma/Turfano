namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Geometria válida — `@turf/boolean-valid`. Checa as condições principais: linhas com ≥2
    /// pontos; anéis de polígono com ≥4 pontos e fechados. NOTA: o `@turf` **não** detecta
    /// auto-interseção do anel externo (um "laço" retorna `true`); spikes/punctures e
    /// interseção furo×externo ficam como refinamento futuro.
    /// </summary>
    public static bool BooleanValid(Geometry geometry) =>
        geometry switch
        {
            Point _ => true,
            MultiPoint _ => true,
            LineString ls => ls.Coordinates.Length >= 2,
            MultiLineString mls => mls.Coordinates.Length >= 2 && mls.Coordinates.All(l => l.Length >= 2),
            Polygon poly => poly.Coordinates.All(RingValid),
            MultiPolygon mpoly => mpoly.Coordinates.All(p => p.All(RingValid)),
            GeometryCollection gc => gc.Geometries.All(BooleanValid),
            _ => false,
        };

    private static bool RingValid(Position[] ring) => ring.Length >= 4 && ring[0].Equals(ring[^1]);
}
