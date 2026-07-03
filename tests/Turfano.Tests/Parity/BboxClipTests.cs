using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US3 — bboxClip (Cohen-Sutherland portado), valores do @turf real.
public class BboxClipTests
{
    [Test]
    public async Task BboxClip_Line_MatchesTurf()
    {
        var line = new GeoJson.LineString(new[] { new Pos(-1, 2), new Pos(5, 2) });
        var clipped = (GeoJson.LineString)G.BboxClip(line, new GeoJson.BBox(0, 0, 4, 4));
        // @turf: [[0,2],[4,2]]
        await Assert.That(clipped.Coordinates.Length).IsEqualTo(2);
        await Assert.That(clipped.Coordinates[0]).IsEqualTo(new Pos(0, 2));
        await Assert.That(clipped.Coordinates[1]).IsEqualTo(new Pos(4, 2));
    }

    [Test]
    public async Task BboxClip_Polygon_MatchesTurf()
    {
        var poly = new GeoJson.Polygon(
            new[]
            {
                new[] { new Pos(1, 1), new Pos(1, 6), new Pos(6, 6), new Pos(6, 1), new Pos(1, 1) },
            }
        );
        var clipped = (GeoJson.Polygon)G.BboxClip(poly, new GeoJson.BBox(0, 0, 4, 4));
        // @turf: [[[1,1],[1,4],[4,4],[4,1],[1,1]]]
        var ring = clipped.Coordinates[0];
        await Assert.That(ring.Length).IsEqualTo(5);
        await Assert.That(ring[0]).IsEqualTo(new Pos(1, 1));
        await Assert.That(ring[1]).IsEqualTo(new Pos(1, 4));
        await Assert.That(ring[2]).IsEqualTo(new Pos(4, 4));
        await Assert.That(ring[3]).IsEqualTo(new Pos(4, 1));
    }
}
