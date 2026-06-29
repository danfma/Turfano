using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US3 — distâncias a geometrias (geodésico), valores do @turf real.
public class MeasureGeomTests
{
    [Test]
    public async Task NearestPointOnLine_And_PointToLineDistance_MatchTurf()
    {
        var line = new GeoJson.LineString(new[] { new Pos(-77, 39), new Pos(-77, 38), new Pos(-77, 37) });
        var pt = G.Point(-76, 38);

        var np = G.NearestPointOnLine(line, pt);
        await Assert.That(np.Point.Coordinates.Lon).IsEqualTo(-77.0).Within(1e-6);
        await Assert.That(np.Point.Coordinates.Lat).IsEqualTo(38.00423401).Within(1e-6);
        await Assert.That(np.Index).IsEqualTo(0);
        await Assert.That(np.Distance.Kilometers).IsEqualTo(87.62123267).Within(1e-6);

        await Assert.That(G.PointToLineDistance(pt, line).Kilometers).IsEqualTo(87.62123267).Within(1e-6);
    }

    [Test]
    public async Task PointToPolygonDistance_And_PointOnFeature_MatchTurf()
    {
        var square = new GeoJson.Polygon(
            new[]
            {
                new[] { new Pos(0, 0), new Pos(0, 10), new Pos(10, 10), new Pos(10, 0), new Pos(0, 0) },
            }
        );

        // ponto fora → distância positiva
        await Assert
            .That(G.PointToPolygonDistance(G.Point(15, 5), square).Kilometers)
            .IsEqualTo(553.85439081)
            .Within(1e-4);

        // ponto dentro → distância negativa (sinal)
        await Assert.That(G.PointToPolygonDistance(G.Point(5, 5), square).Kilometers).IsLessThan(0);

        // pointOnFeature do quadrado → centro [5,5] (dentro)
        var pof = G.PointOnFeature(square).Coordinates;
        await Assert.That(pof.Lon).IsEqualTo(5.0).Within(1e-9);
        await Assert.That(pof.Lat).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task GreatCircle_MatchesTurf()
    {
        var gc = G.GreatCircle(new Pos(0, 0), new Pos(10, 10), npoints: 5);
        await Assert.That(gc.Coordinates.Length).IsEqualTo(5);
        // ponto médio (i=2) = midpoint geodésico
        await Assert.That(gc.Coordinates[2].Lon).IsEqualTo(4.961631).Within(1e-5);
        await Assert.That(gc.Coordinates[2].Lat).IsEqualTo(5.019001).Within(1e-5);
        // extremos = start/end
        await Assert.That(gc.Coordinates[0].Lon).IsEqualTo(0.0).Within(1e-9);
        await Assert.That(gc.Coordinates[4].Lat).IsEqualTo(10.0).Within(1e-9);
    }

    [Test]
    public async Task PolygonTangents_MatchesTurf()
    {
        var square = new GeoJson.Polygon(
            new[]
            {
                new[] { new Pos(0, 0), new Pos(0, 10), new Pos(10, 10), new Pos(10, 0), new Pos(0, 0) },
            }
        );
        var t = G.PolygonTangents(new Pos(15, 5), square);
        // @turf: rtan=[10,10], ltan=[10,0]
        await Assert.That(t.RightTangent.Coordinates).IsEqualTo(new Pos(10, 10));
        await Assert.That(t.LeftTangent.Coordinates).IsEqualTo(new Pos(10, 0));
    }
}
