using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US2 (Onda G) — projection/mask/ellipse/sector/lineArc; valores do @turf real
// (reference/_waveg.mjs).
public class ShapeProjectionTests
{
    [Test]
    public async Task Projection_MatchesTurf_AndRoundTrips()
    {
        // turf.toMercator([-71, 41]) = [-7903683.846322424, 5012341.663847514]
        var mercator = (GeoJson.Point)G.ToMercator(G.Point(-71, 41));
        await Assert.That(mercator.Coordinates.Lon).IsEqualTo(-7903683.846322424).Within(1e-6);
        await Assert.That(mercator.Coordinates.Lat).IsEqualTo(5012341.663847514).Within(1e-6);

        var back = (GeoJson.Point)G.ToWgs84(mercator);
        await Assert.That(back.Coordinates.Lon).IsEqualTo(-71).Within(1e-9);
        await Assert.That(back.Coordinates.Lat).IsEqualTo(41).Within(1e-9);

        // lon > 180 ajustada + clamp no polo: [200, 89] → [-17811118.526923772, 20037508.342789244]
        var clamped = (GeoJson.Point)G.ToMercator(G.Point(200, 89));
        await Assert.That(clamped.Coordinates.Lon).IsEqualTo(-17811118.526923772).Within(1e-6);
        await Assert.That(clamped.Coordinates.Lat).IsEqualTo(20037508.342789244).Within(1e-6);
    }

    [Test]
    public async Task Mask_MatchesTurf()
    {
        var square = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(4, 0), new Pos(4, 4), new Pos(0, 4), new Pos(0, 0) } }
        );
        var mask = G.Mask(square);

        // mundo + 1 furo; anel do mundo começa em [180, 90]; área do @turf
        await Assert.That(mask.Coordinates.Length).IsEqualTo(2);
        await Assert.That(mask.Coordinates[0][0]).IsEqualTo(new Pos(180, 90));
        await Assert.That(mask.Coordinates[1][0]).IsEqualTo(new Pos(0, 0));
        await Assert.That(G.Area(mask).SquareMeters).IsEqualTo(509868212099350.4).Within(1e6);
    }

    [Test]
    public async Task Ellipse_MatchesTurf()
    {
        // turf.ellipse([0,0], 3, 1, {steps: 8}): 9 vértices; 3 primeiros pinados
        var ellipse = G.Ellipse(G.Point(0, 0), Units.Length.FromKilometers(3), Units.Length.FromKilometers(1), steps: 8);
        var ring = ellipse.Coordinates[0];
        await Assert.That(ring.Length).IsEqualTo(9);
        await Assert.That(ring[0].Lon).IsEqualTo(-0.026979610911736143).Within(1e-12);
        await Assert.That(ring[1].Lon).IsEqualTo(-0.015233629627956036).Within(1e-12);
        await Assert.That(ring[1].Lat).IsEqualTo(0.00742245784554217).Within(1e-12);
        await Assert.That(ring[2].Lat).IsEqualTo(0.00899320363724538).Within(1e-12);

        // com angle: 30°
        var rotated = G.Ellipse(
            G.Point(0, 0),
            Units.Length.FromKilometers(3),
            Units.Length.FromKilometers(1),
            Units.Angle.FromDegrees(30),
            steps: 8
        );
        await Assert.That(rotated.Coordinates[0][0].Lon).IsEqualTo(-0.02336502886551228).Within(1e-12);
        await Assert.That(rotated.Coordinates[0][0].Lat).IsEqualTo(0.013489805081979837).Within(1e-12);
    }

    [Test]
    public async Task LineArc_And_Sector_MatchTurf()
    {
        // turf.lineArc([0,0], 5, 20, 120, {steps:8}): 9 pontos
        var arc = G.LineArc(
            G.Point(0, 0),
            Units.Length.FromKilometers(5),
            Units.Angle.FromDegrees(20),
            Units.Angle.FromDegrees(120),
            steps: 8
        );
        await Assert.That(arc.Coordinates.Length).IsEqualTo(9);
        await Assert.That(arc.Coordinates[0].Lon).IsEqualTo(0.015379286772949731).Within(1e-12);
        await Assert.That(arc.Coordinates[0].Lat).IsEqualTo(0.042254234968327524).Within(1e-12);
        await Assert.That(arc.Coordinates[^1].Lon).IsEqualTo(0.038941716055050625).Within(1e-12);
        await Assert.That(arc.Coordinates[^1].Lat).IsEqualTo(-0.022483007362149327).Within(1e-12);

        // sector: centro + arco + centro = 11; wrap 300→30 também 11
        var sector = G.Sector(
            G.Point(0, 0),
            Units.Length.FromKilometers(5),
            Units.Angle.FromDegrees(20),
            Units.Angle.FromDegrees(120),
            steps: 8
        );
        await Assert.That(sector.Coordinates[0].Length).IsEqualTo(11);
        await Assert.That(sector.Coordinates[0][1].Lon).IsEqualTo(0.015379286772949731).Within(1e-12);

        var wrapped = G.Sector(
            G.Point(0, 0),
            Units.Length.FromKilometers(5),
            Units.Angle.FromDegrees(300),
            Units.Angle.FromDegrees(30),
            steps: 8
        );
        await Assert.That(wrapped.Coordinates[0].Length).IsEqualTo(11);
    }
}
