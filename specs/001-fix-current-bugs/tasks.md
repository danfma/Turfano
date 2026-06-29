---
description: "Task list — Correção de bugs do Turfano (Fase 1)"
---

# Tasks: Correção de bugs do Turfano (manutenção)

**Input**: Design documents from `specs/001-fix-current-bugs/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — o spec pede explicitamente testes de regressão (FR-006, SC-003,
SC-005), com disciplina TDD (falhar antes do patch, passar depois).

**Organization**: tarefas agrupadas por user story. Observação-chave: o fix de
`Angles.TwoPi` é **compartilhado** por US1 e US3 → atribuído à US1 (história mais
prioritária que o consome); US3 **depende** dele. US2 (`TransformScale`) é independente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3
- Caminhos de arquivo são absolutos a partir da raiz do repositório

---

## Phase 1: Setup

**Purpose**: estabelecer a linha de base para diferenciar regressões.

- [ ] T001 Confirmar baseline verde no estado pré-patch e registrar a contagem:
  `dotnet build Turfano.slnx -c Debug` (0 erros) e
  `dotnet run --project tests/Turfano.Tests -c Debug` (esperado: 146 testes verdes).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: nenhuma infraestrutura compartilhada precisa ser construída para esta
feature.

> Sem tarefas. O único fix compartilhado (`Angles.TwoPi`) está atribuído à **US1**
> (Phase 3), por ser a história mais prioritária que o consome; a **US3** depende dele.
> Checkpoint: as user stories podem começar após o Setup.

---

## Phase 3: User Story 1 — Rumo (rhumb bearing) correto em todas as direções (Priority: P1) 🎯 MVP

**Goal**: `RhumbBearing` retorna valores idênticos ao TurfJS em todo o intervalo,
inclusive rumos > 180° e cruzamento do antimeridiano.

**Independent Test**: rodar `RhumbBearingTests` — a âncora `9.71°` e os casos > 180°/
antimeridiano passam.

### Tests for User Story 1 ⚠️ (escrever PRIMEIRO, confirmar que FALHAM)

- [ ] T002 [P] [US1] Criar `tests/Turfano.Tests/RhumbBearingTests.cs` com os casos:
  (a) âncora `RhumbBearing((-75.343,39.984),(-75.534,39.123)) ≈ 9.71°` `.Within(0.01)`;
  (b) rumo verdadeiro > 180° (ex.: apontando a sudoeste) batendo com o valor do TurfJS;
  (c) antimeridiano `(179,0)→(-179,0)` resultando no caminho curto (≈ leste);
  (d) `from == to` não lança e não produz `NaN`.
- [ ] T003 [US1] Rodar `dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/RhumbBearingTests/*"`
  e CONFIRMAR FALHA (red) no código atual; registrar a saída como evidência do bug.

### Implementation for User Story 1

- [ ] T004 [US1] Corrigir `src/Turfano/Angles.cs`: `TwoPi = Angle.FromRadians(2 * Math.PI)`
  (era `Math.PI`). Fix compartilhado — também habilita a US3.
- [ ] T005 [US1] Validar **sem editar** `src/Turfano/Turf.RhumbBearing.cs`: confirmar que
  a normalização de `deltaLambda` (`> Angles.Pi → -= Angles.TwoPi`) e o cálculo de
  `bear180` (`-(Angles.TwoPi - bear360)`) ficam corretos com a constante corrigida.
- [ ] T006 [US1] Rodar `RhumbBearingTests` (verde) e a suíte completa
  (`dotnet run --project tests/Turfano.Tests -c Debug`) para garantir não-regressão.

**Checkpoint**: US1 funcional e testável de forma independente (MVP).

---

## Phase 4: User Story 2 — Escala uniforme correta no eixo Y (Priority: P1)

**Goal**: `TransformScale(geom, fator)` sem `FactorY` escala X e Y pelo mesmo fator
(Y deixa de colapsar).

**Independent Test**: rodar `TransformScaleTests` — o caso padrão dobra a bbox em ambos
os eixos.

### Tests for User Story 2 ⚠️ (escrever PRIMEIRO, confirmar que FALHAM)

- [ ] T007 [P] [US2] Criar `tests/Turfano.Tests/TransformScaleTests.cs` com os casos:
  (a) caso padrão — polígono centrado na origem, `fator = 2` sem `FactorY` → largura e
  altura da bbox = 2× as originais (deve FALHAR antes do patch — SC-005);
  (b) `FactorY`/`FactorZ`/`Origin` explícitos escalando cada eixo a partir da origem;
  (c) encolher com `fator < 1`.
- [ ] T008 [US2] Rodar `dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/TransformScaleTests/*"`
  e CONFIRMAR FALHA do caso padrão (red; evidência do bug — SC-005).

### Implementation for User Story 2

- [ ] T009 [US2] Corrigir `src/Turfano/Turf.TransformScale.cs` (linha ~49):
  `var scaledY = dy * (options.FactorY ?? factor);` (parentização).
- [ ] T010 [US2] FR-009 — varrer o padrão de precedência:
  `grep -rn '\* options\.' src/Turfano --include='*.cs' | grep '??'`; corrigir qualquer
  ocorrência equivalente ou registrar como fora de escopo com justificativa.
- [ ] T011 [US2] Rodar `TransformScaleTests` (verde) e a suíte completa para não-regressão.

**Checkpoint**: US1 e US2 funcionais de forma independente (ambas P1).

---

## Phase 5: User Story 3 — Ângulo explementar correto (Priority: P2)

**Goal**: `GetAngle(..., Explementary: true)` retorna `360° − θ`.

**Independent Test**: rodar `GetAngleTests` — o caminho `Explementary` devolve o
suplemento a 360°.

> **Dependência**: a correção desta história vem do **T004** (US1). Esta fase
> acrescenta cobertura e valida; não aplica novo fix de produção.

### Tests for User Story 3 ⚠️

- [ ] T012 [P] [US3] Criar `tests/Turfano.Tests/GetAngleTests.cs`: ângulo base de um trio
  conhecido (sem `Explementary`) e `Explementary = 360° − θ`; borda em `0°`/`360°`.
- [ ] T013 [US3] Rodar `dotnet run --project tests/Turfano.Tests -c Debug -- --treenode-filter "/*/*/GetAngleTests/*"`
  e confirmar verde (correção já aplicada no T004). Nota: se rodado **antes** do T004,
  o caso `Explementary` falha — documenta o bug.

**Checkpoint**: todas as histórias independentemente funcionais.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T014 [P] (Opcional, cosmético) Remover comentários obsoletos
  `// filepath: /Users/danfma/Develop/private/...` dos arquivos `src/Turfano/*.cs`
  afetados (não é critério de sucesso — FR/assumptions do spec).
- [ ] T015 Verificação final (SC-004): `dotnet build Turfano.slnx -c Debug` (0 erros em
  todos os TFMs) + `dotnet run --project tests/Turfano.Tests -c Debug`
  (0 falhas; total = 146 + novos).
- [ ] T016 Atualizar `plans/turfjs-parity-redesign.md`: marcar os checkboxes da Fase 1,
  mudar Status para `Complete` e escrever o Phase Summary.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: vazia.
- **User Stories (Phase 3–5)**: começam após o Setup.
  - **US1 (P1)** e **US2 (P1)** são independentes entre si (arquivos de produção
    distintos: `Angles.cs`/`RhumbBearing.cs` vs `Turf.TransformScale.cs`) → podem rodar
    em paralelo.
  - **US3 (P2)** depende do **T004** (fix de `Angles.TwoPi`, na US1).
- **Polish (Phase 6)**: após todas as histórias.

### Within Each User Story

- Testes escritos e confirmados **vermelhos** antes da correção (TDD).
- Correção → re-rodar testes da história (verde) → suíte completa (não-regressão).

### Parallel Opportunities

- T002, T007, T012 (criar os três arquivos de teste — arquivos diferentes) podem ser
  feitos em paralelo `[P]`.
- US1 e US2 inteiras podem ser conduzidas em paralelo por pessoas diferentes.

---

## Parallel Example

```bash
# Escrever os arquivos de teste das três histórias em paralelo (arquivos distintos):
Task: "T002 RhumbBearingTests.cs"   # US1
Task: "T007 TransformScaleTests.cs" # US2
Task: "T012 GetAngleTests.cs"       # US3 (verde só após T004)
```

---

## Implementation Strategy

### MVP (mínimo demonstrável)

1. Phase 1 (Setup) → 2. US1 completa (T002–T006) → **validar RhumbBearing isolado**.
   Entrega a correção numérica mais central. As duas histórias P1 (US1 + US2) compõem o
   MVP completo desta entrega de manutenção.

### Entrega incremental

1. Setup → US1 (RhumbBearing + fix `TwoPi`) → testar/commit.
2. US2 (TransformScale) → testar/commit.
3. US3 (GetAngle explementar — já verde pelo fix da US1) → testar/commit.
4. Polish → verificação final + atualizar o plano-mãe.

---

## Notes

- `[P]` = arquivos diferentes, sem dependências pendentes.
- O fix de produção é minúsculo (1 linha em `Angles.cs` + 1 parentização em
  `Turf.TransformScale.cs`); o valor está nos **testes de regressão** ancorados no TurfJS.
- Confirmar testes **vermelhos** antes de corrigir (especialmente o caso padrão de
  `TransformScale` — SC-005).
- Commit após cada história ou grupo lógico.
- Fora de escopo (não criar tarefas): redesign (tipos GeoJSON/STJ/unidades próprias),
  remoção do NTS, e os algoritmos ingênuos (`Tin`/`Voronoi`/`Concave`/`Tesselate`).
