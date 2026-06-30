---
description: "Task list — Onda E — Overlay/Clipping (Fase 8, paridade)"
---

# Tasks: Onda E — Overlay / Clipping (paridade com TurfJS)

**Input**: Design documents from `specs/008-parity-overlay/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — overlay/buffer validados por **ÁREA** vs `@turf` (~`1e-5`); `bboxClip`
estrutural (FR-003, SC-001/002).

**Organization**: por user story. **Overlay/buffer via `NtsBridge`** (motor NTS interino,
decisão medida da Fase 2); **`bboxClip` portado**. Fachada `Geo`; NTS escondido das
assinaturas. Harness (T002) foundational. Nada de produção atual é removido (suíte 226 verde).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: paralelizável (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [ ] T001 Confirmar baseline `dotnet build Turfano.slnx -c Debug` (0 erros) + suíte 226/0;
  reusar `src/Turfano/Parity/` e `tests/Turfano.Tests/Parity/`. Confirmar a API da
  `Turfano.Interop.NtsBridge` (`ToNts`/`FromNts`).

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T002 Criar o harness Bun `reference/_overlay.mjs` que emite, por função/fixture, a
  **ÁREA** do resultado do `@turf` (`union`/`difference`/`intersect`/`buffer`/`dissolve`) e a
  **estrutura** (`bboxClip`). Reproduzível (FR-003).

**Checkpoint**: ground-truth (áreas + estrutura) disponível para todas as histórias.

---

## Phase 3: User Story 1 — Overlay de polígonos (Priority: P1) 🎯 MVP

**Goal**: `union`, `difference`, `intersect`, `dissolve` batendo em **área** com o `@turf`.

**Independent Test**: comparar a área do resultado com o `@turf` (reusar `Geo.Area`).

- [X] T003 [US1] `Geo.Union` + `Geo.Difference` + `Geo.Intersect` em
  `src/Turfano/Parity/Overlay.cs` (via `NtsBridge`: `FromNts(ToNts(a).Union/Difference/
  Intersection(ToNts(b)))`; vazio → `null`).
- [X] T004 [US1] `Geo.Dissolve` em `src/Turfano/Parity/Overlay.cs` (Union dos polígonos da
  coleção via NTS + achatar).
- [X] T005 [US1] Testes de **área** vs `@turf` em
  `tests/Turfano.Tests/Parity/OverlayTests.cs` (union/intersect/difference ~`1e-5`, SC-002).

**Checkpoint**: overlay batendo em área com o `@turf` (MVP).

---

## Phase 4: User Story 2 — Buffer (Priority: P1)

**Goal**: `buffer` batendo com o `@turf` (o `@turf` buffer é JTS=NTS).

**Independent Test**: comparar a área do buffer com o `@turf`.

- [ ] T006 [US2] `Geo.Buffer` em `src/Turfano/Parity/Overlay.Buffer.cs` (via `NtsBridge`:
  `NTS Geometry.Buffer(raio)`; **conferir a conversão raio→graus do `@turf`** e validar a área).
- [ ] T007 [US2] Testes de área vs `@turf` em
  `tests/Turfano.Tests/Parity/BufferTests.cs`.

**Checkpoint**: buffer batendo com o `@turf`.

---

## Phase 5: User Story 3 — bboxClip (Priority: P2)

**Goal**: `bboxClip` recortando como o `@turf` (Cohen-Sutherland portado).

**Independent Test**: comparação estrutural com o `@turf`.

- [X] T008 [US3] `Geo.BBoxClip` em `src/Turfano/Parity/Clip.BBoxClip.cs` (portar Cohen-
  Sutherland; re-tipar do `Turf.BBoxClip.cs` e **validar vs `@turf`/`lineclip`**; sem NTS).
- [X] T009 [US3] Testes estruturais vs `@turf` em
  `tests/Turfano.Tests/Parity/BBoxClipTests.cs`.

**Checkpoint**: clipping conforme o `@turf`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T010 Verificação final (SC-004/005): build `Turfano.slnx` (0 erros, net8/9/10) + suíte
  (226 + novos, 0 falhas) + smoke AOT da **serialização** (0 warnings IL) +
  `git diff --stat main -- 'src/Turfano/Turf.*.cs'` vazio.
- [ ] T011 [P] Remover o harness efêmero `reference/_overlay.mjs`.
- [ ] T012 Atualizar `plans/turfjs-parity-redesign.md`: Fase 8 (Onda E) → `Complete` + Phase
  Summary (registrar overlay/buffer = NTS-interino; `bboxClip` portado).

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002 harness)** → **US1/US2/US3**.
- US1/US2/US3 dependem do harness mas são **independentes entre si** (arquivos distintos);
  overlay/buffer reusam a `NtsBridge` (já na `main`); testes reusam `Geo.Area` (Onda A).
- Dentro de cada história: implementar → testar.
- **Polish** após todas.

### Parallel Opportunities

- US1, US2, US3 inteiras podem ser conduzidas em paralelo após o harness (arquivos distintos).
- T003/T004 sequenciais (mesmo arquivo `Overlay.cs`).

---

## Implementation Strategy

### MVP

Setup → Foundational (harness) → **US1** (overlay via NtsBridge — área igual ao `@turf`) +
**US2** (buffer). As duas P1.

### Incremental

1. Harness (áreas + estrutura do `@turf`).
2. US1 overlay → US2 buffer (ambos via NtsBridge).
3. US3 bboxClip (portado).
4. Polish: verificação + limpar harness + atualizar plano-mãe.

---

## Notes

- **Overlay/buffer = NTS-interino** via `NtsBridge` (decisão MEDIDA da Fase 2: área idêntica
  ao `@turf`; `buffer` do `@turf` é JTS=NTS). **NTS escondido** das assinaturas públicas.
- **Validar por ÁREA** (não estrutura exata) p/ overlay/buffer — NTS e `polyclip-ts` podem
  diferir na representação dos anéis com a mesma área. Quando o GT surpreender, seguir o `@turf`.
- **Não-regressão** (FR-004): `Turf.*.cs` NTS, NTS e UnitsNet permanecem; suíte 226 verde.
- **AOT** (FR-005): overlay/buffer carregam o NTS (podem não ser AOT-safe) — aceito; smoke
  de AOT só na serialização (0 warnings).
- **Nomes .NET**: sem acrônimos crípticos; nomes de domínio (`BBox`, `lon`/`lat`) permitidos.
- Fora de escopo: demais ondas, remover NTS, funções fora de overlay/clipping.
