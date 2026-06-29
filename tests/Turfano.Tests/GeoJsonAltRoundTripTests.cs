using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Turfano.GeoJsonAlt;

namespace Turfano.Tests;

// Variante de COMPARAÇÃO (Ideia 1: polimorfismo embutido + converter na propriedade
// Features). Mesmos 13 round-trips da implementação manual + prova de imunidade de naming.
public class GeoJsonAltRoundTripTests
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
        var ti = GeoJsonAltContext.Default.GeoJsonObject;

        var obj = JsonSerializer.Deserialize(json, ti);
        var back = JsonSerializer.Serialize(obj, ti);

        await Assert.That(back).IsEqualTo(json);
    }

    [Test]
    public async Task Naming_IsPinned_RegardlessOfConsumerPolicy()
    {
        // Política HOSTIL (SnakeCaseUpper) + o resolver source-gen do Turfano. Os nomes do
        // RFC 7946 estão fixados por [JsonPropertyName], então NÃO podem ser transformados.
        var hostile = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper,
            TypeInfoResolver = GeoJsonAltContext.Default,
        };

        var polygon = new Turfano.GeoJsonAlt.Polygon(
            new[]
            {
                new[]
                {
                    new Turfano.GeoJsonAlt.Position(0, 0),
                    new Turfano.GeoJsonAlt.Position(1, 0),
                    new Turfano.GeoJsonAlt.Position(1, 1),
                    new Turfano.GeoJsonAlt.Position(0, 0),
                },
            }
        );

        var json = JsonSerializer.Serialize<GeoJsonObject>(polygon, hostile);

        await Assert.That(json).Contains("\"type\":\"Polygon\"");
        await Assert.That(json).Contains("\"coordinates\":");
        await Assert.That(json).DoesNotContain("COORDINATES");
        await Assert.That(json).DoesNotContain("TYPE");
    }
}
