using System.Text.Json;
using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// Bounding box GeoJSON (RFC 7946): array de 2×n números (4 para 2D
/// `[west, south, east, north]`, 6 para 3D). Serializa como array JSON.
/// </summary>
[JsonConverter(typeof(BBoxConverter))]
public readonly struct BBox
{
    public double[] Values { get; }

    public BBox(params double[] values) => Values = values;

    /// <summary>2 (2D) ou 3 (3D), conforme o número de valores (4 ou 6).</summary>
    public int Dimension => (Values?.Length ?? 0) == 6 ? 3 : 2;
}

/// <summary>Serializa <see cref="BBox"/> como array JSON de 4 ou 6 números.</summary>
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
