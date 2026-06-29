using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

/// <summary>
/// Construtores ao estilo TurfJS + invariantes, sobre os tipos GeoJSON próprios. Reduz o
/// atrito de portar código JS (`point()`, `lineString()`, ...).
/// </summary>
public static partial class Geo
{
    public static Point Point(double lon, double lat, double? alt = null) =>
        new(new Position(lon, lat, alt));

    public static Point Point(Position coordinates) => new(coordinates);

    public static MultiPoint MultiPoint(params Position[] coordinates) => new(coordinates);

    public static LineString LineString(params Position[] coordinates) => new(coordinates);

    public static MultiLineString MultiLineString(params Position[][] coordinates) => new(coordinates);

    public static Polygon Polygon(params Position[][] rings) => new(rings);

    public static MultiPolygon MultiPolygon(params Position[][][] coordinates) => new(coordinates);

    public static GeometryCollection GeometryCollection(params Geometry[] geometries) => new(geometries);

    public static Feature Feature(Geometry? geometry = null, JsonObject? properties = null) =>
        new(geometry, properties);

    public static FeatureCollection FeatureCollection(params Feature[] features) => new(features);

    // --- Invariantes (estilo @turf/invariant) ---

    public static string GetType(GeoJsonObject obj) => obj.Type;

    public static Geometry? GetGeom(GeoJsonObject obj) =>
        obj switch
        {
            Feature f => f.Geometry,
            Geometry g => g,
            _ => null,
        };

    public static Position GetCoord(GeoJsonObject obj) =>
        obj switch
        {
            Point p => p.Coordinates,
            Feature { Geometry: Point p } => p.Coordinates,
            _ => throw new ArgumentException("GetCoord espera um Point (ou Feature com Point).", nameof(obj)),
        };
}
