# Contrato: API pública de transformation/mutation (Fase 1)

Novas funções na fachada **`Geo`** (`Turfano.GeoJson`), recebendo/retornando
`Turfano.GeoJson` e usando `Turfano.Units`. As `Turf.*.cs` NTS-based permanecem. Esboço:

```csharp
using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    // mutação (US1)
    public static Geometry CleanCoords(Geometry geometry);
    public static Geometry Flip(Geometry geometry);
    public static Geometry Rewind(Geometry geometry, bool reverse = false);
    public static double Round(double value, int precision = 0);
    public static Geometry Truncate(Geometry geometry, int precision = 6, int coordinates = 3);

    // transformação (US2) — transformScale GEODÉSICO
    public static Geometry TransformRotate(Geometry geometry, Units.Angle angle, Position? pivot = null);
    public static Geometry TransformTranslate(Geometry geometry, Units.Length distance, Units.Angle direction);
    public static Geometry TransformScale(Geometry geometry, double factor, string origin = "centroid");
    public static Geometry Clone(Geometry geometry);

    // geração/suavização (US3)
    public static Polygon Circle(Point center, Units.Length radius, int steps = 64);
    public static LineString BezierSpline(LineString line);
    public static Polygon PolygonSmooth(Polygon polygon, int iterations = 1);
    public static LineString LineOffset(LineString line, Units.Length distance);
    public static Geometry Simplify(Geometry geometry, double tolerance = 1, bool highQuality = false);
}
```

## Invariantes de verificação
- Cada função = `@turf` real (numérico apertado / estrutural) (SC-001).
- `TransformScale` geodésico, não cartesiano (SC-002).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units`; nomes .NET (SC-003).
- Suíte 203 verde; net8/9/10; 0 warnings AOT (SC-004/005).
