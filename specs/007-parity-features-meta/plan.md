# Implementation Plan: Onda D — Feature Conversion, Joins & Meta (paridade)

**Branch**: `007-parity-features-meta` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/007-parity-features-meta/spec.md`

## Summary

Portar as funções de conversão de feature, joins, utilitários de linha e meta-iteração do
TurfJS para a fachada **`Geo`** (`Turfano.GeoJson`), fiéis ao `@turf`. **Estratégia mista**
(como nas ondas anteriores): **re-tipar** as conversões estruturais simples (reshape de
coordenadas), **portar o `@turf`** onde necessário, e **reusar fortemente** os helpers das
Ondas A/B/C (`BooleanPointInPolygon`, `Distance`, `Along`, `NearestPointOnLine`,
`EachSegment`, `FlattenGeometry`, `EachPosition`, `SegmentsIntersect`, `MapPositions`).
Validar **tudo** vs `@turf` real; quando o ground-truth surpreender, seguir o `@turf`.

## Technical Context

**Language/Version**: C# (`Nullable`/`ImplicitUsings`), SDK `10.0.301`, multi-target
`net8.0;net9.0;net10.0`.

**Primary Dependencies**: `Turfano.GeoJson` (fachada `Geo`, Ondas A/B/C na `main`). NTS/
UnitsNet **permanecem** (interinos; `polygonize` pode usar a ponte `NtsBridge` se casar com o
`@turf`). Validação: `@turf` real via bun.

**Storage**: N/A. **Testing**: TUnit + harness Bun (ground-truth do `@turf`, incl. índices
das meta-funções).

**Target Platform**: biblioteca multi-target; AOT-safe (sem reflexão nova).

**Project Type**: biblioteca (adição de funções sobre os tipos da Fase 3).

**Performance Goals**: N/A (paridade é o foco).

**Constraints**: bater com o `@turf` (incl. ordem/índices das meta); fronteira do
`pointsWithinPolygon`; não alterar `Turf.*.cs`/NTS/UnitsNet; suíte 215 verde; 0 warnings AOT;
nomes .NET (sem acrônimos).

**Scale/Scope**: ~19 funções (6 conversão + 2 joins + 5 misc + 6 meta).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constituição não-ratificada (template) → **PASS** trivial. Princípios praticados:
fidelidade ao `@turf` (validar, não presumir), não-regressão, AOT-safety, nomes .NET.

## Project Structure

### Documentation (this feature)

```text
specs/007-parity-features-meta/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/public-api.md   # assinaturas Geo.* (conversão/joins/misc/meta)
├── checklists/requirements.md
└── tasks.md                  # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/                  # NOVAS funções: partials de `Geo`
├── Convert.cs                       # explode, combine, flatten, lineToPolygon, polygonToLine, polygonize
├── Join.cs                          # tag, pointsWithinPolygon
├── Misc.cs                          # kinks, lineChunk, lineSlice, lineSliceAlong, nearestPoint
└── Meta.cs                          # coordEach/coordReduce, featureEach, geomEach, propEach, segmentEach/segmentReduce, flattenEach
tests/Turfano.Tests/Parity/          # testes por função vs @turf
# src/Turfano/Turf.{Explode,Kinks,LineChunk,LineSlice,LineSliceAlong,Meta,NearestPoint}.cs (NTS) → INALTERADOS
```

**Structure Decision**: funções como **partials de `Geo`** (`Turfano.GeoJson`), padrão das
Ondas A/B/C. Recebem/retornam `Turfano.GeoJson`; `Turfano.Units` onde aplicável; opções como
parâmetros. As meta-funções públicas espelham os helpers `internal` com a assinatura/índices
do `@turf`.

## Complexity Tracking

> Sem violações de constituição — seção vazia.
