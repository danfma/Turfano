using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// Contexto System.Text.Json source-generated dos tipos GeoJSON. Fornece o
/// `TypeInfoResolver` (exigido sob Native AOT / trimming) e ativa o polimorfismo embutido
/// (discriminador `type`) declarado em <see cref="GeoJsonObject"/>/<see cref="Geometry"/>.
/// </summary>
[JsonSerializable(typeof(GeoJsonObject))]
public partial class GeoJsonSerializerContext : JsonSerializerContext;
