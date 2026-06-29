# Feature Specification: Sistema de tipos central GeoJSON + unidades próprias + STJ source-gen

**Feature Branch**: `003-geojson-types`

**Created**: 2026-06-29

**Status**: Draft

**Input**: User description: "Sistema de tipos central GeoJSON + unidades próprias + serialização System.Text.Json source-generated (Fase 3 do plano). FUNDAÇÃO do redesign; não porta as ~70 funções nem remove o NTS (interino, conforme docs/nts-evaluation.md)."

## User Scenarios & Testing *(mandatory)*

Feature de **fundação**: introduz os tipos próprios sobre os quais as ondas de paridade
(Fases 4+) vão assentar. O "usuário" é o desenvolvedor consumidor da biblioteca (que
quer GeoJSON nativo, performático e serializável) e o próprio time (que precisa da base
para portar as funções). Esta fase **não** porta as funções nem remove o NTS/UnitsNet
existentes — eles seguem como motor interino, e a suíte atual continua passando.

### User Story 1 - GeoJSON próprio com round-trip fiel ao RFC 7946 (Priority: P1)

Quem (de)serializa GeoJSON com System.Text.Json obtém tipos próprios cujo round-trip
(desserializar → reserializar) produz a **mesma forma** do TurfJS/RFC 7946 (`type`,
`coordinates`, `properties`, `bbox`, `id`), sem precisar do NetTopologySuite nem de
conversores externos.

**Why this priority**: serialização GeoJSON nativa é o motivador central declarado e a
base de tudo; sem os tipos não há onde portar as funções.

**Independent Test**: desserializar fixtures GeoJSON canônicas (do `@turf`) para os novos
tipos e reserializar; a saída casa com a do TurfJS.

**Acceptance Scenarios**:

1. **Given** um `Feature` GeoJSON (Point/LineString/Polygon/Multi*/GeometryCollection)
   com `properties` e `bbox`, **When** desserializo e reserializo, **Then** o JSON
   resultante tem o mesmo `type`, `coordinates`, `properties` e `bbox` (RFC 7946).
2. **Given** uma `FeatureCollection`, **When** round-trip, **Then** a coleção e cada
   feature preservam forma e ordem.
3. **Given** uma `Position` com altitude (`[lon, lat, alt]`) e uma sem (`[lon, lat]`),
   **When** round-trip, **Then** a dimensão é preservada.

---

### User Story 2 - Unidades próprias batendo com o TurfJS (Priority: P1)

Quem usa medidas (distância, ângulo/rumo, área) trabalha com 3 structs de valor próprios
(substituindo o UnitsNet), com `enum` de unidades alinhado ao TurfJS, e cujas conversões
batem numericamente com o `@turf`.

**Why this priority**: a avaliação (Fase 2) confirmou que só 3 quantidades são usadas;
são pré-requisito para portar as funções de medição com fidelidade.

**Independent Test**: rodar as conversões e comparar com o `@turf` (via `reference/`).

**Acceptance Scenarios**:

1. **Given** um comprimento em km, **When** converto para milhas/metros/graus/radianos,
   **Then** os valores batem com `convertLength`/`lengthToRadians`/`radiansToLength` do
   `@turf`.
2. **Given** um rumo, **When** aplico `bearingToAzimuth`, **Then** o resultado bate com o
   `@turf`.
3. **Given** uma área, **When** converto entre unidades, **Then** bate com `convertArea`.

---

### User Story 3 - Serialização AOT/trimming-safe (Priority: P2)

Quem publica com Native AOT ou trimming consegue (de)serializar os tipos do Turfano sem
warnings de reflexão/trimming, porque a serialização é source-generated.

**Why this priority**: performance e AOT são motivadores declarados; uma serialização
reflexiva inviabilizaria o objetivo.

**Independent Test**: publicar um app de teste com AOT/trimming e confirmar build sem
warnings ligados aos tipos do Turfano.

**Acceptance Scenarios**:

1. **Given** um app que usa os tipos do Turfano, **When** publico com AOT (ou trimming),
   **Then** o build conclui sem warnings de trimming/reflexão nos tipos do Turfano.

---

### User Story 4 - Construção estilo Turf + ponte interna com o NTS (Priority: P2)

Quem constrói geometrias usa helpers ao estilo Turf (`point()`, `lineString()`, …); e,
internamente, os novos tipos convertem de/para NTS para que as funções ainda baseadas em
NTS operem sobre eles durante a transição das ondas de paridade.

**Why this priority**: os helpers reduzem o atrito de portar código JS; a ponte interna é
o que viabiliza a migração incremental mantendo o NTS interino.

**Independent Test**: construir geometrias via helpers; converter um novo-tipo para NTS e
de volta preservando a forma.

**Acceptance Scenarios**:

1. **Given** `polygon(...)`/`featureCollection(...)`, **When** construo, **Then** obtenho
   os tipos corretos com `getCoord(s)`/`getType`/`getGeom` funcionando.
2. **Given** um novo-tipo `Polygon`, **When** converto para NTS e de volta (ponte
   interna), **Then** as coordenadas são preservadas.

---

### Edge Cases

