using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US2 — pontos derivados (geodésicos), valores de referência do @turf real.
public class MeasureDerivedTests
{
    [Test]
    public async Task Midpoint_MatchesTurf()
    {
        var m = G.Midpoint(G.Point(0, 0), G.Point(10, 10)).Coordinates;
        await Assert.That(m.Lon).IsEqualTo(4.96163123).Within(1e-6);
        await Assert.That(m.Lat).IsEqualTo(5.0190007).Within(1e-6);
    }

    [Test]
    public async Task Destination_MatchesTurf()
    {
        var d = G.Destination(new Pos(0, 0), Units.Length.FromKilometers(100), Units.Angle.FromDegrees(45)).Coordinates;
        await Assert.That(d.Lon).IsEqualTo(0.63594164).Within(1e-6);
        await Assert.That(d.Lat).IsEqualTo(0.63590247).Within(1e-6);
    }

    [Test]
    public async Task Along_MatchesTurf()
    {
        var line = new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 1), new Pos(1, 1) });
        var a = G.Along(line, Units.Length.FromKilometers(100)).Coordinates;
        await Assert.That(a.Lon).IsEqualTo(0.0).Within(1e-6);
        await Assert.That(a.Lat).IsEqualTo(0.89932036).Within(1e-6);
    }

    [Test]
    public async Task Center_And_CenterOfMass_MatchTurf()
    {
        var square = new GeoJson.Polygon(
            new[]
            {
                new[] { new Pos(0, 0), new Pos(0, 10), new Pos(10, 10), new Pos(10, 0), new Pos(0, 0) },
            }
        );
        var center = G.Center(square).Coordinates;
        await Assert.That(center.Lon).IsEqualTo(5.0).Within(1e-9);
        await Assert.That(center.Lat).IsEqualTo(5.0).Within(1e-9);

        var com = G.CenterOfMass(square).Coordinates;
        await Assert.That(com.Lon).IsEqualTo(5.0).Within(1e-9);
        await Assert.That(com.Lat).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task RhumbDestination_MatchesTurf()
    {
        var d = G.RhumbDestination(new Pos(0, 0), Units.Length.FromKilometers(100), Units.Angle.FromDegrees(45)).Coordinates;
        await Assert.That(d.Lon).IsEqualTo(0.63592858).Within(1e-6);
        await Assert.That(d.Lat).IsEqualTo(0.63591553).Within(1e-6);
    }
}
