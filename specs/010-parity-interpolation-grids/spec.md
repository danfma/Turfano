# Feature Specification: Onda F — Interpolation, Grids & Triangulation (paridade)

**Feature Branch**: `010-parity-interpolation-grids`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Onda F — Interpolation, Grids & Triangulation (Fase 9). Portar grades, interpolação e triangulação do TurfJS para a fachada `Geo` — a categoria onde o legado tem os **algoritmos ingênuos/quebrados** mapeados na Fase 2 (`tin`, `voronoi`, `concave`, `tesselate`, `isolines`, `isobands`). Port fiel ao `@turf`; `Parity/` segue zona livre de NTS."

## User Scenarios & Testing *(mandatory)*

Sexta **onda de paridade**: as funções de **grades** (gerar malhas de pontos/células),
**interpolação** (valores estimados sobre superfícies) e **triangulação/hulls** passam a
existir na fachada `Geo` (sobre `Turfano.GeoJson`), produzindo as **mesmas saídas do
TurfJS**. É a onda que substitui os **6 algoritmos ingênuos** do legado por versões fiéis.
As Ondas A–E e a saída do motor NTS já estão na `main` — reusar os helpers geodésicos
(`Distance`/`Destination`/`Bearing`), `BooleanPointInPolygon`, `Bbox` e o overlay nativo.

### User Story 1 - Grades (Priority: P1)

Quem usa `pointGrid`, `squareGrid`, `hexGrid` (hexágonos ou triângulos) e `triangleGrid`
obtém as mesmas malhas do TurfJS sobre uma bbox, com tamanho de célula em unidades típicas
(km, milhas...) e opção de máscara por polígono.

**Why this priority**: são a base de `interpolate` (US2) e de análises por célula; alto
reuso dos helpers existentes e baixo risco.

**Independent Test**: comparar contagem/coordenadas das células com o `@turf` real.

**Acceptance Scenarios**:

1. **Given** uma bbox e um lado de célula, **When** chamo `PointGrid`, **Then** os pontos
   batem com `turf.pointGrid` (mesma contagem e coordenadas).
2. **Given** uma bbox, **When** chamo `HexGrid`, **Then** os hexágonos batem com
   `turf.hexGrid` (contagem + vértices).
3. **Given** uma máscara poligonal, **When** chamo `SquareGrid` com mask, **Then** só as
   células que intersectam a máscara aparecem, como no `@turf`.

---

### User Story 2 - Interpolação básica (Priority: P1)

Quem usa `planepoint` (valor barycentric num triângulo), `tin` (triangulação de Delaunay a
partir de pontos com propriedade) e `interpolate` (IDW sobre grade) obtém os mesmos
resultados do TurfJS — substituindo o `tin` **ingênuo** do legado por um porte fiel.

**Why this priority**: `tin` é um dos 6 quebrados; `interpolate` depende das grades da US1.

**Independent Test**: `tin` produz a mesma triangulação do `@turf` (não mais o "leque"
ingênuo); `planepoint`/`interpolate` batem numericamente.

**Acceptance Scenarios**:

1. **Given** pontos com uma propriedade `z`, **When** chamo `Tin`, **Then** os triângulos
   batem com `turf.tin` (mesma triangulação, propriedades `a`/`b`/`c`).
2. **Given** um ponto e um triângulo com valores nos vértices, **When** chamo `Planepoint`,
   **Then** o valor interpolado bate com `turf.planepoint`.
3. **Given** pontos amostrais e um tamanho de célula, **When** chamo `Interpolate`, **Then**
   a grade IDW bate com `turf.interpolate`.

---

### User Story 3 - Isolinhas e isobandas (Priority: P2)

Quem usa `isolines`/`isobands` sobre uma grade de pontos com valores obtém as mesmas
linhas/faixas de contorno do TurfJS — **portando a fonte que o `@turf` executa** (marching
squares), **não** re-tipando a versão própria do legado (`TurfUtils`).

**Why this priority**: são 2 dos 6 quebrados; dependem de grade regular (US1).

**Independent Test**: contornos estruturalmente iguais aos do `@turf` nas mesmas fixtures.

**Acceptance Scenarios**:

1. **Given** uma grade de pontos com valores e breaks, **When** chamo `Isolines`, **Then**
   as multilinhas batem com `turf.isolines`.
2. **Given** a mesma grade e faixas, **When** chamo `Isobands`, **Then** os multipolígonos
   batem com `turf.isobands`.

---

### User Story 4 - Hulls e tesselação (Priority: P2)

Quem usa `convex` (casco convexo), `concave` (casco côncavo via tin), `voronoi` (células) e
`tesselate` (triângulos de um polígono) obtém as mesmas geometrias do TurfJS — substituindo
os `voronoi`/`concave`/`tesselate` **ingênuos** do legado.

