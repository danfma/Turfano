# Tasks: Onda G — Paridade total (pacotes restantes)

**Input**: Design documents from `specs/011-parity-remaining/`
**Prerequisites**: plan.md, spec.md, research.md (R1–R10), data-model.md, contracts/public-api.md

**Tests**: incluídos (GT do `@turf` real via bun ANTES do porte; aleatórios estruturais, R6).

**Organization**: US2 primeiro (ganhos rápidos + leva 2), depois US1 (motores), US3, US4.

## Phase 1: Setup

- [ ] T001 Atualizar `NOTICE` com rbush (MIT © Vladimir Agafonkin) e sweepline-intersections (MIT © Rowan Winsemius); atribuição do skmeans parcial (MIT).

## Phase 2: Foundational

*(vazio — helpers existentes cobrem; os motores novos são específicos das USs)*

## Phase 3: US2 — Formas e projeção (P1) 🎯 primeiro

**Goal**: `ToMercator`/`ToWgs84` (leva 2!), `Mask` (polyclip nativo), `Ellipse`, `Sector`.

- [ ] T002 [US2] GT via `reference/_waveg.mjs`: projection round-trip + valores, mask (área/anéis), ellipse/sector (vértices).
- [ ] T003 [P] [US2] Portar `@turf/projection` (54) → `src/Turfano/Parity/Projection.cs` (`ToMercator`/`ToWgs84`).
- [ ] T004 [P] [US2] Portar `@turf/mask` (69) sobre o `OperationRun` nativo → `src/Turfano/Parity/Overlay.Mask.cs`.
- [ ] T005 [P] [US2] Portar `@turf/ellipse` (91) → `src/Turfano/Parity/Shape.Ellipse.cs` e `@turf/sector` (41, usa lineArc — trazer o miolo junto se preciso) → `src/Turfano/Parity/Shape.Sector.cs`.
- [ ] T006 [US2] `tests/Turfano.Tests/Parity/ShapeProjectionTests.cs` (projection/mask/ellipse/sector); suíte verde. (`UnkinkPolygon` fica na US1, depende do rbush.)

## Phase 4: US1 — Operações de linha (P1)

**Goal**: motores espaciais + as 8 funções de linha + unkink.

- [ ] T007 [US1] GT no harness: lineSegment/lineIntersect/lineOverlap/lineSplit/lineArc/angle/nearestPointToLine/shortestPath/unkinkPolygon (fixtures pequenas, saídas completas).
- [ ] T008 [US1] Portar rbush 3.x 1:1 (574, ordem do `Search` preservada — R2) → `src/Turfano/Parity/Spatial/RBushIndex.cs` + wrapper geojson-rbush (R5) → `src/Turfano/Parity/Spatial/GeoJsonSpatialIndex.cs`; teste de unidade da ordem vs GT pequeno.
- [ ] T009 [P] [US1] Portar `@turf/line-segment` (60) → `src/Turfano/Parity/Line.Segment.cs`.
- [ ] T010 [P] [US1] Portar sweepline-intersections 1:1 (~530, R4) → `src/Turfano/Parity/Spatial/SweeplineIntersections.cs` + `@turf/line-intersect` (47) → `src/Turfano/Parity/Line.Intersect.cs`.
- [ ] T011 [US1] Portar `@turf/line-overlap` (80; usa geojson-rbush + lineSegment + nearestPointOnLine + booleanPointOnLine + fast-deep-equal inline) → `src/Turfano/Parity/Line.Overlap.cs`. Depende de T008/T009.
- [ ] T012 [US1] Portar `@turf/line-split` (140; geojson-rbush + lineIntersect + lineSegment...) → `src/Turfano/Parity/Line.Split.cs`. Depende de T008–T010.
- [ ] T013 [P] [US1] Portar `@turf/line-arc` (42) → `src/Turfano/Parity/Line.Arc.cs`, `@turf/angle` (40) → `src/Turfano/Parity/Measure.Angle.cs`, `@turf/nearest-point-to-line` (73) → `src/Turfano/Parity/Measure.NearestPointToLine.cs`.
- [ ] T014 [US1] Portar `@turf/shortest-path` (390; só reusa a fachada — R10) → `src/Turfano/Parity/Line.ShortestPath.cs`.
- [ ] T015 [US1] Portar `@turf/unkink-polygon` (571, simplepolygon embutido; rbush) → `src/Turfano/Parity/Mutate.UnkinkPolygon.cs`. Depende de T008.
- [ ] T016 [US1] `tests/Turfano.Tests/Parity/LineOpsTests.cs` com o GT do T007; suíte verde.

