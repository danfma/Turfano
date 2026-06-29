using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

// ============================================================================
// VARIANTE DE COMPARAÇÃO (Ideia 1) — polimorfismo STJ EMBUTIDO/source-gen +
// converter só na propriedade Features. Paralela à implementação manual em
// Turfano.GeoJson. Existe para comparação; uma das duas sai após a decisão.
//
// Naming: cada propriedade é FIXADA com [JsonPropertyName] (nomes do RFC 7946), de modo
// que a serialização é imune à PropertyNamingPolicy do consumidor (compliance ao aninhar
// uma geometria num objeto do usuário) — mas continua sendo o mecanismo idiomático do STJ
// (compõe com o contexto/options do consumidor, AOT-safe).
// ============================================================================
namespace Turfano.GeoJsonAlt;

[JsonConverter(typeof(PositionConverter))]
public readonly record struct Position(double Lon, double Lat, double? Alt = null);

[JsonConverter(typeof(BBoxConverter))]
public readonly struct BBox
{
    public double[] Values { get; }
    public BBox(params double[] values) => Values = values;
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Point), "Point")]
[JsonDerivedType(typeof(MultiPoint), "MultiPoint")]
[JsonDerivedType(typeof(LineString), "LineString")]
[JsonDerivedType(typeof(MultiLineString), "MultiLineString")]
[JsonDerivedType(typeof(Polygon), "Polygon")]
[JsonDerivedType(typeof(MultiPolygon), "MultiPolygon")]
[JsonDerivedType(typeof(GeometryCollection), "GeometryCollection")]
[JsonDerivedType(typeof(Feature), "Feature")]
[JsonDerivedType(typeof(FeatureCollection), "FeatureCollection")]
public abstract record GeoJsonObject
{
    [JsonIgnore]
    public abstract string Type { get; }

    [JsonPropertyName("bbox")]
    [JsonPropertyOrder(100)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BBox? Bbox { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Point), "Point")]
[JsonDerivedType(typeof(MultiPoint), "MultiPoint")]
[JsonDerivedType(typeof(LineString), "LineString")]
[JsonDerivedType(typeof(MultiLineString), "MultiLineString")]
[JsonDerivedType(typeof(Polygon), "Polygon")]
[JsonDerivedType(typeof(MultiPolygon), "MultiPolygon")]
[JsonDerivedType(typeof(GeometryCollection), "GeometryCollection")]
public abstract record Geometry : GeoJsonObject;

public sealed record Point(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position Coordinates) : Geometry
{ [JsonIgnore] public override string Type => "Point"; }

public sealed record MultiPoint(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[] Coordinates) : Geometry
{ [JsonIgnore] public override string Type => "MultiPoint"; }

public sealed record LineString(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[] Coordinates) : Geometry
{ [JsonIgnore] public override string Type => "LineString"; }

public sealed record MultiLineString(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[][] Coordinates) : Geometry
{ [JsonIgnore] public override string Type => "MultiLineString"; }

public sealed record Polygon(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[][] Coordinates) : Geometry
{ [JsonIgnore] public override string Type => "Polygon"; }

public sealed record MultiPolygon(
    [property: JsonPropertyName("coordinates"), JsonPropertyOrder(1)] Position[][][] Coordinates) : Geometry
{ [JsonIgnore] public override string Type => "MultiPolygon"; }

public sealed record GeometryCollection(
    [property: JsonPropertyName("geometries"), JsonPropertyOrder(1)] Geometry[] Geometries) : Geometry
{ [JsonIgnore] public override string Type => "GeometryCollection"; }

public sealed record Feature(
    [property: JsonPropertyName("geometry"), JsonPropertyOrder(2)] Geometry? Geometry = null,
    [property: JsonPropertyName("properties"), JsonPropertyOrder(3)] JsonObject? Properties = null) : GeoJsonObject
{
    [JsonIgnore] public override string Type => "Feature";

    [JsonPropertyName("id")]
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonNode? Id { get; init; }
}

public sealed record FeatureCollection(
    [property: JsonPropertyName("features"), JsonPropertyOrder(1)]
    [property: JsonConverter(typeof(FeatureArrayConverter))] Feature[] Features) : GeoJsonObject
{ [JsonIgnore] public override string Type => "FeatureCollection"; }

// --- Converters (Position/BBox como array; FeatureArray força serialização como base) ---

public sealed class PositionConverter : JsonConverter<Position>
{
    public override Position Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        r.Read(); var lon = r.GetDouble(); r.Read(); var lat = r.GetDouble(); r.Read();
        double? alt = null; if (r.TokenType == JsonTokenType.Number) { alt = r.GetDouble(); r.Read(); }
        return new Position(lon, lat, alt);
    }
    public override void Write(Utf8JsonWriter w, Position v, JsonSerializerOptions o)
    {
        w.WriteStartArray(); w.WriteNumberValue(v.Lon); w.WriteNumberValue(v.Lat);
        if (v.Alt.HasValue) w.WriteNumberValue(v.Alt.Value); w.WriteEndArray();
    }
}

public sealed class BBoxConverter : JsonConverter<BBox>
{
    public override BBox Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        var values = new List<double>();
        r.Read();
        while (r.TokenType != JsonTokenType.EndArray) { values.Add(r.GetDouble()); r.Read(); }
        return new BBox(values.ToArray());
    }
    public override void Write(Utf8JsonWriter w, BBox v, JsonSerializerOptions o)
    {
        w.WriteStartArray(); foreach (var x in v.Values) w.WriteNumberValue(x); w.WriteEndArray();
    }
}

// Único ponto que o [JsonPolymorphic] não cobre: Feature[] é tipo concreto (folha) e não
// emite o discriminador. Serializa cada item COMO a base (GeoJsonObject), AOT-safe
// (JsonTypeInfo do source-gen, não overload reflexivo).
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
        foreach (var f in value) JsonSerializer.Serialize(w, (GeoJsonObject)f, info);
        w.WriteEndArray();
    }
}

[JsonSerializable(typeof(GeoJsonObject))]
public partial class GeoJsonAltContext : JsonSerializerContext;
