# Feature Specification: Saída do motor NTS — primeira leva (engine exit)

**Feature Branch**: `009-nts-engine-exit`

**Created**: 2026-07-01

**Status**: Draft

**Input**: User description: "Fase 11 (primeira leva) do plano `plans/turfjs-parity-redesign.md`. Substituir o motor NTS interino do overlay por ports nativos (`polyclip-ts`, `@turf/polygonize`) sobre os tipos próprios, e isolar `buffer` + interop num pacote satélite com fronteira empacotada (zero `Coordinate`). Decisão fechada em 2026-07-01 com fatos medidos — não re-litigar."

## User Scenarios & Testing *(mandatory)*

Primeira leva da **saída do NTS**: as operações de overlay deixam de delegar ao motor NTS e
passam a rodar **nativas sobre os tipos próprios** (porte do motor real do TurfJS), e a única
função que permanece NTS-bound (`buffer`) + o interop com o ecossistema NTS são **isolados
num pacote satélite**. O "usuário" é o desenvolvedor que: (a) usa overlay e quer resultado
idêntico ao TurfJS **sem** carregar o NTS; (b) usa `buffer` e aceita o NTS num pacote
opt-in; (c) vive no ecossistema NTS (ex.: EF Core spatial) e quer converter na borda.

### User Story 1 - Overlay nativo (porte do polyclip-ts) (Priority: P1)

Quem usa `Union`, `Difference`, `Intersect`, `Dissolve` obtém os mesmos resultados de antes
(= TurfJS), agora computados **nativamente** sobre os tipos próprios — sem NTS no caminho.

**Why this priority**: é o coração da saída do motor; o `polyclip-ts` (Martinez–Rueda,
1.137 linhas, MIT) é **a fonte que o `@turf` executa** — fidelidade por construção.

**Independent Test**: os **mesmos testes de área da Onda E** passam inalterados (union
`345589333637.49884`, intersect `49387096396.63134`, difference `148281777124.80353`,
dissolve, interseção disjunta → `null`), com o caminho de código livre de NTS.

**Acceptance Scenarios**:

1. **Given** os polígonos A/B da Onda E, **When** chamo `Union`/`Intersect`/`Difference`,
   **Then** as áreas batem com o `@turf` (mesmas âncoras), **And** nenhum tipo NTS participa
   da execução.
2. **Given** dois polígonos disjuntos, **When** chamo `Intersect`, **Then** retorno `null`.
3. **Given** uma coleção de polígonos que se tocam, **When** chamo `Dissolve`, **Then** o
   resultado bate com o da Onda E.

---

### User Story 2 - Polygonize nativo (Priority: P2)

Quem usa `Polygonize` obtém o mesmo resultado de antes (= TurfJS), agora via o porte do
`@turf/polygonize` (grafo puro sobre coordenadas, 635 linhas) — sem o `Polygonizer` do NTS.

**Why this priority**: fecha o `src/Turfano/Parity/` como zona 100% livre de NTS.

**Independent Test**: o teste da Onda D (4 linhas → 1 polígono quadrado) passa inalterado.

**Acceptance Scenarios**:

1. **Given** 4 linhas formando um quadrado, **When** chamo `Polygonize`, **Then** retorno
   1 polígono com os mesmos vértices, sem NTS no caminho.

---

### User Story 3 - Pacote satélite: buffer + interop NTS (Priority: P2)

Quem precisa de `Buffer` usa um **pacote satélite** (que referencia o NTS); quem vive no
ecossistema NTS ganha a **ponte pública** (`ToNts`/`FromNts`) para converter uma vez na
borda. A fronteira usa **sequências empacotadas** (zero objetos `Coordinate`).

**Why this priority**: materializa o isolamento do NTS decidido na Fase 11 e resolve a
preocupação de GC levantada (Coordinate classe + cópia defensiva).

**Independent Test**: o teste de área do buffer da Onda E (~`3.12e10`) passa no satélite;
um teste confirma que a geometria resultante usa sequência empacotada.

**Acceptance Scenarios**:

1. **Given** um ponto e raio de 100 km, **When** chamo o buffer do satélite, **Then** a
   área bate com o `@turf` (mesma âncora da Onda E).
2. **Given** uma geometria própria, **When** converto com a ponte pública e volto, **Then**
   as coordenadas são preservadas (incl. furos e Z — testes da Fase 3 continuam valendo).
