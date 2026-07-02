using System.Text.Json.Nodes;
using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US4 (Onda G) — aleatórios, clusters (kmeans/dbscan/helpers) e collect; valores exatos
// do @turf real capturados em reference/_waveg_us4.mjs (kmeans/dbscan/collect/helpers);
// random/sample validados por estrutura (contagem, contenção na bbox, anéis fechados).
public class RandomClusterTests
{
    private static GeoJson.FeatureCollection FC(params GeoJson.Point[] pts) =>
        new(pts.Select(p => new GeoJson.Feature(p)).ToArray());

    // ===== clustersKmeans =====

    [Test]
    public async Task ClustersKmeans_MatchesTurf()
    {
        // reference/_waveg_us4.mjs: kmeans= 3 pontos cluster 0 (centroide [1/3,1/3]),
        // 3 pontos cluster 1 (centroide [10.333...,10.333...])
        var points = FC(
            G.Point(0, 0),
            G.Point(1, 0),
            G.Point(0, 1),
            G.Point(10, 10),
            G.Point(11, 10),
            G.Point(10, 11)
        );
        var clustered = G.ClustersKmeans(points, numberOfClusters: 2);

        var clusters = clustered.Features.Select(f => f.Properties!["cluster"]!.GetValue<int>()).ToArray();
        await Assert.That(clusters).IsEquivalentTo(new[] { 0, 0, 0, 1, 1, 1 });

        var centroid0 = clustered.Features[0].Properties!["centroid"]!.AsArray();
        await Assert.That(centroid0[0]!.GetValue<double>()).IsEqualTo(1.0 / 3.0).Within(1e-9);
        await Assert.That(centroid0[1]!.GetValue<double>()).IsEqualTo(1.0 / 3.0).Within(1e-9);

        var centroid1 = clustered.Features[3].Properties!["centroid"]!.AsArray();
        await Assert.That(centroid1[0]!.GetValue<double>()).IsEqualTo(31.0 / 3.0).Within(1e-9);
        await Assert.That(centroid1[1]!.GetValue<double>()).IsEqualTo(31.0 / 3.0).Within(1e-9);
    }

    [Test]
    public async Task ClustersKmeans_DefaultK_And_KGreaterThanCount_MatchTurf()
    {
        // sem numberOfClusters: round(sqrt(6/2)) = round(sqrt(3)) = 2 (mesmo resultado)
        var points = FC(
            G.Point(0, 0),
            G.Point(1, 0),
            G.Point(0, 1),
            G.Point(10, 10),
            G.Point(11, 10),
            G.Point(10, 11)
        );
        var defaultK = G.ClustersKmeans(points);
        var clusters = defaultK.Features.Select(f => f.Properties!["cluster"]!.GetValue<int>()).ToArray();
        await Assert.That(clusters).IsEquivalentTo(new[] { 0, 0, 0, 1, 1, 1 });

        // k > n: clampa para n=2; cada ponto vira seu próprio cluster/centroide
        var small = FC(G.Point(0, 0), G.Point(5, 5));
        var clampedK = G.ClustersKmeans(small, numberOfClusters: 5);
        await Assert.That(clampedK.Features[0].Properties!["cluster"]!.GetValue<int>()).IsEqualTo(0);
        await Assert.That(clampedK.Features[1].Properties!["cluster"]!.GetValue<int>()).IsEqualTo(1);
        var c0 = clampedK.Features[0].Properties!["centroid"]!.AsArray();
        await Assert.That(c0[0]!.GetValue<double>()).IsEqualTo(0.0).Within(1e-9);
        var c1 = clampedK.Features[1].Properties!["centroid"]!.AsArray();
        await Assert.That(c1[0]!.GetValue<double>()).IsEqualTo(5.0).Within(1e-9);
    }

    // ===== clustersDbscan =====

    [Test]
    public async Task ClustersDbscan_MatchesTurf()
    {
        // reference/_waveg_us4.mjs: dbscan= [[0,core],[0,core],[0,core],[null,noise],
        // [1,edge],[1,core],[1,core],[1,core]] — índice 4 ([10.1,10.1]) é visitado como
        // "noise" preliminar pelo laço externo ANTES de [10,10] (índice 5) formar o
        // cluster e alcançá-lo via expandCluster: por isso vira "edge", não "core".
        var points = FC(
            G.Point(0, 0),
            G.Point(0.05, 0),
            G.Point(0.05, 0.05),
            G.Point(5, 5),
            G.Point(10.1, 10.1),
            G.Point(10, 10),
            G.Point(10.05, 10),
            G.Point(10.05, 10.05)
        );
        var clustered = G.ClustersDbscan(points, Units.Length.FromKilometers(10), minPoints: 3);

        var labels = clustered
            .Features.Select(f => (
                Cluster: f.Properties!["cluster"]?.GetValue<int>(),
                Dbscan: f.Properties!["dbscan"]!.GetValue<string>()
            ))
            .ToArray();

        await Assert.That(labels[0]).IsEqualTo((Cluster: (int?)0, Dbscan: "core"));
        await Assert.That(labels[1]).IsEqualTo((Cluster: (int?)0, Dbscan: "core"));
        await Assert.That(labels[2]).IsEqualTo((Cluster: (int?)0, Dbscan: "core"));
        await Assert.That(labels[3]).IsEqualTo((Cluster: (int?)null, Dbscan: "noise"));
        await Assert.That(labels[4]).IsEqualTo((Cluster: (int?)1, Dbscan: "edge"));
        await Assert.That(labels[5]).IsEqualTo((Cluster: (int?)1, Dbscan: "core"));
        await Assert.That(labels[6]).IsEqualTo((Cluster: (int?)1, Dbscan: "core"));
        await Assert.That(labels[7]).IsEqualTo((Cluster: (int?)1, Dbscan: "core"));
    }

