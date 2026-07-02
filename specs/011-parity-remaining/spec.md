# Feature Specification: Onda G — Paridade total (pacotes restantes)

**Feature Branch**: `011-parity-remaining`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Sétima e última onda de paridade: fechar **100% da cobertura**
do `@turf` na fachada `Geo`. Cruzamento já medido (115 módulos × fachada atual): faltam
**27 funções** (~2,9 mil linhas de fonte), além de `buffer` (satélite, fora da onda) e
`geojson-rbush` (infra interna)."

## User Scenarios & Testing *(mandatory)*

Última **onda de paridade**: linhas (interseção/overlap/split/segmentos/arcos/caminho),
formas e projeção (ellipse/sector/mask/unkink/**toMercator/toWgs84**), estatística espacial
(centros/médias direcionais/Moran/quadrat) e aleatórios/clusters/agregação. Ao final, o
índice do `@turf` está 100% coberto (ou com exclusões explícitas) — e a `projection` era a
**última função do legado sem versão `Geo`**, destravando a leva 2 da Fase 11.

### User Story 1 - Operações de linha (Priority: P1)

Quem usa `lineSegment`, `lineIntersect`, `lineOverlap`, `lineSplit`, `lineArc`,
`shortestPath`, `nearestPointToLine` e `angle` obtém os mesmos resultados do TurfJS.

**Why this priority**: são as funções de linha que faltam para análises de rede/rotas;
`lineIntersect`/`lineSplit` são amplamente usadas.

**Independent Test**: fixtures de linhas cruzadas/sobrepostas batem com o `@turf` real.

**Acceptance Scenarios**:

1. **Given** duas linhas que se cruzam, **When** chamo `LineIntersect`, **Then** os pontos
   de interseção batem com `turf.lineIntersect`.
2. **Given** uma linha e um divisor, **When** chamo `LineSplit`, **Then** os pedaços batem
   com `turf.lineSplit`.
3. **Given** duas linhas parcialmente sobrepostas, **When** chamo `LineOverlap`, **Then**
   os trechos comuns batem com `turf.lineOverlap`.
4. **Given** um start/end e obstáculos, **When** chamo `ShortestPath`, **Then** o caminho
   bate com `turf.shortestPath`.

---

### User Story 2 - Formas e projeção (Priority: P1)

Quem usa `ellipse`, `sector`, `mask`, `unkinkPolygon` e `toMercator`/`toWgs84` obtém as
mesmas geometrias do TurfJS. O `mask` reusa o motor de overlay nativo (o `@turf` usa o
mesmo polyclip que já portamos); a `projection` fecha o último gap do legado.

**Why this priority**: `projection` é pré-requisito da leva 2 (limpeza final); `mask` é
ganho quase gratuito sobre o motor da Fase 11.

**Independent Test**: geometrias estruturalmente/numericamente iguais às do `@turf`.

**Acceptance Scenarios**:

1. **Given** centro e eixos, **When** chamo `Ellipse`/`Sector`, **Then** os polígonos batem
   com o `@turf`.
2. **Given** um polígono, **When** chamo `Mask`, **Then** o "mundo menos polígono" bate com
   `turf.mask`.
3. **Given** um polígono com auto-interseção, **When** chamo `UnkinkPolygon`, **Then** os
   polígonos simples resultantes batem com `turf.unkinkPolygon`.
4. **Given** qualquer geometria, **When** chamo `ToMercator` e depois `ToWgs84`, **Then**
   faz o round-trip e bate com `turf.toMercator`/`toWgs84`.

---

### User Story 3 - Estatística espacial e centros (Priority: P2)

Quem usa `centerMean`, `centerMedian`, `directionalMean`, `distanceWeight`, `moranIndex`,
`nearestNeighborAnalysis`, `quadratAnalysis` e `standardDeviationalEllipse` obtém os mesmos
números do TurfJS.

**Why this priority**: análises estatísticas — determinísticas, fáceis de validar
numericamente, mas menos usadas que P1.

**Independent Test**: valores numéricos apertados vs o `@turf` real.

**Acceptance Scenarios**:

1. **Given** pontos com pesos, **When** chamo `CenterMean`/`CenterMedian`, **Then** os
   centros batem com o `@turf`.
2. **Given** uma variável em pontos, **When** chamo `MoranIndex`, **Then** o índice e o
   z-score batem com o `@turf`.

---

### User Story 4 - Aleatórios, clusters e agregação (Priority: P2)

Quem usa `randomPosition`/`randomPoint`/`randomLineString`/`randomPolygon`, `sample`,
`clustersKmeans`, `clustersDbscan`, os helpers de cluster (`getCluster`/`clusterEach`/
`clusterReduce`) e `collect` obtém o mesmo comportamento do TurfJS — com validação
**estrutural/distribucional** onde há aleatoriedade.

**Why this priority**: fecham a cobertura; a natureza aleatória exige critério de teste
diferente (estrutura, contagens, bbox), documentado no plano.

**Independent Test**: aleatórios validados por estrutura (contagem, contenção na bbox,
anéis fechados); dbscan/collect/helpers por igualdade com o `@turf`; kmeans conforme o
determinismo apurado no plano.

**Acceptance Scenarios**:

1. **Given** uma bbox e um count, **When** chamo `RandomPoint`, **Then** vêm `count` pontos
   dentro da bbox (estrutura = `@turf`).
2. **Given** pontos e `maxDistance`, **When** chamo `ClustersDbscan`, **Then** os rótulos
   de cluster batem com o `@turf`.
3. **Given** pontos e polígonos, **When** chamo `Collect`, **Then** as propriedades
   agregadas batem com o `@turf`.

---

### Edge Cases

- `lineIntersect` em linhas colineares/sobrepostas; `lineSplit` com divisor que não toca.
- `shortestPath` sem obstáculos (linha direta?) e com start/end dentro de obstáculo.
- `unkinkPolygon` em polígono já simples (identidade?) e com múltiplos kinks.
- `toMercator` perto dos polos (clamp do `@turf`?); round-trip com precisão.
- `mask` com MultiPolygon e com máscara custom.
- `randomPolygon` com `num_vertices`/`max_radial_length`; `sample` com n > população.
- `clustersDbscan` com pontos isolados (noise/edge); `clustersKmeans` com k > n.
- `moranIndex`/`quadratAnalysis` com poucas features (divisões por zero?) — seguir o GT.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As 27 funções do cruzamento MUST existir na fachada `Geo` com
  `Turfano.GeoJson`/`Turfano.Units` nas assinaturas: linhas (`LineSegment`,
  `LineIntersect`, `LineOverlap`, `LineSplit`, `LineArc`, `ShortestPath`,
  `NearestPointToLine`, `Angle`), formas/projeção (`Ellipse`, `Sector`, `Mask`,
  `UnkinkPolygon`, `ToMercator`, `ToWgs84`), estatística (`CenterMean`, `CenterMedian`,
  `DirectionalMean`, `DistanceWeight`, `MoranIndex`, `NearestNeighborAnalysis`,
  `QuadratAnalysis`, `StandardDeviationalEllipse`) e aleatórios/clusters
  (`RandomPosition`/`RandomPoint`/`RandomLineString`/`RandomPolygon`, `Sample`,
  `ClustersKmeans`, `ClustersDbscan`, `GetCluster`/`ClusterEach`/`ClusterReduce`,
  `Collect`).
- **FR-002**: Funções determinísticas MUST bater com o `@turf` real (GT via `reference/`
  com bun, capturado ANTES do porte). Funções aleatórias (`random*`, `sample`, e o kmeans
  se não-determinístico) MUST ser validadas estruturalmente/distribucionalmente, com o
  critério documentado.
- **FR-003**: As dependências externas (`rbush`, `skmeans`, `sweepline-intersections`,
  `geojson-rbush`) MUST ser decididas no plano pelo método da Fase 11 (medição + decisão
  registrada). Atenção registrada: a ORDEM dos resultados do rbush pode afetar saídas —
  avaliar porte 1:1 vs varredura equivalente.
- **FR-004**: O `mask` MUST reusar o motor polyclip nativo (Fase 11), não outro caminho.
- **FR-005**: `src/Turfano/Parity/` MUST permanecer livre de NetTopologySuite; legado
  intocado; suíte (258) verde; AOT 0 warnings; multi-target net8/9/10; `NOTICE` atualizado
  para portes novos com licença conferida.

### Key Entities

Sem entidades públicas novas — funções na fachada `Geo` (motores internos como índice
espacial/kmeans ficam `internal`, padrão `Polyclip`/`Tessellation`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cruzamento final índice-`@turf` × fachada `Geo` = **100% coberto**, com
  exclusões explícitas listadas (`buffer` → satélite; `geojson-rbush` → infra interna).
- **SC-002**: 100% das funções novas com teste TUnit ancorado no `@turf` real
  (numérico/estrutural conforme FR-002).
- **SC-003**: `ToMercator`/`ToWgs84` na fachada `Geo` — pré-requisito da leva 2 cumprido
  (nenhuma função do legado sem equivalente `Geo`).
- **SC-004**: `grep NetTopologySuite src/Turfano/Parity/` vazio; build limpo net8/9/10;
  suíte **258 + novos, 0 falhas**; AOT smoke 0 warnings IL.

## Assumptions

- Cruzamento de cobertura já feito (115 módulos): 27 funções faltantes listadas em FR-001;
  qualquer módulo novo descoberto no plano entra na mesma onda ou é listado como exclusão.
- Aleatoriedade: o `@turf` usa gerador não-semeável; a versão `Geo` segue o mesmo contrato
  (sem seed público), e os testes são estruturais — decisão fechada, detalhes no plano.
- `buffer` permanece no satélite `Turfano.NetTopologySuite` (decisão da Fase 11, não
  re-litigar). `geojson-rbush` é infraestrutura (vira helper interno se necessário).
- **Fora de escopo**: leva 2 da Fase 11 (deletar legado/UnitsNet/split 1.0), otimizações.
