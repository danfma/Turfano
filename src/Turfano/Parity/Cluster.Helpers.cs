using System.Globalization;
using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

// US4 — helpers de cluster (`@turf/clusters`: getCluster/clusterEach/clusterReduce). A
// API JS aceita um `filter` de tipo livre (número/string = existência da chave; objeto =
// igualdade por chave/valor); aqui isso vira sobrecargas idiomáticas em C#. O quirk fino
// (confirmado no GT, `reference/_waveg_us4.mjs`): `clusterEach`/`clusterReduce` agrupam por
// `Object.keys(bins)` — chaves "índice de array" (inteiros não-negativos canônicos, ex.:
// cluster 0/1/2) enumeram em ORDEM NUMÉRICA, não na ordem de inserção; e o `clusterValue`
// entregue ao callback é sempre a chave JÁ STRINGIFICADA (nunca o valor original tipado).
public static partial class Geo
{
    /// <summary>
    /// Filters features whose `properties` contains the key <paramref name="property"/>
    /// (any value) — `@turf/clusters getCluster` with a string/number filter.
    /// </summary>
    public static FeatureCollection GetCluster(FeatureCollection geojson, string property)
    {
        var features = geojson.Features.Where(f => f.Properties?.ContainsKey(property) == true).ToArray();
        return new FeatureCollection(features);
    }

    /// <summary>
    /// Filters features whose `properties[property]` equals <paramref name="value"/>
    /// — `@turf/clusters getCluster` with a single-key object filter (e.g., `{cluster: 0}`).
    /// </summary>
    public static FeatureCollection GetCluster(FeatureCollection geojson, string property, JsonNode? value)
    {
        var features = geojson.Features.Where(f => PropertyEquals(f.Properties, property, value)).ToArray();
        return new FeatureCollection(features);
    }

    /// <summary>
    /// Filters features whose properties match ALL the keys/values of
    /// <paramref name="filter"/> — `@turf/clusters getCluster` with a multi-key object
    /// filter.
    /// </summary>
    public static FeatureCollection GetCluster(FeatureCollection geojson, IReadOnlyDictionary<string, JsonNode?> filter)
    {
        var features = geojson
            .Features.Where(f => filter.All(kv => PropertyEquals(f.Properties, kv.Key, kv.Value)))
            .ToArray();
        return new FeatureCollection(features);
    }

    private static bool PropertyEquals(JsonObject? properties, string key, JsonNode? value)
    {
        if (properties is null)
            return false;
        return JsonNode.DeepEquals(properties[key], value);
    }

    /// <summary>
    /// Iterates over each cluster (grouped by the <paramref name="property"/> property) —
    /// `@turf/clusters clusterEach`. `clusterValue` is the already-stringified key, in the
    /// JS `Object.keys` enumeration order (numeric indices first, ascending; then the
    /// remaining keys, in the order they appeared).
    /// </summary>
    public static void ClusterEach(FeatureCollection geojson, string property, Action<FeatureCollection, string, int> callback)
    {
        var bins = CreateBins(geojson, property);
        for (var index = 0; index < bins.Count; index++)
        {
            var (value, indices) = bins[index];
            var features = new Feature[indices.Count];
            for (var i = 0; i < indices.Count; i++)
                features[i] = geojson.Features[indices[i]];
            callback(new FeatureCollection(features), value, index);
        }
    }

    /// <summary>
    /// Reduces over the clusters with an explicit initial value — `@turf/clusters
    /// clusterReduce` (the "with initialValue" branch: the callback runs for ALL clusters,
    /// including the one at index 0).
    /// </summary>
    public static TResult ClusterReduce<TResult>(
        FeatureCollection geojson,
        string property,
        Func<TResult, FeatureCollection, string, int, TResult> callback,
        TResult initialValue
    )
    {
        var previousValue = initialValue;
        ClusterEach(
            geojson,
            property,
            (cluster, clusterValue, currentIndex) => previousValue = callback(previousValue, cluster, clusterValue, currentIndex)
        );
        return previousValue;
    }

