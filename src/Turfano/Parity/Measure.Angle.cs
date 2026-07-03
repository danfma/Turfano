namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Angle (in degrees) at the vertex `midPoint` between `startPoint` and `endPoint` —
    /// `@turf/angle`. With <paramref name="mercator"/> the azimuths come from the rhumb
    /// (constant bearing); <paramref name="explementary"/> returns 360 - angle.
    /// </summary>
    public static double Angle(
        Position startPoint,
        Position midPoint,
        Position endPoint,
        bool explementary = false,
        bool mercator = false
    )
    {
        var azimuthOA = BearingToAzimuth(
            mercator ? RhumbBearing(midPoint, startPoint) : Bearing(midPoint, startPoint)
        ).Degrees;
        var azimuthOB = BearingToAzimuth(
            mercator ? RhumbBearing(midPoint, endPoint) : Bearing(midPoint, endPoint)
        ).Degrees;

        if (azimuthOB < azimuthOA)
            azimuthOB += 360;

        var angleAOB = azimuthOB - azimuthOA;
        return explementary ? 360 - angleAOB : angleAOB;
    }
}
