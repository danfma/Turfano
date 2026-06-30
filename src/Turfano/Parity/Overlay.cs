namespace Turfano.GeoJson;

// Onda E — overlay via o motor NTS (decisão MEDIDA da Fase 2: área idêntica ao polyclip-ts do
// @turf). O NTS fica escondido atrás da Turfano.Interop.NtsBridge; as assinaturas só usam os
// tipos próprios. Operação planar em graus (lon/lat), como o @turf.
public static partial class Geo
{
    /// <summary>União de duas geometrias — `@turf/union` (motor NTS via `NtsBridge`).</summary>
    public static Geometry? Union(Geometry a, Geometry b) =>
        FromNtsOrNull(Turfano.Interop.NtsBridge.ToNts(a).Union(Turfano.Interop.NtsBridge.ToNts(b)));

    /// <summary>Diferença `a` − `b` — `@turf/difference` (motor NTS via `NtsBridge`).</summary>
    public static Geometry? Difference(Geometry a, Geometry b) =>
        FromNtsOrNull(Turfano.Interop.NtsBridge.ToNts(a).Difference(Turfano.Interop.NtsBridge.ToNts(b)));

    /// <summary>Interseção de duas geometrias — `@turf/intersect` (motor NTS via `NtsBridge`).</summary>
    public static Geometry? Intersect(Geometry a, Geometry b) =>
        FromNtsOrNull(Turfano.Interop.NtsBridge.ToNts(a).Intersection(Turfano.Interop.NtsBridge.ToNts(b)));

    /// <summary>Une os polígonos de uma coleção que se tocam — `@turf/dissolve` (motor NTS).</summary>
    public static Geometry Dissolve(FeatureCollection polygons)
    {
        NetTopologySuite.Geometries.Geometry? accumulator = null;
        foreach (var feature in polygons.Features)
        {
            if (feature.Geometry is { } geom && (geom is Polygon || geom is MultiPolygon))
            {
                var nts = Turfano.Interop.NtsBridge.ToNts(geom);
                accumulator = accumulator is null ? nts : accumulator.Union(nts);
            }
        }
        return accumulator is null
            ? new GeometryCollection(Array.Empty<Geometry>())
            : Turfano.Interop.NtsBridge.FromNts(accumulator);
    }

    private static Geometry? FromNtsOrNull(NetTopologySuite.Geometries.Geometry result) =>
        result is null || result.IsEmpty ? null : Turfano.Interop.NtsBridge.FromNts(result);
}
