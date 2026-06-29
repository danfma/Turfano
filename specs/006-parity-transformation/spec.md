# Feature Specification: Onda C — Transformation & Coordinate Mutation (paridade)

**Feature Branch**: `006-parity-transformation`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "Onda C — Transformation & Coordinate Mutation (Fase 6). Portar as funções de transformação/mutação do TurfJS para a fachada `Geo` (sobre `Turfano.GeoJson`), fiéis ao `@turf`. `transformScale` deve ser GEODÉSICO como o `@turf`, não cartesiano."

## User Scenarios & Testing *(mandatory)*

Terceira **onda de paridade**: as funções que **transformam** geometrias (rotacionar,
transladar, escalar, suavizar, deslocar) e **mutam coordenadas** (limpar, inverter,
arredondar, truncar) passam a existir na fachada `Geo` (sobre `Turfano.GeoJson`), produzindo
as **mesmas saídas do TurfJS**. As Ondas A (measurement) e B (booleans) já estão na `main`,
na fachada `Geo`; reusar os helpers (`Distance`, `Bearing`, `Destination`,
`RhumbDistance`/`RhumbDestination`, `BooleanClockwise`, ...).

### User Story 1 - Mutação de coordenadas (Priority: P1)

Quem usa `cleanCoords`, `flip`, `rewind`, `round`, `truncate` obtém a mesma geometria
mutada do TurfJS.

**Why this priority**: são as mais simples e base de outras (ex.: `cleanCoords` é usado por
`booleanParallel`); entregam valor imediato e baixo risco.

**Independent Test**: cada função sobre fixtures, comparação estrutural com o `@turf`.

**Acceptance Scenarios**:

1. **Given** uma linha com pontos duplicados consecutivos, **When** chamo `CleanCoords`,
   **Then** os duplicados somem, como no `turf.cleanCoords`.
2. **Given** um ponto `[1, 2]`, **When** chamo `Flip`, **Then** retorno `[2, 1]`.
3. **Given** coordenadas com muitas casas, **When** chamo `Truncate(precision: 6)`,
   **Then** batem com `turf.truncate`.
4. **Given** um polígono com anel externo horário, **When** chamo `Rewind`, **Then** o anel
   externo fica anti-horário (RFC 7946), como o `turf.rewind`.

---

### User Story 2 - Transformação geométrica (Priority: P1)

Quem usa `transformRotate`, `transformTranslate`, `transformScale`, `clone` obtém a mesma
geometria transformada do TurfJS — em especial `transformScale` **geodésico**.

**Why this priority**: a Fase 2 marcou `transformScale` como divergente (cartesiano vs
geodésico do Turf); é a correção-chave desta onda.

**Independent Test**: comparar com o `@turf` real; `transformScale` prova a semântica
geodésica.

**Acceptance Scenarios**:

1. **Given** um polígono e um fator 2, **When** chamo `TransformScale`, **Then** a saída bate
   com `turf.transformScale` (escala **geodésica** a partir da origem; X e Y corretos, sem
   colapsar — a divergência da Fase 1/2).
2. **Given** uma geometria, distância e rumo, **When** chamo `TransformTranslate`, **Then**
   bate com `turf.transformTranslate`.
3. **Given** uma geometria e um ângulo, **When** chamo `TransformRotate`, **Then** bate com
   `turf.transformRotate`.

---

### User Story 3 - Geração e suavização (Priority: P2)

Quem usa `circle`, `bezierSpline`, `polygonSmooth`, `lineOffset`, `simplify` obtém a mesma
geometria do TurfJS.

**Why this priority**: úteis, porém mais algorítmicas/pesadas; `simplify` depende da decisão
op-a-op da Fase 2.

**Independent Test**: comparar com o `@turf` real por fixture.

**Acceptance Scenarios**:

1. **Given** um centro e um raio, **When** chamo `Circle`, **Then** o polígono de N passos
   bate com `turf.circle`.
2. **Given** uma linha, **When** chamo `Simplify(tolerance)`, **Then** bate com
   `turf.simplify` (Douglas-Peucker; conferir divergência com `simplify-js`).

---

### Edge Cases

- `cleanCoords` em geometria com 2 pontos / degenerada (não remover abaixo do mínimo válido).
- `rewind` com `reverse: true`; anéis internos (furos) com orientação oposta.
- `truncate`/`round` com `coordinates`/`precision` extremos e altitude (Z).
- `transformScale` com `origin` diferente (centroid, sw, etc.) e fator < 1.
- `circle` com `steps`/`units` variados; `simplify` com `highQuality`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As funções listadas (mutação: `cleanCoords`/`flip`/`rewind`/`round`/`truncate`;
  transform: `transformRotate`/`transformTranslate`/`transformScale`/`clone`; geração:
  `circle`/`bezierSpline`/`polygonSmooth`/`lineOffset`/`simplify`) MUST existir na fachada
  `Geo` (partials em `Turfano.GeoJson`), recebendo/retornando `Turfano.GeoJson` e usando
  `Turfano.Units` onde aplicável; opções como parâmetros.
- **FR-002**: Cada função MUST bater com o `@turf` real (validado via `reference/` com bun) —
  tolerância apertada nas numéricas, comparação estrutural nas que devolvem geometria. Quando
  o ground-truth surpreender, **seguir o `@turf`**, não a suposição.
- **FR-003**: `transformScale` MUST seguir a **semântica geodésica** do `@turf` (escala via
  rumo/distância a partir da origem), **não** a cartesiana do código NTS (divergência da
  Fase 1/2).
- **FR-004**: As funções vivem na fachada `Geo`; as `Turf.*.cs` NTS-based permanecem
  intactas; a suíte existente (203) permanece verde; nenhuma reflexão introduzida (AOT-safe);
  multi-target `net8.0;net9.0;net10.0`.
- **FR-005**: Nomes seguem a convenção .NET (sem acrônimos crípticos; nomes de domínio bem
  conhecidos permitidos) — `CLAUDE.md` ## Conventions, `tasks/lessons.md`.

### Key Entities

Sem entidades novas — a onda adiciona **funções** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) na fachada `Geo`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das funções de transformation/mutation listadas têm teste que bate com o
  `@turf` real (numérico apertado / estrutural).
- **SC-002**: `TransformScale` bate com `turf.transformScale` (escala **geodésica**; X e Y
  corretos, não colapsa) — prova da correção da divergência da Fase 1/2.
- **SC-003**: A superfície opera sobre `Turfano.GeoJson` na fachada `Geo` (sem
  `NetTopologySuite`/`UnitsNet` nas assinaturas públicas das novas funções).
- **SC-004**: Build limpo em `net8.0;net9.0;net10.0`; a suíte existente permanece **verde
  (203, 0 falhas)** + os novos testes.
- **SC-005**: O smoke de AOT continua **0 warnings IL**.

## Assumptions

- As novas funções vivem na **fachada `Geo`** (decisão consolidada nas Ondas A/B).
- O `@turf` real (via `reference/`, bun) é a fonte de verdade.
- `simplify` segue a decisão op-a-op da Fase 2 (`docs/nts-evaluation.md`): Douglas-Peucker,
  conferindo a divergência com o `simplify-js` do `@turf`.
- NTS/UnitsNet **permanecem** (motor interino); a onda só adiciona.
- **Fora de escopo**: as demais ondas (overlay/clipping, interpolation/grids, features/meta,
  restantes), remover o NTS/UnitsNet, e portar funções fora de transformation/mutation.
