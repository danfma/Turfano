using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// Contexto System.Text.Json source-generated para os tipos GeoJSON. Fornece o
/// `TypeInfoResolver` exigido em ambientes com reflexão desabilitada (Native AOT /
/// trimming) e ativa o <see cref="GeoJsonConverter"/> registrado em
/// <see cref="GeoJsonObject"/>.
/// </summary>
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(GeoJsonObject))]
[JsonSerializable(typeof(Geometry))]
[JsonSerializable(typeof(Feature))]
[JsonSerializable(typeof(FeatureCollection))]
public partial class GeoJsonSerializerContext : JsonSerializerContext;
