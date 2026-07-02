namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Sorteia <paramref name="num"/> features (sem repetição) de uma coleção — `@turf/sample`
    /// (shuffle parcial de Fisher–Yates, com `Random.Shared` no lugar do `Math.random`). Se
    /// <paramref name="num"/> for maior que a contagem de features, devolve a coleção
    /// inteira (a fonte, nesse caso, cai num comportamento de índice negativo do JS que
    /// termina equivalendo a isso — reproduzido aqui de forma direta).
    /// </summary>
    public static FeatureCollection Sample(FeatureCollection features, int num)
    {
        return new FeatureCollection(GetRandomSubarray(features.Features, num));
    }

    private static Feature[] GetRandomSubarray(Feature[] source, int size)
    {
        var shuffled = (Feature[])source.Clone();
        var length = shuffled.Length;
        var min = Math.Clamp(length - size, 0, length);
        var i = length;

        while (i-- > min)
        {
            var index = (int)Math.Floor((i + 1) * Random.Shared.NextDouble());
            (shuffled[index], shuffled[i]) = (shuffled[i], shuffled[index]);
        }

        return shuffled[min..];
    }
}
