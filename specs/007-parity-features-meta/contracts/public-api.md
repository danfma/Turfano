# Contrato: API pública de feature conversion / joins / misc / meta (Fase 1)

Novas funções na fachada **`Geo`** (`Turfano.GeoJson`). As `Turf.*.cs` NTS-based permanecem.
Esboço:

```csharp
using Units = Turfano.Units;
using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    // conversão (US1)
    public static FeatureCollection Explode(Geometry geometry);
    public static FeatureCollection Flatten(Geometry geometry);
    public static FeatureCollection Combine(FeatureCollection collection);
    public static Geometry PolygonToLine(Geometry polygon);   // LineString ou MultiLineString
    public static Geometry LineToPolygon(Geometry line);      // Polygon ou MultiPolygon
    public static FeatureCollection Polygonize(Geometry lines);

    // joins (US2)
    public static FeatureCollection PointsWithinPolygon(FeatureCollection points, Geometry polygon);
    public static FeatureCollection Tag(FeatureCollection points, FeatureCollection polygons, string field, string outField);

    // misc (US2)
    public static Feature NearestPoint(Point target, FeatureCollection points);
    public static LineString LineSlice(Point start, Point stop, LineString line);
    public static LineString LineSliceAlong(LineString line, Units.Length start, Units.Length stop);
    public static FeatureCollection LineChunk(LineString line, Units.Length length);
    public static FeatureCollection Kinks(Geometry geometry);

    // meta (US3) — ordem e índices do @turf
    public static void CoordEach(Geometry geometry, Action<Position, int, int, int, int> callback, bool excludeWrapCoord = false);
    public static TResult CoordReduce<TResult>(Geometry geometry, Func<TResult, Position, int, TResult> callback, TResult initial);
    public static void FeatureEach(FeatureCollection collection, Action<Feature, int> callback);
    public static void GeomEach(GeoJsonObject geojson, Action<Geometry, int, int> callback);
    public static void PropEach(FeatureCollection collection, Action<JsonObject?, int> callback);
    public static void SegmentEach(Geometry geometry, Action<(Position, Position), int, int, int, int> callback);
    public static void FlattenEach(GeoJsonObject geojson, Action<Geometry, int, int> callback);
}
```

## Invariantes de verificação
- Cada função = `@turf` real (estrutural/numérico) (SC-001).
- Meta: ordem e índices iguais aos do `@turf` (SC-002).
- `PointsWithinPolygon` filtra como o `@turf`, incl. fronteira (SC-003).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units` (SC-004); suíte 215 verde; net8/9/10;
  0 warnings AOT (SC-005).
```
