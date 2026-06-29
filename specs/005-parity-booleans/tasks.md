---
description: "Task list — Onda B — Booleans/Assertions (Fase 5, paridade)"
---

# Tasks: Onda B — Booleans / Assertions (paridade com TurfJS)

**Input**: Design documents from `specs/005-parity-booleans/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — cada predicado validado vs `@turf` real + fixtures `true/false`
(FR-002, SC-001).

**Organization**: por user story. **Portar o `@turf`, NÃO re-tipar** os `Boolean*.cs` NTS
(divergem na fronteira). Fachada `Geo`. O harness/fixtures (T002) é foundational. Nada de
produção atual é removido (suíte 193 segue verde).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: paralelizável (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [X] T001 Confirmar baseline `dotnet build Turfano.slnx -c Debug` (0 erros) + suíte 193/0;
  reusar `src/Turfano/Parity/` e `tests/Turfano.Tests/Parity/`.

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T002 Criar o harness Bun `reference/_boolean.mjs` que, por `boolean*`, (a) emite o
  resultado do `@turf` em casos-âncora e (b) **varre as fixtures `test/true`/`test/false`**
  dos pacotes `@turf/boolean-*`, emitindo `(nome, args, esperado)` em JSON (FR-002).

**Checkpoint**: ground-truth + fixtures disponíveis para todas as histórias.

---

## Phase 3: User Story 1 — Predicados de ponto/orientação (Priority: P1) 🎯 MVP

**Goal**: `booleanPointInPolygon` (com `ignoreBoundary`), `booleanPointOnLine`,
`booleanClockwise`, `booleanConcave`, `booleanParallel` batendo com o `@turf`.

**Independent Test**: fixtures true/false do `@turf`; o de point-in-polygon prova a borda.

- [X] T003 [US1] `Geo.BooleanPointInPolygon` (porte do `inRing`/`inBBox` do `@turf` com
  tratamento de borda + `ignoreBoundary`) em `src/Turfano/Parity/Boolean.PointInPolygon.cs`.
- [X] T004 [P] [US1] `Geo.BooleanPointOnLine` (+ `ignoreEndVertices`/`epsilon`) em
  `src/Turfano/Parity/Boolean.PointOnLine.cs` (reusar `IsLeft`/projeção de segmento).
- [X] T005 [P] [US1] `Geo.BooleanClockwise` (área sinalizada do anel) +
  `Geo.BooleanParallel` (diferença de rumo dos segmentos) em
  `src/Turfano/Parity/Boolean.ClockwiseParallel.cs`.
- [X] T006 [P] [US1] `Geo.BooleanConcave` (porte do `@turf`) em
  `src/Turfano/Parity/Boolean.Concave.cs`.
- [X] T007 [US1] Testes vs `@turf` + fixtures em
  `tests/Turfano.Tests/Parity/BooleanPointTests.cs`; SC-002: ponto na borda →
  `true`/`false` conforme `ignoreBoundary`.

**Checkpoint**: predicados de ponto/orientação fiéis ao `@turf` (MVP, incl. fronteira).

---

## Phase 4: User Story 2 — Relações de geometria estilo Turf (Priority: P1)

**Goal**: `contains`/`within`/`disjoint`/`intersects`/`crosses`/`overlap`/`touches`/`equal`
com a semântica do `@turf` (não DE-9IM do NTS).

**Independent Test**: fixtures true/false do `@turf` por relação.

- [X] T008 [US2] `Geo.BooleanIntersects` + `Geo.BooleanDisjoint` (duais) em
  `src/Turfano/Parity/Boolean.Intersects.cs` (porte do `@turf`; reusar point-in-polygon e
  interseção de segmentos).
- [X] T009 [US2] `Geo.BooleanContains` + `Geo.BooleanWithin` (argumentos trocados) em
  `src/Turfano/Parity/Boolean.Contains.cs` (porte do `@turf`).
- [X] T010 [P] [US2] `Geo.BooleanCrosses` + `Geo.BooleanTouches` em
  `src/Turfano/Parity/Boolean.CrossesTouches.cs` (porte do `@turf`, por combinação de
  dimensão).
- [X] T011 [P] [US2] `Geo.BooleanOverlap` em `src/Turfano/Parity/Boolean.Overlap.cs`
  (semântica do `@turf`: só-fronteira → `false`).
- [X] T012 [P] [US2] `Geo.BooleanEqual` (igualdade estilo `geojson-equality`, ordem/
  precisão) em `src/Turfano/Parity/Boolean.Equal.cs`.
- [X] T013 [US2] Testes vs `@turf` + fixtures em
  `tests/Turfano.Tests/Parity/BooleanRelationsTests.cs`.

**Checkpoint**: relações batendo com o `@turf`, não com o NTS.

---

## Phase 5: User Story 3 — Validade (Priority: P3)

**Goal**: `booleanValid` com o resultado do `@turf`.

**Independent Test**: fixtures do `@turf/boolean-valid`.

- [X] T014 [US3] `Geo.BooleanValid` (porte do `@turf`) em
  `src/Turfano/Parity/Boolean.Valid.cs`.
- [X] T015 [US3] Testes vs `@turf` + fixtures em
  `tests/Turfano.Tests/Parity/BooleanValidTests.cs`.

**Checkpoint**: validade conforme o `@turf`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 Verificação final (SC-004/005): build `Turfano.slnx` (0 erros, net8/9/10) +
  suíte (193 + novos, 0 falhas) + smoke AOT (0 warnings IL) +
  `git diff --stat main -- 'src/Turfano/Turf.*.cs'` vazio.
- [X] T017 [P] Remover o harness efêmero `reference/_boolean.mjs`.
- [X] T018 Atualizar `plans/turfjs-parity-redesign.md`: Fase 5 (Onda B) → `Complete` +
  Phase Summary.

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002 harness+fixtures)** → **US1/US2/US3**.
- US1 destrava parte da US2 (relações usam `BooleanPointInPolygon`). US3 é independente.
- Dentro de cada história: implementar → testar.
- **Polish** após todas.

### Parallel Opportunities

- T004 ∥ T005 ∥ T006 (US1); T010 ∥ T011 ∥ T012 (US2, arquivos distintos).
- US3 em paralelo com US1/US2 após o harness.

---

## Implementation Strategy

### MVP

Setup → Foundational (harness+fixtures) → **US1** (ponto/orientação, incl. fronteira —
prova a divergência consertada). É o coração da onda.

### Incremental

1. Harness + fixtures.
2. US1 (ponto/orientação) → US2 (relações, usam point-in-polygon).
3. US3 (validade).
4. Polish: verificação + limpar harness + atualizar plano-mãe.

---

## Notes

- **PORTAR o `@turf`, não re-tipar**: os `Turf.Boolean*.cs` atuais delegam a predicados NTS
  (`Contains`/`Overlaps`) que divergem na fronteira (Fase 2). `booleanConcave`/`booleanValid`
  nem existem. Validar TUDO vs `@turf` + fixtures.
- **Não-regressão** (FR-005): `Turf.Boolean*.cs` NTS, NTS e UnitsNet permanecem; suíte 193
  verde; AOT-safe.
- Reusar helpers da Onda A: `PointInPolygon`/`InRing`, `NearestPointOnLine`, `IsLeft`.
- Fora de escopo: demais ondas, remover NTS/UnitsNet, funções fora de booleans.

## Implementation Notes (incremento — em progresso)

- **Entregue e verde (196/0)**: **US1** — `Geo.BooleanPointInPolygon` (Hao, com borda +
  `ignoreBoundary` → SC-002), `BooleanPointOnLine` (+`ignoreEndVertices`/`epsilon`),
  `BooleanClockwise`, `BooleanParallel`, `BooleanConcave` — **portados do `@turf`** (não
  re-tipados do NTS). AOT 0 warnings; `Turf.*.cs` NTS intocados.
- **Convenção aplicada (correção do usuário)**: sem acrônimos crípticos — `RadiansPerDegree`,
  `RightTangent`, `ClassifyPointInPolygon`, etc. (ver `CLAUDE.md` ## Conventions e
  `tasks/lessons.md`). Vale para as próximas funções.
- **Falta (US2 relações + US3) — porte real, pesado**: tamanho dos fontes `@turf` a portar:
  `boolean-touches` **646** linhas, `boolean-contains` **244**, `boolean-within` **198**,
  `boolean-disjoint` 142, `boolean-valid` 92, `boolean-overlap` 54, `boolean-intersects` 24
  (= `!disjoint`), `boolean-equal` 22. Estratégia: começar por `disjoint`+`intersects` (e os
  helpers de interseção de segmento `isLineOnLine`/`isLineInPoly`), depois `contains`/
  `within`, `overlap`, `crosses`, `equal`, e por fim `touches` (o maior). Validar cada um vs
  `@turf` + fixtures `true/false`. Depois: Polish (T016–T018).
