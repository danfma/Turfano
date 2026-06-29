# Feature Specification: Correção de bugs do Turfano (manutenção)

**Feature Branch**: `001-fix-current-bugs`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "Correção de bugs do Turfano (entrega de manutenção independente, sobre o código atual ainda baseado em NetTopologySuite/UnitsNet — NÃO é o redesign). Escopo fechado, derivado da Fase 1 do plano plans/turfjs-parity-redesign.md."

## User Scenarios & Testing *(mandatory)*

Esta é uma entrega de **manutenção** sobre o código atual (ainda baseado em
NetTopologySuite/UnitsNet). O "usuário" é o desenvolvedor que consome a biblioteca
Turfano e espera que os resultados batam com o TurfJS. Cada história abaixo é
independentemente testável e entrega valor sozinha.

### User Story 1 - Rumo (rhumb bearing) correto em todas as direções (Priority: P1)

Quem calcula um rumo de linha de rumo (`RhumbBearing`) entre dois pontos espera um
valor numericamente igual ao do TurfJS, em qualquer direção — inclusive rumos que
passam dos 180° e que cruzam o antimeridiano. Hoje uma constante compartilhada de
"volta completa" está definida como π em vez de 2π, corrompendo a normalização do
rumo (`deltaLambda` e o cálculo de `bear180`) para essas direções.

**Why this priority**: É um resultado numérico silenciosamente errado numa função
pública sem nenhum teste — o pior tipo de bug. Quebra diretamente o objetivo do
projeto (fidelidade ao TurfJS).

**Independent Test**: Calcular `RhumbBearing` para pares de pontos com rumos
conhecidos do TurfJS (incluindo > 180° e cruzando o antimeridiano) e comparar.

**Acceptance Scenarios**:

1. **Given** `from = (-75.343, 39.984)` e `to = (-75.534, 39.123)`, **When** chamo
   `RhumbBearing(from, to)`, **Then** o resultado é ≈ `9.71°` (tolerância `0.01°`),
   igual ao TurfJS.
2. **Given** um par de pontos cujo rumo verdadeiro é maior que 180° (ex.: apontando
   a oeste/sudoeste), **When** chamo `RhumbBearing`, **Then** o valor retornado bate
   com o do TurfJS no intervalo `-180°..180°` (sinal e magnitude corretos).
3. **Given** `from = (179, 0)` e `to = (-179, 0)` (cruzando o antimeridiano),
   **When** chamo `RhumbBearing`, **Then** o rumo corresponde ao caminho curto
   (≈ leste), igual ao TurfJS — e não ao caminho longo ao redor do globo.

---

### User Story 2 - Escala uniforme correta no eixo Y (Priority: P1)

Quem escala uma geometria com `TransformScale(geom, fator)` sem informar um fator
específico de Y espera escala **uniforme**: X e Y multiplicados pelo mesmo fator.
Hoje, no caso padrão (sem `FactorY`), o eixo Y colapsa para uma constante por causa
de um erro de precedência de operador, deformando a geometria.

**Why this priority**: Resultado visivelmente errado numa função pública sem nenhum
teste; afeta qualquer uso padrão de `TransformScale`.

**Independent Test**: Escalar uma geometria conhecida por um fator (ex.: 2) sem
`FactorY` e verificar que a extensão (bbox) dobra em X **e** em Y.

**Acceptance Scenarios**:

1. **Given** um polígono centrado na origem e `fator = 2` sem `FactorY`, **When**
   chamo `TransformScale`, **Then** a largura e a altura do resultado são o dobro das
   originais (escala uniforme), e o resultado bate com o TurfJS.
2. **Given** a mesma chamada rodada **no código anterior ao patch**, **Then** o teste
   do cenário 1 FALHA (demonstrando o bug); **e** rodada no código corrigido, PASSA.
3. **Given** `FactorY`, `FactorZ` e/ou `Origin` informados explicitamente, **When**
   chamo `TransformScale`, **Then** cada eixo é escalado pelo seu fator a partir da
   origem indicada, igual ao TurfJS.

---

### User Story 3 - Ângulo explementar correto (Priority: P2)

Quem chama `GetAngle(..., Explementary: true)` espera receber `360° − ângulo`. Esse
caminho depende da mesma constante de "volta completa" (2π) que hoje está errada.

**Why this priority**: Mesma causa-raiz da US1 (constante 2π), porém num caminho
menos central e atualmente sem teste.

**Independent Test**: Calcular `GetAngle` para três pontos com ângulo conhecido, com
e sem `Explementary`, e verificar que `Explementary` retorna o suplemento a 360°.

**Acceptance Scenarios**:

