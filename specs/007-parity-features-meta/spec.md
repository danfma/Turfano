# Feature Specification: Onda D — Feature Conversion, Joins & Meta (paridade)

**Feature Branch**: `007-parity-features-meta`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "Onda D — Feature Conversion, Joins & Meta (Fase 7). Portar as funções de conversão de feature, joins, utilitários e meta-iteração do TurfJS para a fachada `Geo` (sobre `Turfano.GeoJson`), fiéis ao `@turf`. As meta-funções (`internal` hoje) viram públicas com a semântica de iteração/índices do `@turf`."

## User Scenarios & Testing *(mandatory)*

Quarta **onda de paridade**: as funções de **conversão de feature**, **joins**,
**utilitários de linha** e **meta-iteração** do TurfJS passam a existir na fachada `Geo`
(sobre `Turfano.GeoJson`), produzindo as **mesmas saídas do TurfJS**. As Ondas A/B/C já
estão na `main`, na fachada `Geo`; reusar os helpers (`BooleanPointInPolygon`, `Distance`,
`Along`, `NearestPointOnLine`, `EachSegment`, `FlattenGeometry`, `EachPosition`,
`MapPositions`).

### User Story 1 - Conversão de feature (Priority: P1)

Quem usa `explode`, `combine`, `flatten`, `lineToPolygon`, `polygonToLine`, `polygonize`
obtém a mesma geometria/coleção convertida do TurfJS.

**Why this priority**: conversões são base de muitas outras funções e do interop.

**Independent Test**: comparação estrutural com o `@turf` por função.

**Acceptance Scenarios**:

1. **Given** um polígono, **When** chamo `Explode`, **Then** retorno uma `FeatureCollection`
   de `Point` com cada vértice, como o `turf.explode`.
2. **Given** um `Polygon`, **When** chamo `PolygonToLine`, **Then** retorno o(s) anel(éis)
   como `LineString`/`MultiLineString`, como o `turf.polygonToLine`.
3. **Given** um `MultiPolygon`, **When** chamo `Flatten`, **Then** retorno uma
   `FeatureCollection` de `Polygon`, como o `turf.flatten`.

---

### User Story 2 - Joins e utilitários de linha (Priority: P1)

Quem usa `pointsWithinPolygon`, `tag`, `lineSlice`, `lineSliceAlong`, `lineChunk`,
`nearestPoint`, `kinks` obtém o mesmo resultado do TurfJS.

**Why this priority**: joins e fatiamento de linha são muito usados; `pointsWithinPolygon`
reusa a semântica de fronteira da Onda B.

**Independent Test**: comparação estrutural/numérica com o `@turf`.

**Acceptance Scenarios**:

1. **Given** pontos e um polígono, **When** chamo `PointsWithinPolygon`, **Then** retorno só
   os pontos dentro (semântica do `@turf`, como `booleanPointInPolygon`).
2. **Given** uma linha e dois pontos, **When** chamo `LineSlice`, **Then** retorno a sublinha
   entre eles, como `turf.lineSlice`.
3. **Given** uma coleção de pontos e um ponto de referência, **When** chamo `NearestPoint`,
   **Then** retorno o mais próximo, como `turf.nearestPoint`.

---

### User Story 3 - Meta-iteração pública (Priority: P2)

Quem usa `coordEach`/`coordReduce`, `featureEach`, `geomEach`, `propEach`,
`segmentEach`/`segmentReduce`, `flattenEach` itera com a **mesma ordem e os mesmos índices**
do TurfJS.

**Why this priority**: são utilitários de base (hoje `internal`); expor com a semântica
correta destrava ergonomia e os ports futuros.

**Independent Test**: testes de iteração comparando a sequência de coordenadas/índices com
o `@turf`.

**Acceptance Scenarios**:

1. **Given** uma geometria, **When** itero com `CoordEach`, **Then** a sequência de
   coordenadas e os índices (`coordIndex`, etc.) batem com o `turf.coordEach`.
2. **Given** uma feature, **When** itero com `SegmentEach`, **Then** os segmentos e índices
   batem com o `turf.segmentEach`.

---

### Edge Cases

- `explode`/`flatten` em `GeometryCollection` e geometrias vazias.
- `polygonToLine` de `Polygon` com furos (vira `MultiLineString`).
- `lineSlice`/`lineSliceAlong` com pontos fora da linha (projetar no mais próximo) e
  distâncias além do comprimento.
- `pointsWithinPolygon` com ponto na fronteira (semântica do `@turf`).
- `kinks` sem auto-interseção (coleção vazia).
- Meta: `coordEach` com `excludeWrapCoord`; índices em multi-geometrias.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As funções listadas (conversão, joins, misc, meta) MUST existir na fachada
  `Geo` (partials em `Turfano.GeoJson`), recebendo/retornando `Turfano.GeoJson` e usando
  `Turfano.Units` onde aplicável (`lineChunk`/`lineSliceAlong`); opções como parâmetros.
- **FR-002**: Cada função MUST bater com o `@turf` real (validado via `reference/` com bun) —
  comparação estrutural nas que devolvem geometria/coleção, numérica apertada nas distâncias.
  Quando o ground-truth surpreender, **seguir o `@turf`**.
- **FR-003**: As meta-funções MUST iterar na **mesma ordem e com os mesmos índices** do
  `@turf` (`coordEach`/`segmentEach`/`flattenEach` passam índices).
- **FR-004**: `pointsWithinPolygon` MUST respeitar a semântica de fronteira do `@turf`
  (reusar `BooleanPointInPolygon`).
- **FR-005**: As funções vivem na fachada `Geo`; as `Turf.*.cs` NTS-based permanecem
  intactas; a suíte existente (215) permanece verde; nenhuma reflexão (AOT-safe); multi-target
  `net8.0;net9.0;net10.0`; nomes .NET (sem acrônimos crípticos).

### Key Entities

Sem entidades de dados novas — a onda adiciona **funções** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) na fachada `Geo`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das funções listadas têm teste que bate com o `@turf` real (estrutural/
  numérico).
- **SC-002**: As meta-funções (`CoordEach`/`SegmentEach`/`FlattenEach`) provam **ordem e
  índices iguais** aos do `@turf`.
- **SC-003**: `PointsWithinPolygon` filtra exatamente como o `@turf` (incl. fronteira).
- **SC-004**: A superfície opera sobre `Turfano.GeoJson` na fachada `Geo` (sem
  `NetTopologySuite`/`UnitsNet` nas assinaturas públicas das novas funções).
- **SC-005**: Build limpo em `net8.0;net9.0;net10.0`; a suíte existente permanece **verde
  (215, 0 falhas)** + os novos testes; smoke de AOT **0 warnings IL**.

## Assumptions

- As novas funções vivem na **fachada `Geo`** (decisão consolidada nas Ondas A/B/C).
- O `@turf` real (via `reference/`, bun) é a fonte de verdade.
- As meta-funções públicas espelham os helpers `internal` existentes (`EachPosition`,
  `EachSegment`, `FlattenGeometry`), ajustados para a assinatura/índices do `@turf`.
- `polygonize` (montar polígonos a partir de linhas) é a mais algorítmica; pode usar o
  `NtsBridge` interino se o resultado casar com o `@turf`, senão portar.
- NTS/UnitsNet **permanecem** (motor interino); a onda só adiciona.
- **Fora de escopo**: as demais ondas (overlay/clipping, interpolation/grids, restantes),
  remover o NTS/UnitsNet, e portar funções fora de conversion/joins/misc/meta.
