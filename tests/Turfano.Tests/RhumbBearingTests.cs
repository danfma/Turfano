using NetTopologySuite.Geometries;

namespace Turfano.Tests;

// Valores de referência obtidos rodando o TurfJS real (@turf/rhumb-bearing) em reference/.
// Antes do fix de Angles.TwoPi (= π em vez de 2π), os casos com rumo > 180° (sudoeste,
// oeste, antimeridiano) divergiam; o XML-doc do código documentava o sentido inverso (9.71°).
public class RhumbBearingTests
{
    [Test]
    public async Task Anchor_SouthwestIsNegative_MatchesTurf()
    {
        // turf.rhumbBearing([-75.343,39.984],[-75.534,39.123]) = -170.294175
        var bearing = Turf.RhumbBearing(
            new Coordinate(-75.343, 39.984),
            new Coordinate(-75.534, 39.123)
        );

        await Assert.That(bearing.Degrees).IsEqualTo(-170.294175).Within(0.1);
    }

    [Test]
    public async Task ReverseDirection_IsPositive_MatchesTurf()
    {
        // Sentido inverso: turf.rhumbBearing([-75.534,39.123],[-75.343,39.984]) = 9.705825
        var bearing = Turf.RhumbBearing(
            new Coordinate(-75.534, 39.123),
            new Coordinate(-75.343, 39.984)
        );

        await Assert.That(bearing.Degrees).IsEqualTo(9.705825).Within(0.1);
    }

    [Test]
    public async Task CardinalDirections_MatchTurf()
    {
        // Leste = 90, Oeste = -90, Sul = 180, Norte = 0 (valores do TurfJS).
        await Assert
            .That(Turf.RhumbBearing(new Coordinate(0, 0), new Coordinate(1, 0)).Degrees)
            .IsEqualTo(90)
            .Within(0.001);
        await Assert
            .That(Turf.RhumbBearing(new Coordinate(0, 0), new Coordinate(-1, 0)).Degrees)
            .IsEqualTo(-90)
            .Within(0.001);
        await Assert
            .That(Turf.RhumbBearing(new Coordinate(0, 1), new Coordinate(0, -1)).Degrees)
            .IsEqualTo(180)
            .Within(0.001);
        await Assert
            .That(Turf.RhumbBearing(new Coordinate(0, -1), new Coordinate(0, 1)).Degrees)
            .IsEqualTo(0)
            .Within(0.001);
    }

    [Test]
    public async Task Antimeridian_TakesShortPathEast()
    {
        // turf.rhumbBearing([179,0],[-179,0]) = 90 (caminho curto a leste, +2° de longitude).
        var bearing = Turf.RhumbBearing(new Coordinate(179, 0), new Coordinate(-179, 0));

        await Assert.That(bearing.Degrees).IsEqualTo(90).Within(0.1);
    }

    [Test]
    public async Task SamePoint_DoesNotProduceNaN()
    {
        var p = new Coordinate(-75.343, 39.984);
        var bearing = Turf.RhumbBearing(p, p);

        await Assert.That(double.IsNaN(bearing.Degrees)).IsFalse();
    }
}
