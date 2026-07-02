# Tasks: Onda F — Interpolation, Grids & Triangulation

**Input**: Design documents from `specs/010-parity-interpolation-grids/`
**Prerequisites**: plan.md, spec.md, research.md (R1–R7), data-model.md, contracts/public-api.md

**Tests**: incluídos (padrão das ondas: GT do `@turf` real via bun + TUnit por função).

**Organization**: por user story; GT sempre ANTES do porte (lição das ondas).

## Phase 1: Setup

- [X] T001 Atualizar `NOTICE` com earcut (ISC © Mapbox) e d3-voronoi (BSD-3 © Mike Bostock) — os dois portes 1:1 novos desta onda.

## Phase 2: Foundational

*(vazio — todos os helpers necessários já existem: Distance/Destination/Bbox/Centroid, BooleanWithin/BooleanIntersects, Intersect/Union nativos, JsonObject de propriedades)*

## Phase 3: US1 — Grades (P1) 🎯 MVP

**Goal**: `PointGrid`/`SquareGrid`(+`RectangleGrid`)/`HexGrid`/`TriangleGrid` = `@turf`.

**Independent Test**: contagem + coordenadas das células iguais ao `@turf`; mask filtra.

- [X] T002 [US1] GT das grades via harness bun efêmero `reference/_wavef.mjs`: contagens e primeiras células de `pointGrid`/`squareGrid`/`hexGrid`(hex+triangles)/`triangleGrid` numa bbox fixa, com e sem mask.
- [X] T003 [P] [US1] Portar `@turf/rectangle-grid` (55 linhas) → `src/Turfano/Parity/Grid.RectangleGrid.cs` (`RectangleGrid` + `SquareGrid` wrapper; mask via `BooleanIntersects`).
- [X] T004 [P] [US1] Portar `@turf/point-grid` (42) → `src/Turfano/Parity/Grid.PointGrid.cs` (mask via `BooleanWithin`).
- [X] T005 [P] [US1] Portar `@turf/hex-grid` (113) → `src/Turfano/Parity/Grid.HexGrid.cs` (hexágonos + `triangles: true`; mask via `Intersect` nativo).
- [X] T006 [P] [US1] Portar `@turf/triangle-grid` (133) → `src/Turfano/Parity/Grid.TriangleGrid.cs`.
- [X] T007 [US1] `tests/Turfano.Tests/Parity/GridTests.cs` com os valores do T002; suíte verde.

**Checkpoint**: grades prontas — `Interpolate` (US2) desbloqueado.

## Phase 4: US2 — Interpolação básica (P1)

**Goal**: `Planepoint`/`Tin`/`Interpolate` = `@turf` (tin fiel substitui o "leque" ingênuo).

**Independent Test**: triangulação idêntica; valores numéricos apertados.

- [X] T008 [US2] GT no harness: `planepoint` (ponto dentro/fora do triângulo), `tin` (fixture de pontos com z — triângulos e props `a/b/c`), `interpolate` (grade IDW pequena; gridType square e point).
- [X] T009 [P] [US2] Portar `@turf/planepoint` (32) → `src/Turfano/Parity/Interpolate.Planepoint.cs`.
- [X] T010 [P] [US2] Portar `@turf/tin` (191, Delaunay incremental com supertriângulo/circumcírculos, data-model) → `src/Turfano/Parity/Interpolate.Tin.cs`.
- [X] T011 [US2] Portar `@turf/interpolate` (92, IDW) → `src/Turfano/Parity/Interpolate.Idw.cs` (`gridType`/`property`/`weight`; usa as grades da US1). Depende de T003–T006 e T010.
- [X] T012 [US2] `tests/Turfano.Tests/Parity/InterpolateTests.cs` com os valores do T008 (tin: triangulação NÃO-leque provada); suíte verde.

**Checkpoint**: US1+US2 = MVP da onda (grades + interpolação fiéis).

## Phase 5: US3 — Isolinhas e isobandas (P2)

**Goal**: `Isolines`/`Isobands` = `@turf` (marching squares embutido portado; NÃO o TurfUtils legado).

