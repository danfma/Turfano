using System.Text.Json;
using Turfano.GeoJson;

namespace Turfano.Tests;

// Round-trip (desserializar → reserializar) dos tipos GeoJSON próprios via o contexto
// source-generated + converter manual (Fase 3). As fixtures estão na ordem de saída do
// converter (type → coordinates/geometries/features → bbox), provando a fidelidade exata.
public class GeoJsonRoundTripTests
{
    [Test]
    [Arguments("""{"type":"Point","coordinates":[1,2]}""")]
    [Arguments("""{"type":"Point","coordinates":[1,2,3]}""")]
    [Arguments("""{"type":"MultiPoint","coordinates":[[1,2],[3,4]]}""")]
    [Arguments("""{"type":"LineString","coordinates":[[1,2],[3,4]]}""")]
    [Arguments("""{"type":"MultiLineString","coordinates":[[[1,2],[3,4]],[[5,6],[7,8]]]}""")]
    [Arguments("""{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]}""")]
    [Arguments("""{"type":"MultiPolygon","coordinates":[[[[0,0],[1,0],[1,1],[0,0]]]]}""")]
    [Arguments("""{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2]},{"type":"LineString","coordinates":[[3,4],[5,6]]}]}""")]
    [Arguments("""{"type":"Feature","geometry":{"type":"Point","coordinates":[1,2]},"properties":null}""")]
    [Arguments("""{"type":"Feature","id":7,"geometry":{"type":"Point","coordinates":[1,2]},"properties":{"name":"x"}}""")]
    [Arguments("""{"type":"Feature","id":"abc","geometry":null,"properties":null}""")]
    [Arguments("""{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Point","coordinates":[1,2,3]},"properties":null}]}""")]
    [Arguments("""{"type":"Point","coordinates":[1,2],"bbox":[1,2,1,2]}""")]
    public async Task RoundTrip_PreservesShape(string json)
    {
        var ti = GeoJsonSerializerContext.Default.GeoJsonObject;

        var obj = JsonSerializer.Deserialize(json, ti);
        var back = JsonSerializer.Serialize(obj, ti);

        await Assert.That(back).IsEqualTo(json);
    }
}
