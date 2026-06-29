using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US1 — predicados de ponto/orientação, valores do @turf real (reference/_boolean.mjs).
public class BooleanPointTests
{
    private static GeoJson.Polygon Square() =>
        new(new[] { new[] { new Pos(0, 0), new Pos(0, 10), new Pos(10, 10), new Pos(10, 0), new Pos(0, 0) } });

    [Test]
    public async Task PointInPolygon_And_Boundary_MatchTurf()
    {
        var sq = Square();
        await Assert.That(G.BooleanPointInPolygon(G.Point(5, 5), sq)).IsTrue();
        await Assert.That(G.BooleanPointInPolygon(G.Point(15, 5), sq)).IsFalse();
        // SC-002: ponto na borda
        await Assert.That(G.BooleanPointInPolygon(G.Point(0, 5), sq, ignoreBoundary: false)).IsTrue();
        await Assert.That(G.BooleanPointInPolygon(G.Point(0, 5), sq, ignoreBoundary: true)).IsFalse();
    }

    [Test]
    public async Task PointOnLine_MatchTurf()
    {
        var line = new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 10) });
        await Assert.That(G.BooleanPointOnLine(G.Point(0, 5), line)).IsTrue();
        await Assert.That(G.BooleanPointOnLine(G.Point(1, 5), line)).IsFalse();
        await Assert.That(G.BooleanPointOnLine(G.Point(0, 0), line)).IsTrue();
        await Assert.That(G.BooleanPointOnLine(G.Point(0, 0), line, ignoreEndVertices: true)).IsFalse();
    }

    [Test]
    public async Task Clockwise_Parallel_Concave_MatchTurf()
    {
        await Assert
            .That(G.BooleanClockwise(new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 1), new Pos(1, 0), new Pos(0, 0) })))
            .IsTrue();
        await Assert
            .That(G.BooleanClockwise(new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1), new Pos(0, 0) })))
            .IsFalse();

        await Assert
            .That(G.BooleanParallel(
                new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 1) }),
                new GeoJson.LineString(new[] { new Pos(1, 0), new Pos(1, 1) })))
            .IsTrue();
        await Assert
            .That(G.BooleanParallel(
                new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 1) }),
                new GeoJson.LineString(new[] { new Pos(1, 0), new Pos(2, 1) })))
            .IsFalse();

        var concave = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(5, 3), new Pos(10, 0), new Pos(10, 10), new Pos(0, 10), new Pos(0, 0) } }
        );
        await Assert.That(G.BooleanConcave(concave)).IsTrue();
        await Assert.That(G.BooleanConcave(Square())).IsFalse();
    }
}