## Phase 5: US3 — Estatística espacial (P2)

- [ ] T017 [US3] GT no harness: as 8 funções com fixtures numéricas.
- [ ] T018 [P] [US3] Portar center-mean (30) + center-median (79) → `src/Turfano/Parity/Stat.Centers.cs`.
- [ ] T019 [P] [US3] Portar directional-mean (199) → `src/Turfano/Parity/Stat.DirectionalMean.cs` e distance-weight (79) → `src/Turfano/Parity/Stat.DistanceWeight.cs`.
- [ ] T020 [P] [US3] Portar moran-index (75) → `src/Turfano/Parity/Stat.MoranIndex.cs`, nearest-neighbor-analysis (56) → `src/Turfano/Parity/Stat.NearestNeighbor.cs`, quadrat-analysis (113) → `src/Turfano/Parity/Stat.Quadrat.cs`, standard-deviational-ellipse (86) → `src/Turfano/Parity/Stat.SdEllipse.cs`.
- [ ] T021 [US3] `tests/Turfano.Tests/Parity/StatTests.cs`; suíte verde.

## Phase 6: US4 — Aleatórios, clusters e agregação (P2)

- [ ] T022 [US4] GT no harness: kmeans (determinístico — R3), dbscan, collect, cluster helpers (exatos); random/sample só estrutura de referência.
- [ ] T023 [P] [US4] Portar o caminho executado do skmeans (~100, R3) como `internal KMeans` + `@turf/clusters-kmeans` (28) → `src/Turfano/Parity/Cluster.KMeans.cs`.
- [ ] T024 [P] [US4] Portar `@turf/clusters-dbscan` (104; rbush) → `src/Turfano/Parity/Cluster.Dbscan.cs`. Depende de T008.
- [ ] T025 [P] [US4] Portar clusters helpers (107) → `src/Turfano/Parity/Cluster.Helpers.cs` e `@turf/collect` (43; rbush) → `src/Turfano/Parity/Join.Collect.cs`.
- [ ] T026 [P] [US4] Portar `@turf/random` (157) → `src/Turfano/Parity/RandomShapes.cs` e `@turf/sample` (24) → `src/Turfano/Parity/Misc.Sample.cs` (`Random.Shared`, R6).
- [ ] T027 [US4] `tests/Turfano.Tests/Parity/RandomClusterTests.cs` (exatos p/ kmeans/dbscan/collect/helpers; estruturais p/ random/sample); suíte verde.

## Phase 7: Polish & Cross-Cutting

- [ ] T028 Cruzamento FINAL de cobertura (script do R1 re-rodado): 100% ou exclusões listadas (buffer/geojson-rbush); registrar no research.md.
- [ ] T029 Verificação final: suíte completa verde; AOT 0 warnings; grep NTS em Parity/ vazio; legado intocado; NOTICE ok. Remover `reference/_waveg.mjs`; atualizar `plans/turfjs-parity-redesign.md` (Fase 10: checkboxes + Phase Summary + Verification).

## Dependencies & Execution Order

- US2 (T002–T006) não depende de nada → primeiro (projection destrava a leva 2).
- US1: T008 (rbush) antes de T011/T012/T015; T010 antes de T012.
- US3 independente. US4: T024/T025 dependem de T008.
- GT (T002/T007/T017/T022) SEMPRE antes dos portes da fase.

## Implementation Strategy

Mesma disciplina das ondas: fonte lida por inteiro antes de portar; motores com estado
global → instância; ordem de estruturas (rbush) preservada 1:1; GT antes do código.
