# Research: Onda C — Transformation & Mutation (Fase 0)

Sem `[NEEDS CLARIFICATION]`. Decisões (estratégia **mista por função**, validar TUDO vs `@turf`):

## Decisão 1 — Mutações (US1): re-tipar/portar simples

`flip` (troca lon/lat), `truncate` (corta casas decimais), `round` (arredonda — **não
existe** hoje, porte), `cleanCoords` (remove pontos duplicados consecutivos e colineares
redundantes — conferir o algoritmo do `@turf/clean-coords`), `rewind` (orienta anéis:
exterior e furos). São operações de coordenada puras; portar o algoritmo do `@turf` direto
sobre `EachPosition`/rings dos novos tipos.

## Decisão 2 — `transformScale` GEODÉSICO (US2, correção-chave)

**Decisão**: portar o algoritmo do `@turf/transform-scale`, que escala **geodesicamente**:
para cada ponto, calcula `rhumbDistance` e `rhumbBearing` da origem ao ponto, multiplica a
distância pelo fator e recoloca com `rhumbDestination`. **Não** a versão cartesiana do NTS
(divergência da Fase 1/2). A origem default é o **sw** do bbox? Conferir no `@turf` (pode ser
`centroid`/`sw`/`se`/...). Reusar `RhumbDistance`/`RhumbBearing`/`RhumbDestination` da Onda A.

## Decisão 3 — `transformRotate`/`transformTranslate` (US2)

`transformTranslate`: mover cada ponto por `Destination`(distância, rumo) — reusar
`Destination` (great-circle) ou rhumb conforme o `@turf`. `transformRotate`: rotacionar cada
ponto em torno do pivô usando `rhumbBearing`+`rhumbDistance`+`rhumbDestination` (somar o
ângulo). Conferir o `@turf` e validar. `clone`: cópia profunda dos tipos (records já são
imutáveis; cuidar dos arrays de coordenadas).

## Decisão 4 — Geração/suavização (US3): portar o `@turf`

- `circle`: N passos de `Destination` a partir do centro (rumo 0..360), raio em `Units`.
- `bezierSpline`: porte do algoritmo de spline do `@turf` (Catmull-Rom/`spline-js`).
- `polygonSmooth`: Chaikin (média ponderada de vértices), `iterations`.
- `lineOffset`: deslocamento perpendicular dos segmentos + junções (algoritmo do `@turf`).
- `simplify`: **Douglas-Peucker** (decisão op-a-op da Fase 2); conferir a divergência com o
  `simplify-js` do `@turf` (modo `highQuality` usa DP sem o pré-passo radial).

## Decisão 5 — `rewind` reusa `BooleanClockwise`

`rewind` (RFC 7946 / `@turf/rewind`): por default, **anel externo anti-horário, furos
horários** (conferir a convenção exata do `@turf`, que pode diferir — validar GT). Usar
`BooleanClockwise` (Onda B) para detectar a orientação e inverter quando necessário.

## Decisão 6 — Harness de validação

Harness Bun emitindo as saídas do `@turf` por função/fixture (geometrias serializadas para
comparação estrutural; números para tolerância apertada). Quando o GT surpreender, **seguir o
`@turf`**.

## Riscos

- `transformScale`/`rotate`: a **origem/pivô** default e a unidade de ângulo precisam casar
  com o `@turf` (conferir, não presumir).
- `bezierSpline`/`lineOffset`/`simplify` são algorítmicas — conferir o algoritmo exato do
  `@turf` em `reference/node_modules/@turf/{bezier-spline,line-offset,simplify}`.
