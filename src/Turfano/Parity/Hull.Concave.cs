using System.Globalization;
using Turfano.GeoJson.Polyclip;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Casco côncavo — `@turf/concave`: tin dos pontos (sem duplicatas) + filtro de arestas
    /// por <paramref name="maxEdge"/> + fusão dos triângulos. A fusão usa a UNIÃO n-ária do
    /// motor polyclip nativo em vez do merge topojson do @turf (decisão R3 — mesma região
    /// geométrica; validação por área/vértices). `null` quando não há solução.
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
