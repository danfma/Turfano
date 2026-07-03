namespace Turfano.Units;

/// <summary>Length units aligned with TurfJS (@turf/helpers).</summary>
public enum LengthUnit
{
    Meters,
    Kilometers,
    Miles,
    NauticalMiles,
    Feet,
    Inches,
    Yards,
    Centimeters,
    Millimeters,
    Degrees,
    Radians,
}

/// <summary>
/// Length/distance as an immutable value struct. Conversions exactly reproduce TurfJS's
/// (`convertLength`/`lengthToRadians`/`radiansToLength`/`lengthToDegrees`), using the same
/// factors from `@turf/helpers` (earthRadius = 6371008.8 m). Replaces UnitsNet (only 3
/// quantities are used in the library).
/// </summary>
public readonly record struct Length(double Value, LengthUnit Unit)
{
    // Fatores do @turf: "unidades por radião" (radiansToLength multiplica por isto).
    private static double Factor(LengthUnit u) =>
        u switch
        {
            LengthUnit.Meters => 6371008.8,
            LengthUnit.Kilometers => 6371.0088,
            LengthUnit.Miles => 3958.761333810546,
            LengthUnit.NauticalMiles => 3440.069546436285,
            LengthUnit.Feet => 20902260.511392,
            LengthUnit.Inches => 250826616.45599997,
            LengthUnit.Yards => 6967335.223679999,
            LengthUnit.Centimeters => 637100880,
            LengthUnit.Millimeters => 6371008800,
            LengthUnit.Degrees => 57.29577951308232,
            LengthUnit.Radians => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(u), u, "Unidade de comprimento inválida"),
        };

    /// <summary>Converts to another unit (identical to @turf's `convertLength`).</summary>
    public double As(LengthUnit unit) => Value / Factor(Unit) * Factor(unit);

    public double Meters => As(LengthUnit.Meters);
    public double Kilometers => As(LengthUnit.Kilometers);
    public double Miles => As(LengthUnit.Miles);
    public double NauticalMiles => As(LengthUnit.NauticalMiles);
    public double Feet => As(LengthUnit.Feet);
    public double Inches => As(LengthUnit.Inches);
    public double Yards => As(LengthUnit.Yards);
    public double Centimeters => As(LengthUnit.Centimeters);
    public double Millimeters => As(LengthUnit.Millimeters);

    /// <summary>Angular equivalent (great-circle) — @turf's `lengthToDegrees`.</summary>
    public double Degrees => As(LengthUnit.Degrees);

    /// <summary>Angular equivalent (great-circle) — @turf's `lengthToRadians`.</summary>
    public double Radians => As(LengthUnit.Radians);

    public static Length FromMeters(double v) => new(v, LengthUnit.Meters);
    public static Length FromKilometers(double v) => new(v, LengthUnit.Kilometers);
    public static Length FromMiles(double v) => new(v, LengthUnit.Miles);
    public static Length FromNauticalMiles(double v) => new(v, LengthUnit.NauticalMiles);
    public static Length FromFeet(double v) => new(v, LengthUnit.Feet);
    public static Length FromYards(double v) => new(v, LengthUnit.Yards);
    public static Length FromCentimeters(double v) => new(v, LengthUnit.Centimeters);
    public static Length FromMillimeters(double v) => new(v, LengthUnit.Millimeters);

    /// <summary>@turf's `radiansToLength` (from the great-circle angle).</summary>
    public static Length FromRadians(double v) => new(v, LengthUnit.Radians);

    public static readonly Length Zero = new(0, LengthUnit.Meters);

    public static Length operator +(Length a, Length b) => new(a.Value + b.As(a.Unit), a.Unit);
    public static Length operator -(Length a, Length b) => new(a.Value - b.As(a.Unit), a.Unit);
    public static Length operator *(Length a, double k) => new(a.Value * k, a.Unit);
    public static Length operator /(Length a, double k) => new(a.Value / k, a.Unit);
}
