# Feature Specification: Onda A — Measurement (paridade com TurfJS)

**Feature Branch**: `004-parity-measurement`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "Onda A — Measurement (Fase 4 do plano). Primeira onda de paridade: portar as funções de medição do TurfJS para os novos tipos próprios (Turfano.GeoJson) e structs de unidade (Turfano.Units), com fidelidade numérica ao @turf, corrigindo divergências (centroid)."

## User Scenarios & Testing *(mandatory)*

Primeira **onda de paridade**: as funções de medição do TurfJS passam a existir sobre os
novos tipos (`Turfano.GeoJson`) e unidades (`Turfano.Units`), batendo numericamente com o
`@turf`. O "usuário" é o desenvolvedor que quer medir geometrias e obter os **mesmos
números do TurfJS**. A fundação (tipos + unidades + ponte NTS interna) já está na `main`.

### User Story 1 - Medições escalares fiéis ao @turf (Priority: P1)

Quem mede `area`, `distance`, `bearing`, `length` ou calcula `bbox`/`bboxPolygon`/
`square`/`envelope` sobre os novos tipos obtém exatamente os valores/formas do TurfJS.

**Why this priority**: são as medições mais usadas e a base das demais; entregam valor
imediato e exercitam a integração tipos+unidades.

**Independent Test**: rodar cada função sobre fixtures e comparar com o `@turf` real.

**Acceptance Scenarios**:

1. **Given** o polígono `[[[-5,52],[-4,56],[-2,51],[-7,54],[-5,52]]]`, **When** chamo
   `Area`, **Then** retorno ≈ `32819945055.14 m²` (valor do `@turf`).
2. **Given** dois pontos, **When** chamo `Distance` em km, **Then** bate com
   `turf.distance` (tolerância apertada).
3. **Given** uma linha, **When** chamo `Bbox`, **Then** retorno `[west,south,east,north]`
   igual ao `turf.bbox`.

---

### User Story 2 - Pontos derivados corretos, com `centroid` consertado (Priority: P1)

Quem calcula `centroid`, `center`, `centerOfMass`, `midpoint`, `destination`, `along` ou
`rhumbDestination` obtém o mesmo ponto do TurfJS — incluindo o **conserto do `centroid`**
(que hoje inclui o vértice de fechamento e dá `[0.833,0.833]` em vez de `[1,1]`).

**Why this priority**: `centroid` é uma divergência conhecida (Fase 2) e ponto derivado é
base de muitas funções.

**Independent Test**: comparar cada ponto com o `@turf`; o teste de `centroid` prova `[1,1]`.

**Acceptance Scenarios**:

1. **Given** o polígono `[[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]]`, **When** chamo `Centroid`,
   **Then** retorno `[1,1]` (e **não** `[0.833,0.833]`), igual ao `@turf`.
2. **Given** origem, distância e rumo, **When** chamo `Destination`/`RhumbDestination`,
   **Then** o ponto bate com o `@turf`.
3. **Given** dois pontos, **When** chamo `Midpoint`, **Then** bate com `turf.midpoint`.

---

### User Story 3 - Rumo e distâncias a geometrias (Priority: P2)

Quem usa `rhumbBearing`, `rhumbDistance`, `pointToLineDistance`, `pointToPolygonDistance`,
`nearestPointOnLine`, `pointOnFeature`, `greatCircle` ou `polygonTangents` obtém os
resultados do TurfJS.

**Why this priority**: completam a categoria de medição; algumas são menos centrais.

**Independent Test**: comparar com o `@turf` real por fixture.

**Acceptance Scenarios**:

1. **Given** dois pontos, **When** chamo `RhumbBearing`, **Then** bate com o `@turf`
   (incluindo rumos > 180°, já corrigidos na Fase 1).
2. **Given** um ponto e uma linha, **When** chamo `PointToLineDistance`, **Then** bate com
   `turf.pointToLineDistance`.

---

### User Story 4 - Conversões de unidade na superfície de funções (Priority: P3)

Quem usa `bearingToAzimuth`, `convertLength`, `convertArea`, `degreesToRadians`,
`radiansToDegrees`, `lengthToRadians`, `radiansToLength`, `lengthToDegrees` os encontra na
superfície pública (já implementados em `Turfano.Units`).

