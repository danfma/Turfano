using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// The point in the collection closest to the line — `@turf/nearest-point-to-line`. The
    /// returned feature carries the distance in `properties["dist"]` (km, matching the @turf
    /// default), preserving the point's other properties.
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
