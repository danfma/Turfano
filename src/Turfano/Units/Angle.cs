namespace Turfano.Units;

/// <summary>Angle units.</summary>
public enum AngleUnit
{
    Degrees,
    Radians,
}

/// <summary>
/// Angle/bearing as an immutable value struct. Conversions and azimuth reproduce TurfJS
/// (`degreesToRadians`/`radiansToDegrees`/`bearingToAzimuth`).
/// </summary>
public readonly record struct Angle(double Value, AngleUnit Unit)
{
    public double Radians => Unit == AngleUnit.Radians ? Value : Value * Math.PI / 180.0;
    public double Degrees => Unit == AngleUnit.Degrees ? Value : Value * 180.0 / Math.PI;

    public double As(AngleUnit unit) => unit == AngleUnit.Radians ? Radians : Degrees;

    public static Angle FromDegrees(double v) => new(v, AngleUnit.Degrees);
    public static Angle FromRadians(double v) => new(v, AngleUnit.Radians);

    /// <summary>
    /// Converts a bearing (-180..180, clockwise-positive from north) into an azimuth 0..360
    /// (@turf's `bearingToAzimuth`).
    /// </summary>
    public Angle ToAzimuth()
    {
        var d = Degrees % 360.0;
        if (d < 0)
            d += 360.0;
        return FromDegrees(d);
    }

    public static readonly Angle Zero = new(0, AngleUnit.Degrees);

    public static Angle operator +(Angle a, Angle b) => new(a.Value + b.As(a.Unit), a.Unit);
    public static Angle operator -(Angle a, Angle b) => new(a.Value - b.As(a.Unit), a.Unit);
    public static Angle operator *(Angle a, double k) => new(a.Value * k, a.Unit);
    public static Angle operator /(Angle a, double k) => new(a.Value / k, a.Unit);
}
