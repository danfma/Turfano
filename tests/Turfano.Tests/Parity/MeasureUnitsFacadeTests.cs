using G = Turfano.GeoJson.Geo;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US4 — conversões de unidade na fachada Geo, valores do @turf real.
public class MeasureUnitsFacadeTests
{
    [Test]
    public async Task UnitConversions_OnGeoFacade_MatchTurf()
    {
        await Assert.That(G.BearingToAzimuth(Units.Angle.FromDegrees(-170)).Degrees).IsEqualTo(190).Within(1e-9);
        await Assert
            .That(G.ConvertLength(1, Units.LengthUnit.Kilometers, Units.LengthUnit.Miles))
            .IsEqualTo(0.62137119)
            .Within(1e-6);
        await Assert
            .That(G.ConvertArea(1, Units.AreaUnit.SquareKilometers, Units.AreaUnit.SquareMeters))
            .IsEqualTo(1000000)
            .Within(1e-3);
        await Assert.That(G.DegreesToRadians(180)).IsEqualTo(Math.PI).Within(1e-12);
        await Assert.That(G.RadiansToDegrees(Math.PI)).IsEqualTo(180).Within(1e-9);
    }
}