**Why this priority**: baixo esforço (já existe em Units); conveniência de paridade.

**Independent Test**: as conversões batem com o `@turf` (já cobertas por testes da Fase 3).

**Acceptance Scenarios**:

1. **Given** uma medida, **When** chamo as conversões, **Then** os valores batem com o
   `@turf` (reuso dos structs `Turfano.Units`).

---

### Edge Cases

- `Area`/`Centroid` de geometrias vazias ou degeneradas.
- `Centroid`: anéis de `Polygon` fechados (excluir o vértice de fechamento) vs `LineString`
  abertas; `MultiPolygon`/`GeometryCollection`.
- `Distance`/`RhumbBearing` cruzando o antimeridiano e nos polos.
- `Along`/`Midpoint` em linhas de 1 ponto ou comprimento zero.
- Unidades: `Length` em km/m/miles/degrees/radians coerentes com o `@turf`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Cada função de medição listada MUST existir operando sobre os tipos
  `Turfano.GeoJson` e retornar/aceitar `Turfano.Units` onde aplicável (não NTS/UnitsNet).
- **FR-002**: Cada função MUST bater numericamente com o `@turf` real (validado via
  `reference/` com bun), dentro de tolerância apertada (ex.: `1e-6` relativo ou melhor).
- **FR-003**: `Centroid` MUST excluir o vértice de fechamento dos anéis (resultado `[1,1]`
  no caso da Fase 2, não `[0.833,0.833]`), batendo com o `@turf`.
- **FR-004**: Onde a medição usa rumo/great-circle, MUST seguir a convenção do `@turf`
  (e usar a constante `2π` correta — já saneada na Fase 1).
- **FR-005**: Funções cujo algoritmo do Turf é próprio MUST ser portadas direto sobre os
  novos tipos; onde o NTS for o motor interino (decisão da Fase 2), MUST usar a ponte
  interna `Turfano.Interop.NtsBridge` (sem expor o NTS na assinatura).
- **FR-006**: As conversões de unidade do Turf MUST estar acessíveis na superfície pública
  (reusando `Turfano.Units`).
- **FR-007**: A onda MUST NÃO remover o NTS/UnitsNet nem alterar as `Turf.*.cs` atuais
  (NTS-based) — a superfície nova convive ao lado; a suíte existente (177) permanece verde.
- **FR-008**: Nenhuma reflexão MUST ser introduzida (manter AOT-safety); multi-targeting
  `net8.0;net9.0;net10.0` mantido.

### Key Entities

Não há entidades de dados novas — a onda adiciona **funções** sobre os tipos já definidos
na Fase 3 (`Turfano.GeoJson.*`, `Turfano.Units.*`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das funções de measurement listadas têm teste que bate com o `@turf`
  real dentro de tolerância apertada (`1e-6` ou melhor onde aplicável).
- **SC-002**: `Centroid` do polígono `[[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]]` retorna `[1,1]`
  (prova da correção da divergência da Fase 2).
- **SC-003**: A superfície nova opera sobre `Turfano.GeoJson`/`Turfano.Units` (sem
  `NetTopologySuite`/`UnitsNet` nas assinaturas públicas das novas funções).
- **SC-004**: Build limpo em `net8.0;net9.0;net10.0`; a suíte existente permanece **verde
  (177, 0 falhas)** + os novos testes de measurement.
- **SC-005**: O smoke de AOT continua **0 warnings IL** (nenhuma reflexão introduzida).

## Assumptions

- **Onde vivem as novas funções** (decisão de design assumida, a confirmar): uma nova
  superfície operando sobre `Turfano.GeoJson` — sobrecargas na fachada `Turf` que recebem
  os novos tipos, **ou** um novo ponto de entrada dedicado — mantendo as `Turf.*.cs` atuais
  (NTS-based) intactas durante a transição.
- O `@turf` real (via `reference/`, bun) é a fonte de verdade numérica; corner cases de
  representação numérica JS×C# são ignorados de propósito.
- NTS e UnitsNet **permanecem** (motor/unidades interinos); a onda só adiciona.
- **Fora de escopo**: as outras ondas (booleans, transformation, overlay/clipping,
  interpolation/grids, features/meta, restantes), remover o NTS/UnitsNet, e portar funções
  fora da categoria measurement.
