namespace Turfano.GeoJson;

public static partial class Geo
{
    private const double EarthRadiusMercator = 6378137;
    private const double MaxMercatorExtent = 20037508.342789244;

    /// <summary>WGS84 → Web Mercator (EPSG:3857) — `@turf/projection.toMercator`.</summary>
    public static Geometry ToMercator(Geometry geojson) => MapPositions(geojson, PositionToMercator);

    /// <summary>Web Mercator (EPSG:3857) → WGS84 — `@turf/projection.toWgs84`.</summary>
    public static Geometry ToWgs84(Geometry geojson) => MapPositions(geojson, PositionToWgs84);

    /// <summary>Posição WGS84 → Web Mercator (com o clamp de extensão da fonte).</summary>
    public static Position PositionToMercator(Position lonLat)
    {
        var adjusted = Math.Abs(lonLat.Lon) <= 180 ? lonLat.Lon : lonLat.Lon - Math.Sign(lonLat.Lon) * 360;
        var x = EarthRadiusMercator * adjusted * RadiansPerDegree;
        var y = EarthRadiusMercator * Math.Log(Math.Tan(Math.PI * 0.25 + 0.5 * lonLat.Lat * RadiansPerDegree));

        if (x > MaxMercatorExtent)
            x = MaxMercatorExtent;
        if (x < -MaxMercatorExtent)
            x = -MaxMercatorExtent;
        if (y > MaxMercatorExtent)
            y = MaxMercatorExtent;
        if (y < -MaxMercatorExtent)
            y = -MaxMercatorExtent;

        return new Position(x, y, lonLat.Alt);
    }

    /// <summary>Posição Web Mercator → WGS84.</summary>
    public static Position PositionToWgs84(Position xy)
    {
        var degreesPerRadian = 180 / Math.PI;
        return new Position(
            xy.Lon * degreesPerRadian / EarthRadiusMercator,
            (Math.PI * 0.5 - 2 * Math.Atan(Math.Exp(-xy.Lat / EarthRadiusMercator))) * degreesPerRadian,
            xy.Alt
        );
    }
}
