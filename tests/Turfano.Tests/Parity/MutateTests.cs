using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US1 — mutação de coordenadas, valores do @turf real (reference/_transform.mjs).
public class MutateTests
{
    [Test]
    public async Task Flip_And_Round_And_Truncate_MatchTurf()
    {
        var flipped = (GeoJson.Point)G.Flip(G.Point(1, 2));
        await Assert.That(flipped.Coordinates).IsEqualTo(new Pos(2, 1));

        await Assert.That(G.Round(3.14159, 2)).IsEqualTo(3.14).Within(1e-12);

        var truncated = (GeoJson.Point)G.Truncate(
            new GeoJson.Point(new Pos(70.123456789, 40.123456789, 1.5)),
            precision: 6,
            coordinates: 2
        );
        await Assert.That(truncated.Coordinates.Lon).IsEqualTo(70.123457).Within(1e-9);
        await Assert.That(truncated.Coordinates.Lat).IsEqualTo(40.123457).Within(1e-9);
        await Assert.That(truncated.Coordinates.Alt).IsNull(); // coordinates:2 dropa a altitude
    }

    [Test]
    public async Task CleanCoords_RemovesDuplicatesAndCollinear()
    {
        var dups = (GeoJson.LineString)G.CleanCoords(
            new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 0), new Pos(1, 1), new Pos(2, 2), new Pos(2, 2) })
        );
        await Assert.That(dups.Coordinates.Length).IsEqualTo(2);
        await Assert.That(dups.Coordinates[0]).IsEqualTo(new Pos(0, 0));
        await Assert.That(dups.Coordinates[1]).IsEqualTo(new Pos(2, 2));

        var colinear = (GeoJson.LineString)G.CleanCoords(
            new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 1), new Pos(2, 2), new Pos(2, 0) })
        );
        await Assert.That(colinear.Coordinates.Length).IsEqualTo(3);
        await Assert.That(colinear.Coordinates[1]).IsEqualTo(new Pos(2, 2));
    }

    [Test]
    public async Task Rewind_OrientsExteriorCounterClockwise()
    {
        // exterior horário → @turf rewind inverte para anti-horário
        var cw = new GeoJson.Polygon(new[] { new[] { new Pos(0, 0), new Pos(1, 1), new Pos(1, 0), new Pos(0, 0) } });
        var rewound = (GeoJson.Polygon)G.Rewind(cw);
        await Assert.That(rewound.Coordinates[0][1]).IsEqualTo(new Pos(1, 0));
        await Assert.That(rewound.Coordinates[0][2]).IsEqualTo(new Pos(1, 1));
    }
}
