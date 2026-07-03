using System.Text.Json;
using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// A GeoJSON position (RFC 7946): longitude, latitude, and optional altitude. Immutable
/// value struct (no allocation on hot paths). Serializes as a JSON array
/// `[lon, lat]` or `[lon, lat, alt]`.
/// </summary>
[JsonConverter(typeof(PositionConverter))]
public readonly record struct Position(double Lon, double Lat, double? Alt = null);

/// <summary>Serializes <see cref="Position"/> as a JSON array `[lon, lat]`/`[lon, lat, alt]`.</summary>
public sealed class PositionConverter : JsonConverter<Position>
{
    public override Position Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        r.Read();
        var lon = r.GetDouble();
        r.Read();
        var lat = r.GetDouble();
        r.Read();
        double? alt = null;
        if (r.TokenType == JsonTokenType.Number)
        {
            alt = r.GetDouble();
            r.Read();
        }
        return new Position(lon, lat, alt);
    }

    public override void Write(Utf8JsonWriter w, Position v, JsonSerializerOptions o)
    {
        w.WriteStartArray();
        w.WriteNumberValue(v.Lon);
        w.WriteNumberValue(v.Lat);
        if (v.Alt.HasValue)
            w.WriteNumberValue(v.Alt.Value);
        w.WriteEndArray();
    }
}
