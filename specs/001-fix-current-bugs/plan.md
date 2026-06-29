# Implementation Plan: Correção de bugs do Turfano (manutenção)

**Branch**: `001-fix-current-bugs` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-fix-current-bugs/spec.md`

## Summary

Corrigir dois bugs latentes no Turfano atual (ainda NTS/UnitsNet): (1) a constante
`Angles.TwoPi` vale π em vez de 2π, o que corrompe `RhumbBearing` e o caminho
`Explementary` de `GetAngle`; (2) um erro de precedência de operador em
`Turf.TransformScale` faz o eixo Y colapsar para uma constante no caso padrão.
Abordagem técnica (validada lendo o código): a correção da constante para `2π`
conserta **os três** consumidores afetados de uma vez (verificado nos 4 pontos de uso);
a correção de `TransformScale` é parentização de `(options.FactorY ?? factor)`. Em
seguida, adicionar testes de regressão TUnit ancorados em valores do TurfJS para os
três caminhos hoje **sem nenhum teste**. Correção de comportamento apenas — sem mudança
de API pública, de dependências ou de frameworks-alvo.

## Technical Context

**Language/Version**: C# com `Nullable` e `ImplicitUsings` habilitados; sem
`LangVersion` explícito (padrão do SDK por TFM). SDK fixado por `global.json` em
`10.0.301` (`rollForward: latestMinor`).

**Primary Dependencies**: NetTopologySuite 2.5.0, NetTopologySuite.Features 2.2.0,
UnitsNet 6.0.0-pre013 (lib). Testes: TUnit 1.56.25 sobre Microsoft.Testing.Platform,
NetTopologySuite.Features 2.2.0, NetTopologySuite.IO.GeoJSON4STJ 4.0.0.

**Storage**: N/A.

**Testing**: TUnit. Suíte completa: `dotnet run --project tests/Turfano.Tests`. Teste
único: `dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/<Classe>/*"`.

**Target Platform**: biblioteca multi-target `net8.0;net9.0;net10.0`; projeto de testes
é `Exe` em `net10.0`.

**Project Type**: biblioteca (single project, fachada estática `Turf` em namespace
`Turfano`).

**Performance Goals**: N/A — correção aritmética; não deve haver regressão de
desempenho (nenhum caminho quente alterado de forma material).

**Constraints**: comportamento-apenas. PROIBIDO alterar superfície de API pública,
dependências (NTS/UnitsNet/TUnit) e TFMs. A suíte completa deve permanecer verde.

**Scale/Scope**: ~2 arquivos de produção alterados (`Angles.cs`,
`Turf.TransformScale.cs`) + validação de 2 consumidores (`Turf.RhumbBearing.cs`,
`Turf.Angle.cs`); ~3 arquivos de teste novos. Sem migração de dados.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

A constituição do projeto (`.specify/memory/constitution.md`) **ainda é o template
não-ratificado** (apenas placeholders `[PRINCIPLE_*]`). Não há princípios vinculantes,
portanto o gate **passa trivialmente** — sem violações a justificar.

- Recomendação (fora de escopo desta feature): ratificar uma constituição real via
  `/speckit-constitution` antes das fases de redesign do plano-mãe.
- Princípios de fato já praticados no repositório e respeitados por esta feature:
  fidelidade ao TurfJS, testes com valores de referência do TurfJS, mudanças mínimas
  e cirúrgicas (CLAUDE.md do projeto).

**Resultado do gate**: PASS (sem constituição vinculante). `Complexity Tracking` vazio.

## Project Structure

### Documentation (this feature)

```text
specs/001-fix-current-bugs/
├── plan.md              # Este arquivo (/speckit-plan)
├── research.md          # Fase 0 (/speckit-plan)
├── data-model.md        # Fase 1 (/speckit-plan)
├── quickstart.md        # Fase 1 (/speckit-plan)
├── contracts/
│   └── public-api.md    # Fase 1 — contratos públicos (inalterados) afetados
├── checklists/
│   └── requirements.md  # do /speckit-specify
└── tasks.md             # Fase 2 (/speckit-tasks — NÃO criado aqui)
```

### Source Code (repository root)

```text
src/Turfano/
├── Angles.cs                 # FIX: TwoPi = 2π (hoje = π)
├── Turf.TransformScale.cs    # FIX: precedência → dy * (options.FactorY ?? factor)
├── Turf.RhumbBearing.cs      # VALIDAR: consumidor de Angles.Pi/TwoPi (sem alterar)
└── Turf.Angle.cs             # VALIDAR: GetAngle caminho Explementary (sem alterar)

tests/Turfano.Tests/
├── RhumbBearingTests.cs      # NOVO: rumos >180°, antimeridiano, âncora -170.29°
├── TransformScaleTests.cs    # NOVO: caso padrão (falha antes / passa depois) + FactorY/Z/Origin
└── GetAngleTests.cs          # NOVO: caminho Explementary (360° − θ)
```

**Structure Decision**: single project existente. Nenhuma estrutura nova; a feature
edita 2 arquivos de produção, valida 2, e adiciona 3 arquivos de teste em
`tests/Turfano.Tests`. Sem novos projetos, pastas ou camadas.

## Complexity Tracking

> Sem violações de constituição — seção vazia.
