# Implementation Plan: Onda C — Transformation & Coordinate Mutation (paridade)

**Branch**: `006-parity-transformation` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-parity-transformation/spec.md`

## Summary

Portar as funções de transformação/mutação do TurfJS para a fachada **`Geo`**
(`Turfano.GeoJson`), fiéis ao `@turf`. **Estratégia mista por função** (como nas ondas
anteriores): **re-tipar** onde o código existente já é fiel (mutações simples), **portar o
`@turf`** onde diverge — em especial `transformScale` (a Fase 1/2 confirmou que o NTS é
**cartesiano**, o `@turf` é **geodésico**). `round` nem existe hoje → porte. Validar **tudo**
contra o `@turf` real (numérico apertado / estrutural); quando o ground-truth surpreender,
seguir o `@turf` (lição de `overlap`/`valid` na Onda B). Reusar helpers das Ondas A/B
(`Distance`, `Bearing`, `Destination`, `RhumbDistance`/`RhumbDestination`, `BooleanClockwise`,
`EachSegment`).

## Technical Context

**Language/Version**: C# (`Nullable`/`ImplicitUsings`), SDK `10.0.301`, multi-target
`net8.0;net9.0;net10.0`.

**Primary Dependencies**: `Turfano.GeoJson` (fachada `Geo`, Ondas A/B na `main`). NTS/UnitsNet
**permanecem** (interinos, não nas novas assinaturas). Validação: `@turf` real via bun.

**Storage**: N/A. **Testing**: TUnit + harness Bun (ground-truth do `@turf`).

**Target Platform**: biblioteca multi-target; AOT-safe (sem reflexão nova).

**Project Type**: biblioteca (adição de funções sobre os tipos da Fase 3).

**Performance Goals**: N/A (paridade é o foco).

**Constraints**: bater com o `@turf`; `transformScale` geodésico; não alterar `Turf.*.cs`/
NTS/UnitsNet; suíte 203 verde; 0 warnings AOT; convenção .NET de nomes (sem acrônimos).

**Scale/Scope**: 14 funções (5 mutação + 4 transform + 5 geração/suavização).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constituição não-ratificada (template) → **PASS** trivial. Princípios praticados:
fidelidade ao `@turf` (validar, não presumir), não-regressão, AOT-safety, nomes .NET.

## Project Structure

### Documentation (this feature)

```text
specs/006-parity-transformation/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/public-api.md   # assinaturas Geo.* (transform/mutation)
├── checklists/requirements.md
└── tasks.md                  # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/                  # NOVAS funções: partials de `Geo`
├── Mutate.CleanCoords.cs, Mutate.Flip.cs, Mutate.Rewind.cs, Mutate.RoundTruncate.cs
├── Transform.Rotate.cs, Transform.Translate.cs, Transform.Scale.cs, Transform.Clone.cs
├── Generate.Circle.cs, Generate.BezierSpline.cs, Generate.PolygonSmooth.cs,
│   Generate.LineOffset.cs, Generate.Simplify.cs
tests/Turfano.Tests/Parity/          # testes por função vs @turf
# src/Turfano/Turf.{Transform*,Circle,Simplify,...}.cs (NTS) → INALTERADOS
```

**Structure Decision**: funções como **partials de `Geo`** (`Turfano.GeoJson`), padrão das
Ondas A/B. Recebem/retornam `Turfano.GeoJson`; `Turfano.Units` onde aplicável; opções como
parâmetros. Nomes .NET, sem acrônimos.

## Complexity Tracking

> Sem violações de constituição — seção vazia.
