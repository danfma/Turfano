using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// Source-generated System.Text.Json context for the GeoJSON types. Provides the
/// `TypeInfoResolver` (required under Native AOT / trimming) and enables the built-in
/// polymorphism (`type` discriminator) declared on <see cref="GeoJsonObject"/>/<see cref="Geometry"/>.
/// </summary>
[JsonSerializable(typeof(GeoJsonObject))]
public partial class GeoJsonSerializerContext : JsonSerializerContext;
