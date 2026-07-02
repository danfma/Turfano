# Implementation Plan: Onda F — Interpolation, Grids & Triangulation

**Branch**: `010-parity-interpolation-grids` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/010-parity-interpolation-grids/spec.md`

## Summary

Sexta onda de paridade: grades (`pointGrid`/`squareGrid`/`hexGrid`/`triangleGrid`),
interpolação (`planepoint`/`tin`/`interpolate`) e contornos/hulls/tesselação (`isolines`/
`isobands`/`convex`/`concave`/`voronoi`/`tesselate`) na fachada `Geo`, fiéis ao `@turf`.
Substitui os **6 algoritmos ingênuos** do legado. Dois portes grandes medidos e decididos
na pesquisa: **earcut** (681 linhas) e **d3-voronoi** (~1.004) — mesmo método do polyclip.
`marchingsquares` **não precisa de porte externo** (embutido nos módulos do `@turf`).

## Technical Context

**Language/Version**: C# (.NET SDK 10.0.301), multi-target `net8.0;net9.0;net10.0`
**Primary Dependencies**: nenhuma nova no core; fontes portadas: `@turf/*` (MIT),
`earcut` (ISC © Mapbox), `d3-voronoi` (BSD-3 © Mike Bostock) → atualizar `NOTICE`
**Storage**: N/A | **Testing**: TUnit + harness bun efêmero (`reference/_wavef.mjs`)
**Target Platform**: biblioteca .NET (AOT-safe no core)
**Project Type**: biblioteca (fachada `Geo`, partials em `src/Turfano/Parity/`)
**Performance Goals**: sem metas nesta onda (fora de escopo)
**Constraints**: `Parity/` livre de NTS; legado intocado; suíte 245 verde; nomes .NET
**Scale/Scope**: 13 funções, ~3,4 mil linhas portadas, ~20 testes novos

## Constitution Check

Sem constituição formal (`.specify/memory/constitution.md` é template). Gates do projeto:
fidelidade ao `@turf` (GT via bun), tipos próprios, AOT-safe, sem acrônimos crípticos,
sem regressão — todos endereçados. **PASS** (pré e pós-design).

## Project Structure

### Documentation (this feature)

```text
specs/010-parity-interpolation-grids/
├── plan.md          # este arquivo
├── research.md      # medições + decisões R1–R7
├── data-model.md    # estruturas internas (Delaunay/Fortune/earcut)
├── quickstart.md    # como validar (bun GT + TUnit)
├── contracts/
│   └── public-api.md
└── tasks.md         # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/
├── Grid.PointGrid.cs          # US1 (42 linhas @turf)
├── Grid.RectangleGrid.cs      # US1 (55; squareGrid = wrapper)
├── Grid.HexGrid.cs            # US1 (113; mask via Intersect nativo)
├── Grid.TriangleGrid.cs       # US1 (133)
├── Interpolate.Planepoint.cs  # US2 (32)
├── Interpolate.Tin.cs         # US2 (191, Delaunay incremental autocontido)
├── Interpolate.Idw.cs         # US2 (92, usa grades)
├── Contour.Isolines.cs        # US3 (315, marching squares embutido)
├── Contour.Isobands.cs        # US3 (508, idem)
├── Hull.Convex.cs             # US4 (monotone chain do concaveman, R2)
├── Hull.Concave.cs            # US4 (tin + maxEdge + Union nativo, R3)
├── Tessellation/
│   ├── Earcut.cs              # US4 (porte 1:1 do earcut 3.x, 681)
│   └── FortuneVoronoi.cs      # US4 (porte 1:1 do d3-voronoi 1.1.2, ~1.004)
├── Convert.Tesselate.cs       # US4 (wrapper @turf/tesselate, 71)
└── Misc.Voronoi.cs            # US4 (wrapper @turf/voronoi, 33)

tests/Turfano.Tests/Parity/
├── GridTests.cs               # US1
├── InterpolateTests.cs        # US2
├── ContourTests.cs            # US3
└── HullTessellationTests.cs   # US4
```

**Structure Decision**: mesmo padrão das ondas anteriores — um partial `Geo` por função;
motores internos (`Earcut`, `FortuneVoronoi`) em subpasta como o `Polyclip/` da Fase 11.

## Fases de execução

- **F0 (pesquisa)** ✅: medições e decisões em [research.md](./research.md) — destaque:
  isolines/isobands autocontidos; convex = monotone chain (concavity=∞); concave usa o
  Union nativo (evita topojson, 1.343 linhas); earcut e d3-voronoi portados 1:1.
- **F1 (design)** ✅: [data-model.md](./data-model.md) (estruturas internas),
  [contracts/public-api.md](./contracts/public-api.md) (assinaturas), quickstart.
- **US1 Grades** (P1): rectangleGrid primeiro (base do square), depois point/hex/triangle;
  GT de contagem + coordenadas; mask nas quatro.
- **US2 Interpolação** (P1): planepoint (trivial) → tin (Delaunay do @turf — substitui o
  "leque") → interpolate (IDW sobre as grades; opções `gridType`/`property`/`weight`).
- **US3 Contornos** (P2): isolines → isobands (porte dos módulos autocontidos; atenção às
  opções `zProperty`/`commonProperties`/`breaksProperties`).
- **US4 Hulls/tesselação** (P2): convex → concave → earcut+tesselate → d3-voronoi+voronoi
  (os dois portes grandes por último; nada mais na onda depende deles).
- **Polish**: NOTICE (earcut ISC, d3-voronoi BSD-3), verificação final (suíte, AOT,
  grep NTS vazio, legado intocado), plano-mãe Fase 9.

## Complexity Tracking

| Item | Por quê | Alternativa rejeitada |
|---|---|---|
| Porte earcut (681) | paridade exata do tesselate | triangulação própria (≠ saída do @turf) |
| Porte d3-voronoi (~1.004) | paridade exata do voronoi | Bowyer-Watson próprio (≠ saída); NTS (proibido) |
| Union nativo no concave | evita fecho topojson (1.343) | porte topojson (desproporcional; risco registrado em R3) |
