using System.Text.Json;
using Turfano.GeoJson;
using Turfano.NetTopologySuite;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// Fase 3 (US4) + feature 009: helpers estilo Turf (Geo.*) e a bridge PÚBLICA
// novos-tipos ↔ NTS (NtsConvert, no satélite Turfano.NetTopologySuite).
public class GeoJsonFactoryAndBridgeTests
{
    [Test]
    public async Task Factory_BuildsExpectedTypes()
    {
        var p = Geo.Point(1, 2);
        await Assert.That(p.Coordinates).IsEqualTo(new Pos(1, 2));
        await Assert.That(Geo.GetType(p)).IsEqualTo("Point");
        await Assert.That(Geo.GetCoord(p)).IsEqualTo(new Pos(1, 2));

        var poly = Geo.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1), new Pos(0, 0) } }
        );
        await Assert.That(Geo.GetType(poly)).IsEqualTo("Polygon");
        await Assert.That(poly.Coordinates[0].Length).IsEqualTo(4);
    }

    [Test]
    public async Task NtsConvert_RoundTrip_PreservesCoordinates()
    {
        // Polígono com furo (shell + hole) — ida e volta pela bridge preserva a estrutura.
        var poly = Geo.Polygon(
            new[]
            {
                new[] { new Pos(0, 0), new Pos(4, 0), new Pos(4, 4), new Pos(0, 4), new Pos(0, 0) },
                new[] { new Pos(1, 1), new Pos(2, 1), new Pos(2, 2), new Pos(1, 1) },
            }
        );

        var nts = NtsConvert.ToNts(poly);
        var back = NtsConvert.FromNts(nts);

        // Compara via a serialização GeoJSON (estrutura completa).
        var ti = GeoJsonSerializerContext.Default.GeoJsonObject;
        var origJson = JsonSerializer.Serialize<GeoJsonObject>(poly, ti);
        var backJson = JsonSerializer.Serialize<GeoJsonObject>(back, ti);
        await Assert.That(backJson).IsEqualTo(origJson);
    }

    [Test]
    public async Task NtsConvert_Preserves3DPosition()
    {
        var p = Geo.Point(1, 2, 3);
        var nts = NtsConvert.ToNts(p);
        var back = (Turfano.GeoJson.Point)NtsConvert.FromNts(nts);
        await Assert.That(back.Coordinates).IsEqualTo(new Pos(1, 2, 3));
    }
}
