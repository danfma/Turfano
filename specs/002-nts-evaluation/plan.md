# Implementation Plan: Avaliação NTS × TurfJS + benchmark (Fase 2)

**Branch**: `002-nts-evaluation` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-nts-evaluation/spec.md`

## Summary

Conduzir a avaliação que destrava o redesign: catalogar todas as funções de `Turf`,
**validar** as divergências contra o TurfJS real (harness reprodutível em `reference/`
via Bun), preencher a matriz de decisão op-a-op (portar / NTS-interino / aproximar),
prototipar um benchmark de tipos de valor próprios vs NTS, inventariar o uso de UnitsNet,
e consolidar tudo em `docs/nts-evaluation.md` com recomendação por operação. **Nenhuma
mudança em código de produção** (`src/`); protótipos de benchmark e scripts de
comparação são descartáveis.

## Technical Context

**Language/Version**: C# (a lib `Turfano` atual, p/ as saídas e o benchmark) +
TypeScript sobre **Bun 1.3.14** (o TurfJS real em `reference/`, p/ os valores-verdade).

**Primary Dependencies**: existentes — NetTopologySuite 2.5.0, UnitsNet 6.0.0-pre013,
TUnit 1.56.25; **BenchmarkDotNet** (já presente em `benchmark/TimeAndMemoryUsage`);
`@turf/*` instalado em `reference/node_modules` (rhumb-*, angle, transform-*, turf, ...).

**Storage**: N/A. Saídas em Markdown (`docs/nts-evaluation.md`) e JSON intermediário
descartável (saídas turf vs turfano).

**Testing**: sem testes de produção novos. Verificação = cobertura/estrutura do
documento + o benchmark roda. A suíte existente permanece **156, 0 falhas**.

**Target Platform**: ambiente de dev (macOS/CI .NET 10 + Bun); benchmark em `-c Release`.

**Project Type**: pesquisa/spike — sem novo projeto; usa `benchmark/` e `reference/`
existentes + uma pasta `docs/` nova.

**Performance Goals**: **medir**, não atingir — quantificar tempo e alocação de tipos
próprios vs NTS em `Distance`, `Area`, `WalkAlong` (hipótese do README: ~25% tempo,
~40% memória).

**Constraints**: zero alteração em `src/` de produção (FR-007); divergências e ganhos
**reproduzíveis** (FR-008); protótipos isolados e removíveis.

**Scale/Scope**: ~77 arquivos `.cs` da lib → catalogar todas as funções de `Turf`;
~15 operações pesadas/ingênuas na matriz; 3 rotas no benchmark; 1 documento entregável.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` segue como template **não-ratificado** (placeholders).
Sem princípios vinculantes → o gate **passa trivialmente**. Princípios de fato
praticados e respeitados aqui: fidelidade ao TurfJS, evidência reprodutível (nada de
valor presumido — lição direta da Fase 1), e não-intrusão no código de produção.

**Resultado do gate**: PASS. `Complexity Tracking` vazio.

## Project Structure

### Documentation (this feature)

```text
specs/002-nts-evaluation/
├── plan.md              # Este arquivo (/speckit-plan)
├── research.md          # Fase 0 — decisões de metodologia
├── data-model.md        # Fase 1 — esquema das linhas do doc-entregável
├── quickstart.md        # Fase 1 — como conduzir e verificar
├── contracts/
│   └── evaluation-doc-schema.md  # Estrutura obrigatória de docs/nts-evaluation.md
├── checklists/
│   └── requirements.md  # do /speckit-specify
└── tasks.md             # Fase 2 (/speckit-tasks — NÃO criado aqui)
```

### Source Code (repository root)

```text
docs/
└── nts-evaluation.md            # NOVO — o entregável da fase (decisão + evidências)

benchmark/TimeAndMemoryUsage/
├── (protótipo descartável) PositionStruct + benches comparativos próprios vs NTS
│   para Distance/Area/WalkAlong (BenchmarkDotNet + MemoryDiagnoser)

reference/
└── (scripts Bun descartáveis) harness que emite as saídas do TurfJS p/ as fixtures

# src/Turfano/  → INALTERADO (somente leitura para classificação)
```

**Structure Decision**: pesquisa/spike sem novo projeto. O trabalho produz `docs/`,
estende temporariamente `benchmark/`, e usa scripts efêmeros em `reference/`. `src/` é
apenas lido (classificação de funções). Sem novas dependências de produção.

## Complexity Tracking

> Sem violações de constituição — seção vazia.
