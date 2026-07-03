using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

/// <summary>
/// Construtores ao estilo TurfJS + invariantes, sobre os tipos GeoJSON próprios. Reduz o
/// atrito de portar código JS (`point()`, `lineString()`, ...).
/// </summary>
public static partial class Geo
{
    /// <summary>Cria um Point a partir de longitude/latitude (e altitude opcional) — `@turf/helpers.point`.</summary>
    public static Point Point(double lon, double lat, double? alt = null) =>
        new(new Position(lon, lat, alt));

    /// <summary>Cria um Point a partir de uma Position.</summary>
    public static Point Point(Position coordinates) => new(coordinates);

    /// <summary>Cria um MultiPoint a partir das posições — `@turf/helpers.multiPoint`.</summary>
    public static MultiPoint MultiPoint(params Position[] coordinates) => new(coordinates);

    /// <summary>Cria um LineString a partir das posições — `@turf/helpers.lineString`.</summary>
    public static LineString LineString(params Position[] coordinates) => new(coordinates);

    /// <summary>Cria um MultiLineString a partir das linhas — `@turf/helpers.multiLineString`.</summary>
    public static MultiLineString MultiLineString(params Position[][] coordinates) =>
        new(coordinates);

    /// <summary>Cria um Polygon a partir dos anéis (o primeiro é o exterior, os demais são furos) — `@turf/helpers.polygon`.</summary>
    public static Polygon Polygon(params Position[][] rings) => new(rings);

    /// <summary>Cria um MultiPolygon a partir dos polígonos — `@turf/helpers.multiPolygon`.</summary>
    public static MultiPolygon MultiPolygon(params Position[][][] coordinates) => new(coordinates);

    /// <summary>Cria uma GeometryCollection a partir das geometrias — `@turf/helpers.geometryCollection`.</summary>
    public static GeometryCollection GeometryCollection(params Geometry[] geometries) =>
        new(geometries);

    /// <summary>Cria uma Feature com geometria e propriedades opcionais — `@turf/helpers.feature`.</summary>
    public static Feature Feature(Geometry? geometry = null, JsonObject? properties = null) =>
        new(geometry, properties);

    /// <summary>Cria uma FeatureCollection a partir das features — `@turf/helpers.featureCollection`.</summary>
    public static FeatureCollection FeatureCollection(params Feature[] features) => new(features);

    // --- Invariantes (estilo @turf/invariant) ---

    /// <summary>O tipo GeoJSON do objeto (`"Point"`, `"Polygon"`, ...) — `@turf/invariant.getType`.</summary>
    public static string GetGeoJsonType(GeoJsonObject obj) => obj.Type;

    /// <summary>A geometria de uma Feature (ou a própria, se já for geometria); `null` caso contrário — `@turf/invariant.getGeom`.</summary>
    public static Geometry? GetGeom(GeoJsonObject obj) =>
        obj switch
        {
            Feature f => f.Geometry,
            Geometry g => g,
            _ => null,
        };

    /// <summary>A Position de um Point (ou de uma Feature com Point) — `@turf/invariant.getCoord`.</summary>
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
