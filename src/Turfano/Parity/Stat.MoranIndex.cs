namespace Turfano.GeoJson;

/// <summary>Resultado de <see cref="Geo.MoranIndex"/> — mesmos 4 campos do @turf.</summary>
public sealed record MoranIndexResult(double MoranIndex, double ExpectedMoranIndex, double StdNorm, double ZNorm);

public static partial class Geo
{
    /// <summary>
    /// Índice I de Moran (autocorrelação espacial) — `@turf/moran-index`. &gt; 0: padrão
    /// agrupado; &lt; 0: disperso; ≈ 0: aleatório. Usa <see cref="DistanceWeight"/> como
    /// matriz de pesos (nota: aqui `standardization` tem default `true`, diferente do
    /// `DistanceWeight` isolado, e `threshold` default é 100000 — mesmos defaults do @turf).
    /// </summary>
    public static MoranIndexResult MoranIndex(
        FeatureCollection fc,
        string inputField,
        double threshold = 100000,
        double p = 2,
        bool binary = false,
        double alpha = -1,
        bool standardization = true
    )
    {
        var weight = DistanceWeight(
            fc,
            threshold: threshold,
            p: p,
            binary: binary,
            alpha: alpha,
            standardization: standardization
        );

        var y = fc.Features.Select(f => NumberOrNull(f.Properties?[inputField]) ?? double.NaN).ToArray();
        var n = weight.Length;

        var yMean = y.Average();
        var yVariance = y.Select(v => Math.Pow(v - yMean, 2)).Sum() / y.Length;

        double weightSum = 0,
            s0 = 0,
            s1 = 0,
            s2 = 0;

        for (var i = 0; i < n; i++)
        {
            double subS2 = 0;
            for (var j = 0; j < n; j++)
            {
                weightSum += weight[i][j] * (y[i] - yMean) * (y[j] - yMean);
                s0 += weight[i][j];
                s1 += Math.Pow(weight[i][j] + weight[j][i], 2);
                subS2 += weight[i][j] + weight[j][i];
            }
            s2 += Math.Pow(subS2, 2);
        }
        s1 = 0.5 * s1;

        var moranIndex = weightSum / s0 / yVariance;
        var expectedMoranIndex = -1.0 / (n - 1);
        var vNum = (double)n * n * s1 - n * s2 + 3 * (s0 * s0);
        var vDen = (double)(n - 1) * (n + 1) * (s0 * s0);
        var vNorm = vNum / vDen - expectedMoranIndex * expectedMoranIndex;
        var stdNorm = Math.Sqrt(vNorm);
        var zNorm = (moranIndex - expectedMoranIndex) / stdNorm;

        return new MoranIndexResult(moranIndex, expectedMoranIndex, stdNorm, zNorm);
    }
}
