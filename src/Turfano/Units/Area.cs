namespace Turfano.Units;

/// <summary>Unidades de área alinhadas ao TurfJS (@turf/helpers areaFactors).</summary>
public enum AreaUnit
{
    SquareMeters,
    SquareKilometers,
    SquareMiles,
    SquareNauticalMiles,
    Acres,
    Hectares,
    SquareFeet,
    SquareYards,
    SquareInches,
    SquareCentimeters,
    SquareMillimeters,
}

/// <summary>
/// Área como struct de valor imutável. As conversões reproduzem o `convertArea` do TurfJS,
/// usando os mesmos `areaFactors` de `@turf/helpers` (unidades por metro²). Alguns fatores
/// do @turf são aproximados (ex.: miles, acres) — reproduzimos exatamente para paridade.
/// </summary>
public readonly record struct Area(double Value, AreaUnit Unit)
{
    private static double Factor(AreaUnit u) =>
        u switch
        {
            AreaUnit.SquareMeters => 1,
            AreaUnit.SquareKilometers => 0.000001,
            AreaUnit.SquareMiles => 3.86e-7,
            AreaUnit.SquareNauticalMiles => 2.9155334959812285e-7,
            AreaUnit.Acres => 0.000247105,
            AreaUnit.Hectares => 0.0001,
            AreaUnit.SquareFeet => 10.763910417,
            AreaUnit.SquareYards => 1.195990046,
            AreaUnit.SquareInches => 1550.003100006,
            AreaUnit.SquareCentimeters => 10000,
            AreaUnit.SquareMillimeters => 1000000,
            _ => throw new ArgumentOutOfRangeException(nameof(u), u, "Unidade de área inválida"),
        };

    /// <summary>Converte para outra unidade (idêntico ao `convertArea` do @turf).</summary>
    public double As(AreaUnit unit) => Value / Factor(Unit) * Factor(unit);

    public double SquareMeters => As(AreaUnit.SquareMeters);
    public double SquareKilometers => As(AreaUnit.SquareKilometers);
    public double SquareMiles => As(AreaUnit.SquareMiles);
    public double SquareNauticalMiles => As(AreaUnit.SquareNauticalMiles);
    public double Acres => As(AreaUnit.Acres);
    public double Hectares => As(AreaUnit.Hectares);
    public double SquareFeet => As(AreaUnit.SquareFeet);
    public double SquareYards => As(AreaUnit.SquareYards);

    public static Area FromSquareMeters(double v) => new(v, AreaUnit.SquareMeters);
    public static Area FromSquareKilometers(double v) => new(v, AreaUnit.SquareKilometers);
    public static Area FromSquareMiles(double v) => new(v, AreaUnit.SquareMiles);
    public static Area FromHectares(double v) => new(v, AreaUnit.Hectares);
    public static Area FromAcres(double v) => new(v, AreaUnit.Acres);

    public static readonly Area Zero = new(0, AreaUnit.SquareMeters);

    public static Area operator +(Area a, Area b) => new(a.Value + b.As(a.Unit), a.Unit);
    public static Area operator -(Area a, Area b) => new(a.Value - b.As(a.Unit), a.Unit);
    public static Area operator *(Area a, double k) => new(a.Value * k, a.Unit);
    public static Area operator /(Area a, double k) => new(a.Value / k, a.Unit);
}
