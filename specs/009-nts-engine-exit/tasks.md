---
description: "Task list — Saída do motor NTS, leva 1 (Fase 11, engine exit)"
---

# Tasks: Saída do motor NTS — primeira leva (engine exit)

**Input**: Design documents from `specs/009-nts-engine-exit/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md, quickstart.md

**Tests**: INCLUÍDOS — unitários para as peças do motor (`ExactDecimal`, `SplayTreeSet`) +
**regressão pelas âncoras das Ondas D/E** (valores do `@turf` já pinados) (FR-001/002).

**Organization**: por user story, mas a US1 tem **cadeia interna de dependências** (o porte
segue a ordem do quickstart: decimal → árvore → precision → sweep → operation). Porte 1:1
da fonte (`reference/node_modules/{polyclip-ts,splaytree-ts}`); quando a fonte surpreender,
seguir a fonte. Nada do legado é tocado (suíte 232 segue verde).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: paralelizável (arquivos diferentes, sem dependências pendentes)
- **[Story]**: US1 / US2 / US3
- Caminhos a partir da raiz do repositório

---

## Phase 1: Setup

- [X] T001 Confirmar baseline `dotnet build Turfano.slnx -c Debug` (0 erros) + suíte 232/0;
  criar `src/Turfano/Parity/Polyclip/` e o arquivo `NOTICE` na raiz (MIT `polyclip-ts` ©
  2022 Luiz Felipe Machado Barboza; BSD-3 `splaytree-ts` © 2022 idem; MIT TurfJS).

---

## Phase 2: Foundational (Blocking Prerequisites — as fundações do motor)

- [X] T002 `ExactDecimal` em `src/Turfano/Parity/Polyclip/ExactDecimal.cs`: decimal exato
  (`BigInteger` mantissa + expoente) com `Add/Subtract/Multiply/CompareTo/Abs/Square/
  IsZero/FromDouble/ToDouble` — as únicas operações que o bundle usa (research Decisão 1).
  Se divisão/sqrt aparecer na leitura linha-a-linha, reproduzir defaults do bignumber.js e
  registrar. Testes unitários em `tests/Turfano.Tests/Parity/ExactDecimalTests.cs`
  (exatidão de +,−,×; compare com expoentes distintos; FromDouble round-trip).
- [X] T003 `SplayTreeSet<T>` em `src/Turfano/Parity/Polyclip/SplayTreeSet.cs`: porte 1:1 do
  `splaytree-ts` (687 linhas; `add/addAndReturn/delete/contains/first/last/lastBefore/
  firstAfter/iteração ordenada` — conferir na fonte o conjunto exato usado pelo polyclip).
  Testes em `tests/Turfano.Tests/Parity/SplayTreeSetTests.cs` (ordem, vizinhos, dedupe por
  comparator).

**Checkpoint**: as duas fundações provadas isoladamente — o sweep pode ser portado.

---

## Phase 3: User Story 1 — Overlay nativo, porte do polyclip-ts (Priority: P1) 🎯 MVP

**Goal**: `Union`/`Difference`/`Intersect`/`Dissolve` nativos; âncoras da Onda E intactas.

**Independent Test**: `OverlayTests` (Onda E) passam inalterados, sem NTS no caminho.

- [ ] T004 [US1] `PolyclipPrecision` (compare/orient/snap, eps default `undefined`) em
  `src/Turfano/Parity/Polyclip/PolyclipPrecision.cs` (porte de `constant/compare/orient/
  snap/identity/precision`).
- [ ] T005 [US1] `PolyclipVector` + `PolyclipBBox` em
  `src/Turfano/Parity/Polyclip/PolyclipGeometry.cs` (porte de `vector`/`bbox`:
  cross/dot/comparePoints/interseções de envelope — em `ExactDecimal`).
- [ ] T006 [US1] `SweepEvent` em `src/Turfano/Parity/Polyclip/SweepEvent.cs` (porte de
  `sweep-event`: comparator de fila, link de eventos coincidentes — **atenção máxima aos
  empates**: `<` vs `<=` é onde o porte quebra silenciosamente).
- [ ] T007 [US1] `Segment` em `src/Turfano/Parity/Polyclip/Segment.cs` (porte de `segment`:
  comparator do status, split em interseção, flags de winding/anel).
- [ ] T008 [US1] `RingIn/PolyIn/MultiPolyIn` (geom-in) + `SweepLine` + `PolyclipOperation`
  (union/intersection/difference/xor) + `RingOut/PolyOut/MultiPolyOut` (geom-out) em
  `src/Turfano/Parity/Polyclip/{PolyclipInput,SweepLine,PolyclipOperation,PolyclipOutput}.cs`.
- [ ] T009 [US1] REWIRE `src/Turfano/Parity/Overlay.cs`: `Geo.Union/Difference/Intersect/
  Dissolve` chamam `PolyclipOperation` (entrada `Position[][][]`, saída → `Polygon`/
  `MultiPolygon`/`null`); remover os usos da `NtsBridge`.
- [ ] T010 [US1] Regressão + casos novos: `OverlayTests` (Onda E) passam inalterados;
  adicionar em `tests/Turfano.Tests/Parity/OverlayTests.cs` 2 casos com **furos** e 1 de
  **xor**(se exposto)/idênticos (GT via harness Bun `reference/_polyclip.mjs`, efêmero).

**Checkpoint**: overlay 100% nativo, âncoras do `@turf` verdes (MVP da feature).

---

## Phase 4: User Story 2 — Polygonize nativo (Priority: P2)

**Goal**: `Polygonize` sem NTS; `Parity/` livre de NetTopologySuite.

**Independent Test**: teste da Onda D (4 linhas → quadrado) inalterado; grep vazio.

- [ ] T011 [US2] Porte do `@turf/polygonize` (grafo: nós/arestas, remoção de dangles e
  cut-edges, extração de anéis) em `src/Turfano/Parity/Polyclip/PolygonizeGraph.cs`;
  REWIRE `src/Turfano/Parity/Convert.Polygonize.cs` (remover NTS `Polygonizer`).
- [ ] T012 [US2] Regressão: teste da Onda D passa; confirmar
  `grep -r NetTopologySuite src/Turfano/Parity/` **vazio** (SC-002); adicionar 1 caso de
  linhas que não fecham anel (coleção vazia, GT via bun).

**Checkpoint**: `Parity/` é zona livre de NTS.

---

## Phase 5: User Story 3 — Satélite Turfano.NetTopologySuite (Priority: P2)

**Goal**: Buffer + interop isolados; fronteira empacotada (zero `Coordinate`).

**Independent Test**: teste do buffer (~3.12e10) passa via a nova API; ida-e-volta da
bridge preserva furos/Z; teste confirma sequência empacotada.

- [ ] T013 [US3] Criar `src/Turfano.NetTopologySuite/Turfano.NetTopologySuite.csproj`
  (multi-target net8/9/10, refs `Turfano` + `NetTopologySuite 2.5`), adicionar ao
  `Turfano.slnx`; `tests/Turfano.Tests` ganha ProjectReference ao satélite.
- [ ] T014 [US3] `NtsConvert` público em `src/Turfano.NetTopologySuite/NtsConvert.cs`:
  `ToNts`/`FromNts` (Geometry + Position) com fronteira **empacotada**
  (`GeometryFactory(PackedCoordinateSequenceFactory.DoubleFactory)`, ida via
  `PackedDoubleCoordinateSequence(double[],2,0)`, volta via `GetRawCoordinates()` fast-path
  + `GetOrdinate` fallback). SEM `UnsafeAccessor` (FR-005).
- [ ] T015 [US3] `NtsGeometryExtensions.Buffer` em
  `src/Turfano.NetTopologySuite/NtsGeometryExtensions.cs` (pipeline AEQD → NTS Buffer →
  desprojeção, movido da Onda E; helper local de mapeamento de posições); REMOVER do core
  `src/Turfano/Parity/Overlay.Buffer.cs` e `src/Turfano/Interop/NtsBridge.cs`.
- [ ] T016 [US3] Testes: `BufferTests` atualizados para `geometry.Buffer(...)` (+ teste de
  que a sequência do resultado NTS é `PackedDoubleCoordinateSequence`);
  `GeoJsonFactoryAndBridgeTests` migrados para o `NtsConvert` público (ida-e-volta com
  furos e Z).

**Checkpoint**: NTS isolado no satélite; core sem NTS fora do legado `Turf.*`.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T017 Verificação final (SC-001..005): build `Turfano.slnx` (0 erros, net8/9/10, incl.
  satélite) + suíte (232 + novos, 0 falhas) + smoke AOT serialização (0 warnings IL) +
  `git diff --stat main -- 'src/Turfano/Turf.*.cs'` vazio + `NOTICE` presente.
- [ ] T018 [P] Remover harnesses efêmeros (`reference/_polyclip.mjs` etc.).
- [ ] T019 Atualizar `plans/turfjs-parity-redesign.md`: marcar os itens da leva 1 na Fase
  11 + registrar a otimização futura (caminho double + fallback exato) e o que resta para
  a leva 2 (pós-F/G).

---

## Dependencies & Execution Order

- **Setup (T001)** → **Foundational (T002 `ExactDecimal` ∥ T003 `SplayTreeSet`)** →
  **US1 em cadeia**: T004 → T005 → T006 → T007 → T008 → T009 → T010 (o sweep depende de
  tudo que vem antes).
- **US2 (T011–T012)** depende só das fundações (grafo puro) — pode rodar em paralelo à US1.
- **US3 (T013–T016)** é independente das US1/US2 (satélite) — paralelizável; T015 remove a
  `NtsBridge` do core, então deve concluir **depois** de T009/T011 (últimos consumidores).
- **Polish** após todas.

### Parallel Opportunities

- T002 ∥ T003 (fundações); US3 ∥ US1/US2 (exceto a remoção final da bridge em T015).

---

## Implementation Strategy

### MVP

Setup → Foundational (decimal + árvore, provados isoladamente) → **US1** (o porte do
sweep, em cadeia, validado pelas âncoras da Onda E). É o coração — e o maior risco — da
feature.

### Incremental

1. Fundações com testes unitários próprios (errar aqui custa caro depois).
2. US1 na ordem do quickstart; regressão a cada peça grande.
3. US2 (polygonize) e US3 (satélite) — paralelos.
4. Polish: verificação + NOTICE + plano-mãe.

---

## Notes

- **Porte 1:1 da fonte** (`reference/node_modules/{polyclip-ts,splaytree-ts}/dist/esm/`):
  mesma estrutura de módulos, mesmos comparators, mesmos empates. Quando a fonte
  surpreender, **seguir a fonte** (lição de todas as ondas).
- **Empates do sweep** são o risco nº 1 — os comparators de `sweep-event`/`segment` pedem
  leitura linha-a-linha, não paráfrase.
- **Não-regressão** (FR-007): `Turf.*.cs` legadas, NTS e UnitsNet permanecem no core (a
  referência só cai na leva 2); suíte 232 verde; AOT serialização 0 warnings.
- **Nomes .NET** sem acrônimos crípticos; `UnsafeAccessor` proibido (FR-005).
- Fora de escopo: leva 2 (deletar legado, UnitsNet, referência NTS do core, split 1.0),
  Ondas F/G, otimização de desempenho do sweep.

## Implementation Notes (PAUSA em 2026-07-01 — estado para retomada)

- **Commitado e verde (`06dc5a5`)**: T001–T003 — `NOTICE`, `ExactDecimal` (5 testes,
  valores via Python Decimal) e `SplayTreeSet` (4 testes, incl. stress 2000 ops vs
  `SortedDictionary`). Suíte 241/0 (232 + 9).
- **Na árvore de trabalho, NÃO commitado (untracked)**:
  `src/Turfano/Parity/Polyclip/PolyclipGeometry.cs` — T005 pronto (SweepPoint com
  identidade referencial + ExactVector + ExactBounds + ExactVectorMath com div/sqrt),
  **mas referencia `SweepEvent` (T006, ainda não escrito) → o build do core fica vermelho
  até a T006 existir**. Retomar escrevendo T004 (`PolyclipPrecision`) e T006 em seguida.
- **O bundle do polyclip foi lido INTEGRALMENTE** (1.137 linhas). Achados essenciais já
  aplicados/decididos (não re-derivar):
  - `div`/`sqrt` existem (módulo `vector`) → `ExactDecimal` já implementa 20 casas half-up.
  - O comparator da fila (`SweepEvent.compare`) tem EFEITO COLATERAL: chama `a.link(b)`
    quando os pontos empatam mas são objetos distintos — preservar no porte.
  - `SweepEvent.comparePoints` é comparação EXATA de coordenadas (não usa `precision`).
  - `splice(i)` do JS trunca do índice ao FIM (RingOut.factory) → `GetRange`+`RemoveRange`.
  - `availableLEs.sort(...)` é estável (ES2019) → usar `OrderBy` (List.Sort é instável).
  - Bbox do `MultiPolyIn` inicia com `±Infinity` → sentinelas do `ExactDecimal` prontas.
  - DECISÃO: o singleton global mutável `operation` (type/numMultiPolys/segmentId) vira
    uma instância `OperationRun` por execução (thread-safe; ids por run são equivalentes,
    só servem de tiebreak no `Segment.compare`). Segments recebem a referência via ctor.
  - `precision` default = exato (eps undefined); usar instância estática `Exact`; snap
    identidade; caminho com eps portado mas sem `setPrecision` público.
- **Faltam**: T004 `PolyclipPrecision.cs` → T006 `SweepEvent.cs` → T007 `Segment.cs` →
  T008 `PolyclipInput/SweepLine/PolyclipOperation/PolyclipOutput` → T009 rewire de
  `Overlay.cs` → T010 regressão (âncoras Onda E + casos com furos) → US2 (polygonize) →
  US3 (satélite) → polish.
