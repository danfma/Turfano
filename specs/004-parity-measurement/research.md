# Research: Onda A — Measurement (Fase 0)

Sem `[NEEDS CLARIFICATION]` (a placement das funções está como assumption no spec).
Decisões de implementação:

## Decisão 1 — Onde vivem as novas funções

**Decisão**: sobrecargas na fachada **`Turf`** (partials em `src/Turfano/Parity/`)
recebendo `Turfano.GeoJson` e devolvendo `Turfano.Units`/`Turfano.GeoJson`. As `Turf.*.cs`
NTS-based permanecem; ao fim da migração (todas as ondas), as antigas saem e a `Turf` fica
só com a API de tipos próprios.

**Rationale**: caminho de transição limpo, sem inventar uma fachada paralela; o end-state
é `Turf` sobre tipos próprios. Overload resolution distingue por tipo de parâmetro
(`GeoJson.Polygon` vs `NTS.Geometry`).

**Alternativas**: nova classe estática dedicada (rejeitada — duplicaria a fachada);
extension methods nos tipos (rejeitada — menos Turf-like).

## Decisão 2 — Origem do algoritmo (re-tipar o que já é correto)

**Decisão**: para cada função, partir do algoritmo **já fiel ao `@turf`** nas `Turf.*.cs`
atuais (a Fase 2 confirmou que `area` esférica, `distance` haversine, `bearing`,
`rhumb*`, `along/WalkAlong`, `midpoint` batem com o `@turf`) e **re-tipá-lo** sobre os
novos tipos/unidades. Exceção: `centroid` é **corrigido** (Decisão 3). Validar TUDO contra
o `@turf` real mesmo assim (não confiar só na herança).

**Rationale**: reduz risco — porta lógica comprovada, não reescreve do zero.

## Decisão 3 — Conserto do `centroid`

**Decisão**: o `centroid` próprio deve **excluir o vértice de fechamento** de anéis
fechados (Polygon/MultiPolygon), dividindo pela contagem de vértices únicos — batendo com
o `@turf` (`[1,1]` no caso da Fase 2, não `[0.833,0.833]`). `center` (centro da bbox) e
`centerOfMass` seguem os algoritmos do `@turf`.

## Decisão 4 — Ponte NTS

**Decisão**: a categoria measurement é quase toda **own-impl** — provavelmente **nenhuma**
função precisa do `NtsBridge`. `envelope`/`bbox`/`square` são próprios; `pointOnFeature`
no código atual usa `polygon.Centroid` do NTS para um caso — ao portar, usar o algoritmo
do `@turf` (que usa `pointOnFeature` via `center`/`pointOnSurface`); se precisar de
`pointOnSurface`, avaliar a ponte. Registrar caso a caso na implementação.

## Decisão 5 — Unidades nas assinaturas

**Decisão**: usar `Turfano.Units` — `Distance`/`Length` retorno em `Units.Length` (com
`As(LengthUnit)`), `bearing`/rumo em `Units.Angle`, `area` em `Units.Area`. Onde o `@turf`
recebe uma string de unidade, aceitar `LengthUnit`/`AngleUnit`.

## Decisão 6 — Harness de validação

**Decisão**: um harness Bun (`reference/`) que importa `@turf` e emite os valores por
função/fixture; os testes C# comparam com tolerância apertada (`1e-6` relativo ou melhor).
Mesma metodologia das Fases 2–3 (reprodutível, nada presumido).

## Riscos / observações

- `greatCircle` e `polygonTangents` são menos triviais — conferir o algoritmo exato do
  `@turf` em `reference/node_modules/@turf/{great-circle,polygon-tangents}`.
- `centerOfMass` tem algoritmo específico (centro de massa do polígono) — portar do `@turf`.
- Garantir que as novas sobrecargas não criem ambiguidade com as `Turf.*` NTS (tipos de
  parâmetro distintos resolvem; conferir no build).
