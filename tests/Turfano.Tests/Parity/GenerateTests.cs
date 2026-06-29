using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US3 — geração/suavização, valores do @turf real (reference/_gen.mjs).
public class GenerateTests
{
    [Test]
    public async Task Circle_MatchesTurf()
    {
        var circle = G.Circle(G.Point(0, 0), Units.Length.FromKilometers(100), steps: 4);
        var ring = circle.Coordinates[0];
        await Assert.That(ring.Length).IsEqualTo(5); // 4 passos + fechamento
        // N, W, S, E (rumos 0, -90, -180, -270)
        await Assert.That(ring[0].Lon).IsEqualTo(0.0).Within(1e-5);
        await Assert.That(ring[0].Lat).IsEqualTo(0.89932).Within(1e-5);
        await Assert.That(ring[1].Lon).IsEqualTo(-0.89932).Within(1e-5);
        await Assert.That(ring[1].Lat).IsEqualTo(0.0).Within(1e-5);
        await Assert.That(ring[4]).IsEqualTo(ring[0]); // fechado
    }

    [Test]
    public async Task Simplify_DouglasPeucker_MatchesTurf()
    {
        var line = new GeoJson.LineString(
            new[] { new Pos(0, 0), new Pos(1, 0.1), new Pos(2, -0.1), new Pos(3, 0.05), new Pos(4, 0) }
        );
        var simplified = (GeoJson.LineString)G.Simplify(line, tolerance: 0.1);
        await Assert.That(simplified.Coordinates.Length).IsEqualTo(2);
        await Assert.That(simplified.Coordinates[0]).IsEqualTo(new Pos(0, 0));
        await Assert.That(simplified.Coordinates[1]).IsEqualTo(new Pos(4, 0));
    }

    [Test]
    public async Task PolygonSmooth_Chaikin_MatchesTurf()
    {
        var square = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(0, 4), new Pos(4, 4), new Pos(4, 0), new Pos(0, 0) } }
        );
        var smooth = G.PolygonSmooth(square, iterations: 1);
        var ring = smooth.Coordinates[0];
        await Assert.That(ring.Length).IsEqualTo(9); // 4 arestas × 2 + fechamento
        await Assert.That(ring[0]).IsEqualTo(new Pos(0, 1));
        await Assert.That(ring[1]).IsEqualTo(new Pos(0, 3));
        await Assert.That(ring[2]).IsEqualTo(new Pos(1, 4));
    }
}