3. **Given** o pipeline do buffer, **Then** a fronteira não materializa objetos
   `Coordinate` (sequências empacotadas nos dois sentidos).

---

### Edge Cases

- Overlay: polígonos com furos; multipolígonos; anéis degenerados; resultados vazios
  (→ `null`); polígonos idênticos; toque só em vértice/aresta.
- Polygonize: linhas que não fecham anel (resultado vazio); múltiplos anéis.
- Buffer: raio negativo; `LineString`/`Polygon` de entrada; steps variados.
- Ponte pública: geometrias de terceiros construídas com factories NTS arbitrárias.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `Union`/`Difference`/`Intersect`/`Dissolve` MUST ser computados por um porte
  do **`polyclip-ts`** (MIT) sobre os tipos próprios, sem NTS no caminho de execução; os
  testes-âncora da Onda E MUST passar inalterados.
- **FR-002**: `Polygonize` MUST ser computado por um porte do **`@turf/polygonize`**, sem o
  NTS `Polygonizer`; o teste da Onda D MUST passar inalterado.
- **FR-003**: Após FR-001/FR-002, `src/Turfano/Parity/` MUST ficar **livre de
  NetTopologySuite** (verificável por busca textual vazia). O core ainda referencia o NTS
  pela superfície legada `Turf.*` — a remoção da referência é da **segunda leva** (pós-F/G).
- **FR-004**: MUST existir um **pacote satélite** (nome decidido no plano) que referencia o
  NTS e contém: o `Buffer` (a `partial class` não atravessa assemblies — expor via método de
  extensão ou classe estática própria, decidir no plano) e a **ponte pública**
  (`ToNts`/`FromNts`), com a fronteira em **sequências empacotadas** (zero `Coordinate`,
  via API pública do NTS 2.5: `PackedDoubleCoordinateSequence` +
  `GeometryFactory(CoordinateSequenceFactory)` + `GetRawCoordinates()`).
- **FR-005**: `UnsafeAccessor` MUST NOT ser usado (último recurso documentado na Fase 11,
  condicionado a profiling futuro).
- **FR-006**: Um arquivo **NOTICE** na raiz MUST registrar as atribuições MIT
  (`polyclip-ts`, TurfJS) dos ports.
- **FR-007**: Não-regressão: as `Turf.*.cs` legadas permanecem intactas; a suíte existente
  (232) permanece verde; smoke AOT da serialização 0 warnings; multi-target
  `net8.0;net9.0;net10.0`; nomes .NET (sem acrônimos crípticos).

### Key Entities

Sem entidades de dados novas. Novos artefatos de empacotamento: o **pacote satélite**
(assembly separado com a dependência NTS) e o **NOTICE** de licenças.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Os testes de overlay da Onda E passam **sem NTS no caminho** (mesmas âncoras
  de área, `~1e4` abs); o de polygonize idem.
- **SC-002**: Busca por `NetTopologySuite` em `src/Turfano/Parity/` retorna **vazio**.
- **SC-003**: O pacote satélite compila e seus testes passam: buffer com área = `@turf`
  (~`3.12e10`) e fronteira com sequências empacotadas (zero `Coordinate` materializado).
- **SC-004**: A ponte pública preserva coordenadas em ida-e-volta (furos e Z).
- **SC-005**: `NOTICE` presente com as atribuições MIT; build limpo `net8.0;net9.0;net10.0`;
  suíte 232 + novos **verde**; smoke AOT da serialização **0 warnings IL**.

## Assumptions

- As funções nativas continuam na fachada **`Geo`** (padrão das Ondas A–E); o satélite expõe
  o buffer por método de extensão ou classe própria (decisão do plano).
- O porte do `polyclip-ts` é **fiel à fonte** (mesma aritmética/ordenação do sweep) — quando
  o comportamento surpreender, seguir a fonte/`@turf`, não a intuição (lição das ondas).
- A decisão estratégica (por que portar polyclip e não vendorizar NTS; por que o buffer fica
  NTS-bound) está **fechada e fundamentada na Fase 11 do plano-mãe** — fora de discussão
  nesta feature.
- **Fora de escopo** (segunda leva, pós-Ondas F/G): deletar a superfície legada `Turf.*`,
  remover UnitsNet, remover a referência NTS do core, split/publicação final na 1.0. Também
  fora: as Ondas F e G.
