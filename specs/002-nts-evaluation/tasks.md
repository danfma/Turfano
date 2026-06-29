---
description: "Task list — Avaliação NTS × TurfJS + benchmark (Fase 2)"
---

# Tasks: Avaliação NTS × TurfJS + benchmark

**Input**: Design documents from `specs/002-nts-evaluation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/evaluation-doc-schema.md, quickstart.md

**Tests**: SEM tarefas de teste TDD — é pesquisa/spike, não há código de produção. A
verificação é cobertura do documento + o benchmark rodar + a suíte existente seguir verde.

**Organization**: por user story. **US2 (catálogo+divergências) alimenta US1 (matriz de
decisão)**; US3 (benchmark) e US4 (UnitsNet) são independentes. Entregável central:
`docs/nts-evaluation.md` (estrutura em `contracts/evaluation-doc-schema.md`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3 / US4
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [X] T001 Criar `docs/nts-evaluation.md` com o esqueleto das 6 seções exigidas em
  `specs/002-nts-evaluation/contracts/evaluation-doc-schema.md` (Classificação,
  Divergências, Matriz de decisão, Benchmark, Inventário UnitsNet, Recomendação final).

---

## Phase 2: Foundational (Blocking Prerequisites)

> Sem tarefas. O harness de comparação é específico da US2 (Phase 3) e vive lá.

---

## Phase 3: User Story 2 — Catálogo de divergências validado (Priority: P1)

**Goal**: tabela `função → (classificação, divergência vs TurfJS)` cobrindo 100% das
funções de `Turf`, com cada divergência de wrapper-NTS comprovada por par de valores.

**Independent Test**: Seção 1 cobre todas as funções; cada `divergesFromTurf=yes` tem
evidência na Seção 2.

> Esta história vem **antes** da US1 porque produz os dados que a matriz de decisão usa.

- [X] T002 [P] [US2] Catalogar todas as funções de `Turf` (lendo `src/Turfano/*.cs`):
  classificação `nts-wrapper`/`own`/`naive` + visibilidade `public`/`internal`. Preencher
  a **Seção 1** de `docs/nts-evaluation.md`.
- [X] T003 [US2] Criar o harness de ground-truth do TurfJS em `reference/` (script Bun +
  conjunto canônico de fixtures: pontos/linhas/polígonos) que emite as saídas do `@turf`
  por função/fixture em JSON. Registrar o comando no doc (FR-008).
- [X] T004 [US2] Criar um runner C# descartável (ex.: `reference/`-paralelo ou um
  pequeno console) que emite as saídas do **Turfano** para as mesmas fixtures.
- [X] T005 [US2] Comparar as saídas conforme o critério de `research.md` (Decisão 2:
  escalar `>1e-6` / booleano qualquer diferença / geometria por coordenada-área) e
  preencher a **Seção 2** (pares de valores Turfano vs `@turf`); atualizar a coluna
  `divergesFromTurf` da Seção 1.

**Checkpoint**: catálogo + divergências prontos e reproduzíveis.

---

## Phase 4: User Story 1 — Matriz de decisão op-a-op (Priority: P1) 🎯 MVP

**Goal**: cada operação pesada/ingênua com decisão {portar/NTS-interino/aproximar} +
justificativa + lib/algoritmo do Turf + custo de porte.

**Independent Test**: Seção 3 cobre 100% das ops pesadas/ingênuas com `decision` preenchida.

> **Depende da US2** (usa a magnitude de divergência da Seção 2).

- [X] T006 [US1] Para cada op pesada/ingênua (`union, difference, intersect, dissolve,
  buffer, convex, simplify, bboxClip, tin, voronoi, concave, tesselate, isobands,
  isolines, bezierSpline`), identificar a lib/algoritmo do TurfJS lendo
  `reference/node_modules/@turf/<fn>/` (código + `package.json` deps) e estimar o custo
  de porte (P/M/G). Anotar na **Seção 3** de `docs/nts-evaluation.md`.
- [X] T007 [US1] Preencher a **Seção 3** (matriz op-a-op) com a `decision`
  {portar/nts-interino/aproximar} + `rationale`, usando `divergenceMagnitude` da US2 e o
  `portCost` do T006.

**Checkpoint**: a decisão que destrava a Fase 3 está registrada (MVP da fase).

---

## Phase 5: User Story 3 — Benchmark tipos próprios vs NTS (Priority: P2)

**Goal**: números (tempo + alocação) de `struct` próprio vs NTS em Distance/Area/WalkAlong.

**Independent Test**: o benchmark roda e emite a tabela comparativa.

- [X] T008 [P] [US3] Adicionar protótipo **descartável** em
  `benchmark/TimeAndMemoryUsage/`: um `readonly record struct` de posição + versões
  próprias de `Distance`/`Area`/`WalkAlong`, com benches `[MemoryDiagnoser]` comparando
  contra as versões atuais (NTS).
- [X] T009 [US3] Rodar `dotnet run -c Release --project benchmark/TimeAndMemoryUsage` e
  preencher a **Seção 4** com tempo + bytes alocados (próprios vs NTS) + ressalvas de
  microbenchmark.

**Checkpoint**: evidência de performance registrada.

---

## Phase 6: User Story 4 — Inventário de UnitsNet (Priority: P3)

**Goal**: lista fechada dos tipos UnitsNet usados, com pontos de uso.

**Independent Test**: Seção 5 lista cada tipo e onde é consumido.

- [X] T010 [P] [US4] Inventariar UnitsNet (`grep -rn 'UnitsNet\|Length\|Angle\|Area'
  src/Turfano --include='*.cs'`) e preencher a **Seção 5** (tipo → arquivos/funções).
  Confirmar se o conjunto é fechado (esperado: `Length`, `Angle`, `Area`).

**Checkpoint**: insumo de unidades para a Fase 3 pronto.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T011 Escrever a **Seção 6** (recomendação final por operação: manter/remover NTS +
  conclusão sobre adotar tipos próprios), consolidando as Seções 3 e 4.
- [X] T012 Verificar SC-006: `git diff --stat main -- src/Turfano` vazio e
  `dotnet run --project tests/Turfano.Tests -c Debug` = 156/0; registrar no doc.
- [X] T013 [P] Remover (ou marcar claramente como fixtures de avaliação, não-produção) os
  scripts efêmeros em `reference/` e os protótipos em `benchmark/`, conforme a decisão.
- [X] T014 Atualizar `plans/turfjs-parity-redesign.md`: Fase 2 → `Complete` + Phase
  Summary com a recomendação consolidada (insumo direto da Fase 3).

---

## Dependencies & Execution Order

- **Setup (T001)** antes de tudo.
- **US2 (T002–T005)**: T002 `[P]` (catálogo) ∥ T003 (harness); T003 → T004 → T005 (diff).
- **US1 (T006–T007)**: T006 pode começar cedo (ler `@turf`); T007 **depende da US2** (T005).
- **US3 (T008–T009)** e **US4 (T010)**: independentes — podem rodar em paralelo a US1/US2.
- **Polish**: T011 depende de T007 + T009; T012/T013 perto do fim; T014 por último.

### Parallel Opportunities

- T002 (catálogo) ∥ T003 (harness) ∥ T008 (benchmark) ∥ T010 (UnitsNet) — arquivos
  distintos.
- US3 e US4 inteiras podem ser conduzidas em paralelo às P1 (US2/US1).

---

## Parallel Example

```bash
# Frentes independentes em paralelo:
Task: "T002 catalogar funções → Seção 1"      # US2
Task: "T003 harness Bun de ground-truth"        # US2
Task: "T008 protótipo de benchmark struct vs NTS" # US3
Task: "T010 inventário UnitsNet → Seção 5"      # US4
```

---

## Implementation Strategy

### MVP (mínimo que destrava o redesign)

US2 → US1 (catálogo validado → matriz de decisão op-a-op). Com a Seção 3 preenchida, a
Fase 3 já pode começar; US3/US4 reforçam a decisão e dimensionam os tipos/unidades.

### Incremental

1. Setup (esqueleto do doc).
2. US2 (catálogo + divergências validadas) → US1 (matriz) — **decisão pronta**.
3. US3 (benchmark) + US4 (UnitsNet) em paralelo.
4. Polish: recomendação final + verificação SC-006 + limpeza + atualizar plano-mãe.

---

## Notes

- `[P]` = arquivos diferentes, sem dependências pendentes.
- **Sem código de produção** (FR-007/SC-006): `src/Turfano` é só leitura; tudo novo vive
  em `docs/`, `benchmark/` (descartável) e `reference/` (efêmero).
- Toda divergência/ganho registrado tem o comando que o reproduz (FR-008) — nada presumido.
- Fora de escopo (não criar tarefas): implementar tipos definitivos, remover NTS, portar
  funções (Fase 3+).
