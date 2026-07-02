using BenchmarkDotNet.Attributes;
using Turfano.GeoJson;

namespace TimeAndMemoryUsage;

// Benchmarks sobre a fachada Geo (tipos GeoJSON próprios, zero NTS/UnitsNet) — Fase 11
// (leva 2). Compara as operações mais usadas (Distance/Area/Union) em geometrias
// sintéticas.
[MemoryDiagnoser]
public class TurfanoBench
{
    private static readonly Position Origin = new(-122.4194, 37.7749);
    private static readonly Position Destination = new(-122.4094, 37.7849);

    private static readonly Polygon Square = Geo.Polygon(
        [
            new Position(0, 0),
            new Position(0, 0.01),
            new Position(0.01, 0.01),
            new Position(0.01, 0),
            new Position(0, 0),
        ]
    );

    private static readonly Polygon OverlappingSquare = Geo.Polygon(
        [
            new Position(0.005, 0.005),
            new Position(0.005, 0.015),
            new Position(0.015, 0.015),
            new Position(0.015, 0.005),
            new Position(0.005, 0.005),
        ]
    );

    [Benchmark, BenchmarkCategory("Distance")]
    public double Distance()
    {
        return Geo.Distance(Origin, Destination).Meters;
    }

    [Benchmark, BenchmarkCategory("Area")]
    public double Area()
    {
        return Geo.Area(Square).SquareMeters;
    }

    [Benchmark, BenchmarkCategory("Union")]
    public Geometry? Union()
    {
        return Geo.Union(Square, OverlappingSquare);
    }
}
