# Feature Specification: Onda B — Booleans / Assertions (paridade com TurfJS)

**Feature Branch**: `005-parity-booleans`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "Onda B — Booleans/Assertions (Fase 5). Portar as funções `boolean*` do TurfJS para a fachada `Geo` (sobre `Turfano.GeoJson`), com a **semântica do Turf** (fronteira/`ignoreBoundary`), não a do NTS. Validar contra o `@turf` real (incl. fixtures true/false)."

## User Scenarios & Testing *(mandatory)*

Segunda **onda de paridade**: as funções booleanas/de asserção do TurfJS passam a existir
na fachada `Geo` (sobre os tipos `Turfano.GeoJson`), devolvendo o **mesmo `true/false` do
TurfJS** — inclusive nos casos de fronteira, onde o NTS diverge (Fase 2). O "usuário" é o
desenvolvedor que pergunta "este ponto está no polígono?", "estes se sobrepõem?", etc., e
espera a resposta do Turf. A Onda A (measurement) já está na `main`, na fachada `Geo`.

### User Story 1 - Predicados de ponto e orientação (Priority: P1)

Quem usa `booleanPointInPolygon` (com `ignoreBoundary`), `booleanPointOnLine`,
`booleanClockwise`, `booleanConcave` ou `booleanParallel` obtém o `true/false` do TurfJS,
incluindo a semântica de fronteira.

**Why this priority**: `booleanPointInPolygon` é o predicado mais usado e base de outros
(`pointToPolygonDistance`, `pointOnFeature`); a fronteira é a divergência-chave vs NTS.

**Independent Test**: rodar cada predicado contra as fixtures true/false do `@turf`.

**Acceptance Scenarios**:

1. **Given** um ponto exatamente na borda de um polígono, **When** chamo
   `BooleanPointInPolygon(pt, poly, ignoreBoundary: false)`, **Then** retorna `true`; com
   `ignoreBoundary: true`, retorna `false` (semântica do `@turf`).
2. **Given** um anel de coordenadas, **When** chamo `BooleanClockwise`, **Then** bate com
   `turf.booleanClockwise`.
3. **Given** um ponto sobre um segmento, **When** chamo `BooleanPointOnLine`, **Then** bate
   com `turf.booleanPointOnLine` (incl. `ignoreEndVertices`).

---

### User Story 2 - Relações de geometria estilo Turf (Priority: P1)

Quem usa `booleanContains`, `booleanWithin`, `booleanDisjoint`, `booleanIntersects`,
`booleanCrosses`, `booleanOverlap`, `booleanTouches` ou `booleanEqual` obtém o resultado do
TurfJS — **não** o do predicado DE-9IM do NTS, que diverge na fronteira/igualdade.

**Why this priority**: são o coração dos predicados relacionais; a Fase 2 marcou divergência.

**Independent Test**: fixtures true/false do `@turf` por relação.

**Acceptance Scenarios**:

1. **Given** dois polígonos que só compartilham a fronteira, **When** chamo
   `BooleanOverlap`, **Then** retorna `false` (como o `@turf`).
2. **Given** A dentro de B, **When** chamo `BooleanContains(B, A)`/`BooleanWithin(A, B)`,
   **Then** ambos batem com o `@turf`.
3. **Given** duas geometrias iguais a menos de ordem/precisão, **When** chamo
   `BooleanEqual`, **Then** bate com o `@turf`.

---

### User Story 3 - Validade (Priority: P3)

Quem usa `booleanValid` obtém o `true/false` do TurfJS para a validade da geometria.

**Why this priority**: menos central; útil para sanitização.

**Independent Test**: fixtures do `@turf/boolean-valid`.

**Acceptance Scenarios**:

1. **Given** um polígono auto-interseccionado, **When** chamo `BooleanValid`, **Then**
   retorna `false` (como o `@turf`).

---

### Edge Cases

- **Fronteira**: ponto/segmento exatamente na borda — `ignoreBoundary`/`ignoreEndVertices`
  mudam o resultado (semântica do `@turf`, não do NTS).
- Orientação de anéis (`booleanClockwise`) com anéis degenerados/colineares.
- `booleanEqual` sensível a ordem de vértices e precisão (o `@turf` usa
  `geojson-equality`/tolerância).
- `booleanOverlap`/`booleanTouches` para combinações de dimensão (ponto/linha/polígono).
- Geometrias vazias/degeneradas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As 14 funções `boolean*` listadas MUST existir na fachada `Geo` (partials em
  `Turfano.GeoJson`), recebendo `Turfano.GeoJson` e devolvendo `bool`; opções como
  parâmetros (ex.: `bool ignoreBoundary = false`).
- **FR-002**: Cada função MUST bater com o `@turf` real (validado via `reference/` com bun),
  **incluindo as fixtures `true/false`** dos pacotes `@turf/boolean-*`.
- **FR-003**: A semântica MUST ser a do TurfJS — em especial a de **fronteira**
  (`ignoreBoundary`/`ignoreEndVertices`) e a de **igualdade/orientação** — e **não** a dos
  predicados do NTS onde divergem (Fase 2, `docs/nts-evaluation.md`).
- **FR-004**: Onde for seguro, MAY usar o NTS como motor interino via
  `Turfano.Interop.NtsBridge` **sem alterar a semântica**; caso contrário, MUST portar o
  algoritmo do `@turf`. Reusar helpers da Onda A (`PointInPolygon`/`InRing`,
  `NearestPointOnLine`, `IsLeft`).
- **FR-005**: A onda MUST NÃO remover o NTS/UnitsNet nem alterar as `Turf.*.cs` atuais; a
  suíte existente (193) permanece verde; nenhuma reflexão introduzida (AOT-safe);
  multi-target `net8.0;net9.0;net10.0`.

### Key Entities

Sem entidades novas — a onda adiciona **funções** (predicados `bool`) sobre os tipos da
Fase 3 (`Turfano.GeoJson.*`) na fachada `Geo`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das 14 funções `boolean*` têm teste que bate com o `@turf` real,
  incluindo casos de fronteira (`ignoreBoundary`/`ignoreEndVertices`).
- **SC-002**: `BooleanPointInPolygon` de um ponto na borda retorna `true` com
  `ignoreBoundary: false` e `false` com `ignoreBoundary: true` (prova da semântica de
  fronteira do `@turf`).
- **SC-003**: A superfície opera sobre `Turfano.GeoJson` na fachada `Geo` (sem
  `NetTopologySuite`/`UnitsNet` nas assinaturas públicas das novas funções).
- **SC-004**: Build limpo em `net8.0;net9.0;net10.0`; a suíte existente permanece **verde
  (193, 0 falhas)** + os novos testes booleanos.
- **SC-005**: O smoke de AOT continua **0 warnings IL** (nenhuma reflexão introduzida).

## Assumptions

- As novas funções vivem na **fachada `Geo`** (decisão consolidada na Onda A) — mesmo
  padrão: nomes de tipo resolvem para `Turfano.GeoJson` (sem colisão).
- O `@turf` real (via `reference/`, bun) é a fonte de verdade; usar também as **fixtures
  true/false** que os pacotes `@turf/boolean-*` trazem.
- NTS/UnitsNet **permanecem** (motor interino); a onda só adiciona.
- **Fora de escopo**: as demais ondas (transformation, overlay/clipping, interpolation/
  grids, features/meta, restantes), remover o NTS/UnitsNet, e portar funções fora da
  categoria booleans.
