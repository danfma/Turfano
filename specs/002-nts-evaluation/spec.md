# Feature Specification: Avaliação NTS × TurfJS + benchmark (decisão da Fase 2)

**Feature Branch**: `002-nts-evaluation`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "Avaliação NTS × TurfJS + benchmark (spike/decisão — Fase 2 do plano plans/turfjs-parity-redesign.md). Feature de PESQUISA: não escreve código de produção; produz um documento de decisão (docs/nts-evaluation.md) e protótipos de benchmark descartáveis."

## User Scenarios & Testing *(mandatory)*

Feature de **pesquisa/decisão** (spike). O "usuário" é o time/decisor do redesign, que
precisa escolher — **operação por operação** — entre portar o algoritmo do TurfJS,
manter o NetTopologySuite (NTS) interinamente, ou aproximar, para destravar a Fase 3
(sistema de tipos) e as ondas de paridade sem re-litigar depois. Entregáveis:
`docs/nts-evaluation.md` + um protótipo de benchmark.

### User Story 1 - Matriz de decisão op-a-op confiável (Priority: P1)

O decisor abre `docs/nts-evaluation.md` e encontra, para cada operação pesada/ingênua,
uma decisão registrada (**portar** / **NTS-interino** / **aproximar**) com justificativa,
incluindo qual lib/algoritmo o TurfJS usa e uma estimativa de custo de porte para C#.

**Why this priority**: é o artefato que destrava todas as fases seguintes; sem ele, a
remoção do NTS vira chute. É o objetivo central da fase.

**Independent Test**: abrir o documento e verificar que cada operação da lista pesada/
ingênua tem decisão + justificativa + custo estimado.

**Acceptance Scenarios**:

1. **Given** a lista de operações pesadas/ingênuas (union, difference, intersect,
   dissolve, buffer, convex, simplify, bboxClip, tin, voronoi, concave, tesselate,
   isobands, isolines, bezierSpline), **When** consulto a matriz, **Then** cada uma tem
   exatamente uma decisão em {portar, NTS-interino, aproximar} com justificativa.
2. **Given** uma operação marcada como "portar", **When** leio sua linha, **Then** ela
   nomeia a lib/algoritmo equivalente do TurfJS e uma estimativa de custo (ex.: P/M/G).

---

### User Story 2 - Catálogo de divergências validado contra o TurfJS real (Priority: P1)

O decisor encontra uma tabela `função → (classificação, divergência vs TurfJS)` cobrindo
**todas** as funções atuais da classe `Turf`, onde cada divergência afirmada para um
wrapper-NTS é comprovada por uma comparação numérica entre a saída do Turfano e a do
TurfJS real (`reference/`).

**Why this priority**: a matriz da US1 só é confiável se as divergências forem medidas,
não presumidas. (Já vimos nesta sessão como um "valor de referência" presumido pode
estar errado.)

**Independent Test**: a tabela cobre 100% das funções atuais; cada divergência de
wrapper-NTS traz o par de valores (Turfano vs TurfJS) que a evidencia.

**Acceptance Scenarios**:

1. **Given** o conjunto de funções públicas/internal de `Turf`, **When** consulto a
   tabela, **Then** cada função aparece classificada como wrapper-NTS / própria / ingênua.
2. **Given** uma função wrapper-NTS com divergência afirmada (ex.: `Area` planar vs
   esférica, `Centroid` área vs média, `BooleanPointInPolygon` fronteira), **When** leio
   sua linha, **Then** há um par de saídas (Turfano vs `@turf`) que comprova a divergência.

---

### User Story 3 - Evidência quantitativa de performance (Priority: P2)

O decisor vê números (tempo e alocação) comparando **tipos de valor próprios** (um
`readonly record struct` de posição + geometria imutável mínima) contra os tipos do NTS
(mutáveis) nas rotas quentes, para decidir se a troca de tipos compensa.

**Why this priority**: "performance" é um dos motivadores declarados; precisa de ordem de
grandeza real, não suposição (o README sugere ~25% de tempo e ~40% de memória).

**Independent Test**: rodar o benchmark e obter uma tabela comparativa próprios vs NTS.

**Acceptance Scenarios**:

1. **Given** o projeto de benchmark, **When** rodo `dotnet run -c Release --project
   benchmark/TimeAndMemoryUsage`, **Then** obtenho tempo e bytes alocados para tipos
   próprios vs NTS em ao menos `Distance`, `Area` e `WalkAlong`.

---

### User Story 4 - Inventário de UnitsNet (Priority: P3)

O decisor encontra a lista fechada dos tipos do UnitsNet realmente usados no código
(esperado: só `Length`, `Angle`, `Area`), para dimensionar os structs de unidade
próprios da Fase 3.

