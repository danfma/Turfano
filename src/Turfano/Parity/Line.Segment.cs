namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Decompõe a geometria em segmentos de 2 vértices — `@turf/line-segment` (LineString,
    /// MultiLineString, Polygon e MultiPolygon; anéis incluídos).
    /// </summary>
    public static FeatureCollection LineSegment(Geometry geojson)
    {
        var results = new List<Feature>();
        foreach (var line in LinearParts(geojson))
        {
            for (var i = 1; i < line.Length; i++)
                results.Add(new Feature(new LineString(new[] { line[i - 1], line[i] })));
        }
        return new FeatureCollection(results.ToArray());
    }

    /// <summary>Sequências lineares (linhas e anéis) da geometria, na ordem do flattenEach.</summary>
    internal static IEnumerable<Position[]> LinearParts(Geometry geometry)
    {
        switch (geometry)
        {
            case LineString lineString:
                yield return lineString.Coordinates;
                break;
            case MultiLineString multiLineString:
                foreach (var line in multiLineString.Coordinates)
                    yield return line;
                break;
            case Polygon polygon:
                foreach (var ring in polygon.Coordinates)
                    yield return ring;
                break;
            case MultiPolygon multiPolygon:
                foreach (var poly in multiPolygon.Coordinates)
                foreach (var ring in poly)
                    yield return ring;
                break;
            case GeometryCollection collection:
                foreach (var sub in collection.Geometries)
                foreach (var part in LinearParts(sub))
                    yield return part;
                break;
        }
    }
}
