using NetTopologySuite.Geometries;

namespace Turfano.Tests;

// Escopo: este teste cobre apenas o que o fix de Angles.TwoPi governa — o caminho
// Explementary, que deve devolver (360° − base) e não (180° − base).
//
// DIVERGÊNCIA PRÉ-EXISTENTE (fora do escopo deste fix, para o redesign): o GetAngle do
// Turfano calcula a base com Bearing(start→mid)/Bearing(end→mid), enquanto o TurfJS usa
// bearing(mid→start)/bearing(mid→end). Para ((5,5),(5,6),(3,4)) isso dá ~44.8° no
// Turfano vs 44.982° no @turf/angle (~0.18° de diferença por convergência geodésica).
// Por isso NÃO ancoramos a base no valor do TurfJS aqui — testamos a relação, que é o
// contrato do bug corrigido, independente da base.
public class GetAngleTests
{
    private static readonly Coordinate Start = new(5, 5);
    private static readonly Coordinate Mid = new(5, 6);
    private static readonly Coordinate End = new(3, 4);

    [Test]
    public async Task Explementary_Is360MinusBase()
    {
        var baseAngle = Turf.GetAngle(Start, Mid, End);
        var explementary = Turf.GetAngle(Start, Mid, End, o => o with { Explementary = true });

        // O fix: explementar = 360 − base (antes do fix, com TwoPi = π, dava 180 − base).
        await Assert
            .That(explementary.Degrees)
            .IsEqualTo(360.0 - baseAngle.Degrees)
            .Within(1e-9);
        await Assert
            .That(baseAngle.Degrees + explementary.Degrees)
            .IsEqualTo(360.0)
            .Within(1e-9);
    }
}