    // ===== collect =====

    [Test]
    public async Task Collect_MatchesTurf()
    {
        // reference/_waveg_us4.mjs: collect= [[200,600,null],[100,200,300]] — pt6 (1,1)
        // não tem "population"; entra no array como `null` (equivalente ao `undefined`
        // que o JS empilharia e que o JSON.stringify já vira `null`).
        var poly1 = new GeoJson.Polygon(
            new[] { new[] { new Pos(0, 0), new Pos(10, 0), new Pos(10, 10), new Pos(0, 10), new Pos(0, 0) } }
        );
        var poly2 = new GeoJson.Polygon(
            new[] { new[] { new Pos(10, 0), new Pos(20, 10), new Pos(20, 20), new Pos(20, 0), new Pos(10, 0) } }
        );
        var polygons = new GeoJson.FeatureCollection(
            new[] { new GeoJson.Feature(poly1), new GeoJson.Feature(poly2) }
        );

        GeoJson.Feature PointWithPopulation(double lon, double lat, int? population) =>
            new(G.Point(lon, lat), population is { } p ? new JsonObject { ["population"] = p } : new JsonObject());

        var points = new GeoJson.FeatureCollection(
            new[]
            {
                PointWithPopulation(5, 5, 200),
                PointWithPopulation(1, 3, 600),
                PointWithPopulation(14, 2, 100),
                PointWithPopulation(13, 1, 200),
                PointWithPopulation(19, 7, 300),
                PointWithPopulation(1, 1, null),
            }
        );

        var collected = G.Collect(polygons, points, "population", "values");

        var expected0 = new JsonArray(200, 600, null);
        var expected1 = new JsonArray(100, 200, 300);
        await Assert.That(JsonNode.DeepEquals(collected.Features[0].Properties!["values"], expected0)).IsTrue();
        await Assert.That(JsonNode.DeepEquals(collected.Features[1].Properties!["values"], expected1)).IsTrue();
    }

    // ===== clusters helpers (getCluster/clusterEach/clusterReduce) =====

    private static GeoJson.FeatureCollection HelperFC() =>
        new(
            new[]
            {
                new GeoJson.Feature(
                    G.Point(0, 0),
                    new JsonObject { ["cluster"] = 2, ["marker-symbol"] = "circle" }
                ),
                new GeoJson.Feature(
                    G.Point(1, 1),
                    new JsonObject { ["cluster"] = 0, ["marker-symbol"] = "star" }
                ),
                new GeoJson.Feature(
                    G.Point(2, 2),
                    new JsonObject { ["cluster"] = 1, ["marker-symbol"] = "star" }
                ),
                new GeoJson.Feature(
                    G.Point(3, 3),
                    new JsonObject { ["cluster"] = 0, ["marker-symbol"] = "square" }
                ),
                new GeoJson.Feature(
                    G.Point(4, 4),
                    new JsonObject { ["cluster"] = 2, ["marker-symbol"] = "circle" }
                ),
            }
        );

    [Test]
    public async Task GetCluster_FiltersLikeTurf()
    {
        var fc = HelperFC();

        // reference/_waveg_us4.mjs: getCluster {cluster:0} len=2 ; {marker-symbol:circle} len=2 ; 'cluster' (existência) len=5
        await Assert.That(G.GetCluster(fc, "cluster", 0).Features.Length).IsEqualTo(2);
        await Assert.That(G.GetCluster(fc, "marker-symbol", "circle").Features.Length).IsEqualTo(2);
        await Assert.That(G.GetCluster(fc, "cluster").Features.Length).IsEqualTo(5);

        // filtro-objeto com várias chaves (AND) — só o índice 3 (cluster 0 E square)
        var dictionaryFilter = new Dictionary<string, JsonNode?> { ["cluster"] = 0, ["marker-symbol"] = "square" };
        await Assert.That(G.GetCluster(fc, dictionaryFilter).Features.Length).IsEqualTo(1);
    }

