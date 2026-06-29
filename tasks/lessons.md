# Lessons

Padrões aprendidos com correções, para não repetir.

## 2026-06-29 — Evitar acrônimos crípticos em nomes

**Correção do usuário**: "Vamos evitar acrônimos, sempre que possível, para facilitar o
entendimento do código. Nomes bem conhecidos podem ser mantidos."

**Regra**: nomear variáveis/métodos/propriedades por extenso. Nada de `D2R`, `Rtan`, `Ltan`,
`Pip`, `cmpAI`. Usar `RadiansPerDegree`, `RightTangent`, `LeftTangent`,
`ClassifyPointInPolygon`, `compareStartToIntersect`. **Exceção**: nomes de domínio bem
conhecidos podem ficar — `BBox`, `lon`/`lat`, e a notação geodésica `phi`/`lambda` (que
espelha as fórmulas de referência e ajuda a conferir contra o `@turf`). Convenção também
registrada em `CLAUDE.md` (## Conventions).

## 2026-06-29 — Validar contra o @turf, não confiar no código NTS existente

Ao portar funções (ondas de paridade), **re-tipar a lógica existente não é seguro**: a
validação contra o `@turf` real pegou bugs no código NTS (`RhumbDestination` com `q`
errado; `nearestPointOnLine` planar vs geodésico; `centroid` incluindo o vértice de
fechamento; predicados `Boolean*` com semântica de fronteira do NTS, não do Turf). Sempre
validar cada função contra o `@turf` real (via `reference/` com bun), inclusive fixtures.
