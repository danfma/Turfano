using System.Text.Json.Nodes;
using Units = Turfano.Units;

namespace Turfano.GeoJson;

// US4 — `@turf/clusters-dbscan`, fiel inclusive na ORDEM em que o laço externo visita os
// pontos: o rótulo final ("core" vs "edge") depende de qual visita chega primeiro a cada
// ponto — o próprio laço externo (marca "noise" preliminar sem `visited`) ou a expansão
// BFS de um vizinho já classificado como "core" (marca "edge" só se o `isnoise` preliminar
// já estava true) — não é só geometria, é ORDEM DE ÍNDICE + geometria.
public static partial class Geo
{
    /// <summary>
    /// Agrupa pontos pelo algoritmo DBSCAN — `@turf/clusters-dbscan`. Cada ponto ganha
    /// `dbscan` (`"core"`/`"edge"`/`"noise"`) e, quando pertence a um cluster, `cluster`
    /// (índice, 0-based, na ordem de descoberta).
    /// </summary>
    public static FeatureCollection ClustersDbscan(FeatureCollection points, Units.Length maxDistance, int minPoints = 3)
    {
        var count = points.Features.Length;
        var latDistanceInDegrees = maxDistance.Degrees;
        var maxDistanceKm = maxDistance.Kilometers;

        var positions = new Position[count];
        var items = new List<RBushItem<int>>(count);
        for (var i = 0; i < count; i++)
        {
            if (points.Features[i].Geometry is not Point point)
                throw new ArgumentException("points deve conter apenas Point", nameof(points));
            positions[i] = point.Coordinates;
            items.Add(new RBushItem<int>(point.Coordinates.Lon, point.Coordinates.Lat, point.Coordinates.Lon, point.Coordinates.Lat, i));
        }

        // `new RBush(points.features.length)` — o @turf usa a contagem de pontos como
        // maxEntries (não o padrão 9); isso muda o formato da árvore e, por consequência,
        // a ORDEM de `Search`, então é reproduzido aqui tal e qual.
        var tree = new RBushIndex<int>(count);
        tree.Load(items);

        var visited = new bool[count];
        var assigned = new bool[count];
        var isNoise = new bool[count];
        var clusterIds = new int[count];
        Array.Fill(clusterIds, -1);

        List<RBushItem<int>> RegionQuery(int index)
        {
            var x = positions[index].Lon;
            var y = positions[index].Lat;
            var minY = Math.Max(y - latDistanceInDegrees, -90);
            var maxY = Math.Min(y + latDistanceInDegrees, 90);

            double lonDistanceInDegrees;
            if (minY < 0 && maxY > 0)
                lonDistanceInDegrees = latDistanceInDegrees;
            else if (Math.Abs(minY) < Math.Abs(maxY))
                lonDistanceInDegrees = latDistanceInDegrees / Math.Cos(maxY * RadiansPerDegree);
            else
                lonDistanceInDegrees = latDistanceInDegrees / Math.Cos(minY * RadiansPerDegree);

            var minX = Math.Max(x - lonDistanceInDegrees, -360);
            var maxX = Math.Min(x + lonDistanceInDegrees, 360);

            var candidates = tree.Search(new SpatialBox { MinX = minX, MinY = minY, MaxX = maxX, MaxY = maxY });
            var result = new List<RBushItem<int>>(candidates.Count);
            foreach (var neighbor in candidates)
            {
                var distanceInKm = Distance(positions[index], positions[neighbor.Item]).Kilometers;
                if (distanceInKm <= maxDistanceKm)
                    result.Add(neighbor);
            }
            return result;
        }

        void ExpandCluster(int clusteredId, List<RBushItem<int>> neighbors)
        {
            for (var i = 0; i < neighbors.Count; i++)
            {
                var neighborIndex = neighbors[i].Item;
                if (!visited[neighborIndex])
                {
                    visited[neighborIndex] = true;
                    var nextNeighbors = RegionQuery(neighborIndex);
                    if (nextNeighbors.Count >= minPoints)
                        neighbors.AddRange(nextNeighbors);
                }
                if (!assigned[neighborIndex])
                {
                    assigned[neighborIndex] = true;
                    clusterIds[neighborIndex] = clusteredId;
                }
            }
        }

        var nextClusteredId = 0;
        for (var index = 0; index < count; index++)
        {
            if (visited[index])
                continue;

            var neighbors = RegionQuery(index);
            if (neighbors.Count >= minPoints)
            {
                var clusteredId = nextClusteredId++;
                visited[index] = true;
                ExpandCluster(clusteredId, neighbors);
            }
            else
            {
                isNoise[index] = true;
            }
        }

        var features = new Feature[count];
        for (var index = 0; index < count; index++)
        {
            var original = points.Features[index];
            var properties =
                original.Properties is null ? new JsonObject() : (JsonObject)original.Properties.DeepClone();

            if (clusterIds[index] >= 0)
            {
                properties["dbscan"] = isNoise[index] ? "edge" : "core";
                properties["cluster"] = clusterIds[index];
            }
            else
            {
                properties["dbscan"] = "noise";
            }
            features[index] = new Feature(original.Geometry, properties) { Id = original.Id };
        }
        return new FeatureCollection(features);
    }
}
