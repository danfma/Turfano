namespace Turfano.GeoJson;

/// <summary>
/// Wrapper fino do rbush para features — porte do `@turf/geojson-rbush` (R5): a bbox de
/// cada feature vem da geometria; `Search` preserva a ordem da árvore. Infra interna
/// (não é API pública do Turfano).
/// </summary>
internal sealed class GeoJsonSpatialIndex
{
    private readonly RBushIndex<Feature> tree;
    private readonly Dictionary<Feature, RBushItem<Feature>> itemsByFeature = new();

    public GeoJsonSpatialIndex(int maxEntries = 9) => tree = new RBushIndex<Feature>(maxEntries);

    private static RBushItem<Feature> ToItem(Feature feature)
    {
        var bbox = Geo.Bbox(feature.Geometry!);
        return new RBushItem<Feature>(bbox.Values[0], bbox.Values[1], bbox.Values[2], bbox.Values[3], feature);
    }

    public void Insert(Feature feature)
    {
        var item = ToItem(feature);
        itemsByFeature[feature] = item;
        tree.Insert(item);
    }

    public void Load(IEnumerable<Feature> features)
    {
        var items = new List<RBushItem<Feature>>();
        foreach (var feature in features)
        {
            var item = ToItem(feature);
            itemsByFeature[feature] = item;
            items.Add(item);
        }
        tree.Load(items);
    }

    public void Remove(Feature feature)
    {
        if (itemsByFeature.Remove(feature, out var item))
            tree.Remove(item);
    }

    /// <summary>Features cuja bbox intersecta a da geometria dada, na ordem da árvore.</summary>
    public List<Feature> Search(Geometry geometry)
    {
        var bbox = Geo.Bbox(geometry);
        var box = new SpatialBox
        {
            MinX = bbox.Values[0],
            MinY = bbox.Values[1],
            MaxX = bbox.Values[2],
            MaxY = bbox.Values[3],
        };
        return tree.Search(box).Select(item => item.Item).ToList();
    }
}
