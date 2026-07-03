namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Swaps lon/lat of each position — `@turf/flip`.</summary>
    public static Geometry Flip(Geometry geometry) =>
        MapPositions(geometry, p => new Position(p.Lat, p.Lon, p.Alt));

    /// <summary>Rounds a number to `precision` decimal places — `@turf/round`.</summary>
    public static double Round(double value, int precision = 0) => JsRound(value, precision);

    /// <summary>
    /// Rounds the coordinates to `precision` decimal places and limits them to `coordinates`
    /// dimensions — `@turf/truncate` (despite the name, @turf uses `Math.round`).
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
