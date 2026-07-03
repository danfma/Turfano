using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;

namespace Turfano.Tests;

// US3 — meta-iteração; ordem e índices iguais aos do @turf (reference/_meta.mjs).
public class MetaTests
{
    [Test]
    public async Task CoordEach_OrderAndIndex_MatchTurf()
    {
        var poly = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1), new Pos(0, 0) } }
        );
        var visited = new List<(Pos coord, int index)>();
        G.CoordEach(poly, (c, ci, _, _, _) => visited.Add((c, ci)));

        // @turf: [([0,0],0),([1,0],1),([1,1],2),([0,0],3)] — índice global incl. fechamento
        await Assert.That(visited.Count).IsEqualTo(4);
        await Assert.That(visited[0]).IsEqualTo((new Pos(0, 0), 0));
        await Assert.That(visited[2]).IsEqualTo((new Pos(1, 1), 2));
        await Assert.That(visited[3]).IsEqualTo((new Pos(0, 0), 3));

        // MultiPoint: índices 0,1,2
        var mp = new GeoJson.MultiPoint(new[] { new Pos(0, 0), new Pos(1, 1), new Pos(2, 2) });
        var indices = new List<int>();
        G.CoordEach(mp, (_, ci, _, _, _) => indices.Add(ci));
        await Assert.That(indices).IsEquivalentTo(new[] { 0, 1, 2 });
    }

    [Test]
    public async Task SegmentEach_Index_MatchTurf()
    {
        var poly = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1), new Pos(0, 0) } }
        );
        var segIndices = new List<int>();
        G.SegmentEach(poly, (_, _, _, _, si) => segIndices.Add(si));
        await Assert.That(segIndices).IsEquivalentTo(new[] { 0, 1, 2 }); // 3 segmentos
    }

    [Test]
    public async Task CoordReduce_Sum_Works()
    {
        var line = new GeoJson.LineString(new[] { new Pos(1, 0), new Pos(2, 0), new Pos(3, 0) });
        var sum = G.CoordReduce(line, (acc, c, _) => acc + c.Lon, 0.0);
        await Assert.That(sum).IsEqualTo(6.0).Within(1e-9);
    }

    // @turf (reference/_meta.mjs): coordEach sobre uma FeatureCollection de 2 LineStrings
    // reporta featureIndex REAL (0,0,0,1,1), não 0 fixo; coordIndex continua global (0..4).
    [Test]
    public async Task CoordEach_FeatureCollection_UsesRealFeatureIndex()
    {
        var fc = new GeoJson.FeatureCollection(
            new[]
            {
                new GeoJson.Feature(
                    new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1) })
                ),
                new GeoJson.Feature(
                    new GeoJson.LineString(new[] { new Pos(10, 10), new Pos(11, 10) })
                ),
            }
        );

        var featureIndices = new List<int>();
        var coordIndices = new List<int>();
        G.CoordEach(
            fc,
            (_, coordIndex, featureIndex, _, _) =>
            {
                coordIndices.Add(coordIndex);
                featureIndices.Add(featureIndex);
            }
        );

        await Assert.That(featureIndices).IsEquivalentTo(new[] { 0, 0, 0, 1, 1 });
        await Assert.That(coordIndices).IsEquivalentTo(new[] { 0, 1, 2, 3, 4 });
    }

    // @turf: segmentEach sobre a mesma coleção reporta featureIndex 0,0,1 (2 segmentos na
    // 1ª linha, 1 na 2ª) e segmentIndex reinicia a cada feature (0,1,0).
    [Test]
    public async Task SegmentEach_FeatureCollection_UsesRealFeatureIndex()
    {
        var fc = new GeoJson.FeatureCollection(
            new[]
            {
                new GeoJson.Feature(
                    new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1) })
                ),
                new GeoJson.Feature(
                    new GeoJson.LineString(new[] { new Pos(10, 10), new Pos(11, 10) })
                ),
            }
        );

        var featureIndices = new List<int>();
        var segmentIndices = new List<int>();
        G.SegmentEach(
            fc,
            (_, featureIndex, _, _, segmentIndex) =>
            {
                featureIndices.Add(featureIndex);
                segmentIndices.Add(segmentIndex);
            }
        );

        await Assert.That(featureIndices).IsEquivalentTo(new[] { 0, 0, 1 });
        await Assert.That(segmentIndices).IsEquivalentTo(new[] { 0, 1, 0 });
    }
}
