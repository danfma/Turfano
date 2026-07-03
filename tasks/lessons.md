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

## 2026-07-03 — Mensagem de commit: subject curto na primeira linha

**Correção do usuário**: "o GitHub limita o tamanho das mensagens (na primeira linha) para
visualização rápida... Prefira deixar uma mensagem curta, direta e com referência ao spec,
acompanhada de uma descrição adequada, sem ser prolixo, nas próximas linhas."

**Regra**: primeira linha (subject) do commit ≤ ~50–72 chars, direta, com referência ao
spec/escopo quando houver. Detalhes vão no CORPO (`-m` seguinte), conciso — não enfiar
parágrafos na primeira linha. Vale também para títulos de PR.

## 2026-07-03 — Isolar subagentes que escrevem arquivos (worktree)

Rodar um subagente que edita arquivos no MESMO working tree enquanto eu faço `checkout`/
`commit` em paralelo gera corrida de git (HEAD trocou por baixo, commit incidental de
renames). **Regra**: subagente que muta arquivos → `isolation: "worktree"`, OU ele só
edita (sem git) e eu fico fora do git até ele terminar. Nunca operar git em paralelo com
um subagente mutando o mesmo working tree.

## 2026-07-03 — Workflow de GitHub Pages não pode se auto-habilitar

`actions/configure-pages@v5` com `enablement: true` falha com o `GITHUB_TOKEN` padrão
("Resource not accessible by integration" — habilitar Pages exige admin). Um workflow de
docs com gatilho `push` falha a cada commit até o Pages ser habilitado À MÃO
(Settings → Pages → Source: GitHub Actions), vermelhando a main. **Regra**: workflow de
publicação de Pages = `workflow_dispatch` (manual) até o Pages estar habilitado; não
depender de auto-enable.
