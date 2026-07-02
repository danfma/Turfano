using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US4 (Onda F) — convex/concave/tesselate/voronoi; valores do @turf real (reference/_wavef.mjs).
public class HullTessellationTests
{
    private static GeoJson.FeatureCollection Cloud() =>
        new(
            new[]
            {
                new GeoJson.Feature(G.Point(0, 0)),
                new GeoJson.Feature(G.Point(4, 0)),
                new GeoJson.Feature(G.Point(4, 4)),
                new GeoJson.Feature(G.Point(0, 4)),
                new GeoJson.Feature(G.Point(2, 2)),
                new GeoJson.Feature(G.Point(1, 3)),
                new GeoJson.Feature(G.Point(5, 2)),
            }
        );

    [Test]
    public async Task Convex_MatchesTurf()
    {
        // turf.convex: [[4,0],[0,0],[0,4],[4,4],[5,2],[4,0]]
        var hull = G.Convex(Cloud())!;
        await Assert.That(hull.Coordinates[0]).IsEquivalentTo(
            new[] { new Pos(4, 0), new Pos(0, 0), new Pos(0, 4), new Pos(4, 4), new Pos(5, 2), new Pos(4, 0) }
        );
    }

    [Test]
    public async Task Concave_MatchesTurf_ByAreaAndVertices()
    {
        // turf.concave (600km): anel [[4,4],[5,2],[4,0],[0,0],[0,4],[4,4]] — validação por
        // área + conjunto de vértices (decisão R3: union nativo vs merge topojson)
        var concave = (GeoJson.Polygon)G.Concave(Cloud(), Units.Length.FromKilometers(600))!;
        var expectedRing = new[] { new Pos(4, 4), new Pos(5, 2), new Pos(4, 0), new Pos(0, 0), new Pos(0, 4), new Pos(4, 4) };
        var expectedArea = G.Area(new GeoJson.Polygon(new[] { expectedRing })).SquareMeters;
        await Assert.That(G.Area(concave).SquareMeters).IsEqualTo(expectedArea).Within(1);

        var vertices = concave.Coordinates[0].Take(concave.Coordinates[0].Length - 1).ToHashSet();
        await Assert.That(vertices.SetEquals(expectedRing.Take(5))).IsTrue();

        // maxEdge pequeno demais → sem solução (= @turf null)
        await Assert.That(G.Concave(Cloud(), Units.Length.FromKilometers(1))).IsNull();
    }

    [Test]
    public async Task Tesselate_MatchesTurf()
    {
        // quadrado com furo → 8 triângulos; 3 primeiros do earcut pinados
        var holed = new GeoJson.Polygon(
            new[]
            {
                new[] { new Pos(0, 0), new Pos(4, 0), new Pos(4, 4), new Pos(0, 4), new Pos(0, 0) },
                new[] { new Pos(1, 1), new Pos(2, 1), new Pos(2, 2), new Pos(1, 2), new Pos(1, 1) },
            }
        );
        var triangles = G.Tesselate(holed);
        await Assert.That(triangles.Features.Length).IsEqualTo(8);

        Pos[][] expected =
        {
            new[] { new Pos(0, 0), new Pos(1, 1), new Pos(1, 2), new Pos(0, 0) },
            new[] { new Pos(2, 1), new Pos(1, 1), new Pos(0, 0), new Pos(2, 1) },
            new[] { new Pos(0, 4), new Pos(0, 0), new Pos(1, 2), new Pos(0, 4) },
        };
        for (var i = 0; i < expected.Length; i++)
        {
            var ring = ((GeoJson.Polygon)triangles.Features[i].Geometry!).Coordinates[0];
            await Assert.That(ring).IsEquivalentTo(expected[i]);
        }
    }

    [Test]
    public async Task Voronoi_MatchesTurf()
    {
        var sites = new GeoJson.FeatureCollection(
            new[]
            {
                new GeoJson.Feature(G.Point(1, 1)),
                new GeoJson.Feature(G.Point(3, 1)),
                new GeoJson.Feature(G.Point(2, 3)),
                new GeoJson.Feature(G.Point(1, 3)),
                new GeoJson.Feature(G.Point(3.5, 3.5)),
            }
        );
        var voronoi = G.Voronoi(sites, new GeoJson.BBox(0, 0, 4, 4));
        await Assert.That(voronoi.Features.Length).IsEqualTo(5);

        // células exatas do @turf (d3-voronoi)
        Pos[][] expected =
        {
            new[] { new Pos(0, 2), new Pos(1.5, 2), new Pos(2, 1.75), new Pos(2, 0), new Pos(0, 0), new Pos(0, 2) },
            new[]
            {
                new Pos(2, 0), new Pos(2, 1.75), new Pos(3.0714285714285716, 2.2857142857142856),
                new Pos(4, 2.0999999999999996), new Pos(4, 0), new Pos(2, 0),
            },
            new[]
            {
                new Pos(1.5, 2), new Pos(1.5, 4), new Pos(2.5, 4),
                new Pos(3.0714285714285716, 2.2857142857142856), new Pos(2, 1.75), new Pos(1.5, 2),
            },
            new[] { new Pos(1.5, 4), new Pos(1.5, 2), new Pos(0, 2), new Pos(0, 4), new Pos(1.5, 4) },
            new[]
            {
                new Pos(4, 2.0999999999999996), new Pos(3.0714285714285716, 2.2857142857142856),
                new Pos(2.5, 4), new Pos(4, 4), new Pos(4, 2.0999999999999996),
            },
        };
        for (var i = 0; i < 5; i++)
        {
            var ring = ((GeoJson.Polygon)voronoi.Features[i].Geometry!).Coordinates[0];
            await Assert.That(ring.Length).IsEqualTo(expected[i].Length);
            for (var k = 0; k < ring.Length; k++)
            {
                await Assert.That(ring[k].Lon).IsEqualTo(expected[i][k].Lon).Within(1e-9);
                await Assert.That(ring[k].Lat).IsEqualTo(expected[i][k].Lat).Within(1e-9);
            }
        }
    }
}
