# Research: Avaliação NTS × TurfJS (Fase 0 — metodologia)

Sem `[NEEDS CLARIFICATION]` no spec. Esta pesquisa fixa as **decisões de metodologia**
para a avaliação ser reprodutível e honesta (lição da Fase 1: não presumir valores).

## Decisão 1 — Harness de ground-truth do TurfJS

**Decisão**: um conjunto **canônico de fixtures** (coordenadas/geometrias fixas) +
um script Bun em `reference/` que importa de `@turf/turf` e emite as saídas para essas
fixtures num JSON. Em paralelo, um pequeno runner C# (ou testes) emite as saídas do
Turfano para as **mesmas** fixtures. A comparação é feita sobre os dois JSONs.

**Rationale**: reprodutível, versionável, e separa "o que o TurfJS faz" de "o que o
Turfano faz" sem acoplar a leitura do código. Evita o erro da Fase 1 (confiar em
comentário/cabeça).

**Alternativas**: portar fixtures dos pacotes `@turf/*/test` (úteis, mas heterogêneas)
— usar como complemento, não como base.

## Decisão 2 — Critério de divergência

**Decisão**: por categoria de retorno:
- **Escalares/medidas** (area, distance, bearing, length): divergência se erro relativo
  `> 1e-6` (acima do ruído de ponto flutuante). Registrar o valor dos dois lados.
- **Booleanos** (todos os `Boolean*`): divergência se o resultado `bool` difere em
  qualquer fixture (sem tolerância).
- **Geometrias** (buffer, union, simplify, etc.): comparar via coordenadas com
  tolerância, ou via área/topologia quando a contagem de vértices diverge legitimamente.

**Rationale**: cada categoria tem uma noção própria de "igual ao Turf".

**Alternativas**: igualdade exata de GeoJSON (frágil — ordem/precisão); rejeitada como
critério único.

## Decisão 3 — Protótipo de tipos próprios para o benchmark

**Decisão**: no `benchmark/`, criar um `readonly record struct` mínimo de posição
(`(double X, double Y)`) e reimplementar **apenas** `Distance`, `Area` e `WalkAlong`
sobre ele (e arrays), comparando com as versões atuais baseadas em NTS
(`Coordinate`/`Geometry`). Marcar com `[MemoryDiagnoser]`.

**Rationale**: mede a hipótese (struct imutável vs classe mutável) sem comprometer o
desenho final dos tipos (Fase 3). É descartável.

**Alternativas**: medir só alocação via contadores manuais — menos confiável que
BenchmarkDotNet.

## Decisão 4 — Identificação do algoritmo do TurfJS por função

**Decisão**: para cada operação pesada, ler `reference/node_modules/@turf/<fn>/` (código
`dist` + `package.json` `dependencies`) para nomear a lib/algoritmo real (ex.:
`@turf/union` → `polygon-clipping`/`polyclip-ts`; `@turf/voronoi` → `d3-voronoi`;
`@turf/buffer` → `@turf/jsts`/`turf-jsts`). Registrar a dependência exata e estimar o
custo de porte (P/M/G).

**Rationale**: a decisão "portar vs NTS-interino" depende de **o que** seria portado.

**Alternativas**: inferir pelo nome — rejeitada (impreciso).

## Decisão 5 — Escopo da catalogação

**Decisão**: catalogar todas as funções `public`/`internal` de `Turf` (inventário já
levantado na sessão de priming — ~77 arquivos `Turf.*.cs`), classificando em
wrapper-NTS / própria / ingênua, com base na leitura já feita e confirmando casos-limite.

**Rationale**: SC-001 exige cobertura de 100%.

## Riscos / observações

- Algumas funções `internal` (Isolines/Isobands/Voronoi e as sobrecargas de
  FeatureCollection) não estão expostas — catalogar mesmo assim e anotar a visibilidade.
- Microbenchmark: declarar warmup/JIT/tamanho de entrada; números são ordem de grandeza.
- O harness Bun e o runner C# de comparação são **efêmeros**; ao final, ou são removidos
  ou ficam claramente marcados como fixtures de avaliação (não produção).
