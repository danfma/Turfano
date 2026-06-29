using TArea = Turfano.Units.Area;
using TLength = Turfano.Units.Length;
using TAngle = Turfano.Units.Angle;

namespace Turfano.Tests;

// Valores de referência obtidos rodando o @turf/helpers real (reference/_units.mjs):
// as conversões reproduzem o convertLength/convertArea/lengthToRadians/radiansToLength/
// lengthToDegrees/degreesToRadians/radiansToDegrees/bearingToAzimuth do TurfJS.
public class UnitsTests
{
    [Test]
    public async Task Length_Conversions_MatchTurf()
    {
        await Assert.That(TLength.FromKilometers(1).Miles).IsEqualTo(0.621371192237334).Within(1e-9);
        await Assert.That(TLength.FromKilometers(1).Meters).IsEqualTo(1000).Within(1e-9);
        await Assert.That(TLength.FromMiles(1).Kilometers).IsEqualTo(1.609344).Within(1e-9);
        await Assert.That(TLength.FromMeters(100).Feet).IsEqualTo(328.08400000000006).Within(1e-9);
        await Assert.That(TLength.FromNauticalMiles(1).Kilometers).IsEqualTo(1.852).Within(1e-9);
    }

    [Test]
    public async Task LengthToRadians_And_Degrees_MatchTurf()
    {
        // lengthToRadians(1,'kilometers'), radiansToLength(1,'kilometers'), lengthToDegrees(1,'kilometers')
        await Assert.That(TLength.FromKilometers(1).Radians).IsEqualTo(0.00015696101377226163).Within(1e-15);
        await Assert.That(TLength.FromRadians(1).Kilometers).IsEqualTo(6371.0088).Within(1e-9);
        await Assert.That(TLength.FromKilometers(1).Degrees).IsEqualTo(0.00899320363724538).Within(1e-12);
    }

    [Test]
    public async Task Angle_Conversions_And_Azimuth_MatchTurf()
    {
        await Assert.That(TAngle.FromDegrees(180).Radians).IsEqualTo(Math.PI).Within(1e-12);
        await Assert.That(TAngle.FromRadians(Math.PI).Degrees).IsEqualTo(180).Within(1e-9);
        // bearingToAzimuth
        await Assert.That(TAngle.FromDegrees(-170).ToAzimuth().Degrees).IsEqualTo(190).Within(1e-9);
        await Assert.That(TAngle.FromDegrees(200).ToAzimuth().Degrees).IsEqualTo(200).Within(1e-9);
        await Assert.That(TAngle.FromDegrees(-10).ToAzimuth().Degrees).IsEqualTo(350).Within(1e-9);
    }

    [Test]
    public async Task Area_Conversions_MatchTurf()
    {
        await Assert.That(TArea.FromSquareMeters(1000000).SquareKilometers).IsEqualTo(1).Within(1e-9);
        await Assert.That(TArea.FromSquareKilometers(1).SquareMeters).IsEqualTo(1000000).Within(1e-6);
        await Assert.That(TArea.FromSquareMeters(10000).Hectares).IsEqualTo(1).Within(1e-9);
        await Assert.That(TArea.FromSquareKilometers(1).SquareMiles).IsEqualTo(0.386).Within(1e-9);
        await Assert.That(TArea.FromSquareMeters(4046.8564224).Acres).IsEqualTo(0.9999984562571521).Within(1e-9);
    }
}
