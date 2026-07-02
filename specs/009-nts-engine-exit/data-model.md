# Data Model: Saída do motor NTS — leva 1 (Fase 1)

Sem entidades públicas novas — as assinaturas de `Geo.Union/Difference/Intersect/Dissolve/
Polygonize` **não mudam** (só o motor). Tipos novos:

## Motor polyclip (todos `internal`, `src/Turfano/Parity/Polyclip/`)

| Tipo | Origem (bundle) | Papel |
|---|---|---|
| `ExactDecimal` | substitui `bignumber.js` | decimal exato: `BigInteger` mantissa + expoente; `Add/Subtract/Multiply/CompareTo/Abs/Square/IsZero` |
| `SplayTreeSet<T>` | `splaytree-ts` | conjunto ordenado com comparator; fila de eventos + status do sweep + snap |
| `PolyclipPrecision` | `precision/compare/orient/snap` | comparadores e predicado de orientação (exatos; caminho com eps portado) |
| `PolyclipVector`, `PolyclipBBox` | `vector/bbox` | operações vetoriais e envelopes em `ExactDecimal` |
| `RingIn`, `PolyIn`, `MultiPolyIn` | `geom-in` | normalização da entrada (`Position[][]` → estruturas do sweep) |
| `SweepEvent` | `sweep-event` | evento (ponto, esquerda/direita, link, comparators) |
| `Segment` | `segment` | segmento com flags de anel/winding, split em interseções |
| `SweepLine` | `sweep-line` | status da varredura (SplayTree de segmentos) |
| `PolyclipOperation` | `operation` | executa `union/intersection/difference/xor` |
| `RingOut`, `PolyOut`, `MultiPolyOut` | `geom-out` | reconstrução dos anéis → `Position[][][]` |

## Polygonize nativo (`internal`)

| Tipo | Origem | Papel |
|---|---|---|
| `PolygonizeGraph` (+ `Node`/`Edge`) | `@turf/polygonize` | grafo de arestas, remoção de dangles/cut-edges, extração de anéis |

## Satélite `Turfano.NetTopologySuite` (públicos)

| Tipo | Papel |
|---|---|
| `NtsConvert` (estático) | `ToNts(Turfano.GeoJson.Geometry) → NTS Geometry` e `FromNts(NTS Geometry) → Turfano.GeoJson.Geometry` (+ `Position`↔`Coordinate`); fronteira **empacotada** (`PackedDoubleCoordinateSequence`, `GetRawCoordinates` fast-path, `GetOrdinate` fallback) |
| `NtsGeometryExtensions` (estático) | `Buffer(this Turfano.GeoJson.Geometry, Units.Length radius, int steps = 8) → Geometry?` (pipeline AEQD → NTS Buffer → desprojeção, como na Onda E) |

**Regras/invariantes**
- Âncoras das Ondas D/E inalteradas (FR-001/002); `Parity/` sem NTS (FR-003).
- Fronteira do satélite sem objetos `Coordinate` (FR-004); `UnsafeAccessor` proibido (FR-005).
- `NOTICE` com MIT (`polyclip-ts`, TurfJS) e BSD-3 (`splaytree-ts`) (FR-006).
