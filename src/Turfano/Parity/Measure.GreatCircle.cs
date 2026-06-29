namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Arco de grande círculo entre dois pontos como linha de `npoints` — `@turf/great-circle`
    /// (porte da interpolação da lib `arc`). O tratamento de offset/antimeridiano (que produz
    /// MultiLineString) fica para depois — aqui devolve uma `LineString` (caso sem cruzamento).
    /// </summary>
    public static LineString GreatCircle(Position start, Position end, int npoints = 100)
    {
        var n = Math.Max(npoints, 1);
        if (start.Lon == end.Lon && start.Lat == end.Lat)
            return new LineString(Enumerable.Repeat(start, n).ToArray());

        var lon1 = start.Lon * D2R;
        var lat1 = start.Lat * D2R;
        var lon2 = end.Lon * D2R;
        var lat2 = end.Lat * D2R;

        var w = lon1 - lon2;
        var h = lat1 - lat2;
        var z = Math.Pow(Math.Sin(h / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(w / 2), 2);
        var g = 2 * Math.Asin(Math.Sqrt(z));

        var coords = new Position[n];
        for (var i = 0; i < n; i++)
        {
            var f = n <= 1 ? 0.0 : (double)i / (n - 1);
            var A = Math.Sin((1 - f) * g) / Math.Sin(g);
            var B = Math.Sin(f * g) / Math.Sin(g);
            var x = A * Math.Cos(lat1) * Math.Cos(lon1) + B * Math.Cos(lat2) * Math.Cos(lon2);
            var y = A * Math.Cos(lat1) * Math.Sin(lon1) + B * Math.Cos(lat2) * Math.Sin(lon2);
            var zz = A * Math.Sin(lat1) + B * Math.Sin(lat2);
            var lat = Math.Atan2(zz, Math.Sqrt(x * x + y * y));
            var lon = Math.Atan2(y, x);
            coords[i] = new Position(lon / D2R, lat / D2R);
        }

        return new LineString(coords);
    }
}
