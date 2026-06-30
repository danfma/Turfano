using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Filtra os pontos que estão dentro do polígono — `@turf/points-within-polygon`.</summary>
    public static FeatureCollection PointsWithinPolygon(FeatureCollection points, Geometry polygon)
    {
        var inside = points
            .Features.Where(f => f.Geometry is Point p && PointInAnyPolygon(p, polygon))
            .ToArray();
        return new FeatureCollection(inside);
    }

    /// <summary>Atribui a cada ponto a propriedade `field` do polígono que o contém — `@turf/tag`.</summary>
    public static FeatureCollection Tag(
        FeatureCollection points,
        FeatureCollection polygons,
        string field,
        string outField
    )
    {
        var tagged = points
            .Features.Select(pointFeature =>
            {
                if (pointFeature.Geometry is not Point point)
                    return pointFeature;

                var props =
                    pointFeature.Properties is null
                        ? new JsonObject()
                        : (JsonObject)pointFeature.Properties.DeepClone();

                foreach (var polyFeature in polygons.Features)
                {
                    if (
                        polyFeature.Geometry is { } geom
                        && PointInAnyPolygon(point, geom)
                        && polyFeature.Properties?[field] is { } value
                    )
                    {
                        props[outField] = value.DeepClone();
                        break;
                    }
                }

                return new Feature(pointFeature.Geometry, props) { Id = pointFeature.Id };
            })
            .ToArray();
        return new FeatureCollection(tagged);
    }

    private static bool PointInAnyPolygon(Point point, Geometry polygon)
    {
        if (polygon is Polygon || polygon is MultiPolygon)
            return BooleanPointInPolygon(point, polygon);
        if (polygon is GeometryCollection gc)
            return gc.Geometries.Any(g => PointInAnyPolygon(point, g));
        return false;
    }
}