**Why this priority**: insumo direto da Fase 3; barato de produzir; baixo risco.

**Independent Test**: a lista é fechada e cada tipo aponta onde é usado.

**Acceptance Scenarios**:

1. **Given** o código-fonte, **When** consulto o inventário, **Then** vejo cada tipo
   UnitsNet usado e os arquivos/funções que o consomem.

---

### Edge Cases

- Funções híbridas (parte NTS, parte própria) — classificar pela parte dominante e anotar.
- Operações em que o próprio TurfJS delega a uma lib sem equivalente direto em C#
  (decidir entre portar a lib, NTS-interino ou aproximar — explicitar o trade-off).
- "Divergências" dentro da tolerância de ponto flutuante — não contam como divergência;
  registrar o limiar usado.
- Ressalvas de microbenchmark (warmup/JIT/tamanho de entrada) — declarar que os números
  são indicativos de ordem de grandeza, não prova de produção.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A avaliação MUST catalogar 100% das funções atuais da classe `Turf`,
  classificando cada uma como wrapper-NTS, implementação-própria ou algoritmo-ingênuo.
- **FR-002**: Para cada função wrapper-NTS, a avaliação MUST indicar se há divergência vs
  TurfJS e, havendo, validá-la comparando a saída do Turfano com a do TurfJS real
  (`reference/`), anexando o par de valores.
- **FR-003**: A avaliação MUST produzir uma matriz de decisão op-a-op para as operações
  pesadas/ingênuas, com uma decisão em {portar, NTS-interino, aproximar}, justificativa,
  a lib/algoritmo equivalente do TurfJS e uma estimativa de custo de porte.
- **FR-004**: A avaliação MUST quantificar o ganho (ou ausência dele) de tipos de valor
  próprios vs NTS, com benchmark de tempo e alocação em ao menos `Distance`, `Area` e
  `WalkAlong`.
- **FR-005**: A avaliação MUST inventariar os tipos do UnitsNet efetivamente usados no
  código, com os pontos de uso.
- **FR-006**: A avaliação MUST registrar uma recomendação final por operação
  (manter/remover NTS) e uma conclusão sobre a adoção de tipos próprios.
- **FR-007**: A feature MUST NÃO alterar código de produção em `src/` nem remover
  dependências; protótipos de benchmark são descartáveis e isolados.
- **FR-008**: Toda alegação de divergência ou de ganho MUST ser reproduzível — o
  comando/script que a gera fica registrado no documento.

### Key Entities

- **Linha de classificação**: função → { classificação (wrapper-NTS/própria/ingênua),
  divergência vs TurfJS (sim/não + evidência) }.
- **Linha da matriz de decisão**: operação → { decisão (portar/NTS-interino/aproximar),
  lib/algoritmo TurfJS, magnitude da divergência, custo de porte estimado, justificativa }.
- **Linha de benchmark**: rota (Distance/Area/WalkAlong) → { tempo próprios, tempo NTS,
  alocação próprios, alocação NTS }.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `docs/nts-evaluation.md` existe e a tabela de classificação cobre **100%**
  das funções atuais da classe `Turf`.
- **SC-002**: **100%** das operações pesadas/ingênuas listadas têm decisão + justificativa
  na matriz.
- **SC-003**: Cada divergência afirmada para wrapper-NTS traz um par de valores anexado
  (Turfano vs TurfJS).
- **SC-004**: O benchmark roda via `dotnet run -c Release --project
  benchmark/TimeAndMemoryUsage` e emite tabela com tempo e bytes alocados para próprios
  vs NTS em `Distance`, `Area` e `WalkAlong`.
- **SC-005**: O inventário de UnitsNet é uma lista fechada; cada tipo aponta onde é usado.
- **SC-006**: Zero alterações em `src/` de produção; a suíte de testes permanece verde
  (156, 0 falhas).

## Assumptions

- O TurfJS real (via `@turf` em `reference/`, com `bun`) é a fonte de verdade para
  divergências; ignoram-se corner cases onde o JS não representa números como o C#.
- Os limiares de "divergência significativa" são julgamento **documentado** por operação;
  a decisão final pode ser revisitada na Fase 3 se novos dados surgirem.
- O benchmark é microbenchmark indicativo (BenchmarkDotNet/`MemoryDiagnoser`), para ordem
  de grandeza — não prova de produção.
- Os protótipos de tipos próprios no benchmark são mínimos e **descartáveis**; não são os
  tipos definitivos da Fase 3.
- **Fora de escopo**: implementar os tipos definitivos, remover o NTS de fato, ou portar
  qualquer função (Fase 3+). A stack atual permanece (NTS/UnitsNet/TUnit/net8-9-10).
