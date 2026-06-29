using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Distância great-circle (haversine) entre duas posições — `@turf/distance`.</summary>
    public static Units.Length Distance(Position from, Position to)
    {
        var dLat = (to.Lat - from.Lat) * RadiansPerDegree;
        var dLon = (to.Lon - from.Lon) * RadiansPerDegree;
        var lat1 = from.Lat * RadiansPerDegree;
        var lat2 = to.Lat * RadiansPerDegree;

        var a =
            Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Pow(Math.Sin(dLon / 2), 2) * Math.Cos(lat1) * Math.Cos(lat2);

        var radians = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Units.Length.FromRadians(radians);
    }

    /// <summary>Rumo (inicial ou final) de `from` para `to`, em graus — `@turf/bearing`.</summary>
    public static Units.Angle Bearing(Position from, Position to, bool final = false)
    {
        if (final)
        {
            var back = Bearing(to, from);
            return Units.Angle.FromDegrees((back.Degrees + 180) % 360);
        }

        var lon1 = from.Lon * RadiansPerDegree;
        var lon2 = to.Lon * RadiansPerDegree;
        var lat1 = from.Lat * RadiansPerDegree;
        var lat2 = to.Lat * RadiansPerDegree;
        var dLon = lon2 - lon1;

        var a = Math.Sin(dLon) * Math.Cos(lat2);
        var b = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);
        return Units.Angle.FromRadians(Math.Atan2(a, b));
    }

    /// <summary>Comprimento total de uma linha (soma das distâncias) — `@turf/length`.</summary>
    public static Units.Length Length(LineString line)
    {
        var total = Units.Length.Zero;
        var coords = line.Coordinates;
        for (var i = 0; i < coords.Length - 1; i++)
            total += Distance(coords[i], coords[i + 1]);
        return total;
    }

    /// <summary>Comprimento total de uma multilinha.</summary>
    public static Units.Length Length(MultiLineString lines)
    {
        var total = Units.Length.Zero;
        foreach (var line in lines.Coordinates)
            for (var i = 0; i < line.Length - 1; i++)
                total += Distance(line[i], line[i + 1]);
        return total;
    }
}
