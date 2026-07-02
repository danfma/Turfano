using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// O ponto da coleção mais próximo da linha — `@turf/nearest-point-to-line`. A feature
    /// devolvida carrega a distância em `properties["dist"]` (km, como o default do @turf),
    /// preservando as demais propriedades do ponto.
    /// </summary>
    public static Feature NearestPointToLine(FeatureCollection points, LineString line)
    {
        var candidates = points.Features.Where(f => f.Geometry is Point).ToList();
        if (candidates.Count == 0)
            throw new ArgumentException("points must contain features", nameof(points));

        var closestDistance = double.PositiveInfinity;
        Feature? closest = null;
        foreach (var feature in candidates)
        {
            var d = PointToLineDistance((Point)feature.Geometry!, line).Kilometers;
            if (d < closestDistance)
            {
                closestDistance = d;
                closest = feature;
            }
        }

        // {...{dist}, ...pt.properties} da fonte: as props do ponto têm precedência
        var properties = new JsonObject { ["dist"] = closestDistance };
        if (closest!.Properties is { } original)
        {
            foreach (var pair in original)
                properties[pair.Key] = pair.Value?.DeepClone();
        }
        return new Feature(closest.Geometry, properties);
    }
}
