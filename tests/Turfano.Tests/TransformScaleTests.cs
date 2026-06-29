using NetTopologySuite.Geometries;

namespace Turfano.Tests;

// NOTA DE ESCOPO: o TransformScale do Turfano é uma escala CARTESIANA (dx * fator),
// diferente do TurfJS, que é geodésica (escala por rumo/distância — a bbox do quadrado
// unitário ×2 dá ~2.000305, não 2). Tornar geodésica é tema do redesign; aqui validamos
// o contrato cartesiano do Turfano e, principalmente, a correção do colapso do eixo Y
// (antes do patch, sem FactorY, todos os Y viravam a constante 'fator').
public class TransformScaleTests
{
    private static Polygon UnitSquare() =>
        new Polygon(
            new LinearRing(
                [
                    new Coordinate(-1, -1),
                    new Coordinate(1, -1),
                    new Coordinate(1, 1),
                    new Coordinate(-1, 1),
                    new Coordinate(-1, -1),
                ]
            )
        );

    [Test]
    public async Task DefaultCase_ScalesBothAxesUniformly()
    {
        // Antes do patch, o eixo Y colapsava (Height == 0). SC-005: falha antes, passa depois.
        var scaled = Turf.TransformScale(UnitSquare(), 2.0);
        var env = scaled.EnvelopeInternal;

        await Assert.That(env.Width).IsEqualTo(4.0).Within(1e-9);
        await Assert.That(env.Height).IsEqualTo(4.0).Within(1e-9);
    }

    [Test]
    public async Task ExplicitFactorY_ScalesYIndependently()
    {
        var scaled = Turf.TransformScale(
            UnitSquare(),
            2.0,
            new TransformScaleOptions { FactorY = 3.0 }
        );
        var env = scaled.EnvelopeInternal;

        await Assert.That(env.Width).IsEqualTo(4.0).Within(1e-9); // X: 2×
        await Assert.That(env.Height).IsEqualTo(6.0).Within(1e-9); // Y: 3×
    }

    [Test]
    public async Task ShrinkFactor_ReducesUniformly()
    {
        var scaled = Turf.TransformScale(UnitSquare(), 0.5);
        var env = scaled.EnvelopeInternal;

        await Assert.That(env.Width).IsEqualTo(1.0).Within(1e-9);
        await Assert.That(env.Height).IsEqualTo(1.0).Within(1e-9);
    }

    [Test]
    public async Task ExplicitOrigin_ScalesFromThatPoint()
    {
        // Origem no canto (-1,-1): o canto fica fixo; o oposto (1,1) vai para (3,3).
        var scaled = Turf.TransformScale(
            UnitSquare(),
            2.0,
            new TransformScaleOptions { Origin = new Point(-1, -1) }
        );
        var env = scaled.EnvelopeInternal;

        await Assert.That(env.MinX).IsEqualTo(-1.0).Within(1e-9);
        await Assert.That(env.MinY).IsEqualTo(-1.0).Within(1e-9);
        await Assert.That(env.MaxX).IsEqualTo(3.0).Within(1e-9);
        await Assert.That(env.MaxY).IsEqualTo(3.0).Within(1e-9);
    }
}
