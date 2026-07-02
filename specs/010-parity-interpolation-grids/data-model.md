# Data Model: Onda F — Interpolation, Grids & Triangulation

**Data**: 2026-07-02 | **Feature**: `010-parity-interpolation-grids`

Sem entidades públicas novas — a onda adiciona **funções** na fachada `Geo` sobre os tipos
da Fase 3. As estruturas abaixo são **internas** aos motores portados.

## Grades (US1)

Sem estado: geradores puros bbox→células. Passo em graus derivado do tamanho da célula
(`cellSide` em `Units.Length` → `distance`-equivalente como no `@turf`: as grades do
`@turf` convertem `cellSide` via `lengthToDegrees`? NÃO — medem `distance` entre cantos
da bbox e dividem; portar exatamente a aritmética de cada módulo).

## Delaunay do tin (US2) — estruturas do `@turf/tin`

- `TinTriangle` (interno): índices `a/b/c` + circumcírculo (`x`, `y`, `r`) — o algoritmo é
  o incremental clássico (Bourke): supertriângulo, inserção ponto-a-ponto, buffer de
  arestas com remoção de duplicatas, filtro final do supertriângulo.
- Propriedades de saída: `a`/`b`/`c` = valores `z` dos vértices (de `properties[z]` ou da
  3ª coordenada), como no `@turf`.

## Marching squares (US3) — embutido nos módulos

- `isolines`: grade `z[y][x]` a partir dos pontos (ordenados por lat/lon), função
  `isoContours`-like embutida no bundle → multilinhas por break; reescala das coordenadas
  da grade para a bbox real.
- `isobands`: idem com pares de breaks (lower/upper) → polígonos por faixa; inclui
  `groupNestedPolygons` (anéis internos viram furos, via `booleanPointInPolygon`).
- Estruturas internas seguem o bundle (matrizes `double[][]`, listas de caminhos).

## Earcut (US4) — porte 1:1

- `EarcutNode` (interno, lista circular duplamente ligada): `i` (índice), `x`, `y`,
  `prev/next`, `z` (z-order), `prevZ/nextZ`, `steiner`.
- Entrada achatada (`data[]`, `holeIndices[]`, `dim`) como na fonte; saída índices de
  triângulos. O wrapper `Tesselate` monta os `Polygon`s.

## Fortune / d3-voronoi (US4) — porte 1:1

- `Beach`/`Circle` (red-black tree nodes), `Edge` (semi-arestas com clipping pelo
  extent), `Cell` (site + half-edges ordenadas). Estado por execução (como o
  `OperationRun` da Fase 11 — o d3 usa globais de módulo `beaches/circles/edges/cells`
  redefinidos por chamada; no porte viram campos de uma instância `FortuneVoronoi`).
- Saída: polígonos das células (fechados) na ordem dos sites de entrada; células vazias
  (sites fora do extent/duplicados) → feature ausente como no `@turf/voronoi`.

## Validação

Todas as saídas são `Feature`/`FeatureCollection`/geometrias da Fase 3; propriedades em
`JsonObject`. GT estrutural + numérico via harness bun (quickstart).
