# Implementation Plan: Saída do motor NTS — primeira leva (engine exit)

**Branch**: `009-nts-engine-exit` | **Date**: 2026-07-01 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/009-nts-engine-exit/spec.md`

## Summary

Trocar o motor NTS interino do overlay por um **porte fiel do `polyclip-ts`**
(Martinez–Rueda, a fonte que o `@turf` executa) sobre os tipos próprios, portar o
`@turf/polygonize`, e isolar `Buffer` + interop num **pacote satélite**
`Turfano.NetTopologySuite`. O porte honesto do polyclip são **três peças**: o core (1.137
linhas), a **`SplayTreeSet`** (`splaytree-ts`, 687 linhas, BSD-3) e um **decimal exato
mínimo** substituindo o `bignumber.js` (as coordenadas fluem como decimal arbitrário pelo
algoritmo inteiro; o bundle **não usa divisão** — só `plus/minus/times/compare/abs` — então
o decimal é 100% exato, sem semântica de arredondamento). Validação por regressão: os
testes-âncora das Ondas D/E (valores do `@turf` pinados) precisam continuar verdes com o
motor trocado.

## Technical Context

**Language/Version**: C# (`Nullable`/`ImplicitUsings`), SDK `10.0.301`, multi-target
`net8.0;net9.0;net10.0`.

**Primary Dependencies**: core: nenhuma nova (o porte é autocontido; `System.Numerics.
BigInteger` é da BCL). Satélite: **NetTopologySuite 2.5** (escondido do core).
Fontes do porte: `reference/node_modules/{polyclip-ts,splaytree-ts}` (MIT / BSD-3).

**Storage**: N/A. **Testing**: TUnit; regressão pelas âncoras das Ondas D/E; testes novos
para o satélite (buffer + bridge pública + fronteira empacotada).

**Target Platform**: biblioteca multi-target; core AOT-safe (o porte não usa reflexão).

**Project Type**: biblioteca + novo projeto satélite.

**Performance Goals**: paridade primeiro. O polyclip usa decimal arbitrário em todo o
sweep (como o `@turf` em produção) — otimização "caminho rápido em double com fallback
exato" fica **fora de escopo** (registrada como futuro).

**Constraints**: fidelidade à fonte (mesma ordenação de eventos/predicados); `Parity/`
livre de NTS ao fim; `Turf.*.cs` legadas intactas; suíte 232 verde; `UnsafeAccessor`
proibido; nomes .NET.

**Scale/Scope**: ~2,1 mil linhas de porte delicado (polyclip+splaytree+decimal) + 1
projeto novo + rewiring de 5 funções.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constituição não-ratificada (template) → **PASS** trivial. Princípios: fidelidade à fonte,
decisão fundamentada em medição (Fase 11 do plano-mãe), não-regressão, atribuição de
licenças (NOTICE).

## Project Structure

### Documentation (this feature)

```text
specs/009-nts-engine-exit/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/public-api.md
├── checklists/requirements.md
└── tasks.md                  # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/Polyclip/          # NOVO: motor portado (tudo internal)
├── ExactDecimal.cs                   # decimal exato (BigInteger mantissa + expoente)
├── SplayTreeSet.cs                   # porte do splaytree-ts (BSD-3)
├── PolyclipPrecision.cs              # compare/orient/snap (precision do polyclip)
├── PolyclipGeometry.cs               # geom-in/bbox/vector (entrada + envelopes)
├── SweepEvent.cs, Segment.cs         # eventos e segmentos do sweep
├── SweepLine.cs, PolyclipOperation.cs# a linha de varredura + union/intersection/xor/difference
└── PolyclipOutput.cs                 # geom-out (reconstrução dos anéis)
src/Turfano/Parity/Overlay.cs         # REWIRED: Union/Difference/Intersect/Dissolve → Polyclip
src/Turfano/Parity/Convert.Polygonize.cs  # REWIRED: grafo do @turf/polygonize (sem NTS)
src/Turfano/Interop/NtsBridge.cs      # REMOVIDO do core (vira NtsConvert público no satélite)
src/Turfano/Parity/Overlay.Buffer.cs  # MOVIDO para o satélite

src/Turfano.NetTopologySuite/         # NOVO projeto (referencia Turfano + NTS 2.5)
├── Turfano.NetTopologySuite.csproj
├── NtsConvert.cs                     # bridge PÚBLICA: ToNts/FromNts (fronteira EMPACOTADA)
└── NtsGeometryExtensions.cs          # Buffer como extension de Turfano.GeoJson.Geometry

tests/Turfano.Tests/                  # ganha ProjectReference ao satélite
├── Parity/OverlayTests.cs            # inalterados (regressão do motor)
├── Parity/BufferTests.cs             # atualizados p/ a API do satélite (+ teste de sequência empacotada)
└── GeoJsonFactoryAndBridgeTests.cs   # bridge tests → NtsConvert público
NOTICE                                # atribuições MIT/BSD-3 dos ports
```

**Structure Decision**: motor polyclip como **tipos `internal`** sob `Parity/Polyclip/`
(a API pública não muda: `Geo.Union` etc. mantêm assinaturas). Satélite
**`Turfano.NetTopologySuite`** (convenção do ecossistema): `NtsConvert` estático público
(estilo `Convert`/`JsonConvert`) + `Buffer` como **método de extensão** sobre
`Turfano.GeoJson.Geometry` (partial não atravessa assemblies). O satélite reimplementa
localmente o pequeno helper de mapeamento de posições (não abre `InternalsVisibleTo` novo).

## Complexity Tracking

> Sem violações de constituição. Registro do risco principal: o Martinez–Rueda é o porte
> mais delicado do projeto — mitigado por (a) porte 1:1 da fonte (mesma estrutura de
> módulos), (b) decimal exato (sem arredondamento a divergir), (c) regressão pelas âncoras
> do `@turf` já pinadas nos testes das Ondas D/E.
