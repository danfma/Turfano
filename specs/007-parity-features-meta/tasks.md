---
description: "Task list — Onda D — Feature Conversion/Joins/Meta (Fase 7, paridade)"
---

# Tasks: Onda D — Feature Conversion, Joins & Meta (paridade com TurfJS)

**Input**: Design documents from `specs/007-parity-features-meta/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — cada função validada vs `@turf` real (estrutural/numérico; índices nas
meta) (FR-002, SC-001/002).

**Organization**: por user story. Estratégia **mista** (re-tipar reshape, portar onde preciso,
**reusar** helpers A/B/C). Fachada `Geo`. Harness (T002) foundational. Nada de produção atual
é removido (suíte 215 segue verde).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: paralelizável (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [ ] T001 Confirmar baseline `dotnet build Turfano.slnx -c Debug` (0 erros) + suíte 215/0;
  reusar `src/Turfano/Parity/` e `tests/Turfano.Tests/Parity/`.

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T002 Criar o harness Bun `reference/_features.mjs` que emite, por função/fixture, a
  saída do `@turf` (geometrias/coleções serializadas p/ comparação estrutural; números p/
  distâncias; **sequências de coord/índices** para `coordEach`/`segmentEach`/`flattenEach`).

**Checkpoint**: ground-truth disponível para todas as histórias.

---

## Phase 3: User Story 1 — Conversão de feature (Priority: P1) 🎯 MVP

**Goal**: `explode`, `flatten`, `combine`, `polygonToLine`, `lineToPolygon`, `polygonize`.

**Independent Test**: comparação estrutural com o `@turf` por função.

- [X] T003 [P] [US1] `Geo.Explode` + `Geo.Flatten` em `src/Turfano/Parity/Convert.Explode.cs`
  (reusar `EachPosition`/`FlattenGeometry`).
- [X] T004 [P] [US1] `Geo.PolygonToLine` + `Geo.LineToPolygon` em
  `src/Turfano/Parity/Convert.PolyLine.cs` (reshape de anéis/linhas).
- [X] T005 [P] [US1] `Geo.Combine` em `src/Turfano/Parity/Convert.Combine.cs`
  (Point/LineString/Polygon → Multi*).
- [X] T006 [US1] `Geo.Polygonize` em `src/Turfano/Parity/Convert.Polygonize.cs` (tentar
  `NtsBridge` NTS Polygonizer e **validar vs `@turf`**; senão portar `@turf/polygonize`).
- [X] T007 [US1] Testes vs `@turf` em `tests/Turfano.Tests/Parity/ConvertTests.cs`.

**Checkpoint**: conversões fiéis ao `@turf` (MVP).

---

## Phase 4: User Story 2 — Joins e utilitários de linha (Priority: P1)

**Goal**: `pointsWithinPolygon`, `tag`, `lineSlice`, `lineSliceAlong`, `lineChunk`,
`nearestPoint`, `kinks`.

**Independent Test**: comparação estrutural/numérica com o `@turf`; fronteira em
`pointsWithinPolygon`.

- [X] T008 [US2] `Geo.PointsWithinPolygon` + `Geo.Tag` em `src/Turfano/Parity/Join.cs`
  (reusar `BooleanPointInPolygon` — fronteira do `@turf`, SC-003).
- [X] T009 [P] [US2] `Geo.NearestPoint` em `src/Turfano/Parity/Misc.NearestPoint.cs`
  (min `Distance` sobre a coleção).
- [X] T010 [P] [US2] `Geo.LineSlice` + `Geo.LineSliceAlong` em
  `src/Turfano/Parity/Misc.LineSlice.cs` (reusar `NearestPointOnLine`/`Along`/`Distance`).
- [X] T011 [P] [US2] `Geo.LineChunk` + `Geo.Kinks` em `src/Turfano/Parity/Misc.ChunkKinks.cs`
  (`Along` em passos; auto-interseção via `SegmentsIntersect`).
- [X] T012 [US2] Testes vs `@turf` em `tests/Turfano.Tests/Parity/JoinMiscTests.cs`.

**Checkpoint**: joins e utilitários de linha fiéis ao `@turf`.

---

## Phase 5: User Story 3 — Meta-iteração pública (Priority: P2)

**Goal**: `coordEach`/`coordReduce`, `featureEach`, `geomEach`, `propEach`, `segmentEach`/
`segmentReduce`, `flattenEach` com ordem/índices do `@turf`.

**Independent Test**: testes de iteração comparando coord/índices com o `@turf`.

- [X] T013 [US3] `Geo.CoordEach`/`CoordReduce` + `Geo.SegmentEach`/`SegmentReduce` +
  `Geo.FlattenEach` em `src/Turfano/Parity/Meta.Iteration.cs` (assinatura/índices do `@turf`;
  `excludeWrapCoord`). **SC-002**.
- [X] T014 [P] [US3] `Geo.FeatureEach` + `Geo.GeomEach` + `Geo.PropEach` em
  `src/Turfano/Parity/Meta.Each.cs`.
- [X] T015 [US3] Testes de iteração/índices vs `@turf` em
  `tests/Turfano.Tests/Parity/MetaTests.cs`.

**Checkpoint**: meta-funções públicas iterando como o `@turf`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 Verificação final (SC-005): build `Turfano.slnx` (0 erros, net8/9/10) + suíte
  (215 + novos, 0 falhas) + smoke AOT (0 warnings IL) +
  `git diff --stat main -- 'src/Turfano/Turf.*.cs'` vazio.
- [X] T017 [P] Remover o harness efêmero `reference/_features.mjs`.
- [X] T018 Atualizar `plans/turfjs-parity-redesign.md`: Fase 7 (Onda D) → `Complete` +
  Phase Summary.

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002 harness)** → **US1/US2/US3**.
- US1/US2/US3 dependem do harness mas são **independentes entre si** (arquivos distintos);
  joins/misc reusam helpers já na `main` (`BooleanPointInPolygon`/`Along`/`NearestPointOnLine`/
  `Distance`/`SegmentsIntersect`); meta espelha `EachPosition`/`EachSegment`/`FlattenGeometry`.
- Dentro de cada história: implementar → testar.
- **Polish** após todas.

### Parallel Opportunities

- T003 ∥ T004 ∥ T005 (US1); T009 ∥ T010 ∥ T011 (US2); T013 ∥ T014 (US3).
- US1, US2, US3 inteiras podem ser conduzidas em paralelo após o harness.

---

## Implementation Strategy

### MVP

Setup → Foundational (harness) → **US1** (conversões — reshape de baixo risco) + **US2**
(joins/linha — alto reuso). As duas P1.

### Incremental

1. Harness (ground-truth).
2. US1 conversões → US2 joins/utilitários de linha.
3. US3 meta-iteração (índices do `@turf`).
4. Polish: verificação + limpar harness + atualizar plano-mãe.

---

## Notes

- **Estratégia mista**: re-tipar reshape estrutural; portar onde preciso; **reusar** A/B/C.
  Validar TUDO vs `@turf`. Quando o GT surpreender, **seguir o `@turf`** (lição B/C).
- **Não-regressão** (FR-005): `Turf.*.cs` NTS, NTS e UnitsNet permanecem; suíte 215 verde;
  AOT-safe.
- **Nomes .NET**: sem acrônimos crípticos; nomes de domínio (`BBox`, `lon`/`lat`) permitidos.
- **Pontos sutis**: índices das meta (SC-002) e `polygonize` (NTS-interino vs porte — validar).
- Fora de escopo: demais ondas, remover NTS/UnitsNet, funções fora de conversion/joins/misc/meta.
