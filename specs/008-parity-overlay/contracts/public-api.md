# Contrato: API pública de overlay / clipping (Fase 1)

Novas funções na fachada **`Geo`** (`Turfano.GeoJson`). Overlay/buffer delegam ao **NTS** via
`Turfano.Interop.NtsBridge` (NTS **não** aparece nas assinaturas). `bboxClip` é portado. As
`Turf.*.cs` NTS-based permanecem. Esboço:

```csharp
using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    // overlay (US1) — motor NTS via NtsBridge; null se vazio
    public static Geometry? Union(Geometry a, Geometry b);
    public static Geometry? Difference(Geometry a, Geometry b);
    public static Geometry? Intersect(Geometry a, Geometry b);
    public static Geometry Dissolve(FeatureCollection polygons);

    // buffer (US2) — motor NTS (o @turf buffer é JTS=NTS)
    public static Geometry? Buffer(Geometry geometry, Units.Length radius, int steps = 8);

    // clipping (US3) — PORTADO (Cohen-Sutherland), sem NTS
    public static Geometry BBoxClip(Geometry geometry, BBox bbox);
}
```

## Invariantes de verificação
- Overlay/buffer: **área** = `@turf` real dentro da tolerância da Fase 2 (~`1e-5`) (SC-001/002).
- `BBoxClip` = `@turf` estruturalmente (SC-001).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units` (sem NTS) (SC-003); suíte 226 verde;
  net8/9/10 (SC-004); smoke de serialização 0 warnings (SC-005).
```
