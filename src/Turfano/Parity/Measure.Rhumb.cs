using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Rhumb-line bearing between two positions, in degrees (-180..180) — `@turf/rhumb-bearing`.</summary>
    public static Units.Angle RhumbBearing(Position from, Position to, bool final = false)
    {
        var bear360 = final ? CalcRhumbBearing(to, from) : CalcRhumbBearing(from, to);
        var bear180 = bear360 > 180 ? -(360 - bear360) : bear360;
        return Units.Angle.FromDegrees(bear180);
    }

    private static double CalcRhumbBearing(Position start, Position end)
    {
        var phi1 = start.Lat * RadiansPerDegree;
        var phi2 = end.Lat * RadiansPerDegree;
        var deltaLambda = (end.Lon - start.Lon) * RadiansPerDegree;
        if (deltaLambda > Math.PI)
            deltaLambda -= 2 * Math.PI;
        if (deltaLambda < -Math.PI)
            deltaLambda += 2 * Math.PI;

        var deltaPsi = Math.Log(Math.Tan(phi2 / 2 + Math.PI / 4) / Math.Tan(phi1 / 2 + Math.PI / 4));
        var theta = Math.Atan2(deltaLambda, deltaPsi);
        return (theta / RadiansPerDegree + 360) % 360;
    }

    /// <summary>Distance along a constant-bearing (rhumb) line — `@turf/rhumb-distance`.</summary>
    public static Units.Length RhumbDistance(Position from, Position to)
    {
        var phi1 = from.Lat * RadiansPerDegree;
        var phi2 = to.Lat * RadiansPerDegree;
        var deltaPhi = phi2 - phi1;
        var deltaLambda = Math.Abs(to.Lon - from.Lon) * RadiansPerDegree;
        if (deltaLambda > Math.PI)
            deltaLambda = 2 * Math.PI - deltaLambda;

        double q;
        if (Math.Abs(deltaPhi) < 1e-10)
        {
            q = Math.Cos(phi1);
        }
        else
        {
            var deltaPsi = Math.Log(
                Math.Tan(phi2 / 2 + Math.PI / 4) / Math.Tan(phi1 / 2 + Math.PI / 4)
            );
            q = deltaPhi / deltaPsi;
        }

        var delta = Math.Sqrt(deltaPhi * deltaPhi + q * q * deltaLambda * deltaLambda);
        return Units.Length.FromRadians(delta);
    }
}
