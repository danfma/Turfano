using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Turfano.GeoJson;

// Serialização: polimorfismo System.Text.Json EMBUTIDO (source-gen) pelo discriminador
// GeoJSON `type`, + os nomes do RFC 7946 FIXADOS por [JsonPropertyName] (imunes à
// PropertyNamingPolicy do consumidor ao aninhar geometrias em objetos dele) + um único
// FeatureArrayConverter para o caso que o embutido não cobre (Feature[] é tipo concreto).
// Os valores do discriminador usam nameof(Tipo) para ficarem à prova de refactor.

/// <summary>Base de todos os objetos GeoJSON (RFC 7946).</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Point), nameof(Point))]
[JsonDerivedType(typeof(MultiPoint), nameof(MultiPoint))]
[JsonDerivedType(typeof(LineString), nameof(LineString))]
[JsonDerivedType(typeof(MultiLineString), nameof(MultiLineString))]
[JsonDerivedType(typeof(Polygon), nameof(Polygon))]
[JsonDerivedType(typeof(MultiPolygon), nameof(MultiPolygon))]
[JsonDerivedType(typeof(GeometryCollection), nameof(GeometryCollection))]
[JsonDerivedType(typeof(Feature), nameof(Feature))]
[JsonDerivedType(typeof(FeatureCollection), nameof(FeatureCollection))]
public abstract record GeoJsonObject
{
    /// <summary>Discriminador GeoJSON. Não vai ao fio aqui (o `type` é emitido pelo polimorfismo).</summary>
    [JsonIgnore]
    public abstract string Type { get; }

    /// <summary>Bounding box opcional (RFC 7946).</summary>
    [JsonPropertyName("bbox")]
    [JsonPropertyOrder(100)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BBox? Bbox { get; init; }
}

/// <summary>Base das 7 geometrias GeoJSON.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Point), nameof(Point))]
[JsonDerivedType(typeof(MultiPoint), nameof(MultiPoint))]
[JsonDerivedType(typeof(LineString), nameof(LineString))]
[JsonDerivedType(typeof(MultiLineString), nameof(MultiLineString))]
[JsonDerivedType(typeof(Polygon), nameof(Polygon))]
[JsonDerivedType(typeof(MultiPolygon), nameof(MultiPolygon))]
[JsonDerivedType(typeof(GeometryCollection), nameof(GeometryCollection))]
public abstract record Geometry : GeoJsonObject;

public sealed record Point(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position Coordinates) : Geometry
{
    [JsonIgnore]
    public override string Type => nameof(Point);
}

public sealed record MultiPoint(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[] Coordinates) : Geometry
{
    [JsonIgnore]
    public override string Type => nameof(MultiPoint);
}

public sealed record LineString(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[] Coordinates) : Geometry
{
    [JsonIgnore]
    public override string Type => nameof(LineString);
}

public sealed record MultiLineString(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[][] Coordinates) : Geometry
{
    [JsonIgnore]
    public override string Type => nameof(MultiLineString);
}

public sealed record Polygon(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[][] Coordinates) : Geometry
{
    [JsonIgnore]
    public override string Type => nameof(Polygon);
}

public sealed record MultiPolygon(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[][][] Coordinates) : Geometry
{
    [JsonIgnore]
    public override string Type => nameof(MultiPolygon);
}

public sealed record GeometryCollection(
    [property: JsonPropertyName("geometries"), JsonPropertyOrder(1)] Geometry[] Geometries) : Geometry
{
    [JsonIgnore]
    public override string Type => nameof(GeometryCollection);
}

/// <summary>
/// Feature GeoJSON. `properties` é um <see cref="JsonObject"/> flexível (estilo Turf,
/// AOT-friendly); `id` pode ser string ou número (RFC 7946).
/// </summary>
public sealed record Feature(
    [property: JsonPropertyName("geometry"), JsonPropertyOrder(2)] Geometry? Geometry = null,
    [property: JsonPropertyName("properties"), JsonPropertyOrder(3)] JsonObject? Properties = null) : GeoJsonObject
{
    [JsonIgnore]
    public override string Type => nameof(Feature);

    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Id { get; init; }
}

public sealed record FeatureCollection(
    [property: JsonPropertyName("features"), JsonPropertyOrder(1)]
    [property: JsonConverter(typeof(FeatureArrayConverter))] Feature[] Features) : GeoJsonObject
{
    [JsonIgnore]
    public override string Type => nameof(FeatureCollection);
}

/// <summary>
/// Único ponto que o `[JsonPolymorphic]` embutido não cobre: <c>Feature[]</c> é tipo
/// concreto (folha) e não emitiria o discriminador. Este converter serializa/desserializa
/// cada item COMO a base (<see cref="GeoJsonObject"/>), de forma AOT-safe (usa o
/// <see cref="JsonTypeInfo"/> do source-gen, não os overloads reflexivos).
/// </summary>
public sealed class FeatureArrayConverter : JsonConverter<Feature[]>
{
    public override Feature[] Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
    {
        var info = (JsonTypeInfo<GeoJsonObject>)o.GetTypeInfo(typeof(GeoJsonObject));
        var list = new List<Feature>();
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add((Feature)JsonSerializer.Deserialize(ref reader, info)!);
            reader.Read();
        }
        return list.ToArray();
    }

    public override void Write(Utf8JsonWriter w, Feature[] value, JsonSerializerOptions o)
    {
        var info = (JsonTypeInfo<GeoJsonObject>)o.GetTypeInfo(typeof(GeoJsonObject));
        w.WriteStartArray();
        foreach (var f in value)
            JsonSerializer.Serialize(w, (GeoJsonObject)f, info);
        w.WriteEndArray();
    }
}
