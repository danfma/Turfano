namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Polígono côncavo (algum vértice quebra a convexidade) — `@turf/boolean-concave`.</summary>
    public static bool BooleanConcave(Polygon polygon)
    {
        var coords = polygon.Coordinates[0];
        if (coords.Length <= 4)
            return false;

        var sign = false;
        var n = coords.Length - 1;
        for (var i = 0; i < n; i++)
        {
            var dx1 = coords[(i + 2) % n].Lon - coords[(i + 1) % n].Lon;
            var dy1 = coords[(i + 2) % n].Lat - coords[(i + 1) % n].Lat;
            var dx2 = coords[i].Lon - coords[(i + 1) % n].Lon;
            var dy2 = coords[i].Lat - coords[(i + 1) % n].Lat;
            var z = dx1 * dy2 - dy1 * dx2;

            if (i == 0)
                sign = z > 0;
            else if (sign != z > 0)
                return true;
        }
        return false;
    }
}
