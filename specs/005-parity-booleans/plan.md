# Implementation Plan: Onda B — Booleans / Assertions (paridade)

**Branch**: `005-parity-booleans` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-parity-booleans/spec.md`

## Summary

Portar as 14 funções `boolean*` do TurfJS para a fachada **`Geo`** (`Turfano.GeoJson`), com
a **semântica do Turf**. **Insight-chave (oposto da Onda A)**: aqui **NÃO se re-tipa** — os
`Boolean*.cs` atuais delegam a **predicados do NTS** (`polygon.Contains(point)`,
`feature1.Overlaps(...)`) que **divergem do `@turf` na fronteira** (Fase 2). Logo, **portar
o algoritmo do `@turf`** sobre os novos tipos, reusando os helpers da Onda A
(`PointInPolygon`/`InRing`, `NearestPointOnLine`, `IsLeft`). Duas funções (`booleanConcave`,
`booleanValid`) nem existem hoje → porte completo. Validar contra o `@turf` real **+ as
fixtures true/false** dos pacotes `@turf/boolean-*`.

## Technical Context

**Language/Version**: C# (`Nullable`/`ImplicitUsings`), SDK `10.0.301`, multi-target
`net8.0;net9.0;net10.0`.

**Primary Dependencies**: `Turfano.GeoJson` (fachada `Geo`, Onda A na `main`). NTS/UnitsNet
**permanecem** (interinos, não nas novas assinaturas). Validação: `@turf` real + fixtures
`true/false` via bun em `reference/`.

**Storage**: N/A.

**Testing**: TUnit + harness Bun que (a) emite o resultado do `@turf` por caso e (b) varre
as fixtures `test/true`/`test/false` dos pacotes `@turf/boolean-*`.

**Target Platform**: biblioteca multi-target; AOT-safe (sem reflexão nova).

**Project Type**: biblioteca (adição de predicados `bool` sobre os tipos da Fase 3).

**Performance Goals**: N/A (paridade é o foco).

**Constraints**: semântica do `@turf` (fronteira/igualdade/orientação), **não** a do NTS;
não alterar `Turf.*.cs`/NTS/UnitsNet; suíte 193 verde; 0 warnings AOT.

**Scale/Scope**: 14 predicados booleanos.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constituição não-ratificada (template) → **PASS** trivial. Princípios praticados:
fidelidade ao `@turf` (validar, não presumir), não-regressão, AOT-safety.

## Project Structure

### Documentation (this feature)

```text
specs/005-parity-booleans/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/public-api.md   # assinaturas Geo.Boolean* (bool)
├── checklists/requirements.md
└── tasks.md                  # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/                  # NOVOS predicados: partials de `Geo`
├── Boolean.PointInPolygon.cs        # porte do @turf inRing/inBBox + ignoreBoundary
├── Boolean.PointOnLine.cs           # + ignoreEndVertices/epsilon
├── Boolean.Clockwise.cs, Boolean.Concave.cs, Boolean.Parallel.cs
├── Boolean.Relations.cs             # contains/within/disjoint/intersects/crosses/overlap/touches
├── Boolean.Equal.cs                 # geojson-equality (ordem/precisão)
└── Boolean.Valid.cs

tests/Turfano.Tests/Parity/          # testes por função + varredura de fixtures @turf
# src/Turfano/Turf.Boolean*.cs (NTS) → INALTERADOS (não re-tipados; divergem do @turf)
```

**Structure Decision**: predicados como **partials de `Geo`** (`Turfano.GeoJson`), padrão
da Onda A. Devolvem `bool`; opções como parâmetros (`bool ignoreBoundary = false`,
`bool ignoreEndVertices = false`). Os `Turf.Boolean*.cs` NTS-based permanecem (não são
fonte — divergem).

## Complexity Tracking

> Sem violações de constituição — seção vazia.
