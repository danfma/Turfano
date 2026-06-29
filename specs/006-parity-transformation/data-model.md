# Data Model: Onda C — Transformation & Mutation (Fase 1)

Sem entidades novas — a onda adiciona **funções** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) e `Turfano.Units.*` na fachada `Geo`. Mapa função → assinatura (esboço):

| Função | Assinatura (esboço) | Retorno |
|---|---|---|
| CleanCoords | `Geo.CleanCoords(Geometry g)` | mesma `Geometry` (limpa) |
| Flip | `Geo.Flip(Geometry g)` | `Geometry` (lon/lat trocados) |
| Rewind | `Geo.Rewind(Geometry g, bool reverse = false)` | `Geometry` |
| Round | `Geo.Round(double value, int precision = 0)` | `double` |
| Truncate | `Geo.Truncate(Geometry g, int precision = 6, int coordinates = 3)` | `Geometry` |
| TransformRotate | `Geo.TransformRotate(Geometry g, Units.Angle angle, Position? pivot = null)` | `Geometry` |
| TransformTranslate | `Geo.TransformTranslate(Geometry g, Units.Length distance, Units.Angle direction)` | `Geometry` |
| TransformScale | `Geo.TransformScale(Geometry g, double factor, string origin = "centroid")` | `Geometry` (geodésico) |
| Clone | `Geo.Clone(Geometry g)` | `Geometry` (cópia profunda) |
| Circle | `Geo.Circle(Point center, Units.Length radius, int steps = 64)` | `Polygon` |
| BezierSpline | `Geo.BezierSpline(LineString line, ...)` | `LineString` |
| PolygonSmooth | `Geo.PolygonSmooth(Polygon poly, int iterations = 1)` | `Polygon` |
| LineOffset | `Geo.LineOffset(LineString line, Units.Length distance)` | `LineString` |
| Simplify | `Geo.Simplify(Geometry g, double tolerance = 1, bool highQuality = false)` | `Geometry` |

**Regras/invariantes**
- Cada função bate com o `@turf` (FR-002).
- `TransformScale` é **geodésico** (FR-003, SC-002).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units` (FR-001/SC-003); nomes .NET (FR-005).
