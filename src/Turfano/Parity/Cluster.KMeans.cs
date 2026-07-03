using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

// US4 — clusters K-means fiéis ao `@turf/clusters-kmeans`. O @turf chama
// `skmeans(data, k, data.slice(0, k))`: como os centroides iniciais são sempre dados
// explicitamente, o skmeans nunca entra nos ramos aleatórios ("kmrand"/"kmpp"/sorteio
// puro) — só no laço de Lloyd puro (assign por distância euclidiana ao quadrado,
// recompute das médias, repete até estabilizar). O algoritmo é, portanto, DETERMINÍSTICO.
public static partial class Geo
{
    /// <summary>
    /// Partitions the points into clusters via k-means — `@turf/clusters-kmeans`. Each
    /// point gets `cluster` (the cluster index) and `centroid` (`[lon, lat]` of the
    /// cluster center). Without <paramref name="numberOfClusters"/>, uses
    /// `round(sqrt(n/2))` (JS `Math.round` rounding); values greater than the point count
    /// are reduced to it.
    /// </summary>
    public static FeatureCollection ClustersKmeans(FeatureCollection points, int? numberOfClusters = null)
    {
        var count = points.Features.Length;
        var k = numberOfClusters ?? (int)JsRound(Math.Sqrt(count / 2.0), 0);
        if (k > count)
            k = count;

        var data = new double[count][];
        for (var i = 0; i < count; i++)
        {
            if (points.Features[i].Geometry is not Point point)
                throw new ArgumentException("points deve conter apenas Point", nameof(points));
            data[i] = new[] { point.Coordinates.Lon, point.Coordinates.Lat };
        }

        // initialCentroids = data.slice(0, k) — cópia, não referência (o Lloyd recria os
        // arrays de centroide a cada iteração, mas o slice original nunca é mutado no JS).
        var initialCentroids = new double[k][];
        for (var i = 0; i < k; i++)
            initialCentroids[i] = (double[])data[i].Clone();

        var result = KMeans.Run(data, k, initialCentroids);

        var features = new Feature[count];
        for (var i = 0; i < count; i++)
        {
            var original = points.Features[i];
            var properties =
                original.Properties is null ? new JsonObject() : (JsonObject)original.Properties.DeepClone();
            var clusterId = result.Idxs[i];
            var centroid = result.Centroids[clusterId];
            properties["cluster"] = clusterId;
            properties["centroid"] = new JsonArray(centroid[0], centroid[1]);
            features[i] = new Feature(original.Geometry, properties) { Id = original.Id };
        }
        return new FeatureCollection(features);
    }
}

/// <summary>
/// Port of the ONLY path of `skmeans` (npm package) that `@turf/clusters-kmeans` invokes:
/// pure Lloyd with given initial centroids (the real package also offers random
/// initialization and k-means++, never used by @turf — not ported).
/// </summary>
internal static class KMeans
{
    private const int MaxIterations = 10000; // MAX do skmeans

    public static KMeansResult Run(double[][] data, int k, double[][] initialCentroids)
    {
        var centroids = new double[k][];
        for (var j = 0; j < k; j++)
            centroids[j] = (double[])initialCentroids[j].Clone();

        var dimensions = data.Length > 0 ? data[0].Length : 0;
        var assignments = new int[data.Length];
        var iterationsLeft = MaxIterations;
        bool converged;

        do
        {
            var counts = new int[k];

            // Atribui cada ponto ao centroide mais próximo (distância euclidiana AO
            // QUADRADO — a raiz não é necessária para comparar). Em empate (`<=`, não
            // `<`), o ÚLTIMO índice testado vence — fidelidade ao `dist<=min` da fonte.
            for (var i = 0; i < data.Length; i++)
            {
                var min = double.PositiveInfinity;
                var idx = 0;
                for (var j = 0; j < k; j++)
                {
                    var distance = SquaredEuclideanDistance(data[i], centroids[j]);
                    if (distance <= min)
                    {
                        min = distance;
                        idx = j;
                    }
                }
                assignments[i] = idx;
                counts[idx]++;
            }

            var sums = new double[k][];
            var previous = new double[k][];
            for (var j = 0; j < k; j++)
            {
                sums[j] = new double[dimensions];
                previous[j] = centroids[j];
            }

            for (var i = 0; i < data.Length; i++)
            {
                var sum = sums[assignments[i]];
                var vector = data[i];
                for (var h = 0; h < dimensions; h++)
                    sum[h] += vector[h];
            }

            // Recalcula as médias. `(soma/contagem) || 0` no JS: com contagem 0 a divisão
            // dá NaN (falso) e vira 0 — um cluster sem pontos "zera" o centroide.
            converged = true;
            for (var j = 0; j < k; j++)
            {
                var newCentroid = new double[dimensions];
                for (var h = 0; h < dimensions; h++)
                    newCentroid[h] = counts[j] == 0 ? 0.0 : sums[j][h] / counts[j];
                centroids[j] = newCentroid;

                if (converged)
                    for (var h = 0; h < dimensions; h++)
                        if (previous[j][h] != newCentroid[h])
                        {
                            converged = false;
                            break;
                        }
            }

            // `conv = conv || (--it<=0)`: o `||` do JS faz curto-circuito — só decrementa
            // quando ainda NÃO convergiu (replicado aqui com o `if`, não muda o resultado
            // final, já que uma convergência verdadeira encerra o laço de qualquer forma).
            if (!converged)
            {
                iterationsLeft--;
                if (iterationsLeft <= 0)
                    converged = true;
            }
        } while (!converged);

        return new KMeansResult(assignments, centroids);
    }

    private static double SquaredEuclideanDistance(double[] a, double[] b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            var d = a[i] - b[i];
            sum += d * d;
        }
        return sum;
    }
}

/// <summary>Output of <see cref="KMeans.Run"/>: each point's cluster and the final centroids.</summary>
internal sealed record KMeansResult(int[] Idxs, double[][] Centroids);
