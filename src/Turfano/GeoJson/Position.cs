namespace Turfano.GeoJson;

/// <summary>
/// Uma posição GeoJSON (RFC 7946): longitude, latitude e altitude opcional.
/// Struct de valor imutável — sem alocação no heap nos caminhos quentes.
/// Serializa como array JSON `[lon, lat]` ou `[lon, lat, alt]` (ver <see cref="GeoJsonConverter"/>).
/// </summary>
public readonly record struct Position(double Lon, double Lat, double? Alt = null);
