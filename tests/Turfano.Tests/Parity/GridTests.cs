using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US1 (Onda F) — grades; contagens/coordenadas do @turf real (reference/_wavef.mjs).
public class GridTests
{
    private static GeoJson.BBox Box() => new(-1, -1, 1, 1);

    private static Units.Length Cell() => Units.Length.FromKilometers(50);

    private static GeoJson.Polygon Mask() =>
        new(
            new[]
            {
                new[] { new Pos(-0.5, -0.5), new Pos(-0.5, 0.5), new Pos(0.5, 0.5), new Pos(0.5, -0.5), new Pos(-0.5, -0.5) },
            }
        );

    [Test]
    public async Task PointGrid_MatchesTurf()
    {
        // turf.pointGrid([-1,-1,1,1], 50km): 25 pontos; 1º = [-0.8994573693470465, -0.899320363724538]
        var grid = G.PointGrid(Box(), Cell());
        await Assert.That(grid.Features.Length).IsEqualTo(25);
        var first = ((GeoJson.Point)grid.Features[0].Geometry!).Coordinates;
        await Assert.That(first.Lon).IsEqualTo(-0.8994573693470465).Within(1e-12);
        await Assert.That(first.Lat).IsEqualTo(-0.899320363724538).Within(1e-12);

        // com mask: 9
        await Assert.That(G.PointGrid(Box(), Cell(), Mask()).Features.Length).IsEqualTo(9);
    }

    [Test]
    public async Task SquareGrid_MatchesTurf()
    {
        // turf.squareGrid: 16 células; 1º canto = [-0.899320363724538, -0.899320363724538]
        var grid = G.SquareGrid(Box(), Cell());
        await Assert.That(grid.Features.Length).IsEqualTo(16);
        var corner = ((GeoJson.Polygon)grid.Features[0].Geometry!).Coordinates[0][0];
        await Assert.That(corner.Lon).IsEqualTo(-0.899320363724538).Within(1e-12);
        await Assert.That(corner.Lat).IsEqualTo(-0.899320363724538).Within(1e-12);

        // mask: todas as 16 intersectam
        await Assert.That(G.SquareGrid(Box(), Cell(), Mask()).Features.Length).IsEqualTo(16);
    }

    [Test]
    public async Task HexGrid_MatchesTurf()
    {
        // turf.hexGrid: 3 hexágonos; 1º vértice = [0.1124150454655673, -0.38941714056305565]
        var grid = G.HexGrid(Box(), Cell());
        await Assert.That(grid.Features.Length).IsEqualTo(3);
        var vertex = ((GeoJson.Polygon)grid.Features[0].Geometry!).Coordinates[0][0];
        await Assert.That(vertex.Lon).IsEqualTo(0.1124150454655673).Within(1e-12);
        await Assert.That(vertex.Lat).IsEqualTo(-0.38941714056305565).Within(1e-12);

        // triangles: 18 (3 hex × 6); mask: 3
        await Assert.That(G.HexGrid(Box(), Cell(), triangles: true).Features.Length).IsEqualTo(18);
        await Assert.That(G.HexGrid(Box(), Cell(), Mask()).Features.Length).IsEqualTo(3);
    }

    [Test]
    public async Task TriangleGrid_MatchesTurf()
    {
        // turf.triangleGrid: 50 triângulos; 2º vértice do 1º = [-1, -0.550339818137731]
        var grid = G.TriangleGrid(Box(), Cell());
        await Assert.That(grid.Features.Length).IsEqualTo(50);
        var vertex = ((GeoJson.Polygon)grid.Features[0].Geometry!).Coordinates[0][1];
        await Assert.That(vertex.Lon).IsEqualTo(-1).Within(1e-12);
        await Assert.That(vertex.Lat).IsEqualTo(-0.550339818137731).Within(1e-12);

        // mask: 17
        await Assert.That(G.TriangleGrid(Box(), Cell(), Mask()).Features.Length).IsEqualTo(17);
    }
}
