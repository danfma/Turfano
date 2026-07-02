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
    /// Filtra as features cujo `properties` contém a chave <paramref name="property"/>
    /// (qualquer valor) — `@turf/clusters getCluster` com filtro string/number.
    /// </summary>
    public static FeatureCollection GetCluster(FeatureCollection geojson, string property)
    {
        var features = geojson.Features.Where(f => f.Properties?.ContainsKey(property) == true).ToArray();
        return new FeatureCollection(features);
    }

    /// <summary>
    /// Filtra as features cujo `properties[property]` é igual a <paramref name="value"/>
    /// — `@turf/clusters getCluster` com filtro objeto de uma chave (ex.: `{cluster: 0}`).
    /// </summary>
    public static FeatureCollection GetCluster(FeatureCollection geojson, string property, JsonNode? value)
    {
        var features = geojson.Features.Where(f => PropertyEquals(f.Properties, property, value)).ToArray();
        return new FeatureCollection(features);
    }

    /// <summary>
    /// Filtra as features cujas properties casam TODAS as chaves/valores de
    /// <paramref name="filter"/> — `@turf/clusters getCluster` com filtro objeto de várias
    /// chaves.
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
    /// Itera cada cluster (agrupado pela propriedade <paramref name="property"/>) —
    /// `@turf/clusters clusterEach`. `clusterValue` é a chave já stringificada, na ordem
    /// de enumeração de `Object.keys` do JS (índices numéricos primeiro, ascendentes;
    /// depois as demais chaves, na ordem em que apareceram).
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
    /// Reduz sobre os clusters com valor inicial explícito — `@turf/clusters
    /// clusterReduce` (ramo "com initialValue": o callback roda para TODOS os clusters,
    /// inclusive o de índice 0).
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
    /// Reduz sobre os clusters SEM valor inicial — `@turf/clusters clusterReduce` (ramo
    /// "sem initialValue": o 1º cluster vira o acumulador diretamente, sem passar pelo
    /// callback; a partir do 2º, o callback roda normalmente).
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
    /// Agrupa os índices das features por `properties[property]`, na ordem de enumeração
    /// de chaves de objeto do JS (`Object.keys`): chaves "índice de array" (string que é a
    /// forma canônica de um inteiro não-negativo, sem sinal/zero à esquerda) primeiro, em
    /// ordem numérica ascendente; depois as demais chaves, na ordem de primeira inserção.
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

    /// <summary>Chave "índice de array" (ECMA-262): forma canônica de um inteiro em [0, 2^32-2].</summary>
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

    /// <summary>Converte um valor de propriedade na chave que o JS usaria (`String(value)`).</summary>
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
