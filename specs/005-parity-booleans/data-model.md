# Data Model: Onda B — Booleans / Assertions (Fase 1)

Sem entidades novas — a onda adiciona **predicados `bool`** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) na fachada `Geo`. Mapa função → assinatura (esboço):

| Função | Assinatura (esboço) | Retorno |
|---|---|---|
| BooleanPointInPolygon | `Geo.BooleanPointInPolygon(Point pt, Geometry polygon, bool ignoreBoundary = false)` | `bool` |
| BooleanPointOnLine | `Geo.BooleanPointOnLine(Point pt, LineString line, bool ignoreEndVertices = false, double? epsilon = null)` | `bool` |
| BooleanClockwise | `Geo.BooleanClockwise(LineString ring)` | `bool` |
| BooleanConcave | `Geo.BooleanConcave(Polygon polygon)` | `bool` |
| BooleanParallel | `Geo.BooleanParallel(LineString a, LineString b)` | `bool` |
| BooleanContains | `Geo.BooleanContains(Geometry a, Geometry b)` | `bool` |
| BooleanWithin | `Geo.BooleanWithin(Geometry a, Geometry b)` | `bool` |
| BooleanDisjoint | `Geo.BooleanDisjoint(Geometry a, Geometry b)` | `bool` |
| BooleanIntersects | `Geo.BooleanIntersects(Geometry a, Geometry b)` | `bool` |
| BooleanCrosses | `Geo.BooleanCrosses(Geometry a, Geometry b)` | `bool` |
| BooleanOverlap | `Geo.BooleanOverlap(Geometry a, Geometry b)` | `bool` |
| BooleanTouches | `Geo.BooleanTouches(Geometry a, Geometry b)` | `bool` |
| BooleanEqual | `Geo.BooleanEqual(Geometry a, Geometry b)` | `bool` |
| BooleanValid | `Geo.BooleanValid(Geometry geometry)` | `bool` |

**Regras/invariantes**
- Cada predicado bate com o `@turf` (FR-002), inclusive fronteira (FR-003, SC-002).
- Assinaturas só com `Turfano.GeoJson`; opções como parâmetros (FR-001/SC-003).
- `BooleanContains(a,b) == BooleanWithin(b,a)`; `BooleanDisjoint == !BooleanIntersects`.
