using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Weighted arithmetic mean center of a collection — `@turf/center-mean`. Every
    /// coordinate of every feature contributes to the average, weighted by
    /// <paramref name="weightProperty"/> (default: weight 1). Features with weight ≤ 0 are
    /// ignored (same rule as @turf: `if (weight > 0) { ... }`).
    /// </summary>
    public static Feature CenterMean(
        FeatureCollection features,
        string? weightProperty = null,
        JsonObject? properties = null
    )
    {
        double sumXs = 0,
            sumYs = 0,
            sumWeights = 0;

        for (var featureIndex = 0; featureIndex < features.Features.Length; featureIndex++)
        {
            var feature = features.Features[featureIndex];
            if (feature.Geometry is not { } geometry)
                continue;

            var weight = ResolveWeightOrOne(feature.Properties, weightProperty, featureIndex);
            if (weight > 0)
            {
                EachPosition(
                    geometry,
                    excludeWrapCoord: false,
                    coord =>
                    {
                        sumXs += coord.Lon * weight;
                        sumYs += coord.Lat * weight;
                        sumWeights += weight;
                    }
                );
            }
        }

        return new Feature(new Point(new Position(sumXs / sumWeights, sumYs / sumWeights)), properties);
    }

    /// <summary>
    /// Weighted median center (iterative) — `@turf/center-median`. Starts from the mean
    /// center and refines it through successive inverse-distance-weighted averages
    /// (Weber/Kuhn-Kuenne algorithm), up to `counter` iterations or convergence within
    /// <paramref name="tolerance"/>. The properties carry `medianCandidates` (the
    /// intermediate candidates), same as @turf.
    /// </summary>
    public static Feature CenterMedian(
        FeatureCollection features,
        string? weightProperty = null,
        double tolerance = 0.001,
        int counter = 10
    )
    {
        // Quirk do @turf: `options.tolerance || 1e-3` e `options.counter || 10` — um 0
        // explícito nesses parâmetros também cai no default (falsy).
        var effectiveTolerance = tolerance != 0 ? tolerance : 0.001;
        var effectiveCounter = counter != 0 ? counter : 10;

        var meanCenter = ((Point)CenterMean(features, weightProperty).Geometry!).Coordinates;

        var centroids = new List<(Position Coord, double? Weight)>();
        foreach (var feature in features.Features)
        {
            if (feature.Geometry is not { } geometry)
                continue;
            var centroidCoord = Centroid(geometry).Coordinates;
            var weight = string.IsNullOrEmpty(weightProperty)
                ? (double?)null
                : ResolveOptionalWeight(feature.Properties, weightProperty);
            centroids.Add((centroidCoord, weight));
        }

        var medianCandidates = new List<Position>();
        var median = FindMedian(
            meanCenter,
            new Position(0, 0),
            centroids,
            effectiveTolerance,
            medianCandidates,
            effectiveCounter
        );

        var properties = new JsonObject
        {
            ["medianCandidates"] = new JsonArray(
                medianCandidates.Select(p => (JsonNode)new JsonArray(p.Lon, p.Lat)).ToArray()
            ),
        };
        return new Feature(new Point(median), properties);
    }

    private static Position FindMedian(
        Position candidateMedian,
        Position previousCandidate,
        List<(Position Coord, double? Weight)> centroids,
        double tolerance,
        List<Position> medianCandidates,
        int counter
    )
    {
        double candidateXsum = 0,
            candidateYsum = 0,
            kSum = 0;
        var centroidCount = 0;

        foreach (var (coord, rawWeight) in centroids)
        {
            var weight = rawWeight ?? 1;
            if (weight > 0)
            {
                centroidCount += 1;
                var distanceFromCandidate = weight * Distance(coord, candidateMedian).Kilometers;
                if (distanceFromCandidate == 0)
                    distanceFromCandidate = 1;
                var k = weight / distanceFromCandidate;
                candidateXsum += coord.Lon * k;
                candidateYsum += coord.Lat * k;
                kSum += k;
            }
        }

        if (centroidCount < 1)
            throw new InvalidOperationException("no features to measure");

        var candidateX = candidateXsum / kSum;
        var candidateY = candidateYsum / kSum;

        if (
            centroidCount == 1
            || counter == 0
            || (
                Math.Abs(candidateX - previousCandidate.Lon) < tolerance
                && Math.Abs(candidateY - previousCandidate.Lat) < tolerance
            )
        )
            return new Position(candidateX, candidateY);

        medianCandidates.Add(new Position(candidateX, candidateY));
        return FindMedian(
            new Position(candidateX, candidateY),
            candidateMedian,
            centroids,
            tolerance,
            medianCandidates,
            counter - 1
        );
    }

    /// <summary>
    /// Resolves a feature's weight for `centerMean`: 1 if <paramref name="weightProperty"/>
    /// is not provided or the property is missing; otherwise the property's numeric value
    /// (throws if it isn't numeric) — same rule as `@turf`.
    /// </summary>
    private static double ResolveWeightOrOne(JsonObject? properties, string? weightProperty, int featureIndex)
    {
        if (string.IsNullOrEmpty(weightProperty))
            return 1;
        var node = properties?[weightProperty];
        if (node is null)
            return 1;
        var value = NumberOrNull(node);
        if (value is null)
            throw new ArgumentException($"weight value must be a number for feature index {featureIndex}");
        return value.Value;
    }

    /// <summary>Same resolution, but without a default: `null` means "not provided" (`center-median`).</summary>
    private static double? ResolveOptionalWeight(JsonObject? properties, string weightProperty)
    {
        var node = properties?[weightProperty];
        if (node is null)
            return null;
        var value = NumberOrNull(node);
        if (value is null)
            throw new ArgumentException("weight value must be a number");
        return value;
    }
}
