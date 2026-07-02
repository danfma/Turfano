# Feature Specification: Leva 2 — Limpeza da superfície legada (rumo à 1.0)

**Feature Branch**: `012-legacy-cleanup`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Com a paridade total na `main` (Ondas A–G, 295 testes,
fachada `Geo` cobre 100% do índice `@turf`), deletar a superfície legada e zerar as
dependências do core — a última etapa antes da 1.0."

## User Scenarios & Testing *(mandatory)*

A biblioteca hoje carrega DUAS APIs: a fachada `Geo` (nova, fiel ao `@turf`, tipos
próprios) e a superfície legada `Turf.*` (72 arquivos sobre NTS/UnitsNet, com os
algoritmos divergentes/ingênuos mapeados na Fase 2). Com a paridade 100% na fachada, o
legado só custa: dependências (NTS, UnitsNet), peso de pacote, e confusão de API dupla.
Esta leva **deleta o legado**, deixa o core com **zero dependências** e fecha os
metadados dos dois pacotes para a 1.0.

### User Story 1 - Core sem legado e sem dependências (Priority: P1)

Quem referencia o pacote `Turfano` recebe SÓ a fachada `Geo` + tipos GeoJSON próprios +
unidades próprias — sem NTS, sem UnitsNet, sem a classe `Turf`.

**Why this priority**: é o objetivo da leva; tudo o mais decorre.

**Independent Test**: build do core sem nenhuma PackageReference externa; grep da classe
legada vazio; AOT smoke 0 warnings.

**Acceptance Scenarios**:

1. **Given** o core pós-leva, **When** inspeciono `Turfano.csproj`, **Then** não há
   PackageReference de NetTopologySuite/NetTopologySuite.Features/UnitsNet.
2. **Given** o código do core, **When** procuro a classe `Turf`, `TerritoryUtils`,
   `GeometryExtensions`, `Angles` ou o `BBox` legado da raiz, **Then** não existem.
3. **Given** o smoke AOT, **When** publico, **Then** 0 warnings IL.

---

### User Story 2 - Consumidores atualizados (Priority: P1)

Testes, playground e benchmark compilam e rodam sobre a fachada `Geo`.

**Why this priority**: sem isso a solution não compila após a deleção.

**Independent Test**: `dotnet build Turfano.slnx` limpo; suíte restante 0 falhas.

**Acceptance Scenarios**:

1. **Given** a suíte pós-leva, **When** rodo os testes, **Then** todos os testes da
   fachada nova (Parity/ + serialização + bridge + fundações) permanecem verdes e nenhum
   teste legado sobra.
2. **Given** `samples/Turfano.Playground` e `benchmark/TimeAndMemoryUsage`, **When**
   compilo, **Then** compilam usando a fachada `Geo` (conteúdo mínimo aceitável).

---

### User Story 3 - Fechamento de pacotes (Priority: P2)

Os dois pacotes ficam prontos para a 1.0: `Turfano` (core, zero dependências) e
`Turfano.NetTopologySuite` (satélite com o Buffer e a bridge).

**Why this priority**: metadados/versão são baratos, mas a publicação em si é decisão do
dono do projeto (fora de escopo).

**Independent Test**: `dotnet pack` dos dois projetos gera pacotes com versão
`1.0.0-rc.1` e metadados coerentes.

**Acceptance Scenarios**:

1. **Given** os csproj, **When** inspeciono, **Then** versão `1.0.0-rc.1` e
   Title/Description refletem o reposicionamento (port fiel do TurfJS, tipos próprios).
2. **Given** o `README.md`, **When** leio, **Then** descreve a fachada `Geo`, o satélite
   e aponta o `NOTICE`.

---

### Edge Cases

- Arquivos do core que PARECEM legados mas servem a fachada (não deletar por engano):
  `GeoJson/`, `Units/`, `Parity/`, `NOTICE`, serialização.
- Testes "mistos" (arquivos que testam a fachada mas vivem fora de `Parity/`): manter os
  da fachada (serialização, bridge, factory), deletar só os que exercitam `Turf.*`.
- O satélite continua referenciando NTS — o grep de NTS deve excluir
  `src/Turfano.NetTopologySuite/`.
- `InternalsVisibleTo` do core para os testes deve permanecer (testes de internals:
  ExactDecimal, SplayTreeSet).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A superfície legada MUST ser removida do core: `Turf.*.cs` (72),
  `TerritoryUtils.cs`, `GeometryExtensions.cs`, `Angles.cs`, o `BBox.cs` legado da raiz e
  qualquer arquivo que exista só para servi-los.
- **FR-002**: `Turfano.csproj` MUST ficar sem PackageReference externa (NTS, 
  NTS.Features, UnitsNet removidas; SourceLink pode ficar) e `GlobalUsings.cs` limpo dos
  usings NTS/UnitsNet.
- **FR-003**: Consumidores MUST ser atualizados: testes legados deletados (mantendo TODOS
  os da fachada), playground e benchmark compilando sobre `Geo` (conteúdo mínimo ok),
  AotSmoke conferido.
- **FR-004**: Pacotes MUST fechar para a 1.0: versão `1.0.0-rc.1` nos dois csproj,
  metadados coerentes, `README.md` atualizado. Publicação NuGet FORA de escopo.
- **FR-005**: Nada da fachada nova pode regredir: suíte restante 0 falhas; AOT 0
  warnings; `NOTICE` intacto.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: grep de `class Turf`/`UnitsNet`/`NetTopologySuite` em `src/Turfano/` =
  **vazio** (satélite excluído do grep).
- **SC-002**: `dotnet build Turfano.slnx` limpo em net8/9/10; suíte restante **0
  falhas**; AOT smoke **0 warnings IL**.
- **SC-003**: `dotnet pack` dos dois projetos gera `1.0.0-rc.1` com metadados novos.
- **SC-004**: Plano-mãe Fase 11 marcada Complete (com Phase Summary), Final Recap
  preenchido.

## Assumptions

- **Decisões fechadas (não re-litigar)**: quebra da API legada é aceitável rumo à 1.0
  (Norte do projeto); `Buffer` permanece só no satélite; NTS/UnitsNet saem do core.
- A versão `1.0.0-rc.1` marca o release candidate; a publicação e o carimbo `1.0.0`
  final são do dono do projeto.
- **Fora de escopo**: publicação NuGet, otimizações de desempenho, CI/CD.
