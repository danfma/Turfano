# Research: Onda E — Overlay / Clipping (Fase 0)

Sem `[NEEDS CLARIFICATION]`. Decisões (overlay/buffer **NTS-interino** via `NtsBridge`;
`bboxClip` portado; validar TUDO vs `@turf`):

## Decisão 1 — Overlay via NTS (`NtsBridge`)

**Decisão**: `union`/`difference`/`intersect` chamam os operadores de overlay do NTS sobre as
geometrias convertidas pela ponte: `FromNts(ToNts(a).Union(ToNts(b)))`,
`.Difference(...)`, `.Intersection(...)`. **Rationale**: a Fase 2 mediu — área **idêntica** ao
`polyclip-ts` do `@turf` (`union`=`difference` iguais; `intersect` igual em ~`1e-5`); portar
`polyclip-ts` é caro e sem ganho. `dissolve` = unir (`Union`) os polígonos de uma coleção que
se tocam (overlay NTS) e achatar.

## Decisão 2 — `buffer` via NTS (`Geometry.Buffer`)

**Decisão**: `buffer` chama `NTS Geometry.Buffer(distância)` via a ponte. **Rationale**: o
`buffer` do `@turf` **é** o JTS (= NTS), então o NTS é fidelidade máxima. **Ponto a conferir**:
o `@turf` converte o raio para **graus** (planar) antes de bufferizar (ex.: via
`lengthToDegrees`/projeção). Reproduzir a conversão exata do `@turf` e **validar a área** vs o
`@turf` real; ajustar a conversão até casar.

## Decisão 3 — `bboxClip` portado (Cohen-Sutherland)

**Decisão**: portar o algoritmo do `@turf/bbox-clip` (que usa `lineclip` — Cohen-Sutherland p/
linhas, Sutherland-Hodgman p/ polígonos) sobre os novos tipos. O Turfano já tem um
Cohen-Sutherland próprio (`Turf.BBoxClip.cs`) — **re-tipar e validar vs o `@turf`** (a Fase 2
marcou "a validar"; se divergir, alinhar ao `lineclip`/`@turf`).

## Decisão 4 — Resultados nulos/vazios

**Decisão**: o NTS retorna geometria **vazia** quando não há interseção/diferença; mapear para
`null` (como o `@turf` `intersect`/`difference` retornam `null`). `union` de disjuntos →
`MultiPolygon`. Conferir o comportamento exato do `@turf` no GT.

## Decisão 5 — AOT (registrado)

Overlay/`buffer` carregam o **NTS interino** (que pode usar reflexão) → **podem não ser
AOT-safe**. Isso é **aceito** (decisão da Fase 2; NTS é o motor interino). O smoke de AOT segue
exercitando só a **serialização GeoJSON** (que permanece **0 warnings**); não incluir
overlay/buffer no smoke.

## Decisão 6 — Harness de validação

Harness Bun emitindo, por função, a **área** do resultado do `@turf` (overlay/buffer) e a
**estrutura** (`bboxClip`). Reusar `Geo.Area` (Onda A) para comparar áreas no lado C#.

## Riscos

- `buffer`: a conversão raio→graus do `@turf` precisa casar (validar área; pode haver
  diferença planar×projeção — seguir o `@turf`).
- `bboxClip`: o Cohen-Sutherland próprio pode divergir do `lineclip` do `@turf` em cantos —
  validar e alinhar.
- Mapeamento de vazio→`null` deve casar com o `@turf` (GT).
