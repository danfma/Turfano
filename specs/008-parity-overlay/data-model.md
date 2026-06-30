# Data Model: Onda E — Overlay / Clipping (Fase 1)

Sem entidades de dados novas — a onda adiciona **funções** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) e `Turfano.Units.*` na fachada `Geo`, com o NTS como motor interino
(escondido na `NtsBridge`). Mapa função → assinatura (esboço):

| Função | Assinatura (esboço) | Retorno | Motor |
|---|---|---|---|
| Union | `Geo.Union(Geometry a, Geometry b)` | `Geometry?` (Polygon/MultiPolygon) | NTS |
| Difference | `Geo.Difference(Geometry a, Geometry b)` | `Geometry?` | NTS |
| Intersect | `Geo.Intersect(Geometry a, Geometry b)` | `Geometry?` | NTS |
| Dissolve | `Geo.Dissolve(FeatureCollection polygons)` | `Geometry` (Multi*) | NTS |
| Buffer | `Geo.Buffer(Geometry g, Units.Length radius, int steps = 8)` | `Geometry?` (Polygon/MultiPolygon) | NTS |
| BBoxClip | `Geo.BBoxClip(Geometry g, BBox bbox)` | `Geometry` (recortada) | **portado** |

**Regras/invariantes**
- Overlay/buffer: **área** bate com o `@turf` dentro de tolerância (FR-003, SC-002); resultado
  vazio do NTS → `null` (como o `@turf`).
- `BBoxClip` portado (Cohen-Sutherland), estrutura igual ao `@turf` (FR-002).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units`; **NTS escondido** (FR-001/SC-003);
  nomes .NET.
