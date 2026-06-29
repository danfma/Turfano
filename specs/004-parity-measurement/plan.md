# Implementation Plan: Onda A — Measurement (paridade)

**Branch**: `004-parity-measurement` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-parity-measurement/spec.md`

## Summary

Portar as funções de medição do TurfJS para operarem sobre os novos tipos
(`Turfano.GeoJson`) e unidades (`Turfano.Units`), batendo numericamente com o `@turf`.
**Insight-chave**: a maioria das medições já tem algoritmo fiel ao `@turf` nas `Turf.*.cs`
atuais (área esférica, distância haversine, bearing, rhumb*, along) — esta onda é
sobretudo **re-tipar lógica já correta** sobre os novos tipos + **consertar o `centroid`**
(excluir o vértice de fechamento) + usar os structs de unidade. As novas funções entram
como **sobrecargas na fachada `Turf`** recebendo os tipos `Turfano.GeoJson`; as `Turf.*.cs`
NTS-based permanecem durante a transição.

## Technical Context

**Language/Version**: C# (`Nullable`/`ImplicitUsings`), SDK `10.0.301`, multi-target
`net8.0;net9.0;net10.0`.

**Primary Dependencies**: os novos `Turfano.GeoJson`/`Turfano.Units` (Fase 3, na `main`);
NTS/UnitsNet **permanecem** (interinos, não usados nas novas assinaturas). Validação:
`@turf` real via bun em `reference/`. Testes: TUnit.

**Storage**: N/A.

**Testing**: TUnit + harness Bun (`reference/`) que emite os valores do `@turf` por
função/fixture; os testes C# comparam com tolerância apertada.

**Target Platform**: biblioteca multi-target; AOT-safe (nenhuma reflexão nova).

**Project Type**: biblioteca (adição de funções sobre os tipos da Fase 3).

**Performance Goals**: N/A (paridade numérica é o foco); tipos de valor já sem alocação.

**Constraints**: bater com o `@turf`; **não** alterar as `Turf.*.cs` atuais nem remover
NTS/UnitsNet; suíte existente (177) permanece verde; 0 warnings AOT.

**Scale/Scope**: ~24 funções de measurement + as conversões de unidade (já em `Units`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constituição não-ratificada (template) → gate **passa trivialmente**. Princípios
praticados: fidelidade ao `@turf` (validar, não presumir — lição das Fases 1–3),
não-regressão (suíte 177 verde; NTS/UnitsNet preservados), AOT-safety. **PASS**.

## Project Structure

### Documentation (this feature)

```text
specs/004-parity-measurement/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/public-api.md   # assinaturas das novas funções (sobre Turfano.GeoJson/Units)
├── checklists/requirements.md
└── tasks.md                  # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/                 # NOVAS funções: partials de `Turf` recebendo os tipos novos
├── Measure.Area.cs                 # Turf.Area(GeoJson.Geometry) : Units.Area  (esférica)
├── Measure.Distance.cs, Measure.Bearing.cs, Measure.Length.cs
├── Measure.Bbox.cs (bbox/bboxPolygon/square/envelope)
├── Measure.Centroid.cs             # CONSERTADO (exclui o vértice de fechamento)
├── Measure.Center.cs, Measure.Midpoint.cs, Measure.Destination.cs, Measure.Along.cs
├── Measure.Rhumb.cs (rhumbBearing/rhumbDistance/rhumbDestination)
├── Measure.PointDistances.cs (pointToLine/pointToPolygon/nearestPointOnLine)
├── Measure.PointOnFeature.cs, Measure.GreatCircle.cs, Measure.PolygonTangents.cs
└── Units.cs                        # expõe convert*/...ToRadians/bearingToAzimuth na fachada

tests/Turfano.Tests/Parity/         # testes por função vs @turf
# src/Turfano/Turf.*.cs (NTS) e Units/GeoJson/Interop → INALTERADOS (só leitura/reuso)
```

**Structure Decision**: novas funções como **sobrecargas `Turf`** (partials em
`src/Turfano/Parity/`) recebendo `Turfano.GeoJson` e devolvendo `Turfano.Units`/
`Turfano.GeoJson`. Convivem com as `Turf.*.cs` NTS-based até o fim da migração (quando as
antigas serão removidas). Nos arquivos novos, os tipos próprios são referenciados via
`GeoJson.`/`Units.` para não colidir com os tipos NTS do global using.

## Complexity Tracking

> Sem violações de constituição — seção vazia.
