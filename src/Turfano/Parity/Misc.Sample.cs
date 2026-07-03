namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Draws <paramref name="num"/> features (without repetition) from a collection — `@turf/sample`
    /// (partial Fisher–Yates shuffle, with `Random.Shared` in place of `Math.random`). If
    /// <paramref name="num"/> is greater than the feature count, returns the whole
    /// collection (in that case, the source falls into a JS negative-index behavior that
    /// ends up equivalent — reproduced here directly).
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
