using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// Base de todos os objetos GeoJSON (RFC 7946). A (de)serialização é feita pelo
/// <see cref="GeoJsonConverter"/> (polimorfismo manual pelo discriminador `type`,
/// AOT-safe). O <c>bbox</c> é opcional em qualquer objeto.
/// </summary>
[JsonConverter(typeof(GeoJsonConverter))]
public abstract record GeoJsonObject
{
    /// <summary>Discriminador GeoJSON (`Point`, `Feature`, ...).</summary>
    public abstract string Type { get; }

    /// <summary>Bounding box opcional (RFC 7946).</summary>
    public BBox? Bbox { get; init; }
}

/// <summary>Base das 7 geometrias GeoJSON.</summary>
public abstract record Geometry : GeoJsonObject;

public sealed record Point(Position Coordinates) : Geometry
{
    public override string Type => "Point";
}

public sealed record MultiPoint(Position[] Coordinates) : Geometry
{
    public override string Type => "MultiPoint";
}

public sealed record LineString(Position[] Coordinates) : Geometry
{
    public override string Type => "LineString";
}

public sealed record MultiLineString(Position[][] Coordinates) : Geometry
{
    public override string Type => "MultiLineString";
}

public sealed record Polygon(Position[][] Coordinates) : Geometry
{
    public override string Type => "Polygon";
}

public sealed record MultiPolygon(Position[][][] Coordinates) : Geometry
{
    public override string Type => "MultiPolygon";
}

public sealed record GeometryCollection(Geometry[] Geometries) : Geometry
{
    public override string Type => "GeometryCollection";
}

/// <summary>
/// Feature GeoJSON: geometria + propriedades. `properties` é um <see cref="JsonObject"/>
/// flexível (estilo Turf, AOT-friendly); `id` pode ser string ou número (RFC 7946).
/// </summary>
public sealed record Feature(Geometry? Geometry = null, JsonObject? Properties = null) : GeoJsonObject
{
    public override string Type => "Feature";

    /// <summary>Identificador opcional (string ou número).</summary>
    public JsonNode? Id { get; init; }
}

public sealed record FeatureCollection(Feature[] Features) : GeoJsonObject
{
    public override string Type => "FeatureCollection";
}