    /// <summary>
    /// Reduces over the clusters WITHOUT an initial value — `@turf/clusters clusterReduce`
    /// (the "without initialValue" branch: the 1st cluster becomes the accumulator
    /// directly, without going through the callback; from the 2nd cluster on, the callback
    /// runs normally).
    /// </summary>
    public static FeatureCollection ClusterReduce(
        FeatureCollection geojson,
        string property,
        Func<FeatureCollection, FeatureCollection, string, int, FeatureCollection> callback
    )
    {
        FeatureCollection? previousValue = null;
        ClusterEach(
            geojson,
            property,
            (cluster, clusterValue, currentIndex) =>
                previousValue = currentIndex == 0 ? cluster : callback(previousValue!, cluster, clusterValue, currentIndex)
        );
        return previousValue ?? new FeatureCollection(Array.Empty<Feature>());
    }

    /// <summary>
    /// Groups the feature indices by `properties[property]`, in the JS object-key
    /// enumeration order (`Object.keys`): "array index" keys (a string that is the
    /// canonical form of a non-negative integer, with no sign or leading zero) first, in
    /// ascending numeric order; then the remaining keys, in first-insertion order.
    /// </summary>
    private static List<(string Value, List<int> Indices)> CreateBins(FeatureCollection geojson, string property)
    {
        var insertionOrder = new List<string>();
        var bins = new Dictionary<string, List<int>>();

        for (var i = 0; i < geojson.Features.Length; i++)
        {
            var properties = geojson.Features[i].Properties;
            if (properties is null || !properties.ContainsKey(property))
                continue;

            var key = ToJsPropertyKey(properties[property]);
            if (!bins.TryGetValue(key, out var indices))
            {
                indices = new List<int>();
                bins[key] = indices;
                insertionOrder.Add(key);
            }
            indices.Add(i);
        }

        var indexKeys = new List<(uint Index, string Key)>();
        var stringKeys = new List<string>();
        foreach (var key in insertionOrder)
        {
            if (IsArrayIndexKey(key, out var index))
                indexKeys.Add((index, key));
            else
                stringKeys.Add(key);
        }
        indexKeys.Sort((a, b) => a.Index.CompareTo(b.Index));

        var result = new List<(string, List<int>)>(insertionOrder.Count);
        foreach (var (_, key) in indexKeys)
            result.Add((key, bins[key]));
        foreach (var key in stringKeys)
            result.Add((key, bins[key]));
        return result;
    }

    /// <summary>"Array index" key (ECMA-262): canonical form of an integer in [0, 2^32-2].</summary>
    private static bool IsArrayIndexKey(string key, out uint index)
    {
        index = 0;
        if (key.Length == 0 || key.Length > 10)
            return false;
        if (key[0] == '0')
        {
            // "0" é índice válido; "01" etc. têm zero à esquerda e não são canônicos.
            index = 0;
            return key.Length == 1;
        }
        if (key[0] is < '1' or > '9')
            return false;
        if (!uint.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
            return false;
        if (parsed == uint.MaxValue) // 2^32 - 1 não é índice de array válido
            return false;
        if (parsed.ToString(CultureInfo.InvariantCulture) != key)
            return false;
        index = parsed;
        return true;
    }

    /// <summary>Converts a property value into the key that JS would use (`String(value)`).</summary>
    private static string ToJsPropertyKey(JsonNode? value)
    {
        if (value is null)
            return "null";
        if (value is JsonValue jsonValue)
        {
            if (jsonValue.GetValueKind() == System.Text.Json.JsonValueKind.True)
                return "true";
            if (jsonValue.GetValueKind() == System.Text.Json.JsonValueKind.False)
                return "false";
            if (jsonValue.GetValueKind() == System.Text.Json.JsonValueKind.String)
                return jsonValue.GetValue<string>();
            var number = NumberOrNull(value);
            if (number is { } n)
                return FormatJsNumber(n);
        }
        return value.ToString();
    }

    private static string FormatJsNumber(double n)
    {
        if (double.IsNaN(n))
            return "NaN";
        if (double.IsPositiveInfinity(n))
            return "Infinity";
        if (double.IsNegativeInfinity(n))
            return "-Infinity";
        if (n == Math.Truncate(n) && Math.Abs(n) < 1e15)
            return ((long)n).ToString(CultureInfo.InvariantCulture);
        return n.ToString("R", CultureInfo.InvariantCulture);
    }
}
