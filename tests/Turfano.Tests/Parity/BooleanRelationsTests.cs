using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US2 — relações de geometria, valores do @turf real (reference/_rel.mjs).
public class BooleanRelationsTests
{
    private static GeoJson.Polygon Poly(params (double lon, double lat)[] pts) =>
        new(new[] { pts.Select(p => new Pos(p.lon, p.lat)).ToArray() });

    // A=[0..4]², B overlaps A, C disjoint, small inside A, D shares edge x=4.
    private static GeoJson.Polygon A() => Poly((0, 0), (0, 4), (4, 4), (4, 0), (0, 0));
    private static GeoJson.Polygon B() => Poly((2, 2), (2, 6), (6, 6), (6, 2), (2, 2));
    private static GeoJson.Polygon C() => Poly((10, 10), (10, 12), (12, 12), (12, 10), (10, 10));
    private static GeoJson.Polygon Small() => Poly((1, 1), (1, 2), (2, 2), (2, 1), (1, 1));
    private static GeoJson.Polygon D() => Poly((4, 0), (4, 4), (8, 4), (8, 0), (4, 0));

    [Test]
    public async Task Intersects_And_Disjoint_MatchTurf()
    {
        await Assert.That(G.BooleanIntersects(A(), B())).IsTrue();
        await Assert.That(G.BooleanIntersects(A(), C())).IsFalse();
        await Assert.That(G.BooleanDisjoint(A(), C())).IsTrue();
        await Assert.That(G.BooleanDisjoint(A(), B())).IsFalse();
    }

    [Test]
    public async Task Contains_And_Within_MatchTurf()
    {
        await Assert.That(G.BooleanContains(A(), Small())).IsTrue();
        await Assert.That(G.BooleanContains(A(), B())).IsFalse();
        await Assert.That(G.BooleanWithin(Small(), A())).IsTrue();
        await Assert.That(G.BooleanWithin(B(), A())).IsFalse();
        // ponto estritamente dentro vs na borda
        await Assert.That(G.BooleanContains(A(), G.Point(2, 2))).IsTrue();
        await Assert.That(G.BooleanContains(A(), G.Point(0, 2))).IsFalse(); // borda
    }

    [Test]
    public async Task Equal_MatchTurf()
    {
        await Assert.That(G.BooleanEqual(A(), Poly((0, 0), (0, 4), (4, 4), (4, 0), (0, 0)))).IsTrue();
        await Assert.That(G.BooleanEqual(A(), B())).IsFalse();
    }

    [Test]
    public async Task Overlap_MatchTurf()
    {
        await Assert.That(G.BooleanOverlap(A(), B())).IsTrue(); // sobreposição parcial
        await Assert.That(G.BooleanOverlap(A(), Small())).IsFalse(); // contido, não sobrepõe
        await Assert.That(G.BooleanOverlap(A(), D())).IsTrue(); // tocam-se no canto (4,4)
    }

    [Test]
    public async Task Crosses_MatchTurf()
    {
        var crossLine = new GeoJson.LineString(new[] { new Pos(-1, 2), new Pos(5, 2) });
        await Assert.That(G.BooleanCrosses(crossLine, A())).IsTrue();
        await Assert.That(G.BooleanCrosses(crossLine, C())).IsFalse();
    }

    [Test]
    public async Task Touches_MatchTurf()
    {
        await Assert.That(G.BooleanTouches(A(), D())).IsTrue(); // compartilham a aresta x=4
        await Assert.That(G.BooleanTouches(A(), B())).IsFalse(); // sobrepõem área, não só tocam
    }

    [Test]
    public async Task Valid_MatchTurf()
    {
        await Assert.That(G.BooleanValid(A())).IsTrue();
        await Assert.That(G.BooleanValid(new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 1) }))).IsTrue();
        await Assert.That(G.BooleanValid(G.Point(1, 2))).IsTrue();
        // @turf: anel "laço" (bowtie) ainda é válido (não detecta auto-interseção)
        await Assert
            .That(G.BooleanValid(Poly((0, 0), (2, 2), (2, 0), (0, 2), (0, 0))))
            .IsTrue();
        // anel não fechado → inválido
        await Assert
            .That(G.BooleanValid(new GeoJson.Polygon(new[] { new[] { new Pos(0, 0), new Pos(0, 4), new Pos(4, 4), new Pos(4, 0) } })))
            .IsFalse();
    }
}