**Why this priority**: 3 dos 6 quebrados; são os mais algorítmicos (dependências do `@turf`
a medir no plano: d3-voronoi, earcut).

**Independent Test**: estrutura igual à do `@turf` nas mesmas fixtures.

**Acceptance Scenarios**:

1. **Given** uma coleção de pontos, **When** chamo `Convex`, **Then** o casco bate com
   `turf.convex`.
2. **Given** pontos e `maxEdge`, **When** chamo `Concave`, **Then** o casco côncavo bate
   com `turf.concave`.
3. **Given** pontos e uma bbox, **When** chamo `Voronoi`, **Then** as células batem com
   `turf.voronoi`.
4. **Given** um polígono, **When** chamo `Tesselate`, **Then** os triângulos batem com
   `turf.tesselate`.

---

### Edge Cases

- Grades: bbox menor que uma célula (coleção vazia); máscara que não intersecta nada;
  `hexGrid` com `triangles: true`.
- `tin` com < 3 pontos; pontos colineares; propriedade `z` ausente (usa a coordenada z?
  — conferir o `@turf`).
- `planepoint` com ponto fora do triângulo (o `@turf` extrapola? — conferir GT).
- `isolines`/`isobands` com valores constantes (sem contorno) e breaks fora do range.
- `voronoi` com < 2 pontos; pontos duplicados; células cortadas pela bbox.
- `tesselate` com polígono com furos.
- `concave` sem solução para o `maxEdge` dado (o `@turf` devolve null? — conferir GT).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As funções listadas (grades: `pointGrid`/`squareGrid`/`hexGrid`/
  `triangleGrid`; interpolação: `planepoint`/`tin`/`interpolate`; contornos: `isolines`/
  `isobands`; hulls/tesselação: `convex`/`concave`/`voronoi`/`tesselate`) MUST existir na
  fachada `Geo`, recebendo/retornando `Turfano.GeoJson` e usando `Turfano.Units` onde
  aplicável (tamanhos de célula, `maxEdge`); opções como parâmetros.
- **FR-002**: Cada função MUST bater com o `@turf` real (validado via `reference/` com
  bun) — estrutural para geometrias/coleções, numérica apertada onde couber. Quando o
  ground-truth surpreender, **seguir o `@turf`**.
- **FR-003**: Os **6 algoritmos ingênuos do legado** (`tin`, `voronoi`, `concave`,
  `tesselate`, `isolines`, `isobands`) MUST ganhar versão **fiel** na fachada `Geo` —
  portando a fonte que o `@turf` executa, **não** re-tipando o legado.
- **FR-004**: Onde o `@turf` embute dependência (`marchingsquares`, `d3-voronoi`,
  `earcut`), a decisão porta-vs-alternativa MUST seguir o método da Fase 11: **medir** o
  tamanho/fecho da fonte no plano e decidir com números, registrando a decisão.
- **FR-005**: `src/Turfano/Parity/` MUST permanecer **livre de NetTopologySuite**; as
  `Turf.*.cs` legadas permanecem intactas; a suíte existente (245) permanece verde; smoke
  AOT da serialização 0 warnings; multi-target `net8.0;net9.0;net10.0`; nomes .NET.

### Key Entities

Sem entidades de dados novas — a onda adiciona **funções** sobre os tipos da Fase 3 na
fachada `Geo` (com estruturas internas de triangulação/contorno, como o motor da Fase 11).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das funções listadas têm teste que bate com o `@turf` real
  (estrutural/numérico).
- **SC-002**: Os **6 ingênuos do legado** têm versão fiel na fachada `Geo`, cada um com
  teste provando igualdade com o `@turf` (o `tin` não produz mais o "leque"; o `voronoi`
  produz células reais; etc.).
- **SC-003**: `grep NetTopologySuite src/Turfano/Parity/` permanece **vazio**.
- **SC-004**: Build limpo em `net8.0;net9.0;net10.0`; a suíte existente permanece **verde
  (245, 0 falhas)** + os novos testes; smoke AOT da serialização **0 warnings IL**.

## Assumptions

- As novas funções vivem na **fachada `Geo`** (decisão consolidada nas Ondas A–E/Fase 11).
- O `@turf` real (via `reference/`, bun) é a fonte de verdade; os tamanhos das dependências
  (`marchingsquares`, `d3-voronoi`, `earcut`) são medidos no plano antes de decidir o porte
  (método da Fase 11).
- As grades usam os helpers geodésicos existentes; `interpolate` usa as grades da US1;
  `concave` usa o `tin` da US2 (como no `@turf`).
- NTS/UnitsNet permanecem no legado (leva 2 da Fase 11 cuida da remoção).
- **Fora de escopo**: Onda G (random/clusters/restantes), leva 2 da Fase 11 (deletar
  legado/UnitsNet/split 1.0), e otimizações de desempenho.
