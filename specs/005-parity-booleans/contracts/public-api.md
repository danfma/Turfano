# Contrato: API pública dos predicados booleanos (Fase 1)

Novos predicados na fachada **`Geo`** (`Turfano.GeoJson`), recebendo `Turfano.GeoJson` e
devolvendo `bool`. Os `Turf.Boolean*.cs` NTS-based permanecem (não são fonte — divergem).

```csharp
namespace Turfano.GeoJson;

public static partial class Geo
{
    // ponto / orientação (US1)
    public static bool BooleanPointInPolygon(Point pt, Geometry polygon, bool ignoreBoundary = false);
    public static bool BooleanPointOnLine(Point pt, LineString line, bool ignoreEndVertices = false, double? epsilon = null);
    public static bool BooleanClockwise(LineString ring);
    public static bool BooleanConcave(Polygon polygon);
    public static bool BooleanParallel(LineString a, LineString b);

    // relações (US2) — semântica do @turf, não DE-9IM do NTS
    public static bool BooleanContains(Geometry a, Geometry b);
    public static bool BooleanWithin(Geometry a, Geometry b);
    public static bool BooleanDisjoint(Geometry a, Geometry b);
    public static bool BooleanIntersects(Geometry a, Geometry b);
    public static bool BooleanCrosses(Geometry a, Geometry b);
    public static bool BooleanOverlap(Geometry a, Geometry b);
    public static bool BooleanTouches(Geometry a, Geometry b);
    public static bool BooleanEqual(Geometry a, Geometry b);

    // validade (US3)
    public static bool BooleanValid(Geometry geometry);
}
```

## Invariantes de verificação
- Cada predicado = `@turf` real + fixtures `true/false` (SC-001).
- Ponto na borda: `BooleanPointInPolygon` = `true` (ignoreBoundary=false) / `false`
  (ignoreBoundary=true) (SC-002).
- Assinaturas só com `Turfano.GeoJson` (SC-003); suíte 193 verde; net8/9/10; 0 warnings AOT
  (SC-004/005).
