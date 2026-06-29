using System.Text.Json;
using Turfano.GeoJson;

namespace Turfano.Tests;

// Prova que os nomes GeoJSON (RFC 7946) e o discriminador ficam FIXOS mesmo quando o
// consumidor serializa com uma PropertyNamingPolicy própria — o cenário "salvar uma
// geometria dentro de um objeto do usuário" sem quebrar o GeoJSON.
public class GeoJsonNamingTests
{
    [Test]
    public async Task Naming_IsPinned_RegardlessOfConsumerPolicy()
    {
        var hostile = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper,
            TypeInfoResolver = GeoJsonSerializerContext.Default,
        };

        var polygon = new Turfano.GeoJson.Polygon(
            new[]
            {
                new[]
                {
                    new Turfano.GeoJson.Position(0, 0),
                    new Turfano.GeoJson.Position(1, 0),
                    new Turfano.GeoJson.Position(1, 1),
                    new Turfano.GeoJson.Position(0, 0),
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
