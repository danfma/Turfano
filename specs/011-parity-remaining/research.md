# Research: Onda G — Paridade total (pacotes restantes)

**Data**: 2026-07-02 | **Feature**: `011-parity-remaining`

## R1 — Cruzamento de cobertura (feito antes do spec)

115 módulos `@turf` × fachada `Geo` → **27 funções faltantes** (FR-001), mais duas
exclusões explícitas: `buffer` (satélite `Turfano.NetTopologySuite`, decisão da Fase 11)
e `geojson-rbush` (infra, não é função — vira helper interno, R5). Nenhum outro gap.

## R2 — rbush: portar 1:1 (índice espacial interno)

**Decision**: portar o **rbush 3.x** (574 linhas, MIT © Vladimir Agafonkin) como
`internal` (`Parity/Spatial/`), usado por `clustersDbscan`, `collect`, `unkinkPolygon` e
pelo wrapper R5.

**Rationale**: a **ordem** dos resultados de `search()` depende da estrutura da árvore
(inserção + splits) e os consumidores são sensíveis a ela (a expansão BFS do dbscan define
os ids dos clusters; o line-overlap concatena na ordem retornada). Uma varredura
brute-force equivalente NÃO preserva a ordem → só o porte 1:1 garante paridade.

**Alternatives considered**: varredura linear (rejeitada: ordem diferente → ids/ordem de
saída divergem do `@turf`); árvore .NET pronta (rejeitada: mesma razão).

## R3 — skmeans: portar SÓ o caminho executado (determinístico!)

**Decision**: o `@turf/clusters-kmeans` chama `skmeans(data, k, initialCentroids)` com
**`initialCentroids = data.slice(0, k)`** — os k primeiros pontos. Com centroides dados, o
skmeans roda Lloyd puro (assign + recompute até convergir), **sem Math.random**. Portamos
apenas esse caminho (~100 das ~500 linhas) como `internal KMeans`.

**Rationale**: determinístico → GT exato; o resto do skmeans (init aleatório/kmpp/testes
de distância custom) nunca é executado pelo `@turf` (mesmo racional do R2 da Onda F).

## R4 — sweepline-intersections: portar 1:1

**Decision**: portar o **sweepline-intersections 1.5** (~530 linhas, MIT) como motor do
`lineIntersect` (`Parity/Spatial/`), exatamente como o `@turf` o usa.

**Alternatives considered**: interseção par-a-par própria (rejeitada: ordem/dedup dos
pontos de saída divergiria).

## R5 — geojson-rbush: wrapper interno fino

**Decision**: helper `internal` sobre o rbush portado (bbox de features), usado por
`lineOverlap` e `lineSplit`. Não é API pública.

## R6 — Aleatoriedade (random*, sample)

**Decision**: mesmo contrato do `@turf` (sem seed público); implementação com
`Random.Shared`. Testes **estruturais**: contagem, contenção na bbox, anéis fechados,
`num_vertices`/`max_radial_length` respeitados. `sample` usa shuffle — testar contagem e
subconjunto. Documentado como critério de aceite da US4.

## R7 — mask: reuso do motor polyclip nativo

**Decision**: o `@turf/mask` (69 linhas) usa `polyclip.union` + anel-mundo com furos —
implementar sobre o `OperationRun` da Fase 11 (mesmo motor, mesma saída).

## R8 — projection (toMercator/toWgs84): porte direto

54 linhas, matemática fechada (Web Mercator esférico com clamp). Última função do legado
sem versão `Geo` → **cumpre o pré-requisito da leva 2**.

## R9 — unkink-polygon: autocontido após R2

571 linhas com o **simplepolygon inlined** no bundle (Fraeye) + rbush. Porta-se após o
rbush. Nenhuma outra dependência.

## R10 — shortest-path e estatística: só reuso

`shortestPath` (390) depende apenas de `Bbox`/`BooleanPointInPolygon`/`Distance`/
`TransformScale`/`CleanCoords`/`BboxPolygon` — **todas já existem**. As 8 funções de
estatística (US3) são matemática pura sobre `Distance`/`Centroid`/etc. Porte direto.

## Licenças (NOTICE)

rbush MIT, skmeans MIT, sweepline-intersections MIT — adicionar ao `NOTICE` os que forem
portados 1:1 (rbush, sweepline-intersections; skmeans parcial com atribuição).

## Sequência

US2 primeiro `projection`+`mask` (ganhos rápidos, leva 2), depois US1 (rbush → line-*),
US3 (estatística), US4 (kmeans/dbscan/collect/random). GT antes de cada porte
(`reference/_waveg.mjs`, efêmero).
