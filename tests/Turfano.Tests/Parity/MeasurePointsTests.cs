using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US2 — pontos derivados; prova do conserto do centroid (SC-002): [1,1], não [0.833,0.833].
public class MeasurePointsTests
{
    [Test]
    public async Task Centroid_ExcludesWrapCoord_MatchesTurf()
    {
        var irr = new GeoJson.Polygon(
            new[]
            {
                new[]
                {
                    new Pos(0, 0),
                    new Pos(0, 2),
                    new Pos(1, 1),
                    new Pos(2, 2),
                    new Pos(2, 0),
                    new Pos(0, 0),
                },
            }
        );
        await Assert.That(G.Centroid(irr).Coordinates).IsEqualTo(new Pos(1, 1));

        var sq = new GeoJson.Polygon(
            new[]
            {
                new[]
                {
                    new Pos(0, 0),
                    new Pos(0, 10),
                    new Pos(10, 10),
                    new Pos(10, 0),
                    new Pos(0, 0),
                },
            }
        );
        await Assert.That(G.Centroid(sq).Coordinates).IsEqualTo(new Pos(5, 5));

        var line = new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(10, 10) });
        await Assert.That(G.Centroid(line).Coordinates).IsEqualTo(new Pos(5, 5));
    }
}