- `Position` 2D vs 3D (altitude) — preservar dimensão no round-trip; não inventar `alt`.
- Geometrias/coleções vazias (RFC 7946 permite `coordinates: []`).
- `Feature` sem `properties` (`null`) e sem `bbox`.
- `GeometryCollection` aninhada.
- `id` de Feature pode ser string **ou** número (RFC 7946).
- Anéis de `Polygon`: o RFC 7946 recomenda mão direita; definir se normalizamos na
  (de)serialização ou apenas preservamos.
- Precisão: ignorar corner cases onde o JS não representa números como o C# (decisão do
  plano-mãe).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Os tipos MUST modelar GeoJSON RFC 7946: `Position` (2D/3D), `BBox`,
  geometrias (`Point`, `MultiPoint`, `LineString`, `MultiLineString`, `Polygon`,
  `MultiPolygon`, `GeometryCollection`), `Feature` e `FeatureCollection`.
- **FR-002**: O modelo MUST ser híbrido: `Position`/`BBox` como **structs de valor
  imutáveis** (caminhos quentes sem alocação); geometrias/Feature como **`record`
  selado** numa hierarquia com discriminador.
- **FR-003**: A (de)serialização MUST usar System.Text.Json com discriminador GeoJSON
  `type` (polimorfismo) e `Position`/`BBox` serializando como arrays JSON
  (`[lon, lat]`/`[lon, lat, alt]`).
- **FR-004**: A (de)serialização MUST ser **source-generated** (sem reflexão),
  compatível com Native AOT/trimming.
- **FR-005**: O round-trip MUST preservar `type`, `coordinates`, `properties`, `bbox` e
  `id`, conforme RFC 7946, casando com a forma do TurfJS.
- **FR-006**: As unidades MUST ser 3 structs de valor próprios (comprimento/distância,
  ângulo/rumo, área) com `enum` de unidades alinhado ao TurfJS, operadores e conversões
  `From*/As*`.
- **FR-007**: As conversões de unidade MUST bater numericamente com o `@turf`
  (`convertLength`, `convertArea`, `lengthToRadians`, `radiansToLength`,
  `lengthToDegrees`, `degreesToRadians`, `radiansToDegrees`, `bearingToAzimuth`),
  validado via `reference/`.
- **FR-008**: A biblioteca MUST expor helpers/factory ao estilo Turf (`point`,
  `lineString`, `polygon`, `multiPoint`, `multiLineString`, `multiPolygon`,
  `geometryCollection`, `feature`, `featureCollection`) e acesso/invariantes
  (`getCoord(s)`, `getType`, `getGeom`).
- **FR-009**: MUST haver conversores **internos** (não públicos) entre os novos tipos e o
  NTS, para que as funções ainda baseadas em NTS operem sobre os novos tipos na transição.
- **FR-010**: Esta fase MUST NÃO portar as funções existentes nem remover o
  NTS/UnitsNet; a suíte atual (156) MUST permanecer verde e o multi-targeting
  (`net8.0;net9.0;net10.0`) mantido.

### Key Entities

- **Position**: struct de valor `(Lon, Lat, Alt?)`; serializa como array.
- **BBox**: struct de valor (2D/3D); serializa como array.
- **Geometry** (hierarquia selada): `Point`, `MultiPoint`, `LineString`,
  `MultiLineString`, `Polygon`, `MultiPolygon`, `GeometryCollection` com `coordinates`
  RFC 7946.
- **Feature**: `id` (string|número), `geometry`, `properties`, `bbox`.
- **FeatureCollection**: lista de `Feature` (+ `bbox` opcional).
- **Unidades**: 3 structs (comprimento/distância, ângulo/rumo, área) + `enum` de unidades.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das fixtures GeoJSON canônicas (do `@turf`) fazem round-trip com forma
  idêntica à do TurfJS (`type`+`coordinates`+`properties`+`bbox`).
- **SC-002**: Publicação AOT (ou trimming) de um app de teste conclui com **0 warnings**
  de trimming/reflexão ligados aos tipos do Turfano.
- **SC-003**: 100% das conversões de unidade cobertas têm teste que bate com o `@turf`
  dentro de tolerância numérica (ex.: `1e-9` relativo).
- **SC-004**: A suíte de testes existente permanece **verde (156, 0 falhas)** e o build
  é limpo em `net8.0;net9.0;net10.0`.
- **SC-005**: Um novo-tipo geométrico convertido para NTS e de volta (ponte interna)
  preserva as coordenadas (round-trip exato).

## Assumptions

- **`Feature.properties`** (decisão de design assumida, a confirmar): por padrão
  `JsonObject?` (System.Text.Json.Nodes — flexível e AOT-friendly, espelha o objeto de
  propriedades do Turf), **mais** uma variante genérica `Feature<TProps>` para cenários
  tipados.
- RFC 7946 é o contrato; o `@turf` (via `reference/`, Bun) é a fonte de verdade numérica;
  corner cases de representação numérica JS×C# são ignorados de propósito.
- NTS e UnitsNet **permanecem** na lib atual nesta fase (não são removidos); os novos
  tipos convivem ao lado até as ondas de paridade portarem as funções.
- Os novos tipos vivem no pacote/namespace `Turfano`; a fachada `Turf` atual é preservada.
- **Fora de escopo**: portar as ~70 funções (Fases 4+), remover NTS/UnitsNet, e
  adaptadores NTS **públicos** (interop público segue fora de escopo).
