using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US1 (Onda G) — operações de linha; valores do @turf real (reference/_waveg.mjs).
public class LineOpsTests
{
    [Test]
    public async Task LineSegment_MatchesTurf()
    {
        // polígono-triângulo → 3 segmentos; 1º = [[0,0],[4,0]]
        var poly = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(4, 0), new Pos(4, 4), new Pos(0, 0) } }
        );
        var segments = G.LineSegment(poly);
        await Assert.That(segments.Features.Length).IsEqualTo(3);
        await Assert.That(((GeoJson.LineString)segments.Features[0].Geometry!).Coordinates).IsEquivalentTo(
            new[] { new Pos(0, 0), new Pos(4, 0) }
        );
    }

    [Test]
    public async Task LineIntersect_MatchesTurf()
    {
        // X simples → [2,2]
        var cross = G.LineIntersect(
            new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(4, 4) }),
            new GeoJson.LineString(new[] { new Pos(0, 4), new Pos(4, 0) })
        );
        await Assert.That(cross.Features.Length).IsEqualTo(1);
        await Assert.That(((GeoJson.Point)cross.Features[0].Geometry!).Coordinates).IsEqualTo(new Pos(2, 2));

        // zigzag × horizontal → [0.5,0], [1.5,0], [2.5,0] (ordem do @turf)
        var zig = G.LineIntersect(
            new GeoJson.LineString(new[] { new Pos(0, 1), new Pos(1, -1), new Pos(2, 1), new Pos(3, -1) }),
            new GeoJson.LineString(new[] { new Pos(-1, 0), new Pos(4, 0) })
        );
        var points = zig.Features.Select(f => ((GeoJson.Point)f.Geometry!).Coordinates).ToArray();
        await Assert.That(points).IsEquivalentTo(new[] { new Pos(0.5, 0), new Pos(1.5, 0), new Pos(2.5, 0) });
    }

    [Test]
    public async Task LineOverlap_MatchesTurf()
    {
        var baseLine = new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(2, 0), new Pos(4, 0) });

        // segmento compartilhado → [[2,0],[4,0]]
        var shared = G.LineOverlap(baseLine, new GeoJson.LineString(new[] { new Pos(2, 0), new Pos(4, 0), new Pos(4, 2) }));
        await Assert.That(shared.Features.Length).IsEqualTo(1);
        await Assert.That(((GeoJson.LineString)shared.Features[0].Geometry!).Coordinates).IsEquivalentTo(
            new[] { new Pos(2, 0), new Pos(4, 0) }
        );

        // sub-segmento sobre uma aresta → [[1,0],[2,0]]
        var sub = G.LineOverlap(baseLine, new GeoJson.LineString(new[] { new Pos(1, 0), new Pos(2, 0) }));
        await Assert.That(sub.Features.Length).IsEqualTo(1);
        await Assert.That(((GeoJson.LineString)sub.Features[0].Geometry!).Coordinates).IsEquivalentTo(
            new[] { new Pos(1, 0), new Pos(2, 0) }
        );

        // caso do @turf que NÃO detecta (span por 2 arestas) → vazio
        var spanning = G.LineOverlap(baseLine, new GeoJson.LineString(new[] { new Pos(1, 0), new Pos(3, 0) }));
        await Assert.That(spanning.Features.Length).IsEqualTo(0);
    }

    [Test]
    public async Task LineSplit_MatchesTurf()
    {
        var split = G.LineSplit(
            new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(4, 0) })),
            new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(2, -1), new Pos(2, 1) }))
        );
        await Assert.That(split.Features.Length).IsEqualTo(2);
        await Assert.That(((GeoJson.LineString)split.Features[0].Geometry!).Coordinates).IsEquivalentTo(
            new[] { new Pos(0, 0), new Pos(2, 0) }
        );
        await Assert.That(((GeoJson.LineString)split.Features[1].Geometry!).Coordinates).IsEquivalentTo(
            new[] { new Pos(2, 0), new Pos(4, 0) }
        );
    }

    [Test]
    public async Task Angle_MatchesTurf()
    {
        await Assert.That(G.Angle(new Pos(5, 5), new Pos(5, 6), new Pos(3, 4))).IsEqualTo(44.98231977009905).Within(1e-9);
        await Assert.That(G.Angle(new Pos(5, 5), new Pos(5, 6), new Pos(3, 4), explementary: true))
            .IsEqualTo(315.017680229901).Within(1e-9);
        await Assert.That(G.Angle(new Pos(5, 5), new Pos(5, 6), new Pos(3, 4), mercator: true))
            .IsEqualTo(44.889301678998066).Within(1e-9);
    }

    [Test]
    public async Task NearestPointToLine_MatchesTurf()
    {
        var points = new GeoJson.FeatureCollection(
            new[]
            {
                new GeoJson.Feature(G.Point(0, 0)),
                new GeoJson.Feature(G.Point(0.5, 0.5)),
                new GeoJson.Feature(G.Point(1, 2)),
            }
        );
        var line = new GeoJson.LineString(new[] { new Pos(-1, 1), new Pos(2, 1) });
        var nearest = G.NearestPointToLine(points, line);
        await Assert.That(((GeoJson.Point)nearest.Geometry!).Coordinates).IsEqualTo(new Pos(0.5, 0.5));
        await Assert.That(nearest.Properties!["dist"]!.GetValue<double>()).IsEqualTo(55.635649211082075).Within(1e-6);
    }

    [Test]
    public async Task ShortestPath_MatchesTurf()
    {
        // desvia do quadrado [1,3]²; resolution 100 km → 7 pontos, pinados nas pontas
        var path = G.ShortestPath(
            G.Point(0, 0),
            G.Point(4, 4),
            new GeoJson.FeatureCollection(
                new[]
                {
                    new GeoJson.Feature(
                        new GeoJson.Polygon(
                            new[] { new[] { new Pos(1, 1), new Pos(3, 1), new Pos(3, 3), new Pos(1, 3), new Pos(1, 1) } }
                        )
                    ),
                }
            ),
            Units.Length.FromKilometers(100)
        );
        var coords = ((GeoJson.LineString)path.Geometry!).Coordinates;
        await Assert.That(coords.Length).IsEqualTo(7);
        await Assert.That(coords[0]).IsEqualTo(new Pos(0, 0));
        await Assert.That(coords[1].Lon).IsEqualTo(0.6510009527260093).Within(1e-12);
        await Assert.That(coords[1].Lat).IsEqualTo(0.6510194544131922).Within(1e-12);
        await Assert.That(coords[^1]).IsEqualTo(new Pos(4, 4));
    }

    [Test]
    public async Task UnkinkPolygon_MatchesTurf()
    {
        // gravata borboleta → 2 triângulos exatos do @turf
        var bow = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(2, 2), new Pos(0, 2), new Pos(2, 0), new Pos(0, 0) } }
        );
        var unkinked = G.UnkinkPolygon(bow);
        await Assert.That(unkinked.Features.Length).IsEqualTo(2);
        await Assert.That(((GeoJson.Polygon)unkinked.Features[0].Geometry!).Coordinates[0]).IsEquivalentTo(
            new[] { new Pos(0, 0), new Pos(1, 1), new Pos(2, 0), new Pos(0, 0) }
        );
        await Assert.That(((GeoJson.Polygon)unkinked.Features[1].Geometry!).Coordinates[0]).IsEquivalentTo(
            new[] { new Pos(1, 1), new Pos(2, 2), new Pos(0, 2), new Pos(1, 1) }
        );
    }
}
