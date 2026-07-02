# Research: Onda F — Interpolation, Grids & Triangulation

**Data**: 2026-07-02 | **Feature**: `010-parity-interpolation-grids`

## R1 — Medição das fontes (método da Fase 11: decidir com números)

Medido em `reference/node_modules` (o código que o `@turf` executa de verdade):

| Função | Fonte | Linhas | Deps externas reais | Decisão |
|---|---|---|---|---|
| `pointGrid` | `@turf/point-grid` | 42 | nenhuma (usa `boolean-within` p/ mask) | **porte direto** |
| `squareGrid` | `@turf/square-grid` → `rectangle-grid` | 10 + 55 | nenhuma (`boolean-intersects` p/ mask) | **porte direto** (portar o `rectangleGrid`) |
| `hexGrid` | `@turf/hex-grid` | 113 | nenhuma (`intersect` p/ mask) | **porte direto** (mask via overlay nativo) |
| `triangleGrid` | `@turf/triangle-grid` | 133 | nenhuma (`intersect` p/ mask) | **porte direto** |
| `planepoint` | `@turf/planepoint` | 32 | nenhuma | **porte direto** |
| `tin` | `@turf/tin` | 191 | nenhuma (Delaunay incremental autocontido) | **porte direto** |
| `interpolate` | `@turf/interpolate` | 92 | só grids/centroid/bbox/distance (temos) | **porte direto** |
| `isolines` | `@turf/isolines` | 315 | **NENHUMA** — marching squares **embutido** no bundle | **porte direto** |
| `isobands` | `@turf/isobands` | 508 | **NENHUMA** — idem | **porte direto** |
| `convex` | `@turf/convex` | 24 | `concaveman` (fecho: 383 + rbush 574 + tinyqueue 79 + point-in-polygon 15 + orient2d) | portar **só o hull convexo** (ver R2) |
| `concave` | `@turf/concave` | 197 | `topojson-client`+`server` (fecho 1.343) p/ merge | usar **union nativo** (ver R3) |
| `voronoi` | `@turf/voronoi` | 33 | `d3-voronoi` (1.004, Fortune) | **portar o d3-voronoi** (ver R4) |
| `tesselate` | `@turf/tesselate` | 71 | `earcut` (681, arquivo único) | **portar o earcut 1:1** (ver R5) |

**CORREÇÃO ao spec/premissas**: `isolines`/`isobands` **não** importam a lib
`marchingsquares` — o bundle do `@turf` **embute** o marching squares nos próprios módulos
(315/508 linhas autocontidas). O porte é direto, sem fecho externo.

Estimativa total de porte: **~3,4 mil linhas** (dois portes grandes: earcut 681 e
d3-voronoi ~1.004 — a mesma escala do polyclip da Fase 11, mesmo método).

## R2 — `convex`: portar só o hull convexo do concaveman

**Decision**: `Geo.Convex` porta o **monotone chain** (`fastConvexHull`) interno do
concaveman, sem o parâmetro `concavity` nesta onda.

**Rationale**: o `@turf/convex` chama `concaveman(points, concavity ?? Infinity)`. Com
`concavity = Infinity` o concaveman **nunca "escava"** — devolve exatamente o hull convexo
do seu `fastConvexHull`. Portar o concaveman inteiro (fecho ≈1.050 linhas com rbush/
tinyqueue/orient2d robusto) só serviria ao caminho côncavo, que o `@turf/concave` **nem
usa** (usa tin+dissolve). GT valida a igualdade.

**Alternatives considered**: fecho completo do concaveman (rejeitado: ~1.050 linhas para
um parâmetro que o default nunca exercita); NTS ConvexHull (proibido — Parity/ é zona
livre de NTS).

## R3 — `concave`: dissolver triângulos com o union nativo

**Decision**: `Geo.Concave` = `tin` + filtro por `maxEdge` + **união n-ária via o motor
polyclip nativo** (Fase 11), em vez de portar `topojson-client`+`topojson-server`
(fecho 1.343 linhas) usados pelo `@turf` só para fundir os triângulos.

**Rationale**: o merge topológico do topojson e a união booleana produzem a **mesma
região** quando as peças compartilham arestas exatas (caso do tin). Já temos o motor
exato. 1.343 linhas evitadas.

**Risco registrado**: o merge do topojson preserva vértices colineares na borda; o
`RingOut` do polyclip os **remove**. A validação por GT compara **área + contenção de
vértices** (não igualdade byte-a-byte). Se algum caso real exigir topologia idêntica,
reavaliar o porte do topojson (registrar em nova decisão — não nesta onda).

## R4 — `voronoi`: portar o d3-voronoi

**Decision**: portar o core do **d3-voronoi 1.1.2** (algoritmo de Fortune: beach line,
RB-tree, células cortadas pelo extent) — ~1.004 linhas em `src/`.

**Rationale**: é a fonte que o `@turf` executa (pinned); determinística (sem
Math.random), float puro — porte 1:1 dá paridade por construção, como o polyclip.
Não há alternativa menor com paridade exata (d3-delaunay é outra lib, outra saída).

**Alternatives considered**: Bowyer-Watson próprio + dual (rejeitado: saída difere
estruturalmente do d3 → quebra o SC-001); NTS VoronoiDiagramBuilder (proibido em Parity/;
e diverge do @turf — motivo de o legado ser "quebrado").

## R5 — `tesselate`: portar o earcut

**Decision**: portar o **earcut 3.x** (681 linhas, arquivo único, MIT © Mapbox) 1:1.

**Rationale**: autocontido, determinístico, amplamente testado; o `@turf/tesselate` é um
wrapper fino (71 linhas) sobre ele. Adicionar atribuição ao `NOTICE` (ISC/MIT do earcut).

## R6 — Reuso da fachada existente (nada a criar)

- Mask das grades: `BooleanWithin` (point-grid), `BooleanIntersects` (rectangle/square),
  `Intersect` nativo (hex/triangle) — **todos já existem** (Ondas B/E + Fase 11).
- `interpolate`: `Bbox`, `Centroid`, `Distance` + grades da US1.
- `concave`: `Distance` (filtro maxEdge), `tin` (US2), `Union` nativo.
- Propriedades de features (`z` do tin/interpolate): `JsonObject` (Fase 3).

## R7 — Sequência de implementação

US1 grades → US2 (planepoint → tin → interpolate) → US3 (isolines → isobands) → US4
(convex → concave → tesselate/earcut → voronoi/d3). GT por função via harness bun
(`reference/_wavef.mjs`, efêmero). Os dois portes grandes (earcut, d3-voronoi) ficam por
último — o resto da onda não depende deles.
