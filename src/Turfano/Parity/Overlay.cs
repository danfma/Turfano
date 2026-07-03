using Turfano.GeoJson.Polyclip;

namespace Turfano.GeoJson;

// Fase 11 (leva 1) — overlay NATIVO via o porte do polyclip-ts (Martinez–Rueda, o motor que
// o @turf executa). Sem NTS no caminho: a fidelidade é por construção e é validada por
// regressão pelas âncoras de área da Onda E (valores do @turf pinados nos testes).
public static partial class Geo
{
    /// <summary>Union of two geometries — `@turf/union` (ported polyclip engine).</summary>
    public static Geometry? Union(Geometry a, Geometry b) =>
        RunOverlay(PolyclipOperationType.Union, a, b);

    /// <summary>Difference `a` − `b` — `@turf/difference` (ported polyclip engine).</summary>
    public static Geometry? Difference(Geometry a, Geometry b) =>
        RunOverlay(PolyclipOperationType.Difference, a, b);

    /// <summary>Intersection of two geometries — `@turf/intersect` (ported polyclip engine).</summary>
    public static Geometry? Intersect(Geometry a, Geometry b) =>
        RunOverlay(PolyclipOperationType.Intersection, a, b);

    /// <summary>Unions the touching polygons of a collection — `@turf/dissolve` (n-ary union
    /// of the polyclip engine, as @turf does).</summary>
    public static Geometry Dissolve(FeatureCollection polygons)
    {
        var geometries = new List<Position[][][]>();
        foreach (var feature in polygons.Features)
        {
            if (feature.Geometry is { } geometry && (geometry is Polygon || geometry is MultiPolygon))
                geometries.Add(ToMultiPolygonCoordinates(geometry));
        }

        if (geometries.Count == 0)
            return new GeometryCollection(Array.Empty<Geometry>());

        var result = new OperationRun().Run(
            PolyclipOperationType.Union,
            geometries[0],
            geometries.Skip(1).ToArray()
        );
        return FromOverlayResult(result) ?? new GeometryCollection(Array.Empty<Geometry>());
    }

    private static Geometry? RunOverlay(PolyclipOperationType type, Geometry a, Geometry b)
    {
        var result = new OperationRun().Run(
            type,
            ToMultiPolygonCoordinates(a),
            new[] { ToMultiPolygonCoordinates(b) }
        );
        return FromOverlayResult(result);
    }

    private static Position[][][] ToMultiPolygonCoordinates(Geometry geometry) =>
        geometry switch
        {
            Polygon polygon => new[] { polygon.Coordinates },
            MultiPolygon multiPolygon => multiPolygon.Coordinates,
            _ => throw new ArgumentException(
                "Overlay espera Polygon ou MultiPolygon",
                nameof(geometry)
            ),
        };

    private static Geometry? FromOverlayResult(Position[][][] result) =>
        result.Length switch
        {
            0 => null,
            1 => new Polygon(result[0]),
            _ => new MultiPolygon(result),
        };
}
