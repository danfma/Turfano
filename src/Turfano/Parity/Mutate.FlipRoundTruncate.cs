namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Troca lon/lat de cada posição — `@turf/flip`.</summary>
    public static Geometry Flip(Geometry geometry) =>
        MapPositions(geometry, p => new Position(p.Lat, p.Lon, p.Alt));

    /// <summary>Arredonda um número para `precision` casas — `@turf/round`.</summary>
    public static double Round(double value, int precision = 0) => JsRound(value, precision);

    /// <summary>
    /// Arredonda as coordenadas para `precision` casas e limita a `coordinates` dimensões —
    /// `@turf/truncate` (apesar do nome, o @turf usa `Math.round`).
    /// </summary>
    public static Geometry Truncate(Geometry geometry, int precision = 6, int coordinates = 3) =>
        MapPositions(
            geometry,
            p =>
            {
                var lon = JsRound(p.Lon, precision);
                var lat = JsRound(p.Lat, precision);
                if (coordinates >= 3 && p.Alt is { } alt)
                    return new Position(lon, lat, JsRound(alt, precision));
                return new Position(lon, lat);
            }
        );
}
