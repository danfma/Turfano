using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US1 — overlay via NtsBridge; validado por ÁREA vs o @turf real (reference/_overlay.mjs).
public class OverlayTests
{
    // A=[0,4]², B=[2,6]² (sobrepõem)
    private static GeoJson.Polygon A() =>
        new(new[] { new[] { new Pos(0, 0), new Pos(0, 4), new Pos(4, 4), new Pos(4, 0), new Pos(0, 0) } });

    private static GeoJson.Polygon B() =>
        new(new[] { new[] { new Pos(2, 2), new Pos(2, 6), new Pos(6, 6), new Pos(6, 2), new Pos(2, 2) } });

    [Test]
    public async Task Union_Intersect_Difference_AreaMatchTurf()
    {
        var union = G.Union(A(), B());
        await Assert.That(G.Area(union!).SquareMeters).IsEqualTo(345589333637.49884).Within(1e4);

        var intersect = G.Intersect(A(), B());
        await Assert.That(G.Area(intersect!).SquareMeters).IsEqualTo(49387096396.63134).Within(1e4);

        var difference = G.Difference(A(), B());
        await Assert.That(G.Area(difference!).SquareMeters).IsEqualTo(148281777124.80353).Within(1e4);
    }

    [Test]
    public async Task Dissolve_AreaMatchesTurf()
    {
        var fc = new GeoJson.FeatureCollection(new[] { new GeoJson.Feature(A()), new GeoJson.Feature(B()) });
        var dissolved = G.Dissolve(fc);
        await Assert.That(dissolved.Type).IsEqualTo("Polygon"); // A∪B é um único polígono
        await Assert.That(G.Area(dissolved).SquareMeters).IsEqualTo(345589333637.49884).Within(1e4);
    }

    [Test]
    public async Task Intersect_Disjoint_IsNull()
    {
        var far = new GeoJson.Polygon(
            new[] { new[] { new Pos(10, 10), new Pos(10, 12), new Pos(12, 12), new Pos(12, 10), new Pos(10, 10) } }
        );
        await Assert.That(G.Intersect(A(), far)).IsNull();
    }
}
