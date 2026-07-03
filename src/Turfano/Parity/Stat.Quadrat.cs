using Units = Turfano.Units;

namespace Turfano.GeoJson;

/// <summary>Result of <see cref="Geo.QuadratAnalysis"/> — same 4 fields as @turf.</summary>
public sealed record QuadratAnalysisResult(
    double CriticalValue,
    bool IsRandom,
    double MaxAbsoluteDifference,
    double[] ObservedDistribution
);

public static partial class Geo
{
    // Valores críticos do teste de Kolmogorov-Smirnov por nível de confiança (%) — `@turf/quadrat-analysis K_TABLE`.
    private static readonly Dictionary<int, double> QuadratKTable =
        new()
        {
            [20] = 1.07275,
            [15] = 1.13795,
            [10] = 1.22385,
            [5] = 1.3581,
            [2] = 1.51743,
            [1] = 1.62762,
        };

    /// <summary>
    /// Quadrat analysis — `@turf/quadrat-analysis`. Overlays a square grid onto the study
    /// area, counts points per cell, and compares the observed distribution against the
    /// Poisson distribution (Kolmogorov-Smirnov test) to infer whether the pattern is
    /// random.
    /// </summary>
    public static QuadratAnalysisResult QuadratAnalysis(
        FeatureCollection pointFeatureSet,
        BBox? studyBbox = null,
        int confidenceLevel = 20
    )
    {
        var bbox =
            studyBbox
            ?? Bbox(
                new GeometryCollection(
                    pointFeatureSet.Features.Where(f => f.Geometry is not null).Select(f => f.Geometry!).ToArray()
                )
            );

        var points = pointFeatureSet.Features;
        var numOfPoints = points.Length;
        var sizeOfArea = Area(BboxPolygon(bbox)).SquareMeters;
        var lengthOfSide = Math.Sqrt(sizeOfArea / numOfPoints * 2);

        var grid = SquareGrid(bbox, Units.Length.FromMeters(lengthOfSide));
        var quadrats = grid.Features;
        var numOfQuadrat = quadrats.Length;
        var boxes = quadrats.Select(q => Bbox(q.Geometry!).Values).ToArray();
        var counts = new int[numOfQuadrat];

        var sumOfPoint = 0;
        foreach (var pointFeature in points)
        {
            var coord = ((Point)pointFeature.Geometry!).Coordinates;
            for (var k = 0; k < numOfQuadrat; k++)
            {
                if (InBBox(coord, boxes[k]))
                {
                    counts[k] += 1;
                    sumOfPoint += 1;
                    break;
                }
            }
        }

        var maxCnt = counts.Length == 0 ? 0 : counts.Max();

        var expectedDistribution = new double[maxCnt + 1];
        var lambda = (double)sumOfPoint / numOfQuadrat;
        double cumulativeProbability = 0;
        for (var x = 0; x <= maxCnt; x++)
        {
            cumulativeProbability += Math.Exp(-lambda) * Math.Pow(lambda, x) / Factorial(x);
            expectedDistribution[x] = cumulativeProbability;
        }

        var observedDistribution = new double[maxCnt + 1];
        var cumulativeObservedQuads = 0;
        for (var x = 0; x <= maxCnt; x++)
        {
            for (var k = 0; k < numOfQuadrat; k++)
                if (counts[k] == x)
                    cumulativeObservedQuads += 1;
            observedDistribution[x] = (double)cumulativeObservedQuads / numOfQuadrat;
        }

        double maxDifference = 0;
        for (var x = 0; x <= maxCnt; x++)
        {
            var difference = Math.Abs(expectedDistribution[x] - observedDistribution[x]);
            if (difference > maxDifference)
                maxDifference = difference;
        }

        var criticalValue = QuadratKTable[confidenceLevel] / Math.Sqrt(numOfQuadrat);
        var isRandom = !(maxDifference > criticalValue);

        return new QuadratAnalysisResult(criticalValue, isRandom, maxDifference, observedDistribution);
    }

    private static bool InBBox(Position point, double[] bbox) =>
        bbox[0] <= point.Lon && bbox[1] <= point.Lat && bbox[2] >= point.Lon && bbox[3] >= point.Lat;

    private static double Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
}
