# Research: Saída do motor NTS — leva 1 (Fase 0)

Sem `[NEEDS CLARIFICATION]`. Fatos verificados na fonte em 2026-07-01 (bundle
`reference/node_modules/polyclip-ts@0.16.8`):

## Decisão 1 — O porte do polyclip são TRÊS peças (não uma)

O `polyclip-ts` importa `bignumber.js` e `splaytree-ts` (não estão nas 1.137 linhas do
bundle). **As coordenadas fluem como `BigNumber` pelo algoritmo inteiro** (embrulhadas no
`geom-in`, desembrulhadas no `geom-out`). Porte completo:

1. **`ExactDecimal`** (substitui o `bignumber.js`): decimal de precisão arbitrária com
   mantissa `System.Numerics.BigInteger` + expoente. **Verificado no bundle**: as únicas
   operações usadas são `plus/minus/times/comparedTo/abs/exponentiatedBy(2)/
   isLessThanOrEqualTo/sqrt(?)` — **nenhum `dividedBy` encontrado** → todas as operações
   são **exatas**, sem semântica de arredondamento a reproduzir. O polyclip **não
   configura** o BigNumber (sem `BigNumber.config`). *Tarefa de verificação no porte*: se
   alguma divisão/sqrt aparecer na leitura linha-a-linha, reproduzir os defaults do
   bignumber.js (20 casas, half-up) e registrar.
2. **`SplayTreeSet`** (porte do `splaytree-ts`, 687 linhas, BSD-3): usada pelo sweep
   (fila de eventos e status) e pelo `snap`. Porte 1:1 — a **ordem de iteração/critério do
   comparator** faz parte do comportamento do algoritmo.
3. **Core do polyclip** (1.137 linhas, MIT): módulos do bundle (comentários `// src/*.ts`):
   `constant`, `compare`, `orient`, `snap`, `identity`, `precision`, `bbox`, `vector`,
   `sweep-event`, `segment`, `sweep-line`, `operation`, `geom-in`, `geom-out`, `index`.
   Porte 1:1 mantendo nomes descritivos (.NET, sem acrônimos).

**Precisão default**: `precision = set()` (eps `undefined`) → comparações exatas, `orient`
exato, `snap` identidade. É assim que o `@turf` o executa. O caminho com eps também é
portado (é pequeno), mas não é o default.

## Decisão 2 — Aritmética: decimal exato, NÃO `decimal` do C# nem double-double

- `decimal` (C#): 28-29 dígitos — o produto de dois doubles com 17 dígitos exige ~34 →
  arredondaria exatamente onde o BigNumber foi introduzido para ser exato. **Rejeitado.**
- Double-double (estilo JTS `CGAlgorithmsDD`): divergiria da fonte (outros predicados,
  outros empates). **Rejeitado** — fidelidade por construção exige a mesma aritmética.
- **`ExactDecimal` sobre `BigInteger`**: +,−,× exatos por construção; `CompareTo` por
  normalização de expoente. ~150–250 linhas. **Escolhido.**

## Decisão 3 — Satélite: nome e forma da API

- **Nome**: `Turfano.NetTopologySuite` (convenção do ecossistema .NET para pacotes de
  integração; nada críptico).
- **Bridge pública**: classe estática **`NtsConvert`** (estilo `Convert`/`JsonConvert`):
  `ToNts(Geometry|Position)` / `FromNts(NTS Geometry|Coordinate)`. Expõe tipos NTS de
  propósito (é o interop).
- **Buffer**: método de **extensão** sobre `Turfano.GeoJson.Geometry`
  (`geometry.Buffer(radius, steps)`) — `partial class Geo` não atravessa assemblies.
  `Geo.Buffer` sai do core (breaking pré-1.0, aceito na Fase 11).
- O satélite reimplementa localmente o helper trivial de mapear posições (sem abrir
  `InternalsVisibleTo` novo do core).

## Decisão 4 — Fronteira empacotada no `NtsConvert` (degrau 1 da escada da Fase 11)

- Ida: `Position[]` → `double[]` cru → `PackedDoubleCoordinateSequence(double[], 2, 0)` via
  `GeometryFactory(PackedCoordinateSequenceFactory.DoubleFactory)` — **zero `Coordinate`**.
- Volta: fast-path `GetRawCoordinates()` quando a sequência é empacotada (sem cópia);
  fallback `GetOrdinate(i, ordinate)` para geometrias de terceiros (qualquer factory).
- `UnsafeAccessor` **proibido** nesta feature (último recurso documentado na Fase 11).

## Decisão 5 — Polygonize nativo (porte do `@turf/polygonize`)

Grafo não-dirigido de arestas (nós = coordenadas, arestas = segmentos), remoção de
dangles/cut-edges e extração de anéis — 635 linhas, puro sobre coordenadas. Porte direto
sob `Parity/` (internal), `Geo.Polygonize` mantém a assinatura.

## Decisão 6 — Licenças (NOTICE na raiz)

- `polyclip-ts` — MIT, © 2022 Luiz Felipe Machado Barboza.
- `splaytree-ts` — BSD-3-Clause, © 2022 Luiz Felipe Machado Barboza.
- TurfJS — MIT (fonte dos algoritmos portados nas Ondas A–E e do polygonize).

## Riscos

- **Empates do sweep**: a ordenação de eventos (comparators de `sweep-event`/`segment`) é
  onde um `<=`/`<` trocado corrompe silenciosamente — porte linha-a-linha + regressão pelas
  âncoras das Ondas D/E.
- **Desempenho**: decimal arbitrário no sweep é mais lento que double (o `@turf` paga o
  mesmo). Otimização futura fora de escopo (registrar no plano-mãe ao concluir).
- A suíte atual só cobre casos-âncora; considerar 1–2 casos extras de overlay com furos
  (GT via bun) no fechamento.
