using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

// US3 — meta-iteração pública na fachada Geo, fiel à ordem/índices do @turf.
public static partial class Geo
{
    /// <summary>
    /// Itera cada coordenada — `@turf/meta coordEach`. Callback recebe `(coord, coordIndex,
    /// featureIndex, multiFeatureIndex, geometryIndex)`; `coordIndex` é global e crescente.
    /// </summary>
    public static void CoordEach(
        Geometry geometry,
        Action<Position, int, int, int, int> callback,
        bool excludeWrapCoord = false
    )
    {
        var coordIndex = 0;
        CoordEachGeometry(geometry, callback, excludeWrapCoord, featureIndex: 0, ref coordIndex);
    }

    private static void CoordEachGeometry(
        Geometry geometry,
        Action<Position, int, int, int, int> callback,
        bool excludeWrapCoord,
        int featureIndex,
        ref int coordIndex
    )
    {
        switch (geometry)
        {
            case Point p:
                callback(p.Coordinates, coordIndex, featureIndex, 0, 0);
                coordIndex++;
                break;
            case MultiPoint mp:
                for (var j = 0; j < mp.Coordinates.Length; j++)
                {
                    callback(mp.Coordinates[j], coordIndex, featureIndex, j, 0);
                    coordIndex++;
                }
                break;
            case LineString ls:
                foreach (var c in ls.Coordinates)
                {
                    callback(c, coordIndex, featureIndex, 0, 0);
                    coordIndex++;
                }
                break;
            case MultiLineString mls:
                for (var j = 0; j < mls.Coordinates.Length; j++)
                    foreach (var c in mls.Coordinates[j])
                    {
                        callback(c, coordIndex, featureIndex, j, 0);
                        coordIndex++;
                    }
                break;
            case Polygon poly:
            {
                var wrap = excludeWrapCoord ? 1 : 0;
                for (var j = 0; j < poly.Coordinates.Length; j++)
                    for (var k = 0; k < poly.Coordinates[j].Length - wrap; k++)
                    {
                        callback(poly.Coordinates[j][k], coordIndex, featureIndex, 0, j);
                        coordIndex++;
                    }
                break;
            }
            case MultiPolygon mpoly:
            {
                var wrap = excludeWrapCoord ? 1 : 0;
                for (var j = 0; j < mpoly.Coordinates.Length; j++)
                {
                    var geometryIndex = 0;
                    foreach (var ring in mpoly.Coordinates[j])
                    {
                        for (var l = 0; l < ring.Length - wrap; l++)
                        {
                            callback(ring[l], coordIndex, featureIndex, j, geometryIndex);
                            coordIndex++;
                        }
                        geometryIndex++;
                    }
                }
                break;
            }
            case GeometryCollection gc:
                foreach (var g in gc.Geometries)
                    CoordEachGeometry(g, callback, excludeWrapCoord, featureIndex, ref coordIndex);
                break;
        }
    }

    /// <summary>Reduz sobre as coordenadas — `@turf/meta coordReduce`.</summary>
    public static TResult CoordReduce<TResult>(
        Geometry geometry,
        Func<TResult, Position, int, TResult> callback,
        TResult initial
    )
    {
        var accumulator = initial;
        CoordEach(geometry, (coord, coordIndex, _, _, _) => accumulator = callback(accumulator, coord, coordIndex));
        return accumulator;
    }

    /// <summary>
    /// Itera cada segmento — `@turf/meta segmentEach`. Callback recebe `(segment,
    /// featureIndex, multiFeatureIndex, geometryIndex, segmentIndex)`.
    /// </summary>
    public static void SegmentEach(
        Geometry geometry,
        Action<(Position Start, Position End), int, int, int, int> callback
    )
    {
        var segmentIndex = 0;
        foreach (var (ring, multiFeatureIndex, geometryIndex) in SegmentRings(geometry))
            for (var i = 0; i < ring.Length - 1; i++)
            {
                callback((ring[i], ring[i + 1]), 0, multiFeatureIndex, geometryIndex, segmentIndex);
                segmentIndex++;
            }
    }

