using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US1 — conversão de feature, valores do @turf real (reference/_features.mjs).
public class ConvertTests
{
    private static GeoJson.Polygon Triangle() =>
        new(new[] { new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1), new Pos(0, 0) } });

    [Test]
    public async Task Explode_AllVertices_MatchTurf()
    {
        var exploded = G.Explode(Triangle());
        await Assert.That(exploded.Features.Length).IsEqualTo(4); // inclui o vértice de fechamento
        await Assert.That(((GeoJson.Point)exploded.Features[0].Geometry!).Coordinates).IsEqualTo(new Pos(0, 0));
        await Assert.That(((GeoJson.Point)exploded.Features[2].Geometry!).Coordinates).IsEqualTo(new Pos(1, 1));
    }

    [Test]
    public async Task Flatten_And_Combine_MatchTurf()
    {
        var flat = G.Flatten(new GeoJson.MultiPoint(new[] { new Pos(0, 0), new Pos(1, 1) }));
        await Assert.That(flat.Features.Length).IsEqualTo(2);
        await Assert.That(flat.Features[0].Geometry).IsTypeOf<GeoJson.Point>();

        var combined = G.Combine(
            new GeoJson.FeatureCollection(new[] { new GeoJson.Feature(G.Point(0, 0)), new GeoJson.Feature(G.Point(1, 1)) })
        );
        await Assert.That(combined.Features.Length).IsEqualTo(1);
        var multi = (GeoJson.MultiPoint)combined.Features[0].Geometry!;
        await Assert.That(multi.Coordinates.Length).IsEqualTo(2);
    }

    [Test]
    public async Task PolygonToLine_And_LineToPolygon_MatchTurf()
    {
        var line = (GeoJson.LineString)G.PolygonToLine(Triangle());
        await Assert.That(line.Coordinates.Length).IsEqualTo(4);
        await Assert.That(line.Coordinates[0]).IsEqualTo(new Pos(0, 0));

        // polígono com furo → MultiLineString
        var withHole = new GeoJson.Polygon(
            new[]
            {
                new[] { new Pos(0, 0), new Pos(4, 0), new Pos(4, 4), new Pos(0, 4), new Pos(0, 0) },
                new[] { new Pos(1, 1), new Pos(2, 1), new Pos(2, 2), new Pos(1, 1) },
            }
        );
        await Assert.That(G.PolygonToLine(withHole)).IsTypeOf<GeoJson.MultiLineString>();

        var poly = (GeoJson.Polygon)G.LineToPolygon(
            new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1), new Pos(0, 0) })
        );
        await Assert.That(poly.Coordinates[0].Length).IsEqualTo(4);
    }

    [Test]
    public async Task Polygonize_MatchesTurf()
    {
        // 4 linhas formando um quadrado → 1 polígono (forma igual ao @turf; ordem pode diferir)
        var lines = new GeoJson.FeatureCollection(
            new[]
            {
                new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(4, 0) })),
                new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(4, 0), new Pos(4, 4) })),
                new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(4, 4), new Pos(0, 4) })),
                new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(0, 4), new Pos(0, 0) })),
            }
        );
        var result = G.Polygonize(lines);
        await Assert.That(result.Features.Length).IsEqualTo(1);
        var ring = ((GeoJson.Polygon)result.Features[0].Geometry!).Coordinates[0];
        await Assert.That(ring.Length).IsEqualTo(5); // 4 vértices + fechamento
        var vertices = ring.Take(4).ToHashSet();
        await Assert.That(vertices.Contains(new Pos(0, 0))).IsTrue();
        await Assert.That(vertices.Contains(new Pos(4, 4))).IsTrue();
    }
}
