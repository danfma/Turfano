namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Anel no sentido horário (área sinalizada &gt; 0) — `@turf/boolean-clockwise`.</summary>
    public static bool BooleanClockwise(LineString ring)
    {
        var coords = ring.Coordinates;
        var sum = 0.0;
        for (var i = 1; i < coords.Length; i++)
        {
            var prev = coords[i - 1];
            var cur = coords[i];
            sum += (cur.Lon - prev.Lon) * (cur.Lat + prev.Lat);
        }
        return sum > 0;
    }

    /// <summary>Duas linhas paralelas (segmento a segmento) — `@turf/boolean-parallel`.</summary>
    public static bool BooleanParallel(LineString a, LineString b)
    {
        var s1 = a.Coordinates;
        var s2 = b.Coordinates;
        var n = Math.Min(s1.Length - 1, s2.Length - 1);
        for (var i = 0; i < n; i++)
        {
            if (!IsParallel(s1[i], s1[i + 1], s2[i], s2[i + 1]))
                return false;
        }
        return true;
    }

    private static bool IsParallel(Position a1, Position a2, Position b1, Position b2)
    {
        var slope1 = BearingToAzimuth(RhumbBearing(a1, a2)).Degrees;
        var slope2 = BearingToAzimuth(RhumbBearing(b1, b2)).Degrees;
        return Math.Abs(slope1 - slope2) < 1e-9 || Math.Abs((slope2 - slope1) % 180) < 1e-9;
    }
}
