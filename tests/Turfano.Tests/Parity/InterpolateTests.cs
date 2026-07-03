using System.Text.Json.Nodes;
using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US2 (Onda F) — planepoint/tin/interpolate; valores do @turf real (reference/_wavef.mjs).
public class InterpolateTests
{
    private static GeoJson.Feature PointWithZ(double lon, double lat, double z) =>
        new(new GeoJson.Point(new Pos(lon, lat)), new JsonObject { ["z"] = z });

    [Test]
    public async Task Planepoint_MatchesTurf()
    {
        // triângulo (0,0)(2,0)(1,2) com a=10,b=20,c=30
        var triangle = new GeoJson.Feature(
            new GeoJson.Polygon(
                new[] { new[] { new Pos(0, 0), new Pos(2, 0), new Pos(1, 2), new Pos(0, 0) } }
            ),
            new JsonObject
            {
                ["a"] = 10,
                ["b"] = 20,
                ["c"] = 30,
            }
        );
        await Assert.That(G.Planepoint(G.Point(1, 0.5), triangle)).IsEqualTo(18.75).Within(1e-12);
        // fora do triângulo: extrapola (o @turf não valida) → 72.5
        await Assert.That(G.Planepoint(G.Point(5, 5), triangle)).IsEqualTo(72.5).Within(1e-12);

        // z pela 3ª coordenada
        var triangleZ = new GeoJson.Feature(
            new GeoJson.Polygon(
                new[]
                {
                    new[]
                    {
                        new Pos(0, 0, 10),
                        new Pos(2, 0, 20),
                        new Pos(1, 2, 30),
                        new Pos(0, 0, 10),
                    },
                }
            )
        );
        await Assert.That(G.Planepoint(G.Point(1, 0.5), triangleZ)).IsEqualTo(18.75).Within(1e-12);
    }

    [Test]
    public async Task Tin_MatchesTurf_NotAFan()
    {
        var points = new GeoJson.FeatureCollection(
            new[]
            {
                PointWithZ(0, 0, 1),
                PointWithZ(2, 0, 2),
                PointWithZ(1, 2, 3),
                PointWithZ(3, 2, 4),
                PointWithZ(0.5, 3, 5),
            }
        );
        var tin = G.Tin(points, "z");

        // @turf: 4 triângulos exatos (Delaunay — o "leque" ingênuo daria outra estrutura)
        await Assert.That(tin.Features.Length).IsEqualTo(4);

        var first = (GeoJson.Polygon)tin.Features[0].Geometry!;
        await Assert
            .That(first.Coordinates[0])
            .IsEquivalentTo(new[] { new Pos(0, 0), new Pos(0.5, 3), new Pos(1, 2), new Pos(0, 0) });
        var props0 = tin.Features[0].Properties!;
        await Assert.That(props0["a"]!.GetValue<double>()).IsEqualTo(1);
        await Assert.That(props0["b"]!.GetValue<double>()).IsEqualTo(5);
        await Assert.That(props0["c"]!.GetValue<double>()).IsEqualTo(3);

        var last = (GeoJson.Polygon)tin.Features[3].Geometry!;
        await Assert
            .That(last.Coordinates[0])
            .IsEquivalentTo(new[] { new Pos(2, 0), new Pos(1, 2), new Pos(3, 2), new Pos(2, 0) });
    }

    [Test]
    public async Task Interpolate_Idw_MatchesTurf()
    {
        var samples = new GeoJson.FeatureCollection(
            new[]
            {
                new GeoJson.Feature(G.Point(0, 0), new JsonObject { ["elevation"] = 10 }),
                new GeoJson.Feature(G.Point(1, 0), new JsonObject { ["elevation"] = 20 }),
                new GeoJson.Feature(G.Point(0, 1), new JsonObject { ["elevation"] = 30 }),
                new GeoJson.Feature(G.Point(1, 1), new JsonObject { ["elevation"] = 40 }),
            }
        );

        // gridType square, 50 km: 4 células; elevações do @turf
        var square = G.Interpolate(samples, Units.Length.FromKilometers(50));
        await Assert.That(square.Features.Length).IsEqualTo(4);
        double[] expectedSquare = { 21.095970638964225, 26.301731472107143, 23.69867170291542 };
        for (var i = 0; i < 3; i++)
        {
            await Assert
                .That(square.Features[i].Properties!["elevation"]!.GetValue<double>())
                .IsEqualTo(expectedSquare[i])
                .Within(1e-9);
        }

        // gridType point, weight 2: 9 pontos
        var pointGrid = G.Interpolate(
            samples,
            Units.Length.FromKilometers(50),
            gridType: GeoJson.GridType.Point,
            weight: 2
        );
        await Assert.That(pointGrid.Features.Length).IsEqualTo(9);
        double[] expectedPoint = { 10.248931249415731, 21.798407515585676, 29.9170510399431 };
        for (var i = 0; i < 3; i++)
        {
            await Assert
                .That(pointGrid.Features[i].Properties!["elevation"]!.GetValue<double>())
                .IsEqualTo(expectedPoint[i])
                .Within(1e-9);
        }
    }
}
