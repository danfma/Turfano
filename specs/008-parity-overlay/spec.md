# Feature Specification: Onda E — Overlay / Clipping (paridade)

**Feature Branch**: `008-parity-overlay`

**Created**: 2026-06-30

**Status**: Draft

**Input**: User description: "Onda E — Overlay / Clipping (Fase 8; depende da matriz da Fase 2). Expor `union`/`difference`/`intersect`/`dissolve`/`buffer` na fachada `Geo` via o **motor NTS interino** (a Fase 2 mediu: área idêntica ao `@turf`; o `buffer` do `@turf` **é** o JTS=NTS), e **portar** o `bboxClip`. Validar tudo vs o `@turf` real."

## User Scenarios & Testing *(mandatory)*

Quinta **onda de paridade**: as operações de **overlay** (juntar/subtrair/interseccionar
polígonos) e **clipping** passam a existir na fachada `Geo` (sobre `Turfano.GeoJson`),
produzindo os **mesmos resultados do TurfJS**. **Decisão herdada da Fase 2** (com medição):
para overlay e `buffer`, o **NTS bate com o `@turf`** (área idêntica ao `polyclip-ts`; o
`buffer` do `@turf` **é** o JTS = NTS) — então estas funções **usam o motor NTS** via a ponte
`Turfano.Interop.NtsBridge`, escondido atrás da fachada. Só o `bboxClip` é portado.

### User Story 1 - Overlay de polígonos (Priority: P1)

Quem usa `union`, `difference`, `intersect`, `dissolve` obtém o mesmo polígono resultante do
TurfJS (área e forma).

**Why this priority**: overlay é o coração desta categoria e o mais usado.

**Independent Test**: comparar a **área** do resultado com o `@turf` (tolerância apertada,
conforme a Fase 2) e a forma estruturalmente onde fizer sentido.

**Acceptance Scenarios**:

1. **Given** dois polígonos que se sobrepõem, **When** chamo `Union`, **Then** a área do
   resultado bate com `turf.union` (igual dentro da tolerância da Fase 2).
2. **Given** dois polígonos que se sobrepõem, **When** chamo `Intersect`, **Then** a área da
   interseção bate com `turf.intersect`.
3. **Given** dois polígonos, **When** chamo `Difference(A, B)`, **Then** a área de A menos B
   bate com `turf.difference`.

---

### User Story 2 - Buffer (Priority: P1)

Quem usa `buffer` obtém o mesmo polígono expandido/contraído do TurfJS.

**Why this priority**: `buffer` é muito usado; e a Fase 2 mostrou que o `buffer` do `@turf`
**é** o JTS (= NTS), logo o NTS é fidelidade máxima.

**Independent Test**: comparar a área/forma do buffer com o `@turf`.

**Acceptance Scenarios**:

1. **Given** um ponto e um raio, **When** chamo `Buffer`, **Then** o polígono resultante bate
   com `turf.buffer` (área dentro da tolerância).

---

### User Story 3 - bboxClip (Priority: P2)

Quem usa `bboxClip` obtém a geometria recortada pela bounding box, como o TurfJS.

**Why this priority**: clipping é útil; é a única função **portada** (Cohen-Sutherland do
`@turf/lineclip`), independente do NTS.

**Independent Test**: comparação estrutural com o `@turf`.

**Acceptance Scenarios**:

1. **Given** uma linha/polígono e uma bbox, **When** chamo `BBoxClip`, **Then** a geometria
   recortada bate com `turf.bboxClip`.

---

### Edge Cases

- Overlay de polígonos disjuntos (`intersect` → vazio/null; `union` → MultiPolygon).
- `difference` onde B cobre A (resultado vazio) ou não toca A (resultado = A).
- `dissolve` de uma coleção de polígonos adjacentes (une os que se tocam).
- `buffer` com raio negativo (contrai) e com `Point`/`LineString`.
- `bboxClip` de geometria totalmente dentro / totalmente fora / cruzando a bbox.
- Resultados nulos/vazios do NTS — mapear para `null`/coleção vazia como o `@turf`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `union`, `difference`, `intersect`, `dissolve`, `buffer` MUST existir na fachada
  `Geo` (partials em `Turfano.GeoJson`), usando o **motor NTS** via `Turfano.Interop.NtsBridge`
  (ida-e-volta novos-tipos ↔ NTS), com o NTS **escondido** das assinaturas públicas.
- **FR-002**: `bboxClip` MUST ser **portado** do `@turf` (Cohen-Sutherland / `lineclip`) sobre
  os novos tipos, sem depender do NTS.
- **FR-003**: Cada função MUST bater com o `@turf` real (validado via `reference/` com bun):
  para overlay/buffer, **área** dentro de tolerância apertada (conforme a Fase 2) e forma
  estrutural onde fizer sentido; para `bboxClip`, comparação estrutural. Quando o ground-truth
  surpreender, **seguir o `@turf`**.
- **FR-004**: As assinaturas MUST usar só `Turfano.GeoJson` e `Turfano.Units` (buffer:
  raio em `Length`); opções como parâmetros. As `Turf.*.cs` NTS-based permanecem intactas; a
  suíte existente (226) permanece verde; multi-target `net8.0;net9.0;net10.0`; nomes .NET.
- **FR-005**: **AOT**: overlay/`buffer` carregam a dependência **interina do NTS** (que pode
  usar reflexão) e podem não ser AOT-safe — isso é aceito (decisão da Fase 2). O smoke de AOT
  continua exercitando só a **serialização GeoJSON** (que permanece **0 warnings**).

### Key Entities

Sem entidades novas — a onda adiciona **funções** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) na fachada `Geo`, com o NTS como motor interino (escondido).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das funções listadas têm teste que bate com o `@turf` real (área para
  overlay/buffer dentro da tolerância da Fase 2; estrutural para `bboxClip`).
- **SC-002**: `Union`/`Intersect`/`Difference` de dois polígonos de teste batem em **área**
  com o `@turf` (igual em ~`1e-5`, como medido na Fase 2).
- **SC-003**: A superfície opera sobre `Turfano.GeoJson` na fachada `Geo` (sem
  `NetTopologySuite` nas assinaturas públicas — escondido na `NtsBridge`).
- **SC-004**: Build limpo em `net8.0;net9.0;net10.0`; a suíte existente permanece **verde
  (226, 0 falhas)** + os novos testes.
- **SC-005**: O smoke de AOT da **serialização** continua **0 warnings IL** (overlay/buffer
  ficam fora do smoke, por carregarem o NTS interino).

## Assumptions

- As novas funções vivem na **fachada `Geo`** (decisão consolidada nas Ondas A–D).
- **Decisão da Fase 2** (`docs/nts-evaluation.md`): overlay (`union`/`difference`/`intersect`/
  `dissolve`) e `buffer` ficam **NTS-interino** (área idêntica medida; `buffer` do `@turf` é
  o JTS=NTS). `bboxClip` é portado (alinhar ao `lineclip`).
- O `@turf` real (via `reference/`, bun) é a fonte de verdade; tolerância de área conforme a
  Fase 2 (~`1e-5`).
- NTS **permanece** como motor interino (aqui é o caminho aceito, não um débito); a onda só
  adiciona à fachada `Geo`.
- **Fora de escopo**: as demais ondas (interpolation/grids, triangulação, restantes), remover
  o NTS, e portar funções fora de overlay/clipping.
