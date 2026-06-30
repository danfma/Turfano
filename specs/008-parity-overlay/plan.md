# Implementation Plan: Onda E — Overlay / Clipping (paridade)

**Branch**: `008-parity-overlay` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-parity-overlay/spec.md`

## Summary

Expor overlay (`union`/`difference`/`intersect`/`dissolve`), `buffer` e `bboxClip` na fachada
**`Geo`** (`Turfano.GeoJson`), fiéis ao `@turf`. **Estratégia (oposta às ondas anteriores,
herdada da MEDIÇÃO da Fase 2)**: overlay e `buffer` usam o **motor NTS** via a ponte
`Turfano.Interop.NtsBridge` — ida-e-volta novos-tipos ↔ NTS — porque o NTS dá **área idêntica**
ao `@turf` (`polyclip-ts`) e o `buffer` do `@turf` **é** o JTS (= NTS). Só o `bboxClip` é
**portado** (Cohen-Sutherland, re-tipando o que já existe). Validar **tudo** vs `@turf`
(área para overlay/buffer; estrutural para `bboxClip`).

## Technical Context

**Language/Version**: C# (`Nullable`/`ImplicitUsings`), SDK `10.0.301`, multi-target
`net8.0;net9.0;net10.0`.

**Primary Dependencies**: `Turfano.GeoJson` (fachada `Geo`); **NTS via `NtsBridge`** (motor
interino aceito para overlay/buffer). Validação: `@turf` real via bun.

**Storage**: N/A. **Testing**: TUnit + harness Bun (área do `@turf` para overlay/buffer;
estrutura para `bboxClip`).

**Target Platform**: biblioteca multi-target. **AOT**: overlay/`buffer` carregam o NTS
interino (podem não ser AOT-safe); o smoke de AOT segue exercitando só a **serialização**
(0 warnings).

**Project Type**: biblioteca (adição de funções sobre os tipos da Fase 3).

**Performance Goals**: N/A (paridade é o foco).

**Constraints**: bater com o `@turf` (área ~`1e-5` p/ overlay/buffer); NTS **escondido** das
assinaturas; não alterar `Turf.*.cs`; suíte 226 verde; nomes .NET.

**Scale/Scope**: 6 funções (4 overlay + buffer + bboxClip).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constituição não-ratificada (template) → **PASS** trivial. Princípios praticados:
fidelidade ao `@turf` (validar, não presumir — aqui a Fase 2 já mediu), não-regressão,
decisão de motor baseada em evidência, nomes .NET.

## Project Structure

### Documentation (this feature)

```text
specs/008-parity-overlay/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/public-api.md   # assinaturas Geo.* (overlay/buffer/bboxClip)
├── checklists/requirements.md
└── tasks.md                  # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/                  # NOVAS funções: partials de `Geo`
├── Overlay.cs                       # union, difference, intersect, dissolve (via NtsBridge)
├── Overlay.Buffer.cs                # buffer (via NtsBridge / NTS Geometry.Buffer)
└── Clip.BBoxClip.cs                 # bboxClip (Cohen-Sutherland portado)
tests/Turfano.Tests/Parity/          # testes por função vs @turf
# src/Turfano/Turf.{Union,Difference,Intersect,Buffer,BBoxClip}.cs (NTS) → INALTERADOS
```

**Structure Decision**: funções como **partials de `Geo`** (`Turfano.GeoJson`), padrão das
Ondas A–D. Overlay/buffer delegam ao NTS via `NtsBridge` (NTS **não** aparece nas assinaturas).
`bboxClip` é portado. Nomes .NET.

## Complexity Tracking

> Sem violações de constituição — seção vazia.
