using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US1 — medições escalares sobre os novos tipos, valores de referência do @turf real
// (reference/_measure.mjs).
public class MeasureScalarTests
{
    [Test]
    public async Task Area_MatchesTurf()
    {
        var poly = new GeoJson.Polygon(
            new[]
            {
                new[]
                {
                    new Pos(-5, 52),
                    new Pos(-4, 56),
                    new Pos(-2, 51),
                    new Pos(-7, 54),
                    new Pos(-5, 52),
                },
            }
        );
        await Assert.That(Turf.Area(poly).SquareMeters).IsEqualTo(32819945055.137505).Within(1e-2);
    }

    [Test]
    public async Task Distance_And_Bearing_MatchTurf()
    {
        await Assert
            .That(Turf.Distance(new Pos(0, 0), new Pos(1, 1)).Kilometers)
            .IsEqualTo(157.24959847)
            .Within(1e-6);
        await Assert
            .That(Turf.Bearing(new Pos(0, 0), new Pos(1, 1)).Degrees)
            .IsEqualTo(44.99563646)
            .Within(1e-6);
        await Assert
            .That(Turf.Bearing(new Pos(0, 0), new Pos(1, 1), final: true).Degrees)
            .IsEqualTo(45.00436354)
            .Within(1e-6);
    }

    [Test]
    public async Task Bbox_And_Envelope_AreCorrect()
    {
        var line = new GeoJson.LineString(new[] { new Pos(-74, 40), new Pos(-78, 42), new Pos(-82, 35) });

        var bbox = Turf.Bbox(line);
        await Assert.That(bbox.Values[0]).IsEqualTo(-82.0);
        await Assert.That(bbox.Values[1]).IsEqualTo(35.0);
        await Assert.That(bbox.Values[2]).IsEqualTo(-74.0);
        await Assert.That(bbox.Values[3]).IsEqualTo(42.0);

        // Envelope = BboxPolygon(Bbox): anel fechado de 5 pontos no canto SW -82,35.
        var env = Turf.Envelope(line);
        await Assert.That(env.Coordinates[0].Length).IsEqualTo(5);
        await Assert.That(env.Coordinates[0][0]).IsEqualTo(new Pos(-82, 35));
        await Assert.That(env.Coordinates[0][2]).IsEqualTo(new Pos(-74, 42));
    }
}
