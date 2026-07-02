using NetTopologySuite.Geometries.Implementation;
using Turfano.NetTopologySuite;
using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US3 (feature 009) — buffer no satélite Turfano.NetTopologySuite (extensão AEQD + NTS),
// com a fronteira EMPACOTADA (zero objetos Coordinate). Área validada vs o @turf real.
public class BufferTests
{
    [Test]
    public async Task Buffer_Point_AreaMatchesTurf()
    {
        var buffered = G.Point(0, 0).Buffer(Units.Length.FromKilometers(100), steps: 8);
        // @turf: área ≈ 31213810675 m² (polígono de 32 lados ~ círculo de 100 km)
        await Assert.That(G.Area(buffered!).SquareMeters).IsEqualTo(31213810675.38849).Within(1e7);
    }

    [Test]
    public async Task NtsConvert_UsesPackedSequences()
    {
        // a fronteira não materializa Coordinate: a sequência da geometria é empacotada
        var polygon = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(0, 4), new Pos(4, 4), new Pos(4, 0), new Pos(0, 0) } }
        );
        var nts = (global::NetTopologySuite.Geometries.Polygon)NtsConvert.ToNts(polygon);
        await Assert
            .That(((global::NetTopologySuite.Geometries.LineString)nts.ExteriorRing).CoordinateSequence)
            .IsTypeOf<PackedDoubleCoordinateSequence>();
    }
}
