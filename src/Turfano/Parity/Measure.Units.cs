using Units = Turfano.Units;

namespace Turfano.GeoJson;

// US4 — conversões do TurfJS expostas na fachada Geo (reuso direto de Turfano.Units).
public static partial class Geo
{
    /// <summary>Converte um rumo (-180..180) em azimute (0..360) — `@turf/bearing-to-azimuth`.</summary>
    public static Units.Angle BearingToAzimuth(Units.Angle bearing) => bearing.ToAzimuth();

    public static double DegreesToRadians(double degrees) => Units.Angle.FromDegrees(degrees).Radians;

    public static double RadiansToDegrees(double radians) => Units.Angle.FromRadians(radians).Degrees;

    public static double ConvertLength(double value, Units.LengthUnit from, Units.LengthUnit to) =>
        new Units.Length(value, from).As(to);

    public static double ConvertArea(double value, Units.AreaUnit from, Units.AreaUnit to) =>
        new Units.Area(value, from).As(to);

    public static double LengthToRadians(double distance, Units.LengthUnit unit) =>
        new Units.Length(distance, unit).Radians;

    public static double RadiansToLength(double radians, Units.LengthUnit unit) =>
        Units.Length.FromRadians(radians).As(unit);

    public static double LengthToDegrees(double distance, Units.LengthUnit unit) =>
        new Units.Length(distance, unit).Degrees;
}
