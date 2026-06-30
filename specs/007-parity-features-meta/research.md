# Research: Onda D — Feature Conversion, Joins & Meta (Fase 0)

Sem `[NEEDS CLARIFICATION]`. Decisões (estratégia **mista por função**, validar TUDO vs `@turf`):

## Decisão 1 — Conversões estruturais (US1): reshape direto

`explode` (geometria → `FeatureCollection` de `Point`, um por vértice — reusar
`EachPosition`), `flatten` (`Multi*` → partes simples; reusar `FlattenGeometry`), `combine`
(coleção de `Point`/`LineString`/`Polygon` → `Multi*`), `polygonToLine` (anéis → `LineString`/
`MultiLineString`), `lineToPolygon` (linha(s) → `Polygon`/`MultiPolygon`, fechando se preciso).
São reshapes de coordenadas — portar o algoritmo do `@turf` direto sobre os novos tipos.

## Decisão 2 — `polygonize` (a mais algorítmica)

Montar polígonos a partir de uma rede de linhas. **Decisão**: tentar a ponte interina
`Turfano.Interop.NtsBridge` (NTS `Polygonizer`) e **validar contra o `@turf`**; se a
semântica casar, usar; senão, portar o algoritmo do `@turf/polygonize`. Registrar a escolha.

## Decisão 3 — Joins (US2): reusar a fronteira da Onda B

`pointsWithinPolygon` = filtra os pontos com `BooleanPointInPolygon` (semântica de fronteira
do `@turf`, já portada). `tag` = para cada ponto, se dentro de um polígono da coleção,
copia a propriedade nomeada do polígono para o ponto (reusar `BooleanPointInPolygon`).

## Decisão 4 — Utilitários de linha (US2): reusar Distance/Along/NearestPointOnLine

`lineSlice(start, stop, line)` = projeta `start`/`stop` na linha (`NearestPointOnLine`) e
recorta entre eles. `lineSliceAlong(line, startDist, stopDist)` = recorta por distâncias
(reusar `Along`/`Distance`). `lineChunk(line, length)` = quebra em pedaços de comprimento
(reusar `Along` em passos). `nearestPoint(target, coleção)` = `min` de `Distance`.
`kinks(line/poly)` = auto-interseções via `SegmentsIntersect` (Onda B) entre pares de
segmentos não adjacentes.

## Decisão 5 — Meta-iteração pública (US3): índices do `@turf`

Expor `CoordEach`/`CoordReduce`, `FeatureEach`, `GeomEach`, `PropEach`, `SegmentEach`/
`SegmentReduce`, `FlattenEach` na fachada `Geo`, espelhando os helpers `internal`
(`EachPosition`/`EachSegment`/`FlattenGeometry`) mas com a **assinatura/índices do `@turf`**:
`coordEach(callback(coord, coordIndex, featureIndex, multiFeatureIndex, geometryIndex))`,
`segmentEach(callback(segment, featureIndex, multiFeatureIndex, geometryIndex, segmentIndex))`.
**Conferir a ordem e os índices** contra o `@turf` (não presumir). `excludeWrapCoord` em
`coordEach`.

## Decisão 6 — Harness de validação

Harness Bun emitindo as saídas do `@turf` por função/fixture (geometrias/coleções
serializadas p/ comparação estrutural; números p/ distâncias; **sequências de coord/índices**
para as meta-funções).

## Riscos

- `polygonize` e `kinks`: conferir o algoritmo exato do `@turf`
  (`reference/node_modules/@turf/{polygonize,kinks}`); decidir NTS-interino vs porte.
- Meta: os **índices** (`multiFeatureIndex`/`geometryIndex`/`segmentIndex`) são sutis —
  validar contra o `@turf`, é o ponto do SC-002.
