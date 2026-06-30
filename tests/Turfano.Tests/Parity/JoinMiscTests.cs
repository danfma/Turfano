using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US2 — joins e utilitários de linha, valores do @turf real (reference/_features.mjs).
public class JoinMiscTests
{
    private static GeoJson.FeatureCollection FC(params GeoJson.Point[] pts) =>
        new(pts.Select(p => new GeoJson.Feature(p)).ToArray());

    [Test]
    public async Task PointsWithinPolygon_MatchesTurf()
    {
        var poly = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(0, 10), new Pos(10, 10), new Pos(10, 0), new Pos(0, 0) } }
        );
        var within = G.PointsWithinPolygon(FC(G.Point(5, 5), G.Point(15, 5), G.Point(1, 1)), poly);
        await Assert.That(within.Features.Length).IsEqualTo(2); // [5,5] e [1,1]; [15,5] fora
        await Assert.That(((GeoJson.Point)within.Features[0].Geometry!).Coordinates).IsEqualTo(new Pos(5, 5));
    }

    [Test]
    public async Task NearestPoint_MatchesTurf()
    {
        var nearest = G.NearestPoint(G.Point(0, 0), FC(G.Point(1, 1), G.Point(5, 5), G.Point(0.5, 0.5)));
        await Assert.That(((GeoJson.Point)nearest.Geometry!).Coordinates).IsEqualTo(new Pos(0.5, 0.5));
        await Assert.That(nearest.Properties!["featureIndex"]!.GetValue<int>()).IsEqualTo(2);
    }

    [Test]
    public async Task LineSlice_And_LineSliceAlong_MatchTurf()
    {
        var line = new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(10, 0) });
        var sliced = G.LineSlice(G.Point(2, 0), G.Point(8, 0), line);
        await Assert.That(sliced.Coordinates[0].Lon).IsEqualTo(2.0).Within(1e-6);
        await Assert.That(sliced.Coordinates[^1].Lon).IsEqualTo(8.0).Within(1e-6);

        var along = G.LineSliceAlong(
            new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 10) }),
            Units.Length.FromKilometers(1),
            Units.Length.FromKilometers(5)
        );
        await Assert.That(along.Coordinates[0].Lat).IsEqualTo(0.00899).Within(1e-4);
        await Assert.That(along.Coordinates[^1].Lat).IsEqualTo(0.04497).Within(1e-4);
    }

    [Test]
    public async Task Kinks_And_LineChunk_MatchTurf()
    {
        var selfIntersecting = new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(4, 4), new Pos(0, 4), new Pos(4, 0) });
        var kinks = G.Kinks(selfIntersecting);
        await Assert.That(kinks.Features.Length).IsEqualTo(1);
        await Assert.That(((GeoJson.Point)kinks.Features[0].Geometry!).Coordinates).IsEqualTo(new Pos(2, 2));

        var chunks = G.LineChunk(
            new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 10) }),
            Units.Length.FromKilometers(500)
        );
        await Assert.That(chunks.Features.Length).IsEqualTo(3);
    }
}
