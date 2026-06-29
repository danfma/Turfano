---
description: "Task list — Onda A — Measurement (Fase 4, paridade)"
---

# Tasks: Onda A — Measurement (paridade com TurfJS)

**Input**: Design documents from `specs/004-parity-measurement/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — cada função é validada contra o `@turf` real (FR-002, SC-001).

**Organization**: por user story. O **harness Bun** de ground-truth (T002) é foundational
(compartilhado). As histórias são independentes (funções/arquivos distintos) → paralelas
após o harness. Nada de produção atual é removido (suíte 177 segue verde).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: paralelizável (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3 / US4
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [X] T001 Criar `src/Turfano/Parity/` e `tests/Turfano.Tests/Parity/`; confirmar baseline
  `dotnet build Turfano.slnx -c Debug` (0 erros) + suíte 177/0.

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T002 Criar o harness Bun `reference/_measure.mjs` que importa `@turf` e emite, por
  função/fixture, os valores de ground-truth (area, distance, bearing, length, bbox,
  centroid, center, centerOfMass, midpoint, destination, along, rhumb*, pointTo*,
  nearestPointOnLine, greatCircle, polygonTangents) em JSON. Reproduzível (FR-002).

**Checkpoint**: ground-truth disponível para todas as histórias.

---

## Phase 3: User Story 1 — Medições escalares (Priority: P1) 🎯 MVP

**Goal**: `area`/`distance`/`bearing`/`length`/`bbox`/`bboxPolygon`/`square`/`envelope`
sobre os novos tipos, batendo com o `@turf`.

**Independent Test**: rodar os testes de measurement escalar — todos batem com o `@turf`.

- [X] T003 [P] [US1] `Turf.Area(GeoJson.Geometry)` (esférica) em
  `src/Turfano/Parity/Measure.Area.cs` (re-tipar de `Turf.Area.cs`, devolve `Units.Area`).
- [X] T004 [P] [US1] `Turf.Distance`/`Turf.Bearing`/`Turf.Length` em
  `src/Turfano/Parity/Measure.Distance.cs` (re-tipar; `Units.Length`/`Units.Angle`).
- [X] T005 [P] [US1] `Turf.Bbox`/`BboxPolygon`/`Square`/`Envelope` em
  `src/Turfano/Parity/Measure.Bbox.cs` (devolvem `GeoJson.BBox`/`GeoJson.Polygon`).
- [X] T006 [US1] Testes TUnit vs `@turf` em `tests/Turfano.Tests/Parity/MeasureScalarTests.cs`
  (area-âncora `32819945055.14`, distance/bearing/length/bbox), tolerância apertada.

**Checkpoint**: medições escalares fiéis ao `@turf` (MVP da onda).

---

## Phase 4: User Story 2 — Pontos derivados + `centroid` consertado (Priority: P1)

**Goal**: `centroid` (corrigido), `center`, `centerOfMass`, `midpoint`, `destination`,
`along`, `rhumbDestination` batendo com o `@turf`.

**Independent Test**: rodar os testes — o de `centroid` prova `[1,1]`.

- [X] T007 [US2] `Turf.Centroid(GeoJson.Geometry)` **CONSERTADO** (exclui o vértice de
  fechamento) em `src/Turfano/Parity/Measure.Centroid.cs`.
- [X] T008 [P] [US2] `Turf.Center`/`CenterOfMass`/`Midpoint` em
  `src/Turfano/Parity/Measure.Center.cs` (portar o algoritmo do `@turf`).
- [X] T009 [P] [US2] `Turf.Destination`/`Along`/`RhumbDestination` em
  `src/Turfano/Parity/Measure.Destination.cs` (re-tipar).
- [X] T010 [US2] Testes vs `@turf` em `tests/Turfano.Tests/Parity/MeasurePointsTests.cs`;
  o teste de `Centroid([[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]])` prova `[1,1]` (SC-002).

**Checkpoint**: pontos derivados corretos; divergência da Fase 2 consertada.

---

## Phase 5: User Story 3 — Rumo + distâncias a geometrias (Priority: P2)

**Goal**: `rhumbBearing`/`rhumbDistance`/`pointTo*Distance`/`nearestPointOnLine`/
`pointOnFeature`/`greatCircle`/`polygonTangents` batendo com o `@turf`.

**Independent Test**: testes por função vs `@turf`.

- [X] T011 [P] [US3] `Turf.RhumbBearing`/`RhumbDistance` em
  `src/Turfano/Parity/Measure.Rhumb.cs` (re-tipar; rumos > 180° já corretos da Fase 1).
- [ ] T012 [P] [US3] `Turf.PointToLineDistance`/`PointToPolygonDistance`/
  `NearestPointOnLine` em `src/Turfano/Parity/Measure.PointDistances.cs`.
- [ ] T013 [P] [US3] `Turf.PointOnFeature`/`GreatCircle`/`PolygonTangents` em
  `src/Turfano/Parity/Measure.Surface.cs` (conferir o algoritmo exato do `@turf` em
  `reference/node_modules/@turf/{great-circle,polygon-tangents}`).
- [ ] T014 [US3] Testes vs `@turf` em `tests/Turfano.Tests/Parity/MeasureGeomTests.cs`.

**Checkpoint**: categoria measurement de geometrias completa.

---

## Phase 6: User Story 4 — Conversões de unidade na fachada (Priority: P3)

**Goal**: expor `bearingToAzimuth`/`convert*`/`*ToRadians`/`*ToDegrees` na superfície.

**Independent Test**: as conversões batem com o `@turf` (reuso de `Turfano.Units`).

- [X] T015 [US4] Expor `Turf.BearingToAzimuth`/`ConvertLength`/`ConvertArea`/
  `DegreesToRadians`/`RadiansToDegrees`/`LengthToRadians`/`RadiansToLength`/
  `LengthToDegrees` (reuso de `Turfano.Units`) em `src/Turfano/Parity/Units.cs`.
- [X] T016 [US4] Testes (ou confirmar cobertura pelos testes de `UnitsTests` da Fase 3).

**Checkpoint**: conversões de unidade acessíveis na fachada.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T017 Verificação final (SC-004/005): `dotnet build Turfano.slnx -c Debug`
  (0 erros, net8/9/10) + suíte (177 + novos, 0 falhas) +
  `dotnet build tests/Turfano.AotSmoke -c Release` (0 warnings IL) +
  `git diff --stat main -- 'src/Turfano/Turf.*.cs'` vazio.
- [ ] T018 [P] Remover o harness efêmero `reference/_measure.mjs`.
- [ ] T019 Atualizar `plans/turfjs-parity-redesign.md`: Fase 4 (Onda A) → `Complete` +
  Phase Summary.

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002 harness)** → **US1–US4**.
- US1–US4 dependem do harness (T002) mas são **independentes entre si** (funções/arquivos
  distintos) → podem rodar em paralelo.
- Dentro de cada história: implementar → testar.
- **Polish** após todas.

### Parallel Opportunities

- T003 ∥ T004 ∥ T005 (US1, arquivos distintos); idem T008/T009, T011/T012/T013.
- **US1, US2, US3, US4 inteiras** podem ser conduzidas em paralelo após o harness.

---

## Parallel Example

```bash
# Após o harness (T002), várias frentes em paralelo:
Task: "T003 Area"            # US1
Task: "T007 Centroid (fix)"  # US2
Task: "T011 Rhumb"           # US3
Task: "T015 conversões"      # US4
```

---

## Implementation Strategy

### MVP

Setup → Foundational (harness) → US1 (escalares) + US2 (centroid consertado) — as duas P1.
Já entrega as medições mais usadas + a correção de divergência.

### Incremental

1. Harness (ground-truth).
2. US1 escalares → US2 pontos/centroid (P1).
3. US3 rumo/distâncias + US4 conversões (paralelo).
4. Polish: verificação + limpar harness + atualizar plano-mãe.

---

## Notes

- `[P]` = arquivos diferentes, sem dependências pendentes.
- **Re-tipar, não reescrever**: partir da lógica já fiel ao `@turf` das `Turf.*.cs` atuais
  (Fase 2 confirmou). Exceção: `centroid` (conserto). Validar TUDO vs `@turf` real.
- **Não-regressão** (FR-007): `Turf.*.cs` NTS-based, NTS e UnitsNet permanecem; suíte 177
  verde.
- Fora de escopo (não criar tarefas): outras ondas, remover NTS/UnitsNet, funções fora de
  measurement.

## Implementation Notes (incremento — em progresso)

- **DECISÃO DE COLOCAÇÃO (resolvida)**: as funções de tipos novos vivem na **fachada
  `Geo`** (partials em `Turfano.GeoJson`), não como sobrecargas em `Turf`. No namespace
  `Turfano.GeoJson` os nomes `Point`/`Polygon`/`Length`/... resolvem para os tipos próprios
  (o namespace local vence o global using do NTS), eliminando a colisão `Length` (e as
  futuras `Point`/`Polygon`/`Feature`). `Geo` virou `partial` (única mudança em `Geo.cs`).
  A `Turfano.Units` entra via alias `Units`. **`Geo` é a fachada única da API de tipos
  novos** (construtores + measurement + futuras ondas).
- **Entregue e verde (189/0)**: **US1** (Area/Distance/Bearing/Length/Bbox/BboxPolygon/
  Square/Envelope), **US2** (Centroid consertado `[1,1]` + Center/CenterOfMass/Midpoint/
  Destination/Along/RhumbDestination), **US4** (BearingToAzimuth/Convert*/...ToRadians/...
  ToDegrees), **US3-rumo** (RhumbBearing/RhumbDistance) — tudo em `Geo.*`, validado vs
  `@turf` real. AOT 0 warnings; `Turf.*.cs` NTS intocados.
- **Bug pego pela validação**: o `RhumbDestination` NTS existente diverge do `@turf`
  (`q = deltaPsi/sin(br)` em vez de `(phi2-phi1)/deltaPsi`); a versão `Geo` bate com o
  `@turf`. Idem `rhumbDistance`/`rhumbBearing`: docs antigos diziam `97.994`/`9.71`; o
  `@turf` real dá `97.129`/`-170.294`.
- **Falta (US3, continuação)**: `Geo.PointToLineDistance`/`PointToPolygonDistance`/
  `NearestPointOnLine`/`PointOnFeature`/`GreatCircle`/`PolygonTangents`. Notas: `GreatCircle`
  e `PolygonTangents` precisam da fonte do `@turf` (`reference/node_modules/@turf/...`);
  o GT de `polygonTangents` sai como **FeatureCollection** (ajustar o harness). Padrão:
  re-tipar/portar em `Geo.*` + GT no harness + teste vs `@turf`. Depois: Polish (T017–T019).