1. **Given** três pontos cujo ângulo é `θ`, **When** chamo `GetAngle` com
   `Explementary: true`, **Then** o resultado é `360° − θ` (igual ao TurfJS).

---

### Edge Cases

- `RhumbBearing` exatamente em rumos de `180°`/`-180°` e nos polos.
- `RhumbBearing` com `from == to` (distância zero) — não deve lançar nem produzir NaN.
- `TransformScale` com `fator < 1` (encolher) e com `Origin` explícito fora do centro.
- `GetAngle` quando o ângulo é `0°`/`360°` (o explementar não deve "estourar" o
  intervalo).
- Garantir que nenhuma outra função que usa `Angles.Pi`/`Angles.TwoPi` passe a
  divergir após a correção da constante.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `RhumbBearing` MUST retornar valores numericamente equivalentes aos do
  TurfJS em todo o intervalo de rumos, incluindo rumos maiores que 180° e o
  cruzamento do antimeridiano.
- **FR-002**: A constante compartilhada que representa uma volta completa (2π) MUST
  valer 2π (hoje vale π, igual à constante de meia-volta), e todos os seus
  consumidores (`RhumbBearing`, caminho `Explementary` de `GetAngle`) MUST produzir a
  matemática de wraparound correta.
- **FR-003**: `TransformScale` MUST escalar o eixo Y pelo fator informado no caso
  padrão (sem fator de Y específico), resultando em escala uniforme em X e Y.
- **FR-004**: `TransformScale` MUST respeitar fatores por eixo (`FactorY`/`FactorZ`) e
  o ponto de origem (`Origin`) quando informados.
- **FR-005**: O caminho `Explementary` de `GetAngle` MUST retornar `360° − ângulo`
  corretamente.
- **FR-006**: O projeto MUST passar a incluir testes automatizados de regressão,
  ancorados em valores de referência do TurfJS, para `RhumbBearing`, `TransformScale`
  e o caminho `Explementary` de `GetAngle` (hoje todos sem nenhum teste).
- **FR-007**: A correção MUST NÃO alterar a superfície de API pública, as dependências
  (NetTopologySuite, UnitsNet, TUnit) nem os frameworks-alvo (`net8.0;net9.0;net10.0`):
  é correção de comportamento, não redesign.
- **FR-008**: Nenhuma outra função pode regredir — a suíte de testes completa MUST
  permanecer verde após as correções.
- **FR-009**: A varredura por outros usos do padrão de precedência `a * x.Nullable ?? b`
  no projeto MUST ser feita; qualquer ocorrência equivalente encontrada MUST ser
  corrigida ou registrada como fora de escopo com justificativa.

### Key Entities

Não se aplica — esta feature não introduz nem altera entidades de dados; corrige
comportamento de funções existentes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `RhumbBearing(from=(-75.343,39.984), to=(-75.534,39.123))` retorna
  `9.71°` com tolerância de `0.01°` (valor de referência do TurfJS).
- **SC-002**: Escalar uma geometria por fator `2` sem `FactorY` dobra a extensão em
  **ambos** os eixos (X e Y); o eixo Y deixa de colapsar.
- **SC-003**: 100% das funções antes sem cobertura (`RhumbBearing`, `TransformScale`,
  caminho `Explementary` de `GetAngle`) passam a ter ao menos um teste de regressão
  ancorado em valor de referência do TurfJS.
- **SC-004**: A suíte de testes automatizada completa passa com 0 falhas (incluindo os
  novos testes) e o build produz 0 erros em todos os frameworks-alvo.
- **SC-005**: O teste do caso padrão de `TransformScale` FALHA no código anterior ao
  patch e PASSA no código corrigido (demonstra o bug e a correção — diff de
  comportamento).

## Assumptions

- Os valores de referência do TurfJS (extraídos do `@turf` em `reference/`) são a
  fonte de verdade para as asserções numéricas; corner cases onde o JS não representa
  números como o C# são ignorados de propósito.
- A stack atual permanece inalterada: TUnit, NetTopologySuite, UnitsNet,
  multi-targeting `net8.0;net9.0;net10.0`.
- Está **fora de escopo**: qualquer parte do redesign (novos tipos GeoJSON, remoção do
  NTS, unidades próprias, serialização STJ) e os algoritmos comprovadamente ingênuos
  (`Tin`/`Voronoi`/`Concave`/`Tesselate`/`Isobands`), que pertencem a fases posteriores
  do plano `plans/turfjs-parity-redesign.md`.
- A limpeza dos comentários obsoletos `// filepath: /Users/danfma/Develop/private/...`
  é desejável, porém **opcional**: não é critério de sucesso desta feature.
