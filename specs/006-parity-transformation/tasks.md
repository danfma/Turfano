---
description: "Task list — Onda C — Transformation & Mutation (Fase 6, paridade)"
---

# Tasks: Onda C — Transformation & Coordinate Mutation (paridade com TurfJS)

**Input**: Design documents from `specs/006-parity-transformation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — cada função validada vs `@turf` real (numérico apertado / estrutural)
(FR-002, SC-001).

**Organization**: por user story. Estratégia **mista** (re-tipar onde fiel, **portar o
`@turf` onde diverge** — `transformScale` geodésico). Fachada `Geo`. Harness (T002)
foundational. Nada de produção atual é removido (suíte 203 segue verde).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: paralelizável (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [X] T001 Confirmar baseline `dotnet build Turfano.slnx -c Debug` (0 erros) + suíte 203/0;
  reusar `src/Turfano/Parity/` e `tests/Turfano.Tests/Parity/`.

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T002 Criar o harness Bun `reference/_transform.mjs` que emite, por função/fixture, a
  saída do `@turf` (geometrias serializadas p/ comparação estrutural; números p/ tolerância),
  incl. `transformScale` (geodésico), `circle`, `simplify`, `rewind`, `truncate` (FR-002).

**Checkpoint**: ground-truth disponível para todas as histórias.

---

## Phase 3: User Story 1 — Mutação de coordenadas (Priority: P1) 🎯 MVP

**Goal**: `cleanCoords`, `flip`, `rewind`, `round`, `truncate` batendo com o `@turf`.

**Independent Test**: comparação estrutural com o `@turf` por função.

- [X] T003 [P] [US1] `Geo.Flip` + `Geo.Round` (+ `Geo.Truncate`) em
  `src/Turfano/Parity/Mutate.FlipRoundTruncate.cs` (operações de coordenada; reusar
  `EachPosition`/reconstrução de geometria).
- [X] T004 [P] [US1] `Geo.CleanCoords` em `src/Turfano/Parity/Mutate.CleanCoords.cs`
  (remove duplicados consecutivos + colineares redundantes; algoritmo do `@turf`).
- [X] T005 [P] [US1] `Geo.Rewind` em `src/Turfano/Parity/Mutate.Rewind.cs` (orienta anéis;
  reusar `BooleanClockwise`; conferir a convenção do `@turf` no GT).
- [X] T006 [US1] Testes vs `@turf` em `tests/Turfano.Tests/Parity/MutateTests.cs`
  (flip `[1,2]→[2,1]`, truncate, cleanCoords, rewind).

**Checkpoint**: mutações fiéis ao `@turf` (MVP, baixo risco).

---

## Phase 4: User Story 2 — Transformação geométrica (Priority: P1)

**Goal**: `transformRotate`, `transformTranslate`, `transformScale` (geodésico), `clone`.

**Independent Test**: comparar com o `@turf`; `transformScale` prova a semântica geodésica.

- [X] T007 [US2] `Geo.TransformScale` **GEODÉSICO** (rhumbDistance/rhumbBearing/
  rhumbDestination a partir da origem) em `src/Turfano/Parity/Transform.Scale.cs`
  (conferir a origem default no `@turf`). **SC-002**.
- [X] T008 [P] [US2] `Geo.TransformTranslate` em `src/Turfano/Parity/Transform.Translate.cs`
  (mover cada ponto por distância/rumo; reusar `Destination`/`Rhumb*` conforme o `@turf`).
- [X] T009 [P] [US2] `Geo.TransformRotate` + `Geo.Clone` em
  `src/Turfano/Parity/Transform.RotateClone.cs` (rotacionar em torno do pivô; cópia profunda).
- [X] T010 [US2] Testes vs `@turf` em `tests/Turfano.Tests/Parity/TransformTests.cs`;
  `transformScale` bate com o `@turf` (geodésico, não colapsa).

**Checkpoint**: transformações fiéis; divergência da Fase 1/2 (`transformScale`) consertada.

---

## Phase 5: User Story 3 — Geração e suavização (Priority: P2)

**Goal**: `circle`, `bezierSpline`, `polygonSmooth`, `lineOffset`, `simplify`.

**Independent Test**: comparar com o `@turf` por fixture.

- [X] T011 [P] [US3] `Geo.Circle` em `src/Turfano/Parity/Generate.Circle.cs` (N passos de
  `Destination` do centro; raio em `Units.Length`).
- [X] T012 [P] [US3] `Geo.Simplify` (Douglas-Peucker; decisão da Fase 2) em
  `src/Turfano/Parity/Generate.Simplify.cs` (conferir divergência com `simplify-js`).
- [ ] T013 [P] [US3] `Geo.PolygonSmooth` (Chaikin) + `Geo.LineOffset` em
  `src/Turfano/Parity/Generate.SmoothOffset.cs` (portar o algoritmo do `@turf`).
- [ ] T014 [P] [US3] `Geo.BezierSpline` em `src/Turfano/Parity/Generate.BezierSpline.cs`
  (porte do algoritmo de spline do `@turf`).
- [ ] T015 [US3] Testes vs `@turf` em `tests/Turfano.Tests/Parity/GenerateTests.cs`.

**Checkpoint**: geração/suavização conforme o `@turf`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T016 Verificação final (SC-004/005): build `Turfano.slnx` (0 erros, net8/9/10) +
  suíte (203 + novos, 0 falhas) + smoke AOT (0 warnings IL) +
  `git diff --stat main -- 'src/Turfano/Turf.*.cs'` vazio.
- [ ] T017 [P] Remover o harness efêmero `reference/_transform.mjs`.
- [ ] T018 Atualizar `plans/turfjs-parity-redesign.md`: Fase 6 (Onda C) → `Complete` +
  Phase Summary.

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002 harness)** → **US1/US2/US3**.
- US1/US2/US3 dependem do harness mas são **independentes entre si** (arquivos distintos);
  `rewind` reusa `BooleanClockwise` (já na `main`); `transformScale`/`circle` reusam
  `Rhumb*`/`Destination` (já na `main`).
- Dentro de cada história: implementar → testar.
- **Polish** após todas.

### Parallel Opportunities

- T003 ∥ T004 ∥ T005 (US1); T008 ∥ T009 (US2); T011 ∥ T012 ∥ T013 ∥ T014 (US3).
- US1, US2, US3 inteiras podem ser conduzidas em paralelo após o harness.

---

## Implementation Strategy

### MVP

Setup → Foundational (harness) → **US1** (mutações, baixo risco) + **US2** (`transformScale`
geodésico — a correção-chave). As duas P1.

### Incremental

1. Harness (ground-truth).
2. US1 mutações → US2 transformações (incl. `transformScale` geodésico).
3. US3 geração/suavização (mais algorítmicas).
4. Polish: verificação + limpar harness + atualizar plano-mãe.

---

## Notes

- **Estratégia mista**: re-tipar onde o existente é fiel; **portar o `@turf`** onde diverge
  (`transformScale` é cartesiano no NTS, geodésico no `@turf`). Validar TUDO vs `@turf`.
  Quando o GT surpreender, **seguir o `@turf`** (lição de `overlap`/`valid` na Onda B).
- **Não-regressão** (FR-004): `Turf.*.cs` NTS, NTS e UnitsNet permanecem; suíte 203 verde;
  AOT-safe.
- **Nomes .NET** (FR-005): sem acrônimos crípticos; nomes de domínio (`BBox`, `lon`/`lat`)
  permitidos (ver `CLAUDE.md` ## Conventions, `tasks/lessons.md`).
- Reusar helpers das Ondas A/B: `Distance`/`Bearing`/`Destination`/`Rhumb*`/`BooleanClockwise`/
  `EachSegment`/`EachPosition`.
- Fora de escopo: demais ondas, remover NTS/UnitsNet, funções fora de transformation/mutation.
