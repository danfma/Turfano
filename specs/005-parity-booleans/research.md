# Research: Onda B — Booleans / Assertions (Fase 0)

Sem `[NEEDS CLARIFICATION]`. Decisões:

## Decisão 1 — Portar o `@turf`, NÃO re-tipar o NTS

**Decisão**: os predicados novos portam o **algoritmo do `@turf`** sobre os tipos próprios.
**Não** re-tipar os `Turf.Boolean*.cs` atuais — verificado que delegam a predicados do NTS
(`polygon.Contains(point)`, `feature1.Overlaps(...)`) com semântica de **fronteira/igualdade
diferente** do `@turf` (Fase 2). `booleanConcave` e `booleanValid` nem existem hoje.

**Rationale**: a onda exige fidelidade à semântica do Turf; o NTS diverge exatamente onde
importa (borda). É o oposto da Onda A (lá a maioria já era fiel e bastava re-tipar).

## Decisão 2 — `booleanPointInPolygon` + fronteira

**Decisão**: portar o `inRing`/`inBBox` do `@turf/boolean-point-in-polygon`, que trata
explicitamente o ponto **na borda** e respeita `ignoreBoundary`. (O `PointInPolygon`/`InRing`
ray-cast da Onda A não modela a borda — estender/portar a versão do `@turf`.) Para
`MultiPolygon`, dentro de algum polígono e fora dos furos.

## Decisão 3 — Predicados relacionais (a parte difícil)

**Decisão**: portar os algoritmos do `@turf` para `contains/within/disjoint/intersects/
crosses/overlap/touches`. Eles compõem peças simples (point-in-polygon, interseção de
segmentos, point-on-line) — reusar helpers da Onda A (`IsLeft`, `NearestPointOnLine`) e o
`booleanPointInPolygon` desta onda. `booleanDisjoint`/`booleanIntersects` são duais;
`booleanContains`/`booleanWithin` idem (argumentos trocados).

## Decisão 4 — `booleanEqual`, `booleanClockwise`, `booleanParallel`, `booleanConcave`

**Decisão**: 
- `booleanClockwise`: soma da área sinalizada do anel (porte direto; provavelmente já fiel).
- `booleanParallel`: compara os segmentos por diferença de rumo (porte do `@turf`).
- `booleanConcave`: testa se algum vértice quebra a convexidade (porte do `@turf`).
- `booleanEqual`: igualdade estilo `geojson-equality` (mesma forma a menos de ordem e dentro
  de precisão) — portar com tolerância; **não** o `Equals` exato do NTS.

## Decisão 5 — Uso do NtsBridge

**Decisão**: por padrão **não** usar o NTS (a semântica diverge). Só considerar o
`NtsBridge` para uma peça onde o NTS comprovadamente case com o `@turf` (improvável nos
relacionais). Registrar caso a caso.

## Decisão 6 — Harness de validação + fixtures

**Decisão**: harness Bun que (a) chama cada `boolean*` do `@turf` em casos-âncora e (b)
**varre as fixtures `test/true` e `test/false`** que os pacotes `@turf/boolean-*` trazem,
emitindo `(nome, esperado)` para os testes C# reproduzirem. Cobre os casos de fronteira.

## Riscos

- Os relacionais (`overlap`/`touches`/`crosses`) têm muitos casos por combinação de
  dimensão (ponto/linha/polígono) — cobrir com as fixtures do `@turf`.
- `booleanEqual` com precisão/ordem: seguir o `geojson-equality` (tolerância default).
