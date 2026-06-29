using BenchmarkDotNet.Attributes;
using NetTopologySuite.Geometries;
using Turfano;

namespace TimeAndMemoryUsage;

// PROTÓTIPO DESCARTÁVEL (Fase 2) — compara tipos de valor próprios (struct Pos + double)
// contra os tipos do NTS (Coordinate/Geometry, classes) + UnitsNet, nas rotas Distance e
// Area. Objetivo: medir alocação/tempo. NÃO são os tipos definitivos da Fase 3.
[MemoryDiagnoser]
[ShortRunJob]
public class NtsVsOwnBench
{
    private static readonly (double x, double y)[] RingData =
        [(-5, 52), (-4, 56), (-2, 51), (-7, 54), (-5, 52)];

    [Benchmark]
    public double DistanceNts()
    {
        var a = new Coordinate(0, 0);
        var b = new Coordinate(1, 1);
        return Turf.Distance(a, b).Meters;
    }

    [Benchmark]
    public double DistanceOwn()
    {
        var a = new Pos(0, 0);
        var b = new Pos(1, 1);
        return Geo.DistanceMeters(a, b);
    }

    [Benchmark]
    public double AreaNts()
    {
        var ring = new Coordinate[RingData.Length];
        for (int i = 0; i < RingData.Length; i++)
            ring[i] = new Coordinate(RingData[i].x, RingData[i].y);
        var poly = new Polygon(new LinearRing(ring));
        return Turf.Area(poly).SquareMeters;
    }

    [Benchmark]
    public double AreaOwn()
    {
        var ring = new Pos[RingData.Length];
        for (int i = 0; i < RingData.Length; i++)
            ring[i] = new Pos(RingData[i].x, RingData[i].y);
        return Geo.RingAreaSqMeters(ring);
    }
}

public readonly record struct Pos(double X, double Y);

public static class Geo
{
    private const double R = 6371008.8;
    private const double D2R = Math.PI / 180.0;

    public static double DistanceMeters(Pos a, Pos b)
    {
        var dLat = (b.Y - a.Y) * D2R;
        var dLon = (b.X - a.X) * D2R;
        var lat1 = a.Y * D2R;
        var lat2 = b.Y * D2R;
        var h =
            Math.Pow(Math.Sin(dLat / 2), 2)
            + Math.Pow(Math.Sin(dLon / 2), 2) * Math.Cos(lat1) * Math.Cos(lat2);
        return R * 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
    }

    public static double RingAreaSqMeters(Pos[] coords)
    {
        var n = coords.Length - 1;
        if (n <= 2)
            return 0;
        double total = 0;
        for (int i = 0; i < n; i++)
        {
            var lower = coords[i];
            var middle = coords[(i + 1) % n];
            var upper = coords[(i + 2) % n];
            total += (upper.X * D2R - lower.X * D2R) * Math.Sin(middle.Y * D2R);
        }
        return Math.Abs(total * R * R / 2);
    }
}
