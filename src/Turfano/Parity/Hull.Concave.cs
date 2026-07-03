using System.Globalization;
using Turfano.GeoJson.Polyclip;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Concave hull — `@turf/concave`: TIN of the points (deduplicated) + edge filtering by
    /// <paramref name="maxEdge"/> + triangle merging. The merge uses the n-ary UNION from the
    /// native polyclip engine instead of the @turf topojson merge (decision R3 — same
    /// geometric region; validated by area/vertex count). `null` when there is no solution.
    /// </summary>
    public static Geometry? Concave(FeatureCollection points, Units.Length maxEdge)
    {
        var maxEdgeKm = maxEdge.Kilometers;

        // removeDuplicates da fonte (chave "lon-lat", ordem de inserção)
        var seen = new HashSet<string>();
        var cleaned = new List<Feature>();
        foreach (var feature in points.Features)
        {
            if (feature.Geometry is not Point point)
                continue;
            var key =
                point.Coordinates.Lon.ToString("R", CultureInfo.InvariantCulture)
                + "-"
                + point.Coordinates.Lat.ToString("R", CultureInfo.InvariantCulture);
            if (seen.Add(key))
                cleaned.Add(feature);
        }

        var tinTriangles = Tin(new FeatureCollection(cleaned.ToArray()));
        var filtered = new List<Position[][]>();
        foreach (var triangle in tinTriangles.Features)
        {
            var ring = ((Polygon)triangle.Geometry!).Coordinates[0];
            var dist1 = Distance(ring[0], ring[1]).Kilometers;
            var dist2 = Distance(ring[1], ring[2]).Kilometers;
            var dist3 = Distance(ring[0], ring[2]).Kilometers;
            if (dist1 <= maxEdgeKm && dist2 <= maxEdgeKm && dist3 <= maxEdgeKm)
                filtered.Add(((Polygon)triangle.Geometry!).Coordinates);
        }

        if (filtered.Count < 1)
            return null;

        var merged = new OperationRun().Run(
            PolyclipOperationType.Union,
            new[] { filtered[0] },
            filtered.Skip(1).Select(triangle => new[] { triangle }).ToArray()
        );
        return FromOverlayResult(merged);
    }
}
