using System.Text.Json;
using Turfano.GeoJson;

// Smoke de AOT/trimming (SC-002): exercita a (de)serialização GeoJSON pelo contexto
// source-gen. Com IsAotCompatible=true, qualquer reflexão nesta rota viraria warning
// IL2xxx/IL3xxx no build. Se este projeto compila limpo, os tipos GeoJSON são AOT-safe.
var ti = GeoJsonSerializerContext.Default.GeoJsonObject;

const string json =
    """{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]},"properties":{"name":"x"}}]}""";

var obj = JsonSerializer.Deserialize(json, ti);
var back = JsonSerializer.Serialize(obj, ti);

Console.WriteLine(back == json ? "AOT_SMOKE_OK" : "AOT_SMOKE_MISMATCH");
return back == json ? 0 : 1;
