using GeoJson = Turfano.GeoJson;
using Units = Turfano.Units;

namespace Turfano;

public static partial class Turf
{
    /// <summary>
    /// Distância great-circle (haversine) entre duas posições, idêntica ao `@turf/distance`.
    /// O resultado é uma <see cref="Units.Length"/> (use `.Kilometers`/`.Meters`/...).
    /// </summary>
    public static Units.Length Distance(GeoJson.Position from, GeoJson.Position to)
    {
        var dLat = (to.Lat - from.Lat) * PiOver180;
        var dLon = (to.Lon - from.Lon) * PiOver180;
        var lat1 = from.Lat * PiOver180;
        var lat2 = to.Lat * PiOver180;

        var a =
            Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Pow(Math.Sin(dLon / 2), 2) * Math.Cos(lat1) * Math.Cos(lat2);

        var radians = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Units.Length.FromRadians(radians);
    }

    /// <summary>
    /// Rumo inicial (ou final, se <paramref name="final"/>) de `from` para `to`, em graus,
    /// idêntico ao `@turf/bearing` (-180..180).
    /// </summary>
    public static Units.Angle Bearing(GeoJson.Position from, GeoJson.Position to, bool final = false)
    {
        if (final)
        {
            var back = Bearing(to, from);
            return Units.Angle.FromDegrees((back.Degrees + 180) % 360);
        }

        var lon1 = from.Lon * PiOver180;
        var lon2 = to.Lon * PiOver180;
        var lat1 = from.Lat * PiOver180;
        var lat2 = to.Lat * PiOver180;
        var dLon = lon2 - lon1;

        var a = Math.Sin(dLon) * Math.Cos(lat2);
        var b = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        return Units.Angle.FromRadians(Math.Atan2(a, b));
    }

    // NOTA (descoberta de design): um método `Length(...)` na classe `Turf` sombreia o TIPO
    // `Length` (UnitsNet), usado sem qualificação nos partials NTS-based (`Length.FromMeters`,
    // `Length EarthRadius`...), quebrando o build. Como não alteramos os arquivos antigos
    // (FR-007), o comprimento sobre os novos tipos fica para quando a colocação for revista
    // (fachada dedicada) ou os arquivos NTS saírem. Implementação pronta abaixo, comentada:
    //
    // public static Units.Length Length(GeoJson.LineString line) { ... soma de Distance ... }
}
