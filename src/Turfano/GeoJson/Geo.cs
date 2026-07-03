using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

/// <summary>
/// TurfJS-style constructors plus invariants, built on Turfano's own GeoJSON types. Reduces
/// the friction of porting JS code (`point()`, `lineString()`, ...).
/// </summary>
public static partial class Geo
{
    /// <summary>Creates a Point from longitude/latitude (and optional altitude) — `@turf/helpers.point`.</summary>
    public static Point Point(double lon, double lat, double? alt = null) =>
        new(new Position(lon, lat, alt));

    /// <summary>Creates a Point from a Position.</summary>
    public static Point Point(Position coordinates) => new(coordinates);

    /// <summary>Creates a MultiPoint from the given positions — `@turf/helpers.multiPoint`.</summary>
    public static MultiPoint MultiPoint(params Position[] coordinates) => new(coordinates);

    /// <summary>Creates a LineString from the given positions — `@turf/helpers.lineString`.</summary>
    public static LineString LineString(params Position[] coordinates) => new(coordinates);

    /// <summary>Creates a MultiLineString from the given lines — `@turf/helpers.multiLineString`.</summary>
    public static MultiLineString MultiLineString(params Position[][] coordinates) =>
        new(coordinates);

    /// <summary>Creates a Polygon from the given rings (the first is the exterior, the rest are holes) — `@turf/helpers.polygon`.</summary>
    public static Polygon Polygon(params Position[][] rings) => new(rings);

    /// <summary>Creates a MultiPolygon from the given polygons — `@turf/helpers.multiPolygon`.</summary>
    public static MultiPolygon MultiPolygon(params Position[][][] coordinates) => new(coordinates);

    /// <summary>Creates a GeometryCollection from the given geometries — `@turf/helpers.geometryCollection`.</summary>
    public static GeometryCollection GeometryCollection(params Geometry[] geometries) =>
        new(geometries);

    /// <summary>Creates a Feature with optional geometry and properties — `@turf/helpers.feature`.</summary>
    public static Feature Feature(Geometry? geometry = null, JsonObject? properties = null) =>
        new(geometry, properties);

    /// <summary>Creates a FeatureCollection from the given features — `@turf/helpers.featureCollection`.</summary>
    public static FeatureCollection FeatureCollection(params Feature[] features) => new(features);

    // --- Invariantes (estilo @turf/invariant) ---

    /// <summary>The GeoJSON type of the object (`"Point"`, `"Polygon"`, ...) — `@turf/invariant.getType`.</summary>
    public static string GetGeoJsonType(GeoJsonObject obj) => obj.Type;

    /// <summary>The geometry of a Feature (or the object itself, if it's already a geometry); `null` otherwise — `@turf/invariant.getGeom`.</summary>
    public static Geometry? GetGeom(GeoJsonObject obj) =>
        obj switch
        {
            Feature f => f.Geometry,
            Geometry g => g,
            _ => null,
        };

    /// <summary>The Position of a Point (or of a Feature wrapping a Point) — `@turf/invariant.getCoord`.</summary>
    public static Position GetCoord(GeoJsonObject obj) =>
        obj switch
        {
            Point p => p.Coordinates,
            Feature { Geometry: Point p } => p.Coordinates,
            _ => throw new ArgumentException(
                "GetCoord espera um Point (ou Feature com Point).",
                nameof(obj)
            ),
        };
}
