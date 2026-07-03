using System.Text.Json;
using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// GeoJSON bounding box (RFC 7946): an array of 2×n numbers (4 for 2D
/// `[west, south, east, north]`, 6 for 3D). Serialized as a JSON array.
/// </summary>
[JsonConverter(typeof(BBoxConverter))]
public readonly struct BBox
{
    public double[] Values { get; }

    public BBox(params double[] values) => Values = values;

    /// <summary>2 (2D) or 3 (3D), depending on the number of values (4 or 6).</summary>
    public int Dimension => (Values?.Length ?? 0) == 6 ? 3 : 2;
}

/// <summary>Serializes <see cref="BBox"/> as a JSON array of 4 or 6 numbers.</summary>
public sealed class BBoxConverter : JsonConverter<BBox>
{
    public override BBox Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
    {
        var values = new List<double>();
        r.Read();
        while (r.TokenType != JsonTokenType.EndArray)
        {
            values.Add(r.GetDouble());
            r.Read();
        }
        return new BBox(values.ToArray());
    }

    public override void Write(Utf8JsonWriter w, BBox v, JsonSerializerOptions o)
    {
        w.WriteStartArray();
        foreach (var x in v.Values)
            w.WriteNumberValue(x);
        w.WriteEndArray();
    }
}
