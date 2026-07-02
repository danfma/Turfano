# Implementation Plan: Onda G — Paridade total (pacotes restantes)

**Branch**: `011-parity-remaining` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/011-parity-remaining/spec.md`

## Summary

Última onda: **27 funções** fecham 100% do índice `@turf` na fachada `Geo`. Motores
internos novos: **rbush 1:1** (ordem de resultados importa — R2), **sweepline-intersections
1:1** (R4), **kmeans determinístico** (só o caminho executado — R3). `mask` reusa o
polyclip nativo; `projection` cumpre o pré-requisito da leva 2; aleatórios com testes
estruturais (R6).

## Technical Context

**Language/Version**: C# (.NET SDK 10.0.301), multi-target `net8.0;net9.0;net10.0`
**Primary Dependencies**: nenhuma nova no core; portes: `@turf/*` (MIT), rbush (MIT),
sweepline-intersections (MIT), skmeans parcial (MIT) → `NOTICE`
**Testing**: TUnit + harness bun efêmero (`reference/_waveg.mjs`); aleatórios: estrutural
**Project Type**: biblioteca (fachada `Geo`, partials em `src/Turfano/Parity/`)
**Constraints**: `Parity/` livre de NTS; legado intocado; suíte 258 verde; AOT 0 warnings
**Scale/Scope**: 27 funções (~2,9 mil linhas @turf) + motores (~1,2 mil) ≈ 4 mil portadas

## Constitution Check

Sem constituição formal. Gates do projeto (fidelidade GT, tipos próprios, AOT, nomes,
sem regressão): endereçados. **PASS** (pré e pós-design).

## Project Structure

### Documentation (this feature)

```text
specs/011-parity-remaining/
├── plan.md, research.md (R1–R10), data-model.md, quickstart.md
├── contracts/public-api.md
└── tasks.md  # (/speckit-tasks)
```

### Source Code (repository root)

```text
src/Turfano/Parity/
├── Spatial/RBushIndex.cs          # rbush 3.x 1:1 (R2, internal)
├── Spatial/SweeplineIntersections.cs  # 1:1 (R4, internal)
├── Spatial/GeoJsonSpatialIndex.cs # geojson-rbush fino (R5, internal)
├── Cluster.KMeans.cs              # clustersKmeans + internal KMeans (R3)
├── Cluster.Dbscan.cs              # clustersDbscan
├── Cluster.Helpers.cs             # getCluster/clusterEach/clusterReduce
├── Join.Collect.cs                # collect
├── RandomShapes.cs                # randomPosition/Point/LineString/Polygon (R6)
├── Misc.Sample.cs                 # sample
├── Line.Segment.cs / Line.Intersect.cs / Line.Overlap.cs / Line.Split.cs
├── Line.Arc.cs                    # lineArc (+ base p/ sector)
├── Line.ShortestPath.cs           # shortestPath (grid A*-like do @turf)
├── Measure.NearestPointToLine.cs / Measure.Angle.cs
├── Shape.Ellipse.cs / Shape.Sector.cs
├── Overlay.Mask.cs                # mask via OperationRun (R7)
├── Mutate.UnkinkPolygon.cs        # unkink (simplepolygon embutido, R9)
├── Projection.cs                  # toMercator/toWgs84 (R8)
└── Stat.*.cs                      # centerMean/Median, directionalMean, distanceWeight,
                                   # moranIndex, nearestNeighbor, quadrat, sdEllipse

tests/Turfano.Tests/Parity/
├── LineOpsTests.cs, ShapeProjectionTests.cs, StatTests.cs, RandomClusterTests.cs
```

**Structure Decision**: padrão das ondas; motores em `Spatial/` (como `Polyclip/`,
`Tessellation/`).

## Fases de execução

- **F0 (pesquisa)** ✅: R1–R10 em [research.md](./research.md) — kmeans é DETERMINÍSTICO
  (centroides = k primeiros pontos); rbush portado 1:1 pela ordem; shortest-path só reusa.
- **F1 (design)** ✅: data-model, contracts, quickstart.
- **US2 Formas/projeção** (P1, primeiro — ganhos rápidos): projection → mask (polyclip) →
  ellipse/sector → unkink (precisa do rbush? sim → mover unkink p/ depois do rbush em US1).
- **US1 Linhas** (P1): rbush + geojson-rbush → lineSegment → sweepline + lineIntersect →
  lineOverlap → lineSplit → lineArc → angle/nearestPointToLine → shortestPath → unkink.
- **US3 Estatística** (P2): 8 funções (porte direto, GT numérico).
- **US4 Random/clusters** (P2): kmeans (determinístico, GT exato) → dbscan (GT exato) →
  helpers/collect (GT) → random/sample (estrutural).
- **Polish**: NOTICE, cruzamento final 100%, verificação (suíte/AOT/grep/legado),
  plano-mãe Fase 10.

## Complexity Tracking

| Item | Por quê | Alternativa rejeitada |
|---|---|---|
| Porte rbush (574) | ordem de `search()` afeta saídas (dbscan/overlap) | brute-force (ordem diverge) |
| Porte sweepline-intersections (~530) | ordem/dedup das interseções | par-a-par próprio |
| skmeans parcial (~100/500) | caminho executado é determinístico | porte integral (código morto) |