    [Test]
    public async Task ClusterEach_OrdersByNumericKeyLikeObjectKeys()
    {
        var fc = HelperFC();

        // reference/_waveg_us4.mjs: clusterEach order=[["0","string",2,0],["1","string",1,1],["2","string",2,2]]
        // — quirk do Object.keys do JS: chaves "índice de array" (0,1,2) enumeram em ordem
        // NUMÉRICA, não na ordem de inserção (que teria sido 2,0,1).
        var captured = new List<(string Value, int Count, int Index)>();
        G.ClusterEach(fc, "cluster", (cluster, value, index) => captured.Add((value, cluster.Features.Length, index)));

        await Assert.That(captured.Count).IsEqualTo(3);
        await Assert.That(captured[0]).IsEqualTo(("0", 2, 0));
        await Assert.That(captured[1]).IsEqualTo(("1", 1, 1));
        await Assert.That(captured[2]).IsEqualTo(("2", 2, 2));

        var total = 0;
        G.ClusterEach(fc, "cluster", (_, _, _) => total++);
        await Assert.That(total).IsEqualTo(3);
    }

    [Test]
    public async Task ClusterReduce_WithAndWithoutInitialValue_MatchTurf()
    {
        var fc = HelperFC();

        // reference/_waveg_us4.mjs: clusterReduce withInitial sumFeatures=5
        var sum = G.ClusterReduce(
            fc,
            "cluster",
            (previous, cluster, _, _) => previous + cluster.Features.Length,
            0
        );
        await Assert.That(sum).IsEqualTo(5);

        // reference/_waveg_us4.mjs: clusterReduce noInitial n=5 (1º cluster vira o acumulador)
        var merged = G.ClusterReduce(
            fc,
            "cluster",
            (previous, cluster, _, _) => new GeoJson.FeatureCollection(previous.Features.Concat(cluster.Features).ToArray())
        );
        await Assert.That(merged.Features.Length).IsEqualTo(5);
    }

    // ===== random* (contrato estrutural — sem GT numérico) =====

    [Test]
    public async Task RandomPosition_And_RandomPoint_StayWithinBBox()
    {
        var bbox = new GeoJson.BBox(0, 0, 10, 10);

        var position = G.RandomPosition(bbox);
        await Assert.That(position.Lon).IsBetween(0, 10);
        await Assert.That(position.Lat).IsBetween(0, 10);

        var points = G.RandomPoint(25, bbox);
        await Assert.That(points.Features.Length).IsEqualTo(25);
        foreach (var feature in points.Features)
        {
            await Assert.That(feature.Geometry is GeoJson.Point).IsTrue();
            var coord = ((GeoJson.Point)feature.Geometry!).Coordinates;
            await Assert.That(coord.Lon).IsBetween(0, 10);
            await Assert.That(coord.Lat).IsBetween(0, 10);
        }
    }

    [Test]
    public async Task RandomLineString_HasRequestedVertexCount()
    {
        var bbox = new GeoJson.BBox(0, 0, 10, 10);
        var lines = G.RandomLineString(4, bbox, numVertices: 6);

        await Assert.That(lines.Features.Length).IsEqualTo(4);
        foreach (var feature in lines.Features)
        {
            var line = (GeoJson.LineString)feature.Geometry!;
            await Assert.That(line.Coordinates.Length).IsEqualTo(6);
        }
    }

    [Test]
    public async Task RandomPolygon_ProducesClosedRingWithRequestedVertexCount()
    {
        var bbox = new GeoJson.BBox(0, 0, 20, 20);
        var polygons = G.RandomPolygon(4, bbox, numVertices: 6, maxRadialLength: 2);

        await Assert.That(polygons.Features.Length).IsEqualTo(4);
        foreach (var feature in polygons.Features)
        {
            var ring = ((GeoJson.Polygon)feature.Geometry!).Coordinates[0];
            await Assert.That(ring.Length).IsEqualTo(7); // numVertices + 1
            await Assert.That(ring[0]).IsEqualTo(ring[^1]); // anel fechado
        }
    }

    [Test]
    public async Task RandomPolygon_ThrowsWhenRadiusExceedsBBox()
    {
        var bbox = new GeoJson.BBox(0, 0, 2, 2);
        await Assert.That(() => G.RandomPolygon(1, bbox, maxRadialLength: 10)).Throws<ArgumentException>();
    }

    // ===== sample (contrato estrutural) =====

    [Test]
    public async Task Sample_ReturnsRequestedCountAsSubsetOfSource()
    {
        var source = FC(
            G.Point(0, 0),
            G.Point(1, 1),
            G.Point(2, 2),
            G.Point(3, 3),
            G.Point(4, 4)
        );

        var sampled = G.Sample(source, 3);
        await Assert.That(sampled.Features.Length).IsEqualTo(3);
        foreach (var feature in sampled.Features)
            await Assert.That(source.Features.Contains(feature)).IsTrue();

        // num > população: devolve a coleção inteira (sem duplicar, sem lançar)
        var oversized = G.Sample(source, 100);
        await Assert.That(oversized.Features.Length).IsEqualTo(source.Features.Length);
    }
}
