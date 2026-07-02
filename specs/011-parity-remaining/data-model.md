# Data Model: Onda G — Paridade total

**Data**: 2026-07-02 | **Feature**: `011-parity-remaining`

Sem entidades públicas novas. Estruturas internas dos motores:

## RBushIndex (R2, `Parity/Spatial/`)

- Nó: `Children` (lista), `Height`, `IsLeaf`, bbox (`MinX/MinY/MaxX/MaxY`); itens folha
  carregam payload genérico + bbox. API do rbush: `Load` (bulk, OMT), `Insert`, `Search`
  (retorna na ordem da travessia — é ESSA ordem que os consumidores dependem), `Remove`.
- `GeoJsonSpatialIndex` (R5): itens = `Feature` com bbox da geometria.

## SweeplineIntersections (R4, `Parity/Spatial/`)

- Evento (endpoint de segmento, ordenado x→y), fila (sort), varredura com lista ativa;
  saída = lista de pontos `[x, y]` na ordem do algoritmo (dedup opcional do lib).

## KMeans (R3, `Cluster.KMeans.cs`, internal)

- Entrada: `double[][] data`, `k`, centroides iniciais = `data[0..k)`. Lloyd: assign por
  distância euclidiana ao quadrado → recompute médias → até estabilizar (o skmeans compara
  centroides com tolerância/iterações — portar o critério exato do caminho usado).
- Saída: `Idxs` (cluster por ponto), `Centroids`.

## ShortestPath (porte do @turf)

- Grade sobre a bbox expandida (`transformScale` ×1.15 na fonte? portar exato), células
  bloqueadas por `BooleanPointInPolygon` nos obstáculos, busca A*/dijkstra da fonte.

## UnkinkPolygon (R9)

- `simplepolygon` embutido no bundle: decomposição de anel auto-intersectante em anéis
  simples (grafo de pseudo-vértices nas interseções; usa rbush para achar interseções).

## Aleatórios (R6)

- `Random.Shared` no lugar de `Math.random`; sem estado global próprio; sem seed público
  (contrato do @turf).

## Estatística (US3)

- Funções puras: pesos/médias sobre `Distance`/`Centroid`/`Bearing` existentes; matrizes
  de peso (`distanceWeight`) como `double[][]` internos ao cálculo.
