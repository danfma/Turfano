namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Breaks the geometry down into 2-vertex segments — `@turf/line-segment` (LineString,
    /// MultiLineString, Polygon, and MultiPolygon; rings included).
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

    /// <summary>Linear sequences (lines and rings) of the geometry, in flattenEach order.</summary>
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
