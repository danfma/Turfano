---
description: "Task list — Sistema de tipos central GeoJSON (Fase 3)"
---

# Tasks: Sistema de tipos central GeoJSON + unidades + STJ source-gen

**Input**: Design documents from `specs/003-geojson-types/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — o spec é orientado a testes (round-trip RFC 7946, conversões de
unidade = `@turf`, smoke de AOT, ponte NTS).

**Organization**: por user story. O **spike de serialização** (T002) é o **gate
foundational** que destrava US1/US3/US4; **US2 (unidades) é independente** e pode rodar em
paralelo. Nada de produção atual é removido (suíte 156 segue verde).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: paralelizável (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3 / US4
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [ ] T001 Criar as pastas `src/Turfano/GeoJson/`, `src/Turfano/Units/`,
  `src/Turfano/Interop/` (namespace `Turfano`) e confirmar baseline:
  `dotnet build Turfano.slnx -c Debug` (0 erros) + suíte 156/0.

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ Bloqueia US1/US3/US4. Começar pelo SPIKE.**

- [ ] T002 **SPIKE (gate)** em `tests/Turfano.Tests/`: validar STJ **source-generated** +
  polimorfismo **multinível** (`GeoJsonObject` e `Geometry`, discriminador `"type"`) +
  `JsonConverter<Position>` serializando como array. Confirmar suporte OU adotar o
  **fallback** (converter polimórfico manual, `research.md`). Registrar a decisão num
  comentário/arquivo. **Nenhuma tarefa de tipos avança antes disto.**
- [ ] T003 [P] Implementar `Position` (`readonly record struct`) + `PositionConverter`
  (array `[lon,lat,alt?]`, preserva dimensão) em `src/Turfano/GeoJson/Position.cs`.
- [ ] T004 [P] Implementar `BBox` (`readonly record struct`, 2D/3D) + `BBoxConverter`
  em `src/Turfano/GeoJson/BBox.cs`.
- [ ] T005 Implementar a hierarquia `GeoJsonObject` → `Geometry` → `Point`, `MultiPoint`,
  `LineString`, `MultiLineString`, `Polygon`, `MultiPolygon`, `GeometryCollection`
  (`coordinates` RFC 7946) em `src/Turfano/GeoJson/`. (depende de T003/T004)
- [ ] T006 Implementar `Feature` (`Id` string|número, `Geometry`, `Properties`
  `JsonObject?`, `Bbox`) + `Feature<TProps>` + `FeatureCollection` em
  `src/Turfano/GeoJson/`. (depende de T005)
- [ ] T007 Implementar `GeoJsonSerializerContext` (source-gen) ligando o polimorfismo e os
  converters, conforme a decisão do T002, em `src/Turfano/GeoJson/`. (depende de T002/T005/T006)

**Checkpoint**: tipos compilam e (de)serializam um GeoJSON simples.

---

## Phase 3: User Story 1 — Round-trip GeoJSON RFC 7946 (Priority: P1) 🎯 MVP

**Goal**: round-trip (desserializar→reserializar) com forma idêntica à do TurfJS.

**Independent Test**: rodar os testes de round-trip — forma casa com o `@turf`.

- [ ] T008 [US1] Coletar fixtures GeoJSON canônicas (do `@turf` via `reference/`):
  Point/Line/Polygon/Multi*/GC, `Feature` (props/bbox/id), `FeatureCollection`,
  `Position` 2D e 3D. Salvar em `tests/Turfano.Tests/fixtures/` (ou recurso embutido).
- [ ] T009 [US1] Testes de round-trip TUnit em `tests/Turfano.Tests/GeoJsonRoundTripTests.cs`:
  desserializar → reserializar → comparar forma (`type`/`coordinates`/`properties`/`bbox`)
  com a saída do `@turf`.
- [ ] T010 [US1] Ajustar a serialização até o round-trip casar 100% das fixtures (SC-001).

**Checkpoint**: GeoJSON próprio round-trip fiel — MVP da fase.

---

## Phase 4: User Story 2 — Unidades próprias = TurfJS (Priority: P1)

**Goal**: 3 structs de unidade com conversões batendo com o `@turf`.

**Independent Test**: testes de conversão batem com o `@turf`. (independente da US1)

- [ ] T011 [P] [US2] Implementar os 3 structs (`Length`/`Distance`, `Angle`/`Bearing`,
  `Area`) + `enum`s de unidade (estilo Turf) + `From*`/`As*` + operadores em
  `src/Turfano/Units/`.
- [ ] T012 [P] [US2] Harness Bun efêmero `reference/_units.mjs` emitindo
  `convertLength`/`convertArea`/`lengthToRadians`/`radiansToLength`/`lengthToDegrees`/
  `degreesToRadians`/`radiansToDegrees`/`bearingToAzimuth` do `@turf`.
- [ ] T013 [US2] Testes TUnit `tests/Turfano.Tests/UnitsTests.cs` comparando as conversões
  com o `@turf` dentro de `1e-9` relativo (SC-003).

**Checkpoint**: unidades prontas para as funções de medição (Fase 4+).

---

## Phase 5: User Story 3 — Serialização AOT/trimming-safe (Priority: P2)

**Goal**: (de)serializar os tipos sob AOT/trimming sem warnings.

**Independent Test**: publicar AOT/trimming → 0 warnings nos tipos do Turfano.

- [ ] T014 [US3] Criar app de teste mínimo (ex.: `tests/Turfano.AotSmoke/`) que
  (de)serializa os tipos do Turfano via `GeoJsonSerializerContext`.
- [ ] T015 [US3] `dotnet publish tests/Turfano.AotSmoke -c Release -p:PublishAot=true`
  (ou `-p:PublishTrimmed=true`) e confirmar **0 warnings** de trimming/reflexão (IL2/IL3)
  nos tipos do Turfano (SC-002).

**Checkpoint**: fundação compatível com AOT.

---

## Phase 6: User Story 4 — Helpers estilo Turf + ponte interna NTS (Priority: P2)

**Goal**: construção estilo Turf + conversores internos ↔ NTS para a transição.

**Independent Test**: construir via helpers; round-trip novo-tipo↔NTS preserva coords.

- [ ] T016 [P] [US4] Implementar helpers/factory (`point`, `lineString`, `polygon`,
  `multiPoint`, `multiLineString`, `multiPolygon`, `geometryCollection`, `feature`,
  `featureCollection`) + `getCoord(s)`/`getType`/`getGeom` em `src/Turfano/GeoJson/Factory.cs`.
- [ ] T017 [P] [US4] Implementar `internal static class NtsBridge` (`ToNts`/`FromNts` para
  `Position↔Coordinate` e cada geometria) em `src/Turfano/Interop/NtsBridge.cs`.
- [ ] T018 [US4] Testes TUnit: construção via helpers + round-trip novo-tipo→NTS→novo-tipo
  preservando coordenadas (SC-005), em `tests/Turfano.Tests/`.

**Checkpoint**: base pronta para as ondas de paridade portarem funções.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T019 Verificação final (SC-004): `dotnet build Turfano.slnx -c Debug` (0 erros,
  net8/9/10) + `dotnet run --project tests/Turfano.Tests -c Debug` (156 + novos, 0 falhas).
- [ ] T020 [P] Remover/parametrizar os harnesses efêmeros em `reference/`
  (`_units.mjs` etc.), conforme a decisão de versionamento.
- [ ] T021 Atualizar `plans/turfjs-parity-redesign.md`: Fase 3 → `Complete` + Phase
  Summary; **confirmar ou revisar** a decisão de `Feature.properties`
  (`JsonObject?` + `Feature<TProps>`).

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002 SPIKE → T003/T004 [P] → T005 → T006 → T007)**.
- **US1 (T008–T010)** depende de Foundational.
- **US2 (T011–T013)** é **independente** de Foundational → pode rodar em paralelo.
- **US3 (T014–T015)** depende de Foundational (tipos + contexto source-gen).
- **US4 (T016–T018)** depende de Foundational (tipos).
- **Polish** após todas.

### Parallel Opportunities

- T003 ∥ T004 (Position/BBox, arquivos distintos).
- **US2 inteira** (unidades) ∥ Foundational/US1 (não depende dos tipos GeoJSON).
- T016 ∥ T017 (Factory vs NtsBridge).

---

## Parallel Example

```bash
# Após o SPIKE (T002), frentes em paralelo:
Task: "T003 Position + converter"            # Foundational
Task: "T004 BBox + converter"                # Foundational
Task: "T011 3 structs de unidade"            # US2 (independente)
Task: "T012 harness Bun de conversões"       # US2
```

---

## Implementation Strategy

### MVP (núcleo da fundação)

Setup → Foundational (com **SPIKE primeiro**) → US1 (round-trip). Com isso, há tipos
GeoJSON próprios que serializam fiel ao RFC 7946 — a base das ondas de paridade. US2
(unidades) entra em paralelo (também P1, independente).

### Incremental

1. SPIKE (derruba o risco de serialização).
2. Tipos (Foundational) → US1 round-trip.
3. US2 unidades (paralelo).
4. US3 AOT smoke + US4 helpers/ponte NTS.
5. Polish: não-regressão (156) + atualizar plano-mãe + decisão de `Feature.properties`.

---

## Notes

- `[P]` = arquivos diferentes, sem dependências pendentes.
- **SPIKE primeiro**: se o STJ source-gen não suportar o polimorfismo multinível, adotar o
  converter polimórfico manual (research.md) antes de construir os tipos.
- **Não-regressão**: NTS/UnitsNet e os `Turf.*.cs` atuais permanecem; a suíte 156 segue
  verde durante toda a fase.
- Toda conversão de unidade e o round-trip são validados contra o `@turf` real — nada
  presumido (lição das Fases 1–2).
- Fora de escopo (não criar tarefas): portar as ~70 funções, remover NTS/UnitsNet,
  adaptadores NTS públicos.
