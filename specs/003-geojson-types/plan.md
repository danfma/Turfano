# Implementation Plan: Sistema de tipos central GeoJSON + unidades + STJ source-gen

**Branch**: `003-geojson-types` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-geojson-types/spec.md`

## Summary

Construir a **fundação** do redesign: tipos GeoJSON RFC 7946 próprios (`Position`/`BBox`
como structs de valor; geometrias/Feature como `record` selado), serialização
**System.Text.Json source-generated** (AOT/trim-safe) com discriminador `type`, 3 structs
de unidade próprios (substituindo conceitualmente o UnitsNet, validados contra o `@turf`),
helpers ao estilo Turf, e uma **ponte interna** novos-tipos↔NTS para a transição. **Sem**
portar funções nem remover NTS/UnitsNet — a suíte atual (156) permanece verde.

## Technical Context

**Language/Version**: C# (`Nullable`+`ImplicitUsings`), SDK `10.0.301`, multi-target
`net8.0;net9.0;net10.0`.

**Primary Dependencies**: System.Text.Json (in-box; **source generator** para
`JsonSerializerContext`). Permanecem (interino): NetTopologySuite 2.5.0, UnitsNet
6.0.0-pre013. Validação numérica: `@turf` via Bun em `reference/`. Testes: TUnit.

**Storage**: N/A. Formato de fio: GeoJSON RFC 7946.

**Testing**: TUnit (round-trip + conversões de unidade). Smoke de AOT/trimming via app de
teste com `PublishAot`/`PublishTrimmed`.

**Target Platform**: biblioteca multi-target net8/9/10; STJ source-gen + polimorfismo
disponíveis em net8+.

**Project Type**: biblioteca (acréscimo de tipos novos ao `src/Turfano`).

**Performance Goals**: tipos de valor sem alocação nos caminhos quentes (`Position`/`BBox`
struct); serialização sem reflexão (source-gen).

**Constraints**: AOT/trimming sem warnings nos tipos do Turfano; **suíte 156 permanece
verde**; nada de produção (NTS/UnitsNet/funções) é removido; multi-target mantido.

**Scale/Scope**: ~9 tipos GeoJSON + `Position`/`BBox` + 3 structs de unidade + 1
`JsonSerializerContext` + 2 converters (Position/BBox) + helpers + ponte interna NTS.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constituição (`.specify/memory/constitution.md`) ainda é template **não-ratificado** →
gate **passa trivialmente**. Princípios praticados e respeitados: fidelidade ao TurfJS,
evidência reprodutível (validar unidades contra o `@turf`), e não-regressão (suíte 156
verde; NTS/UnitsNet preservados). **Resultado**: PASS. `Complexity Tracking` vazio.

## Project Structure

### Documentation (this feature)

```text
specs/003-geojson-types/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/public-api.md   # tipos públicos + helpers + forma JSON (RFC 7946)
├── checklists/requirements.md
└── tasks.md                  # (/speckit-tasks — depois)
```

### Source Code (repository root)

```text
src/Turfano/
├── GeoJson/                       # NOVOS tipos (namespace Turfano)
│   ├── Position.cs                # readonly record struct (+ converter)
│   ├── BBox.cs                    # readonly record struct (+ converter)
│   ├── GeoJsonObject.cs           # base abstrata polimórfica (discriminador "type")
│   ├── Geometry.cs + Point.cs ... # record selados (Point..GeometryCollection)
│   ├── Feature.cs, FeatureCollection.cs
│   ├── GeoJsonSerializerContext.cs# [JsonSerializable]+[JsonSourceGenerationOptions]
│   └── Factory.cs                 # helpers point()/lineString()/... + getCoord/getType
├── Units/                         # NOVOS structs de unidade (3) + enums + conversões
│   └── (Length/Distance, Angle/Bearing, Area)
├── Interop/NtsBridge.cs           # internal — novos-tipos ↔ NTS
└── (arquivos atuais Turf.*.cs)    # INALTERADOS nesta fase

tests/Turfano.Tests/               # round-trip + conversões de unidade (novos testes)
samples/ ou tests/ Aot smoke       # app mínimo p/ PublishAot/PublishTrimmed
```

**Structure Decision**: acrescentar pastas `GeoJson/`, `Units/`, `Interop/` em
`src/Turfano` (namespace `Turfano`), sem tocar nos `Turf.*.cs` atuais. A fachada `Turf`
segue intacta. Smoke de AOT num app de teste dedicado.

## Complexity Tracking

> Sem violações de constituição — seção vazia.
