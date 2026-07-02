namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Matriz de pesos espaciais (Minkowski p-norm) entre os centroides das features —
    /// `@turf/distance-weight`. Distância calculada diretamente sobre as coordenadas
    /// lon/lat do centroide (planar — o próprio @turf ignora a curvatura aqui). Acima de
    /// <paramref name="threshold"/> o peso é 0; senão, `1` (binário) ou `dis^alpha`.
    /// </summary>
    public static double[][] DistanceWeight(
        FeatureCollection fc,
        double threshold = 10000,
        double p = 2,
        bool binary = false,
        double alpha = -1,
        bool standardization = false
    )
    {
        // Quirk do @turf: `options.threshold || 1e4`, `options.p || 2`, `options.alpha || -1`
        // — um 0 explícito nesses três também cai no default (falsy).
        threshold = threshold != 0 ? threshold : 10000;
        p = p != 0 ? p : 2;
        alpha = alpha != 0 ? alpha : -1;

        var centroids = fc.Features.Where(f => f.Geometry is not null).Select(f => Centroid(f.Geometry!).Coordinates).ToArray();
        var n = centroids.Length;

        var weights = new double[n][];
        for (var i = 0; i < n; i++)
            weights[i] = new double[n];

        for (var i = 0; i < n; i++)
            for (var j = i; j < n; j++)
            {
                var dis = PNormDistance(centroids[i], centroids[j], p);
                weights[i][j] = dis;
                weights[j][i] = dis;
            }

        for (var i = 0; i < n; i++)
            for (var j = 0; j < n; j++)
            {
                var dis = weights[i][j];
                if (dis == 0)
                    continue;

                if (binary)
                    weights[i][j] = dis <= threshold ? 1 : 0;
                else
                    weights[i][j] = dis <= threshold ? Math.Pow(dis, alpha) : 0;
            }

        if (standardization)
        {
            for (var i = 0; i < n; i++)
            {
                var rowSum = weights[i].Sum();
                for (var j = 0; j < n; j++)
                    weights[i][j] /= rowSum;
            }
        }

        return weights;
    }

    /// <summary>Distância de Minkowski p-norm entre dois pontos (coordenadas cruas) — `@turf/distance-weight pNormDistance`.</summary>
    private static double PNormDistance(Position a, Position b, double p)
    {
        var xDiff = a.Lon - b.Lon;
        var yDiff = a.Lat - b.Lat;
        if (p == 1)
            return Math.Abs(xDiff) + Math.Abs(yDiff);
        return Math.Pow(Math.Pow(xDiff, p) + Math.Pow(yDiff, p), 1.0 / p);
    }
}
