using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US2 — transformação geométrica, valores do @turf real. transformScale é GEODÉSICO.
public class TransformTests
{
    [Test]
    public async Task TransformScale_IsGeodesic_MatchesTurf()
    {
        var sq = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(0, 2), new Pos(2, 2), new Pos(2, 0), new Pos(0, 0) } }
        );
        var scaled = (GeoJson.Polygon)G.TransformScale(sq, 2);
        var ring = scaled.Coordinates[0];

        // SC-002: escala geodésica a partir do centroide [1,1] — bate com o @turf (não colapsa).
        await Assert.That(ring[1].Lon).IsEqualTo(-1.0006097146816728).Within(1e-6);
        await Assert.That(ring[1].Lat).IsEqualTo(3.0).Within(1e-6);
        await Assert.That(ring[2].Lon).IsEqualTo(3.000609714681673).Within(1e-6);
        await Assert.That(ring[2].Lat).IsEqualTo(3.0).Within(1e-6);
    }

    [Test]
    public async Task TransformTranslate_MatchesTurf()
    {
        var moved = (GeoJson.Point)G.TransformTranslate(
            G.Point(0, 0),
            Units.Length.FromKilometers(100),
            Units.Angle.FromDegrees(35)
        );
        await Assert.That(moved.Coordinates.Lon).IsEqualTo(0.515843182340177).Within(1e-6);
        await Assert.That(moved.Coordinates.Lat).IsEqualTo(0.736680114415675).Within(1e-6);
    }

    [Test]
    public async Task TransformRotate_MatchesTurf()
    {
        var poly = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(0, 1), new Pos(1, 1), new Pos(1, 0), new Pos(0, 0) } }
        );
        var rotated = (GeoJson.Polygon)G.TransformRotate(poly, Units.Angle.FromDegrees(90), new Pos(0, 0));
        var ring = rotated.Coordinates[0];
        await Assert.That(ring[1].Lon).IsEqualTo(1.0).Within(1e-3);
        await Assert.That(ring[1].Lat).IsEqualTo(0.0).Within(1e-3);
        await Assert.That(ring[3].Lon).IsEqualTo(0.0).Within(1e-3);
        await Assert.That(ring[3].Lat).IsEqualTo(-1.0).Within(1e-3);
    }

    [Test]
    public async Task Clone_IsDeepCopy()
    {
        var line = new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 1) });
        var clone = (GeoJson.LineString)G.Clone(line);
        await Assert.That(clone.Coordinates[0]).IsEqualTo(new Pos(0, 0));
        await Assert.That(ReferenceEquals(clone.Coordinates, line.Coordinates)).IsFalse();
    }
}
