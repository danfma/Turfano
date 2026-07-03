using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

// US4 — `@turf/collect`: agrega em cada polígono os valores de uma propriedade dos pontos
// que caem dentro dele. A fonte NUNCA clona (mutava `polygons` diretamente); aqui os tipos
// são imutáveis, então construímos features novas com as mesmas properties + a agregada.
public static partial class Geo
{
    /// <summary>
    /// Aggregates, for each polygon, the <paramref name="inProperty"/> values of the points
    /// that fall inside it, as an array in <paramref name="outProperty"/> — `@turf/collect`.
    /// A point without <paramref name="inProperty"/> still gets pushed onto the array (as
    /// `null` — the equivalent of the `undefined` that JS would push; when serialized to
    /// JSON, JS also turns it into `null`). The order of the values follows the spatial
    /// index (rbush) traversal, not the insertion order of the points.
    /// </summary>
    public static FeatureCollection Collect(
        FeatureCollection polygons,
        FeatureCollection points,
        string inProperty,
        string outProperty
    )
    {
        var tree = new RBushIndex<JsonNode?>(6); // `new rbush(6)` na fonte
        var items = new List<RBushItem<JsonNode?>>(points.Features.Length);
        foreach (var pointFeature in points.Features)
        {
            if (pointFeature.Geometry is not Point point)
                continue; // a fonte assume `item.geometry.coordinates`; sem Point não há o que agregar
            var value = pointFeature.Properties?[inProperty];
            var lon = point.Coordinates.Lon;
            var lat = point.Coordinates.Lat;
            items.Add(new RBushItem<JsonNode?>(lon, lat, lon, lat, value?.DeepClone()));
        }
        tree.Load(items);

        var polygonFeatures = new Feature[polygons.Features.Length];
        for (var i = 0; i < polygons.Features.Length; i++)
        {
            var polyFeature = polygons.Features[i];
            var properties =
                polyFeature.Properties is null ? new JsonObject() : (JsonObject)polyFeature.Properties.DeepClone();

            var values = new JsonArray();
            if (polyFeature.Geometry is { } polyGeometry)
            {
                var bbox = Bbox(polyGeometry).Values;
                var candidates = tree.Search(
                    new SpatialBox
                    {
                        MinX = bbox[0],
                        MinY = bbox[1],
                        MaxX = bbox[2],
                        MaxY = bbox[3],
                    }
                );
                foreach (var candidate in candidates)
                {
                    var candidatePoint = new Point(new Position(candidate.MinX, candidate.MinY));
                    if (BooleanPointInPolygon(candidatePoint, polyGeometry))
                        values.Add(candidate.Item?.DeepClone());
                }
            }

            properties[outProperty] = values;
            polygonFeatures[i] = new Feature(polyFeature.Geometry, properties) { Id = polyFeature.Id };
        }
        return new FeatureCollection(polygonFeatures);
    }
}
