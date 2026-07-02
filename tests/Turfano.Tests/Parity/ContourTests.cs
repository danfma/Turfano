using System.Text.Json.Nodes;
using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US3 (Onda F) — isolines/isobands; estrutura completa do @turf real (reference/_wavef.mjs).
public class ContourTests
{
    // grade 5x5 em [0,4]² com pico 10 no centro: z = 10 − (|x−2| + |y−2|)·2
    private static GeoJson.FeatureCollection Grid()
    {
        var features = new List<GeoJson.Feature>();
        for (var y = 0; y <= 4; y++)
        {
            for (var x = 0; x <= 4; x++)
            {
                var z = 10 - (Math.Abs(x - 2) + Math.Abs(y - 2)) * 2;
                features.Add(new GeoJson.Feature(new GeoJson.Point(new Pos(x, y)), new JsonObject { ["elevation"] = z }));
            }
        }
        return new GeoJson.FeatureCollection(features.ToArray());
    }

    [Test]
    public async Task Isolines_MatchesTurf()
    {
        var iso = G.Isolines(Grid(), new double[] { 5, 9 });
        await Assert.That(iso.Features.Length).IsEqualTo(2);

        // break 5: 4 linhas abertas (cantos); primeira = [[0,1.5],[0.5,1],[1,0.5],[1.5,0]]
        var lines5 = (GeoJson.MultiLineString)iso.Features[0].Geometry!;
        await Assert.That(iso.Features[0].Properties!["elevation"]!.GetValue<double>()).IsEqualTo(5);
        await Assert.That(lines5.Coordinates.Length).IsEqualTo(4);
        await Assert.That(lines5.Coordinates[0]).IsEquivalentTo(
            new[] { new Pos(0, 1.5), new Pos(0.5, 1), new Pos(1, 0.5), new Pos(1.5, 0) }
        );

        // break 9: anel fechado ao redor do pico
        var lines9 = (GeoJson.MultiLineString)iso.Features[1].Geometry!;
        await Assert.That(lines9.Coordinates.Length).IsEqualTo(1);
        await Assert.That(lines9.Coordinates[0]).IsEquivalentTo(
            new[] { new Pos(2, 2.5), new Pos(1.5, 2), new Pos(2, 1.5), new Pos(2.5, 2), new Pos(2, 2.5) }
        );
    }

    [Test]
    public async Task Isobands_MatchesTurf_WithHole()
    {
        var bands = G.Isobands(Grid(), new double[] { 0, 5, 9 });
        await Assert.That(bands.Features.Length).IsEqualTo(2);

        // banda 0-5: 4 polígonos de canto; 1º = triângulo do canto (0,0)
        await Assert.That(bands.Features[0].Properties!["elevation"]!.GetValue<string>()).IsEqualTo("0-5");
        var band05 = (GeoJson.MultiPolygon)bands.Features[0].Geometry!;
        await Assert.That(band05.Coordinates.Length).IsEqualTo(4);
        await Assert.That(band05.Coordinates[0][0]).IsEquivalentTo(
            new[] { new Pos(1.5, 0), new Pos(1, 0.5), new Pos(0.5, 1), new Pos(0, 1.5), new Pos(0, 0), new Pos(1.5, 0) }
        );

        // banda 5-9: 1 polígono COM FURO (o pico >9); furo = anel do 9 invertido
        await Assert.That(bands.Features[1].Properties!["elevation"]!.GetValue<string>()).IsEqualTo("5-9");
        var band59 = (GeoJson.MultiPolygon)bands.Features[1].Geometry!;
        await Assert.That(band59.Coordinates.Length).IsEqualTo(1);
        await Assert.That(band59.Coordinates[0].Length).IsEqualTo(2); // exterior + furo
        await Assert.That(band59.Coordinates[0][0].Length).IsEqualTo(17);
        await Assert.That(band59.Coordinates[0][1]).IsEquivalentTo(
            new[] { new Pos(2.5, 2), new Pos(2, 1.5), new Pos(1.5, 2), new Pos(2, 2.5), new Pos(2.5, 2) }
        );
    }
}
