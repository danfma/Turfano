namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Ângulo (em graus) no vértice `midPoint` entre `startPoint` e `endPoint` —
    /// `@turf/angle`. Com <paramref name="mercator"/> os azimutes vêm do rumo constante
    /// (rhumb); <paramref name="explementary"/> devolve 360 − ângulo.
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
