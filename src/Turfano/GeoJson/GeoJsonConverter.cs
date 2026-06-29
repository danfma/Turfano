using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Turfano.GeoJson;

/// <summary>
/// Converter polimórfico manual para GeoJSON (RFC 7946). Despacha pelo discriminador
/// `type` na leitura e emite `type` + propriedades na escrita. Sem reflexão (AOT-safe);
/// registrado em <see cref="GeoJsonObject"/> via atributo e usado pelo
/// <see cref="GeoJsonSerializerContext"/>.
///
/// Decisão da Fase 3 (spike T002): o `[JsonPolymorphic]` embutido descarta o discriminador
/// em coleções tipadas como concreto (ex.: <c>Feature[]</c>); por isso o polimorfismo é
/// manual.
/// </summary>
public sealed class GeoJsonConverter : JsonConverter<GeoJsonObject>
{
    public override GeoJsonObject Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return ReadObject(doc.RootElement);
    }

    private static GeoJsonObject ReadObject(JsonElement e)
    {
        var type = e.GetProperty("type").GetString();
        GeoJsonObject obj = type switch
        {
            "Point" => new Point(ReadPosition(e.GetProperty("coordinates"))),
            "MultiPoint" => new MultiPoint(ReadPositions(e.GetProperty("coordinates"))),
            "LineString" => new LineString(ReadPositions(e.GetProperty("coordinates"))),
            "MultiLineString" => new MultiLineString(ReadPositions2(e.GetProperty("coordinates"))),
            "Polygon" => new Polygon(ReadPositions2(e.GetProperty("coordinates"))),
            "MultiPolygon" => new MultiPolygon(ReadPositions3(e.GetProperty("coordinates"))),
            "GeometryCollection" => new GeometryCollection(ReadGeometries(e.GetProperty("geometries"))),
            "Feature" => ReadFeature(e),
            "FeatureCollection" => new FeatureCollection(ReadFeatures(e.GetProperty("features"))),
            _ => throw new JsonException($"GeoJSON 'type' desconhecido: {type}"),
        };

        if (e.TryGetProperty("bbox", out var bb) && bb.ValueKind == JsonValueKind.Array)
            obj = obj with { Bbox = ReadBBox(bb) };

        return obj;
    }

    private static Feature ReadFeature(JsonElement e)
    {
        Geometry? geom = null;
        if (e.TryGetProperty("geometry", out var g) && g.ValueKind != JsonValueKind.Null)
            geom = (Geometry)ReadObject(g);

        JsonObject? props = null;
        if (e.TryGetProperty("properties", out var p) && p.ValueKind == JsonValueKind.Object)
            props = JsonNode.Parse(p.GetRawText())!.AsObject();

        JsonNode? id = null;
        if (e.TryGetProperty("id", out var idEl) && idEl.ValueKind != JsonValueKind.Null)
            id = JsonNode.Parse(idEl.GetRawText());

        return new Feature(geom, props) { Id = id };
    }

    private static Geometry[] ReadGeometries(JsonElement arr)
    {
        var list = new List<Geometry>(arr.GetArrayLength());
        foreach (var g in arr.EnumerateArray())
            list.Add((Geometry)ReadObject(g));
        return list.ToArray();
    }

    private static Feature[] ReadFeatures(JsonElement arr)
    {
        var list = new List<Feature>(arr.GetArrayLength());
        foreach (var f in arr.EnumerateArray())
            list.Add((Feature)ReadObject(f));
        return list.ToArray();
    }

    private static Position ReadPosition(JsonElement a)
    {
        var n = a.GetArrayLength();
        return new Position(a[0].GetDouble(), a[1].GetDouble(), n >= 3 ? a[2].GetDouble() : null);
    }

    private static Position[] ReadPositions(JsonElement a)
    {
        var list = new List<Position>(a.GetArrayLength());
        foreach (var p in a.EnumerateArray())
            list.Add(ReadPosition(p));
        return list.ToArray();
    }

    private static Position[][] ReadPositions2(JsonElement a)
    {
        var list = new List<Position[]>(a.GetArrayLength());
        foreach (var p in a.EnumerateArray())
            list.Add(ReadPositions(p));
        return list.ToArray();
    }

    private static Position[][][] ReadPositions3(JsonElement a)
    {
        var list = new List<Position[][]>(a.GetArrayLength());
        foreach (var p in a.EnumerateArray())
            list.Add(ReadPositions2(p));
        return list.ToArray();
    }

    private static BBox ReadBBox(JsonElement a)
    {
        var values = new double[a.GetArrayLength()];
        for (var i = 0; i < values.Length; i++)
            values[i] = a[i].GetDouble();
        return new BBox(values);
    }

    public override void Write(
        Utf8JsonWriter w,
        GeoJsonObject value,
        JsonSerializerOptions options
    )
    {
        w.WriteStartObject();
        w.WriteString("type", value.Type);

        switch (value)
        {
            case Point p:
                w.WritePropertyName("coordinates");
                WritePosition(w, p.Coordinates);
                break;
            case MultiPoint mp:
                w.WritePropertyName("coordinates");
                WritePositions(w, mp.Coordinates);
                break;
            case LineString ls:
                w.WritePropertyName("coordinates");
                WritePositions(w, ls.Coordinates);
                break;
            case MultiLineString mls:
                w.WritePropertyName("coordinates");
                WritePositions2(w, mls.Coordinates);
                break;
            case Polygon poly:
                w.WritePropertyName("coordinates");
                WritePositions2(w, poly.Coordinates);
                break;
            case MultiPolygon mpoly:
                w.WritePropertyName("coordinates");
                WritePositions3(w, mpoly.Coordinates);
                break;
            case GeometryCollection gc:
                w.WritePropertyName("geometries");
                w.WriteStartArray();
                foreach (var g in gc.Geometries)
                    Write(w, g, options);
                w.WriteEndArray();
                break;
            case Feature f:
                if (f.Id is not null)
                {
                    w.WritePropertyName("id");
                    f.Id.WriteTo(w);
                }
                w.WritePropertyName("geometry");
                if (f.Geometry is null)
                    w.WriteNullValue();
                else
                    Write(w, f.Geometry, options);
                w.WritePropertyName("properties");
                if (f.Properties is null)
                    w.WriteNullValue();
                else
                    f.Properties.WriteTo(w);
                break;
            case FeatureCollection fc:
                w.WritePropertyName("features");
                w.WriteStartArray();
                foreach (var feat in fc.Features)
                    Write(w, feat, options);
                w.WriteEndArray();
                break;
        }

        if (value.Bbox is { } bbox)
        {
            w.WritePropertyName("bbox");
            WriteBBox(w, bbox);
        }

        w.WriteEndObject();
    }

    private static void WritePosition(Utf8JsonWriter w, Position p)
    {
        w.WriteStartArray();
        w.WriteNumberValue(p.Lon);
        w.WriteNumberValue(p.Lat);
        if (p.Alt.HasValue)
            w.WriteNumberValue(p.Alt.Value);
        w.WriteEndArray();
    }

    private static void WritePositions(Utf8JsonWriter w, Position[] a)
    {
        w.WriteStartArray();
        foreach (var p in a)
            WritePosition(w, p);
        w.WriteEndArray();
    }

    private static void WritePositions2(Utf8JsonWriter w, Position[][] a)
    {
        w.WriteStartArray();
        foreach (var p in a)
            WritePositions(w, p);
        w.WriteEndArray();
    }

    private static void WritePositions3(Utf8JsonWriter w, Position[][][] a)
    {
        w.WriteStartArray();
        foreach (var p in a)
            WritePositions2(w, p);
        w.WriteEndArray();
    }

    private static void WriteBBox(Utf8JsonWriter w, BBox bbox)
    {
        w.WriteStartArray();
        foreach (var v in bbox.Values)
            w.WriteNumberValue(v);
        w.WriteEndArray();
    }
}
