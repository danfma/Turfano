# Research: Correção de bugs do Turfano (Fase 0)

Não há marcadores `[NEEDS CLARIFICATION]` no spec. Esta pesquisa documenta a
análise de causa-raiz (validada lendo o código) e a fonte dos valores de referência.

## Decisão 1 — Causa-raiz e correção de `Angles.TwoPi`

**Decisão**: alterar `src/Turfano/Angles.cs` para `TwoPi = Angle.FromRadians(2 * Math.PI)`.

**Análise (validada nos 4 pontos de uso)**: hoje `Angles.cs` define tanto `Pi` quanto
`TwoPi` como `Angle.FromRadians(Math.PI)`. Consumidores:

1. `Turf.RhumbBearing.cs` → `CalculateRhumbBearing`: normaliza
   `if (deltaLambda > Angles.Pi) deltaLambda -= Angles.TwoPi;` e o ramo negativo
   simétrico. Com `TwoPi = π`, subtrai meia-volta (errado); com `2π`, envolve
   corretamente `deltaLambda` para `(-π, π]`.
2. `Turf.RhumbBearing.cs` → `bear180 = bear360 > Angles.Pi ? -(Angles.TwoPi - bear360) : bear360`.
   Para `bear360 ∈ (180°,360°)`, o correto é `bear360 − 360°` (resultado em
   `(-180°,0°)`). Com `TwoPi = 2π = 360°` isso é obtido; com `π = 180°` dava
   `bear360 − 180°` (sinal/magnitude errados).
3. `Turf.Angle.cs` → `GetAngle` caminho `Explementary`: `return Angles.TwoPi - angleA0;`
   = `360° − θ` somente se `TwoPi = 360°`.
4. `Angles.Pi` permanece `π` (correto) e é usado apenas em comparações — não muda.

**Conclusão**: a correção de **uma** constante conserta `RhumbBearing` (ambos os
trechos) e `GetAngle` explementar simultaneamente. Nenhuma outra alteração de produção
é necessária para esses três caminhos.

**Alternativas consideradas**: corrigir cada call-site manualmente (rejeitada —
mascara a causa-raiz e deixa a constante errada para usos futuros).

## Decisão 2 — Causa-raiz e correção de `Turf.TransformScale`

**Decisão**: alterar a linha `var scaledY = dy * options.FactorY ?? factor;` para
`var scaledY = dy * (options.FactorY ?? factor);`.

**Análise**: `*` tem precedência maior que `??`, então a expressão atual é
`(dy * options.FactorY) ?? factor`. Como `options.FactorY` é `double?` e, no caso
padrão, é `null`, `dy * null` resulta em `null`, e `?? factor` devolve a **constante**
`factor` — o eixo Y deixa de depender de `dy`, colapsando a geometria. A parentização
restaura `dy * (FactorY ?? factor)`, escalando Y pelo fator de Y (ou pelo fator geral).

**Varredura associada (FR-009)**: procurar no projeto outros usos de
`a * x.Nullable ?? b` (grep por `\* options\.` e `?? ` próximos) e corrigir/registrar.

**Alternativas consideradas**: inicializar `FactorY`/`FactorZ` com o valor de `factor`
no `Default` em vez de tratar `null` (rejeitada — o `OrDefault` usa `Empty == Default`
e mudaria a semântica de "não informado"; a parentização é a correção mínima e local).

## Decisão 3 — Fonte dos valores de referência do TurfJS

**Decisão**: usar valores do TurfJS como verdade. Âncoras principais:

- `rhumbBearing([-75.343, 39.984], [-75.534, 39.123]) = -170.294175°` (verificado
  rodando o `@turf/rhumb-bearing` real em `reference/`). **Atenção**: o XML-doc do
  próprio `Turf.RhumbBearing.cs` diz `9.71°`, que é o **sentido inverso**
  (`[-75.534,39.123]→[-75.343,39.984] = 9.705825°`) e coincide com a saída do bug —
  por isso era enganoso. Cardinais TurfJS: E=`90`, W=`-90`, S=`180`, N=`0`;
  antimeridiano `[179,0]→[-179,0] = 90` (caminho curto). Tolerância de teste: `.Within(0.1)`.
- `transformScale`: para um polígono e `factor = 2` sem `FactorY`, a bbox resultante
  tem largura e altura = 2× as originais, escalando a partir do centro da bbox
  (comportamento do `@turf/transform-scale` com `origin: "centroid"`/centro de bbox).
- `GetAngle` explementar: `360° − θ` para o mesmo trio de pontos do teste não-explementar.

**Como obter mais referências quando necessário**: rodar o TurfJS via `reference/`
(projeto Bun) — `bun add @turf/turf` e um script `bun run` que imprime os valores —
ou usar os fixtures dos pacotes `@turf/*` em `reference/node_modules/@turf`.

**Alternativas consideradas**: derivar valores "na mão" pela fórmula (rejeitada como
fonte primária — o objetivo é paridade com o TurfJS, não com a fórmula teórica).

## Riscos / observações

- A correção de `Angles.TwoPi` altera o resultado de **qualquer** função que use essa
  constante. Mitigação: rodar a suíte completa (FR-008) e conferir que nenhuma outra
  função regrediu (hoje só `RhumbBearing`/`GetAngle` a usam).
- `TransformScale` não tinha teste; o novo teste de caso padrão deve **falhar antes** do
  patch (SC-005) — capturar essa demonstração ao implementar (TDD).