    /// <summary>Reduz sobre os segmentos — `@turf/meta segmentReduce`.</summary>
    public static TResult SegmentReduce<TResult>(
        Geometry geometry,
        Func<TResult, (Position Start, Position End), int, TResult> callback,
        TResult initial
    )
    {
        var accumulator = initial;
        SegmentEach(geometry, (segment, _, _, _, segmentIndex) => accumulator = callback(accumulator, segment, segmentIndex));
        return accumulator;
    }

    private static IEnumerable<(Position[] Ring, int MultiFeatureIndex, int GeometryIndex)> SegmentRings(Geometry g)
    {
        switch (g)
        {
            case LineString ls:
                yield return (ls.Coordinates, 0, 0);
                break;
            case MultiLineString mls:
                for (var j = 0; j < mls.Coordinates.Length; j++)
                    yield return (mls.Coordinates[j], j, 0);
                break;
            case Polygon poly:
                for (var j = 0; j < poly.Coordinates.Length; j++)
                    yield return (poly.Coordinates[j], 0, j);
                break;
            case MultiPolygon mpoly:
                for (var j = 0; j < mpoly.Coordinates.Length; j++)
                    for (var k = 0; k < mpoly.Coordinates[j].Length; k++)
                        yield return (mpoly.Coordinates[j][k], j, k);
                break;
            case GeometryCollection gc:
                foreach (var sub in gc.Geometries)
                    foreach (var ring in SegmentRings(sub))
                        yield return ring;
                break;
        }
    }

    /// <summary>Itera cada feature de uma coleção — `@turf/meta featureEach`.</summary>
    public static void FeatureEach(FeatureCollection collection, Action<Feature, int> callback)
    {
        for (var i = 0; i < collection.Features.Length; i++)
            callback(collection.Features[i], i);
    }

    /// <summary>Itera cada propriedade de uma coleção — `@turf/meta propEach`.</summary>
    public static void PropEach(FeatureCollection collection, Action<JsonObject?, int> callback)
    {
        for (var i = 0; i < collection.Features.Length; i++)
            callback(collection.Features[i].Properties, i);
    }

    /// <summary>Itera cada geometria — `@turf/meta geomEach`. Callback `(geom, featureIndex, geometryIndex)`.</summary>
    public static void GeomEach(GeoJsonObject geojson, Action<Geometry, int, int> callback)
    {
        void Walk(Geometry g, int featureIndex)
        {
            if (g is GeometryCollection gc)
            {
                var geometryIndex = 0;
                foreach (var sub in gc.Geometries)
                {
                    callback(sub, featureIndex, geometryIndex);
                    geometryIndex++;
                }
            }
            else
            {
                callback(g, featureIndex, 0);
            }
        }

        switch (geojson)
        {
            case FeatureCollection collection:
                for (var i = 0; i < collection.Features.Length; i++)
                    if (collection.Features[i].Geometry is { } g)
                        Walk(g, i);
                break;
            case Feature feature:
                if (feature.Geometry is { } fg)
                    Walk(fg, 0);
                break;
            case Geometry geometry:
                Walk(geometry, 0);
                break;
        }
    }

    /// <summary>Itera cada parte simples — `@turf/meta flattenEach`. Callback `(geom, featureIndex, multiFeatureIndex)`.</summary>
    public static void FlattenEach(GeoJsonObject geojson, Action<Geometry, int, int> callback)
    {
        void Walk(Geometry g, int featureIndex)
        {
            var multiFeatureIndex = 0;
            foreach (var part in FlattenGeometry(g))
            {
                callback(part, featureIndex, multiFeatureIndex);
                multiFeatureIndex++;
            }
        }

        switch (geojson)
        {
            case FeatureCollection collection:
                for (var i = 0; i < collection.Features.Length; i++)
                    if (collection.Features[i].Geometry is { } g)
                        Walk(g, i);
                break;
            case Feature feature:
                if (feature.Geometry is { } fg)
                    Walk(fg, 0);
                break;
            case Geometry geometry:
                Walk(geometry, 0);
                break;
        }
    }
}
