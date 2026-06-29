using G = Turfano.GeoJson.Geo;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US3 (rumo) — valores do @turf real (o doc antigo NTS dizia 9.71/97.994; o @turf real dá
// -170.294/97.129 — confiamos no @turf).
public class MeasureRhumbTests
{
    [Test]
    public async Task RhumbBearing_And_Distance_MatchTurf()
    {
        var a = new Pos(-75.343, 39.984);
        var b = new Pos(-75.534, 39.123);

        await Assert.That(G.RhumbBearing(a, b).Degrees).IsEqualTo(-170.29417536).Within(1e-6);
        await Assert.That(G.RhumbDistance(a, b).Kilometers).IsEqualTo(97.12923943).Within(1e-6);
    }
}
