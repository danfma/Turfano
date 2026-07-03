using Units = Turfano.Units;

namespace Turfano.GeoJson;

// US4 — conversões do TurfJS expostas na fachada Geo (reuso direto de Turfano.Units).
public static partial class Geo
{
    /// <summary>Converte um rumo (-180..180) em azimute (0..360) — `@turf/bearing-to-azimuth`.</summary>
    public static Units.Angle BearingToAzimuth(Units.Angle bearing) => bearing.ToAzimuth();

    /// <summary>Converte graus em radianos — `@turf/helpers.degreesToRadians`.</summary>
    public static double DegreesToRadians(double degrees) => Units.Angle.FromDegrees(degrees).Radians;

    /// <summary>Converte radianos em graus — `@turf/helpers.radiansToDegrees`.</summary>
    public static double RadiansToDegrees(double radians) => Units.Angle.FromRadians(radians).Degrees;

    /// <summary>Converte um comprimento entre unidades — `@turf/helpers.convertLength`.</summary>
    public static double ConvertLength(double value, Units.LengthUnit from, Units.LengthUnit to) =>
        new Units.Length(value, from).As(to);

    /// <summary>Converte uma área entre unidades — `@turf/helpers.convertArea`.</summary>
    public static double ConvertArea(double value, Units.AreaUnit from, Units.AreaUnit to) =>
        new Units.Area(value, from).As(to);

    /// <summary>Converte uma distância no arco angular correspondente, em radianos — `@turf/helpers.lengthToRadians`.</summary>
    public static double LengthToRadians(double distance, Units.LengthUnit unit) =>
        new Units.Length(distance, unit).Radians;

    /// <summary>Converte um arco em radianos numa distância na unidade dada — `@turf/helpers.radiansToLength`.</summary>
    public static double RadiansToLength(double radians, Units.LengthUnit unit) =>
        Units.Length.FromRadians(radians).As(unit);

    /// <summary>Converte uma distância no equivalente angular, em graus — `@turf/helpers.lengthToDegrees`.</summary>
    public static double LengthToDegrees(double distance, Units.LengthUnit unit) =>
        new Units.Length(distance, unit).Degrees;
}
