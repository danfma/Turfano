# Data Model: Avaliação NTS × TurfJS (Fase 1)

Sem entidades de software. As "entidades" são os **registros do documento entregável**
`docs/nts-evaluation.md` — o esquema das linhas das tabelas. Formalizadas aqui para que
o entregável seja inequívoco.

## Linha de Classificação (Seção 1 do doc)

| Campo | Tipo | Regra |
|---|---|---|
| `function` | string | nome da função de `Turf` (ex.: `Area`, `BooleanPointInPolygon`) |
| `visibility` | enum | `public` \| `internal` |
| `classification` | enum | `nts-wrapper` \| `own` \| `naive` |
| `divergesFromTurf` | enum | `yes` \| `no` \| `n/a` |
| `evidenceRef` | string? | link/âncora para o par de valores na Seção 2 (obrigatório se `yes`) |

Regra: cobre **100%** das funções atuais (SC-001).

## Linha de Divergência validada (Seção 2 do doc)

| Campo | Tipo | Regra |
|---|---|---|
| `function` | string | função wrapper-NTS avaliada |
| `fixture` | string | a fixture de entrada usada |
| `turfOutput` | valor | saída do `@turf` real (via `reference/`) |
| `turfanoOutput` | valor | saída do Turfano para a mesma fixture |
| `category` | enum | `scalar` \| `boolean` \| `geometry` |
| `diverges` | bool | conforme o critério da `research.md` Decisão 2 |

Regra: toda divergência afirmada na Seção 1 tem ≥1 linha aqui (SC-003).

## Linha da Matriz de decisão (Seção 3 do doc)

| Campo | Tipo | Regra |
|---|---|---|
| `operation` | string | operação pesada/ingênua |
| `turfAlgorithm` | string | lib/algoritmo do TurfJS (ex.: `polygon-clipping`) |
| `divergenceMagnitude` | enum | `none` \| `small` \| `large` |
| `portCost` | enum | `P` \| `M` \| `G` |
| `decision` | enum | `portar` \| `nts-interino` \| `aproximar` |
| `rationale` | string | justificativa |

Regra: 100% das ops pesadas/ingênuas têm uma linha com `decision` preenchida (SC-002).

## Linha de Benchmark (Seção 4 do doc)

| Campo | Tipo | Regra |
|---|---|---|
| `route` | enum | `Distance` \| `Area` \| `WalkAlong` |
| `timeOwn` / `timeNts` | duração | de BenchmarkDotNet |
| `allocOwn` / `allocNts` | bytes | de `MemoryDiagnoser` |

Regra: as 3 rotas presentes com números próprios vs NTS (SC-004).

## Linha de Inventário UnitsNet (Seção 5 do doc)

| Campo | Tipo | Regra |
|---|---|---|
| `unitsNetType` | string | ex.: `Length`, `Angle`, `Area` |
| `usedIn` | string[] | arquivos/funções que o consomem |

Regra: lista fechada (SC-005).
