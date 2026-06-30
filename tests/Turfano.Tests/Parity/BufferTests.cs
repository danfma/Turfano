using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US2 — buffer via projeção azimutal-equidistante + NTS; validado por ÁREA vs o @turf real.
public class BufferTests
{
    [Test]
    public async Task Buffer_Point_AreaMatchesTurf()
    {
        var buffered = G.Buffer(G.Point(0, 0), Units.Length.FromKilometers(100), steps: 8);
        // @turf: área ≈ 31213810675 m² (polígono de 32 lados ~ círculo de 100 km)
        await Assert.That(G.Area(buffered!).SquareMeters).IsEqualTo(31213810675.38849).Within(1e7);
    }
}