**Independent Test**: contornos estruturalmente iguais nas mesmas fixtures de grade.

- [ ] T013 [US3] GT no harness: grade regular (ex.: `pointGrid` com z conhecido) → `isolines(breaks)` e `isobands(breaks)`; capturar multilinhas/multipolígonos completos de um caso pequeno + contagens de um caso maior.
- [ ] T014 [US3] Portar `@turf/isolines` (315, autocontido) → `src/Turfano/Parity/Contour.Isolines.cs` (grade z[y][x] + marching squares embutido + reescala p/ bbox; `zProperty`).
- [ ] T015 [US3] Portar `@turf/isobands` (508, autocontido) → `src/Turfano/Parity/Contour.Isobands.cs` (pares de breaks; `groupNestedPolygons` com `BooleanPointInPolygon`).
- [ ] T016 [US3] `tests/Turfano.Tests/Parity/ContourTests.cs` com os valores do T013; suíte verde.

## Phase 6: US4 — Hulls e tesselação (P2)

**Goal**: `Convex`/`Concave`/`Tesselate`/`Voronoi` = `@turf` (decisões R2–R5).

**Independent Test**: estrutura igual ao `@turf` nas mesmas fixtures.

- [ ] T017 [US4] GT no harness: `convex` (nuvem de pontos), `concave` (pontos + maxEdge, incl. caso sem solução → null), `tesselate` (polígono com furo), `voronoi` (pontos + bbox — células completas de caso pequeno).
- [ ] T018 [P] [US4] `Convex` (monotone chain do concaveman, R2) → `src/Turfano/Parity/Hull.Convex.cs`.
- [ ] T019 [P] [US4] `Concave` (tin + filtro maxEdge + união n-ária nativa, R3) → `src/Turfano/Parity/Hull.Concave.cs`. Depende de T010.
- [ ] T020 [P] [US4] Portar earcut 3.x 1:1 (681, lista circular + z-order, data-model) → `src/Turfano/Parity/Tessellation/Earcut.cs` + wrapper `Tesselate` (71) → `src/Turfano/Parity/Convert.Tesselate.cs`.
- [ ] T021 [US4] Portar d3-voronoi 1.1.2 1:1 (~1004: RB-tree, beach line, circles, clipping por extent; globais do módulo viram instância, data-model) → `src/Turfano/Parity/Tessellation/FortuneVoronoi.cs` + wrapper `Voronoi` (33) → `src/Turfano/Parity/Misc.Voronoi.cs`.
- [ ] T022 [US4] `tests/Turfano.Tests/Parity/HullTessellationTests.cs` com os valores do T017; suíte verde.

## Phase 7: Polish & Cross-Cutting

- [ ] T023 Verificação final: suíte completa verde (245 + novos); AOT smoke 0 warnings IL; `grep -rn "NetTopologySuite" src/Turfano/Parity/` vazio; `git diff main -- 'src/Turfano/Turf.*.cs'` vazio (legado intocado); NOTICE atualizado.
- [ ] T024 Remover `reference/_wavef.mjs`; atualizar `plans/turfjs-parity-redesign.md` (Fase 9: checkboxes + Phase Summary + Verification result).

## Dependencies & Execution Order

- **US1 (T002–T007)**: sem dependências → MVP.
- **US2 (T008–T012)**: `Interpolate` (T011) depende das grades (T003–T006) e do tin (T010); `planepoint`/`tin` independentes.
- **US3 (T013–T016)**: independente de US2 (usa grade própria); depois de US1 por conveniência de fixtures.
- **US4 (T017–T022)**: `Concave` (T019) depende do tin (T010); earcut/d3-voronoi (T020–T021) independentes de tudo — por último por tamanho.
- GT (T002/T008/T013/T017) SEMPRE antes dos portes da fase.

## Implementation Strategy

MVP = US1+US2. Depois US3, US4 (earcut e d3-voronoi por último — os dois portes grandes,
mesma disciplina do polyclip: fonte lida por inteiro antes de escrever, estado global →
instância, GT antes do código).
