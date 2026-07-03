using Units = Turfano.Units;

namespace Turfano.GeoJson;

// US4 — conversões do TurfJS expostas na fachada Geo (reuso direto de Turfano.Units).
public static partial class Geo
{
    /// <summary>Converts a bearing (-180..180) to an azimuth (0..360) — `@turf/bearing-to-azimuth`.</summary>
    public static Units.Angle BearingToAzimuth(Units.Angle bearing) => bearing.ToAzimuth();

    /// <summary>Converts degrees to radians — `@turf/helpers.degreesToRadians`.</summary>
    public static double DegreesToRadians(double degrees) => Units.Angle.FromDegrees(degrees).Radians;

    /// <summary>Converts radians to degrees — `@turf/helpers.radiansToDegrees`.</summary>
    public static double RadiansToDegrees(double radians) => Units.Angle.FromRadians(radians).Degrees;

    /// <summary>Converts a length between units — `@turf/helpers.convertLength`.</summary>
    public static double ConvertLength(double value, Units.LengthUnit from, Units.LengthUnit to) =>
        new Units.Length(value, from).As(to);

    /// <summary>Converts an area between units — `@turf/helpers.convertArea`.</summary>
    public static double ConvertArea(double value, Units.AreaUnit from, Units.AreaUnit to) =>
        new Units.Area(value, from).As(to);

    /// <summary>Converts a distance to its corresponding angular arc, in radians — `@turf/helpers.lengthToRadians`.</summary>
    public static double LengthToRadians(double distance, Units.LengthUnit unit) =>
        new Units.Length(distance, unit).Radians;

    /// <summary>Converts an arc in radians to a distance in the given unit — `@turf/helpers.radiansToLength`.</summary>
    public static double RadiansToLength(double radians, Units.LengthUnit unit) =>
        Units.Length.FromRadians(radians).As(unit);

    /// <summary>Converts a distance to its angular equivalent, in degrees — `@turf/helpers.lengthToDegrees`.</summary>
    public static double LengthToDegrees(double distance, Units.LengthUnit unit) =>
        new Units.Length(distance, unit).Degrees;
}
