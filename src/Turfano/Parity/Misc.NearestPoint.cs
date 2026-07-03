using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// The point in the collection closest to a target — `@turf/nearest-point`. Adds
    /// `featureIndex` and `distanceToPoint` (km) to the properties, matching `@turf`.
    /// </summary>
    public static Feature NearestPoint(Point target, FeatureCollection points)
    {
        var nearest = points.Features[0];
        var minDistance = double.PositiveInfinity;
        var nearestIndex = 0;

        for (var i = 0; i < points.Features.Length; i++)
        {
            if (points.Features[i].Geometry is Point candidate)
            {
                var distance = Distance(target.Coordinates, candidate.Coordinates).Kilometers;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = points.Features[i];
                    nearestIndex = i;
                }
            }
        }

        var props =
            nearest.Properties is null ? new JsonObject() : (JsonObject)nearest.Properties.DeepClone();
        props["featureIndex"] = nearestIndex;
        props["distanceToPoint"] = minDistance;
        return new Feature(nearest.Geometry, props) { Id = nearest.Id };
    }
}
