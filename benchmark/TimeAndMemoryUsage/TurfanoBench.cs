using BenchmarkDotNet.Attributes;
using NetTopologySuite.Geometries;
using Turfano;
using UnitsNet;

namespace TimeAndMemoryUsage;

[MemoryDiagnoser]
public class TurfanoBench
{
    private const double QuarterSide = 0.01;
    private readonly Length _walkDistance = Length.FromKilometers(2);

    private readonly LinearRing _lineRing = new([
        new Coordinate(0, 0),
        new Coordinate(0, QuarterSide),
        new Coordinate(QuarterSide, QuarterSide),
        new Coordinate(QuarterSide, 0),
        new Coordinate(0, 0),
    ]);

    [Benchmark, BenchmarkCategory("Along")]
    public Point WalkInLineRing()
    {
        return Turf.WalkAlong(_lineRing, _walkDistance);
    }
}
