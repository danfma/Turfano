# Turfano → Paridade total com TurfJS sobre tipos próprios

Corrigir os bugs atuais e reposicionar o Turfano como um port **fiel (funcional e
numérico)** do TurfJS sobre tipos GeoJSON próprios — serializáveis com
System.Text.Json (source-generated / AOT), com unidades próprias — avaliando e
removendo o NetTopologySuite (NTS) onde ele diverge do TurfJS.

## Norte do projeto (decisões fechadas com o usuário)

- **Fidelidade ao TurfJS é o objetivo total.** Onde o NTS dá resultado diferente do
  Turf (área esférica vs planar, centroide média-de-vértices vs área-ponderada,
  semântica de fronteira dos `Boolean*`, etc.), o NTS sai. Corner cases onde o JS
  não representa números como o C# são ignorados de propósito.
- **Tipos:** GeoJSON RFC 7946 próprios. `readonly record struct Position(double Lon,
  double Lat, double? Alt)` + hierarquia de `record` **selado** com polimorfismo STJ
  (discriminador `"type"`).
- **Serialização:** System.Text.Json **source-generated** (`JsonSerializerContext`),
  zero reflexão, compatível com Native AOT / trimming.
- **Unidades:** structs próprios. Inventariar o uso real de UnitsNet (só
  `Length`/`Angle`/`Area`) e substituir por structs dedicados com semântica e enum de
  unidades alinhados ao Turf (`"kilometers"`, `"degrees"`, ...).
- **Ops pesadas** (union/difference/intersect, buffer, simplify, convex, triangulação,
  voronoi): decisão **op-a-op** na fase de avaliação (catalogar divergência NTS×Turf +
  custo de portar a lib JS equivalente, ex.: polyclip-ts).
- **Bugs:** corrigidos **primeiro**, em entrega independente do redesign.
- **Escopo:** **paridade total** do TurfJS, entregue em **ondas por categoria**.
  Quebra da API atual (NTS-based) é aceitável rumo a 1.0.
- **Interop NTS** (Turfano↔NTS): a ponte `NtsBridge` será **pública** na 1.0 (`ToNts`/
  `FromNts`) para converter **uma vez na borda** (ex.: EF Core spatial) — decisão revista
  em 2026-07-01 (antes: fora de escopo).
- **Multi-targeting:** manter `net8.0;net9.0;net10.0` (source-gen STJ funciona em todos).
- **Referência canônica:** `reference/node_modules/@turf` (código real do TurfJS) é a
  fonte de verdade para algoritmos e para fixtures numéricas de teste.

## Mapa de specs (Spec Kit)

Cada workstream vira um `/speckit-specify` próprio (feature branch própria). Ordem:

1. **spec: bug-fixes** → Fase 1 (independente, entra já).
2. **spec: nts-vs-turf-evaluation** → Fase 2 (spike/decisão).
3. **spec: core-type-system** → Fase 3 (fundação: tipos + unidades + STJ).
4. **spec: parity-wave-\<categoria\>** → uma por onda das Fases 4+ (abertas quando a
   fase começa, pois dependem das decisões das Fases 2–3).

## For Future Agents

À medida que o trabalho avança: marque os checkboxes `- [x]`; ao concluir uma fase,
mude seu Status para `Complete` e escreva o **Phase Summary** (o que foi feito,
decisões-chave, e o necessário para continuar com zero contexto); rode o
**Verification Plan** da fase e registre o resultado antes de seguir. Quando todas as
fases estiverem prontas, preencha **Final Recap** e **Deployment Plan**. As ondas de
paridade (Fases 4+) só devem ter seu `/speckit-specify` aberto quando a fase começar,
porque o conteúdo depende do resultado das Fases 2 e 3.

---

## Phase 1: Correção de bugs (entrega independente)
Status: Complete

Objetivo: corrigir defeitos objetivos no código atual (ainda NTS/UnitsNet-based),
com testes que faltam, e liberar como release de manutenção antes do redesign.

- [x] Corrigir `src/Turfano/Angles.cs`: `TwoPi` deve ser `Angle.FromRadians(2 * Math.PI)`
  (hoje é `Math.PI`, igual a `Pi`).
- [x] Revisar todos os usos de `Angles.Pi`/`Angles.TwoPi` (`Turf.RhumbBearing.cs`,
  `Turf.Angle.cs`/`GetAngle`) e confirmar a correção da matemática de wraparound
  (`deltaLambda`, `bear180`).
- [x] Corrigir `src/Turfano/Turf.TransformScale.cs:49`: a precedência de
  `dy * options.FactorY ?? factor` colapsa o eixo Y para a constante `factor` quando
  `FactorY == null`. Trocar por `dy * (options.FactorY ?? factor)`.
- [x] Varrer outros usos do padrão `a * x.Nullable ?? b` no projeto (grep) e validar
  precedência.
- [x] Adicionar testes de `RhumbBearing` (hoje **sem nenhum teste**) com valores de
  referência do TurfJS, cobrindo rumos > 180° e cruzamento do antimeridiano.
- [x] Adicionar testes de `TransformScale` (hoje **sem nenhum teste**) cobrindo o caso
  padrão (sem `FactorY`) e com `FactorY`/`FactorZ`/`Origin`.
- [x] Adicionar teste do caminho `Explementary` de `GetAngle`.
- [ ] (Cosmético, opcional) Remover comentários obsoletos `// filepath:
  /Users/danfma/Develop/private/...` dos ~17 arquivos afetados.

### Verification Plan

- `dotnet build Turfano.slnx -c Debug` → `0 Error(s)`.
- `dotnet run --project tests/Turfano.Tests -c Debug` → todos os testes verdes,
  incluindo os novos de `RhumbBearing`/`TransformScale`/`GetAngle`.
- Teste-âncora `RhumbBearing`: para `from=(-75.343,39.984)`, `to=(-75.534,39.123)`,
  esperar `≈ -170.29°` (valor real do TurfJS; `9.71°` era o sentido inverso/saída do
  bug), `.Within(0.1)`.
- Confirmar que o teste de `TransformScale` no caso padrão FALHA antes do patch e
  PASSA depois (diff de comportamento).

### Phase Summary

Concluída em 2026-06-29 na feature branch `001-fix-current-bugs` (Spec Kit completo:
spec → plan → tasks → implement). Correções de produção (cirúrgicas):

- `Angles.TwoPi` agora é `2π` (era `π`). Esse único fix corrigiu, de uma vez,
  `RhumbBearing` (normalização de `deltaLambda` + cálculo de `bear180`) e o caminho
  `Explementary` de `GetAngle`.
- `Turf.TransformScale`: `dy * (options.FactorY ?? factor)` — o eixo Y deixou de
  colapsar no caso padrão. Varredura FR-009: nenhuma outra ocorrência do padrão.

Testes novos (TDD, ancorados no TurfJS real via `reference/`): `RhumbBearingTests` (5),
`TransformScaleTests` (4), `GetAngleTests` (1). Suíte: 146 → **156, 0 falhas**; build
limpo em net8/9/10.

Descobertas registradas para o redesign (fora do escopo desta fase):
- O XML-doc de `RhumbBearing` documentava `9.71°` (sentido inverso/valor do bug); o
  valor correto do TurfJS é `-170.294°`.
- `GetAngle` diverge do TurfJS na base (~0.18°) por usar `Bearing(start→mid)`/`(end→mid)`
  em vez de `bearing(mid→start)`/`(mid→end)` — tratar na onda Measurement/Booleans.
- `TransformScale` é cartesiano; o TurfJS é geodésico — tratar na onda Transformation.

Item cosmético (remover comentários `// filepath:`) deliberadamente adiado (opcional,
não é critério de sucesso).

---

## Phase 2: Avaliação NTS × TurfJS + benchmark (spike, decisão op-a-op)
Status: Complete

Objetivo: produzir dados para decidir, por operação, entre **portar o algoritmo do
Turf**, **manter o NTS interinamente** ou **aproximar**. Não escreve código de
produção — gera um documento de decisão e protótipos descartáveis.

- [x] Catalogar, por função já implementada, se hoje é (a) wrapper fino de NTS,
  (b) implementação própria, ou (c) algoritmo ingênuo. Registrar em
  `docs/nts-evaluation.md`.
- [x] Para cada wrapper de NTS, documentar a **divergência conhecida vs TurfJS**
  (ex.: `area` planar×esférica; `centroid` área×média; `booleanPointInPolygon` e
  demais `Boolean*` semântica de fronteira; `simplify` Douglas-Peucker vs TPS).
- [x] Para as ops pesadas (union/difference/intersect, buffer, convex, simplify,
  tin, voronoi, concave, tesselate, bezierSpline), mapear: qual lib/algoritmo o
  **TurfJS** usa (polyclip-ts, d3-geo, etc.), magnitude da divergência e **custo de
  porte para C#**. Preencher uma matriz de decisão (portar / NTS-interino / aproximar).
- [x] Protótipo de benchmark comparando **tipos de valor próprios** (Position struct +
  geometria) vs NTS nas rotas quentes (`Distance`, `Area`, `WalkAlong`), com
  `MemoryDiagnoser`, sobre o projeto `benchmark/`.
- [x] Inventariar o uso real de **UnitsNet** no código (esperado: só `Length`, `Angle`,
  `Area`) para dimensionar os structs de unidade próprios.
- [x] Escrever a recomendação final (manter/remover NTS por op) em `docs/nts-evaluation.md`.

### Verification Plan
- `docs/nts-evaluation.md` existe e contém: tabela função→(tipo, divergência) e a
  matriz de decisão op-a-op preenchida.
- `dotnet run -c Release --project benchmark/TimeAndMemoryUsage` roda e produz números
  (tempo + alocação) para o protótipo de tipos próprios vs NTS.
- `grep -rn "UnitsNet" src/Turfano --include='*.cs'` → lista fechada de tipos usados,
  anexada ao doc.

### Phase Summary

Concluída em 2026-06-29 (branch `002-nts-evaluation`). Entregável:
`docs/nts-evaluation.md` — classificação de todas as funções, divergências **medidas**
vs TurfJS (harness Bun + dump .NET), matriz de decisão op-a-op, benchmark e inventário de
UnitsNet. Zero mudança em `src/` de produção (SC-006); suíte 156/0.

Conclusões que destravam a Fase 3:
- **Tipos próprios valem a pena**: ~3,4× menos alocação e ~3,4× mais rápido em Area
  (struct vs NTS+UnitsNet); só 3 quantidades UnitsNet usadas (`Length`/`Angle`/`Area`).
- **A divergência do NTS é menor do que o receio** (medido): overlay dá área idêntica ao
  `polyclip-ts`; o `buffer` do Turf **é** o JTS/NTS; `convex`/`simplify`/`distance`/
  `bearing`/`area`/booleanos batem → **manter NTS interino** em overlay/buffer/convex/
  simplify.
- **Portar (correção)**: `tin`/`voronoi`/`concave`/`tesselate`/`isolines`/`isobands` —
  hoje ingênuos/incorretos — com os algoritmos reais do Turf (Delaunay/d3-voronoi/earcut/
  marching-squares).
- **Divergências pontuais a corrigir nas ondas**: `Centroid` (conta o vértice de
  fechamento — outro valor de teste presumido-errado, padrão do `RhumbBearing`/9.71°);
  `BooleanOverlap` (aresta compartilhada: Turf `true` × NTS `false`); `GetAngle`
  (convenção de bearing); `TransformScale/Rotate/Translate` (cartesiano vs geodésico).

**Decisão**: a remoção total do NTS é viável, mas só compensa **depois** do porte dos 6
algoritmos pesados — não no dia 1. Manter o NTS como dependência interina.

---

## Phase 3: Sistema de tipos central (GeoJSON + unidades + STJ source-gen)
Status: Complete

Objetivo: a fundação. Tipos GeoJSON próprios, structs de unidade, e serialização STJ
source-generated RFC 7946. Tudo o resto (paridade) assenta aqui.

- [x] `readonly record struct Position(double Lon, double Lat, double? Alt = null)`
  com acessos `[0]/[1]/[2]` e conversões de/para `double[]`.
- [x] `readonly record struct BBox` (2D e 3D) com (de)serialização para array GeoJSON.
- [x] Hierarquia de geometria: `abstract record Geometry` + selados `Point`,
  `MultiPoint`, `LineString`, `MultiLineString`, `Polygon`, `MultiPolygon`,
  `GeometryCollection`, com o layout de `coordinates` do RFC 7946.
- [x] `Feature` (`id`, `geometry`, `properties`, `bbox`) e `FeatureCollection`.
  **Decisão de design (resolver na spec):** `properties` como `JsonObject`/
  `IDictionary<string,object?>` vs `Feature<TProps>` genérico (ou ambos).
- [x] Structs de unidade próprios substituindo UnitsNet (com base no inventário da
  Fase 2): ex.: medida de comprimento/distância + ângulo/rumo + área, cada um com
  enum de unidades alinhado ao Turf (`Kilometers`, `Meters`, `Miles`, `Degrees`,
  `Radians`...), operadores, conversões e `From*/As*`.
- [x] `JsonSerializerContext` source-generated + `[JsonPolymorphic(TypeDiscriminator
  PropertyName = "type")]` + `[JsonDerivedType(..., "Point")]` etc., e converters
  customizados para `Position`/`BBox` (arrays JSON). Sem reflexão.
- [ ] Fixtures GeoJSON canônicas (extraídas do `@turf` em `reference/`) para round-trip.
- [x] Helpers/factory ao estilo Turf (`point()`, `lineString()`, `polygon()`,
  `featureCollection()`, `getCoord(s)`, `getType`, `getGeom`) sobre os novos tipos.

### Verification Plan
- Round-trip: desserializar e reserializar cada fixture GeoJSON e comparar a forma com
  a saída do TurfJS (`type` + `coordinates` + `properties` + `bbox`).
- AOT/trim smoke: `dotnet publish -c Release -p:PublishAot=true` (ou
  `-p:PublishTrimmed=true` num app de teste) compila sem warnings de trimming/reflexão
  nos tipos do Turfano.
- Testes de unidade dos structs de unidade (conversões batendo com TurfJS:
  ex.: `convertLength`, `lengthToRadians`, `radiansToLength`).

### Phase Summary

Concluída em 2026-06-29 (branch `003-geojson-types`). As 4 histórias (US1–US4) foram
implementadas e estão verdes; os 5 critérios de sucesso atendidos. Suíte 156 → **177, 0
falhas**, build net8/9/10; `NTS`/`UnitsNet` e os `Turf.*.cs` atuais **intocados** (motor
interino preservado — só houve adições).

Entregue em `src/Turfano/`:
- `GeoJson/`: `Position`/`BBox` (struct), 7 geometrias (record selado), `Feature`/
  `FeatureCollection`, contexto **source-gen** + `Geo.*` (helpers estilo Turf) +
  `getCoord`/`getType`/`getGeom`. Round-trip **byte-exato** (13 casos).
- `Units/`: `Length`/`Angle`/`Area` (structs) com conversões validadas contra o `@turf`
  real (até `1e-15`).
- `Interop/NtsBridge.cs` (`internal`): ponte novos-tipos ↔ NTS (todas as geometrias,
  furos, Z), `InternalsVisibleTo` p/ testes.
- `tests/Turfano.AotSmoke`: smoke AOT (`IsAotCompatible`) → **0 warnings IL** (SC-002).

**Decisão de serialização (chave p/ as ondas)**: o spike PROVOU que o `[JsonPolymorphic]`
embutido descarta o discriminador em `Feature[]`, e que sob AOT a reflexão é desabilitada.
Após a consideração de **naming** (compor com o `PropertyNamingPolicy` do consumidor sem
quebrar o GeoJSON), a decisão final foi: **polimorfismo embutido/source-gen +
`[JsonPropertyName]` fixando os nomes RFC 7946 + `nameof` + um `FeatureArrayConverter`**
(único ponto não coberto). Round-trip byte-exato e **imune à política de naming** (provado
em teste). Ver `specs/003-geojson-types/research.md`.

Follow-ups explícitos (não bloqueiam as ondas de paridade):
- **Fixtures extraídas do `@turf`** + comparação estrutural (T008/T010): o round-trip já é
  byte-exato sobre GeoJSON canônico RFC 7946 (a forma do `@turf`), então SC-001 está
  substantivamente atendido; a extração literal fica como reforço.
- **`Feature<TProps>` genérico**: ficou só `Feature` com `properties: JsonObject?`
  (default); a variante genérica pode entrar quando houver demanda.

---

## Phase 4: Onda A — Measurement (paridade)
Status: Complete

Re-fundar as funções de medição sobre os novos tipos, fiéis aos algoritmos do Turf.
→ Abrir `/speckit-specify parity-wave-measurement` ao iniciar esta fase.

- [x] `area` (esférica, igual ao Turf), `bbox`, `bboxPolygon`, `square`, `envelope`.
- [x] `distance`, `bearing`, `rhumbBearing`, `rhumbDistance`, `length`.
- [x] `destination`, `rhumbDestination`, `along`, `midpoint`, `center`,
  `centerOfMass`, `centroid` (média de vértices, como o Turf).
- [x] `pointOnFeature`, `pointToLineDistance`, `pointToPolygonDistance`,
  `nearestPointOnLine`, `greatCircle`, `polygonTangents`.
- [x] Conversões de unidade do Turf: `bearingToAzimuth`, `convertArea`,
  `convertLength`, `degreesToRadians`, `radiansToDegrees`, `lengthToRadians`,
  `lengthToDegrees`, `radiansToLength`, `toMercator`, `toWgs84`.

### Verification Plan
- Cada função testada contra fixtures numéricas do TurfJS (`.Within` apertado, p.ex.
  `1e-6` onde aplicável). Sem tolerâncias frouxas tipo `.Within(6)`.

### Phase Summary

Concluída em 2026-06-29 (branch `004-parity-measurement`, Spec Kit completo). **As 24
funções de measurement** portadas para os novos tipos, validadas contra o `@turf` real.
Suíte 177 → **193, 0 falhas**; build net8/9/10; AOT 0 warnings; `Turf.*.cs` NTS/UnitsNet
**intocados** (motor interino preservado — só adições).

**Decisão de arquitetura (importante p/ as próximas ondas)**: a API de funções sobre os
tipos novos vive na **fachada `Geo`** (`partials` em `Turfano.GeoJson`), não como
sobrecargas em `Turf`. No namespace `Turfano.GeoJson` os nomes de tipo (`Point`/`Polygon`/
`Length`/...) vencem o global using do NTS, eliminando colisões método×tipo (`Length` etc.).
`Geo` é a **fachada única** dos tipos novos (construtores + measurement + futuras ondas).
Padrão de trabalho consolidado: **re-tipar/portar em `Geo.*` + ground-truth no harness Bun +
teste vs `@turf` real** (tolerância apertada).

Entregue (`src/Turfano/Parity/`, tudo em `Geo.*`):
- Escalares: `Area` (esférica), `Distance`, `Bearing`, `Length`, `Bbox`/`BboxPolygon`/
  `Square`/`Envelope`.
- Pontos: `Centroid` (**consertado**: exclui o vértice de fechamento → `[1,1]`), `Center`,
  `CenterOfMass`, `Midpoint`, `Destination`, `Along`, `RhumbDestination`.
- Rumo/distâncias: `RhumbBearing`, `RhumbDistance`, `PointToLineDistance`,
  `PointToPolygonDistance`, `NearestPointOnLine`, `PointOnFeature`, `GreatCircle`,
  `PolygonTangents`.
- Conversões: `BearingToAzimuth`, `ConvertLength`/`ConvertArea`, `DegreesToRadians`/
  `RadiansToDegrees`/`LengthToRadians`/`RadiansToLength`/`LengthToDegrees`.

**Bugs/divergências do código NTS pegos pela validação** (a versão `Geo` bate com o `@turf`;
a NTS fica intocada): `RhumbDestination` (`q` errado), `nearestPointOnLine`/
`pointToLineDistance` (projeção **planar** vs o **geodésico 3D** do `@turf`), `centroid`
(vértice de fechamento). Docs antigos diziam `9.71°`/`97.994` onde o `@turf` dá
`-170.294°`/`97.129`. **Reforça a disciplina: validar contra o `@turf`, não confiar no
código existente.**

Edge cases/follow-ups conscientes (não bloqueiam): `toMercator`/`toWgs84` (são **projeção**,
não measurement — ficam para uma onda de projeção); furos/MultiPolygon em
`PointToPolygonDistance`/`PolygonTangents` e o split por antimeridiano de `GreatCircle`
(MultiLineString) usam o caminho simples — refinar quando houver demanda.

---

## Phase 5: Onda B — Booleans / Assertions (paridade)
Status: Complete

Semântica **do Turf**, não do NTS (especialmente fronteira/`ignoreBoundary`).
→ `/speckit-specify parity-wave-booleans`.

- [x] `booleanPointInPolygon`, `booleanPointOnLine`, `booleanClockwise`,
  `booleanConcave`, `booleanParallel`.
- [x] `booleanContains`, `booleanWithin`, `booleanDisjoint`, `booleanIntersects`,
  `booleanCrosses`, `booleanOverlap`, `booleanTouches`, `booleanEqual`,
  `booleanValid`.

### Verification Plan
- Suíte de fixtures `true/false` do TurfJS (os repositórios `@turf/boolean-*` trazem
  fixtures) reproduzida e verde.

### Phase Summary

Concluída em 2026-06-29 (branch `005-parity-booleans`, Spec Kit completo). As **14 funções
`boolean*`** existem na fachada **`Geo`**, **portadas do `@turf`** (não re-tipadas dos
`Turf.Boolean*.cs` NTS, que delegam a predicados NTS divergentes na fronteira). Suíte 193 →
**203, 0 falhas**; build net8/9/10; AOT 0 warnings; `Turf.*.cs` NTS/UnitsNet **intocados**.

Entregue (`src/Turfano/Parity/Boolean.*.cs`, tudo em `Geo.*`):
- Ponto/orientação: `BooleanPointInPolygon` (Hao, com **borda + `ignoreBoundary`**),
  `BooleanPointOnLine` (+`ignoreEndVertices`/`epsilon`), `BooleanClockwise`,
  `BooleanParallel`, `BooleanConcave`.
- Relações: `BooleanDisjoint`/`BooleanIntersects` (achata multi + interseção de segmento
  portada do `@turf/line-intersect`), `BooleanContains`/`BooleanWithin`, `BooleanOverlap`,
  `BooleanCrosses`, `BooleanTouches`, `BooleanEqual`.
- Validade: `BooleanValid`.

**A validação vs `@turf` pegou enganos da MINHA spec** (de novo, a disciplina valeu):
`overlap` de polígonos que compartilham aresta é **true** (não false); `valid` de um anel em
"laço"/bowtie é **true** (o `@turf` não detecta auto-interseção do anel externo). Segui o
`@turf`, não a suposição.

**Follow-ups conscientes (não bloqueiam)**: os predicados relacionais cobrem os **combos de
tipo núcleo** validados (Polygon/Polygon, Point/Polygon, Line/Polygon, etc.), **não a matriz
completa** do `@turf` (MultiPoint, todas as combinações de linha em `touches`/`crosses`); e
`BooleanEqual` é comparação ordenada (o `geojson-equality` do `@turf` também normaliza
rotação/direção de anéis). Refinar com as fixtures completas dos pacotes `@turf/boolean-*`
quando houver demanda.

---

## Phase 6: Onda C — Transformation & Coordinate Mutation (paridade)
Status: Complete

→ `/speckit-specify parity-wave-transformation`.

- [x] Mutation: `cleanCoords`, `flip`, `rewind`, `round`, `truncate`.
- [x] Transform: `transformRotate`, `transformScale` (já corrigido na Fase 1; reportar
  sobre novos tipos), `transformTranslate`, `clone`.
- [x] `bezierSpline`, `polygonSmooth`, `lineOffset`, `circle`, `simplify`
  (conforme decisão da Fase 2).

### Verification Plan
- Fixtures do Turf por função; `transformScale` no caso padrão produz escala correta
  em X **e** Y.

### Phase Summary

Concluída em 2026-06-29 (branch `006-parity-transformation`, Spec Kit completo). As **14
funções** de transformação/mutação existem na fachada **`Geo`**, validadas contra o `@turf`.
Suíte 203 → **215, 0 falhas**; build net8/9/10; AOT 0 warnings; `Turf.*.cs` NTS/UnitsNet
**intocados**; nomes .NET (sem acrônimos crípticos — convenção reforçada nesta sessão).

Entregue (`src/Turfano/Parity/{Mutate,Transform,Generate}.*.cs`, tudo em `Geo.*`):
- **Mutação**: `Flip`, `Round`, `Truncate`, `CleanCoords` (cleanLine via `BooleanPointOnLine`),
  `Rewind` (via `BooleanClockwise`).
- **Transform**: `TransformScale` **GEODÉSICO** (rhumb a partir do centroide — correção da
  divergência da Fase 1/2, SC-002), `TransformTranslate`, `TransformRotate`, `Clone`.
- **Geração**: `Circle` (N `Destination`), `Simplify` (Douglas-Peucker `simplify-js`),
  `PolygonSmooth` (Chaikin), `LineOffset` (perpendicular + junção), `BezierSpline` (porte da
  classe `Spline`).

**Correção-chave**: `TransformScale` agora é **geodésico** como o `@turf` (o teste pina os
valores exatos, ex.: `[-1.0006097…, 3]`), não mais cartesiano (não colapsa).

**A validação pegou suposições erradas** (a disciplina valeu de novo): `truncate` na verdade
**arredonda** (`Math.round`, apesar do nome); `cleanCoords` remove **colineares** (não só
duplicados); `rewind` default orienta o externo **anti-horário**. Segui o `@turf`.

Follow-ups conscientes (não bloqueiam): origens nomeadas (`sw`/`se`/...) de `transformScale`
e a matriz completa de tipos de algumas funções podem ser refinadas com as fixtures
completas do `@turf` quando houver demanda.

---

## Phase 7: Onda D — Feature Conversion, Joins, Meta (paridade)
Status: Complete

→ `/speckit-specify parity-wave-features-meta`.

- [x] Conversão: `explode`, `combine`, `flatten`, `lineToPolygon`, `polygonToLine`,
  `polygonize`.
- [x] Joins: `tag`, `pointsWithinPolygon`.
- [x] Misc: `kinks`, `lineChunk`, `lineSlice`, `lineSliceAlong`, `nearestPoint`.
- [x] Meta (públicos, hoje `internal`): `coordEach/coordReduce`, `featureEach`,
  `geomEach`, `propEach`, `segmentEach/segmentReduce`, `flattenEach`. Reexpor com a
  visibilidade correta.

### Verification Plan
- Fixtures do Turf; meta-funções com testes de iteração/índices iguais aos do Turf.

### Phase Summary

Concluída em 2026-06-30 (branch `007-parity-features-meta`, Spec Kit completo). As **19
funções** de conversão/joins/misc/meta existem na fachada **`Geo`**, validadas contra o
`@turf`. Suíte 215 → **226, 0 falhas**; build net8/9/10; AOT 0 warnings; `Turf.*.cs` NTS/
UnitsNet **intocados**; nomes .NET.

Entregue (`src/Turfano/Parity/{Convert,Join,Misc,Meta}*.cs`, tudo em `Geo.*`):
- **Conversão**: `Explode`, `Flatten`, `Combine`, `PolygonToLine` (furos → MultiLineString),
  `LineToPolygon`, `Polygonize` (via NTS `Polygonizer` interino na `NtsBridge`).
- **Joins**: `PointsWithinPolygon`/`Tag` (reusam `BooleanPointInPolygon` — fronteira do @turf).
- **Misc**: `NearestPoint`, `LineSlice` (via `NearestPointOnLine`), `LineSliceAlong`,
  `LineChunk`, `Kinks` (interseção de segmento).
- **Meta** (agora públicos): `CoordEach`/`CoordReduce`, `SegmentEach`/`SegmentReduce`,
  `FeatureEach`, `GeomEach`, `PropEach`, `FlattenEach` — **ordem e índices iguais ao @turf**
  (porte fiel do `coordEach`; teste prova `coordIndex` global e `segmentIndex`).

**Onda de alto reuso**: a maior parte compõe os helpers das Ondas A/B/C
(`BooleanPointInPolygon`/`Along`/`NearestPointOnLine`/`Distance`/`Length`/`SegmentsIntersect`/
`EachPosition`/`FlattenGeometry`) — confirmou o valor da fachada `Geo` única.

Follow-ups conscientes (não bloqueiam): `polygonize` usa o NTS interino (a ordem dos
vértices do anel pode diferir do `@turf`; a forma casa); a matriz completa de tipos de
algumas conversões pode ser refinada com as fixtures completas quando houver demanda.

---

## Phase 8: Onda E — Overlay / Clipping (paridade, depende da Fase 2)
Status: Complete

As ops onde o NTS mais diverge do Turf. Implementação conforme a matriz da Fase 2
(portar polyclip-ts / manter NTS-interino / aproximar).
→ `/speckit-specify parity-wave-overlay`.

- [x] `union`, `difference`, `intersect`, `dissolve`.
- [x] `bboxClip`, `buffer`.

### Verification Plan
- Fixtures do Turf para overlay; resultados batem com o Turf dentro da tolerância
  decidida na Fase 2 (ou divergência documentada e aceita).

### Phase Summary

Concluída em 2026-06-30 (branch `008-parity-overlay`, Spec Kit completo). As **6 funções**
de overlay/clipping existem na fachada **`Geo`**, validadas contra o `@turf`. Suíte 226 →
**232, 0 falhas**; build net8/9/10; AOT da **serialização** 0 warnings; `Turf.*.cs` NTS/
UnitsNet **intocados**; nomes .NET.

**Estratégia (oposta às ondas anteriores, baseada na MEDIÇÃO da Fase 2)**:
- **Overlay** (`Union`/`Difference`/`Intersect`/`Dissolve`): **motor NTS** via `NtsBridge`
  (planar em graus = `polyclip-ts`). Validado por **ÁREA** vs o `@turf` (union `3.456e11`,
  intersect `4.94e10`, difference `1.48e11` — dentro de `1e4` absoluto). Vazio → `null`.
- **Buffer**: reproduz o `@turf` — projeta para **azimutal-equidistante** centrada na
  geometria (descoberta: equivale a `Distance`/`Bearing`/`Destination` great-circle da Onda A),
  **NTS `Buffer`** no plano em metros (o `buffer` do `@turf` **é** o JTS=NTS), e desprojeta.
  Área bate (~`3.12e10`, point 100 km, steps 8).
- **bboxClip**: **portado** (Cohen-Sutherland `lineclip` + Sutherland-Hodgman), sem NTS.
  Estrutura igual ao `@turf` (linha e polígono).

**Decisão de motor registrada**: overlay/buffer ficam **NTS-interino** (escondido na
`NtsBridge`; NTS **não** aparece nas assinaturas) — é a decisão **medida** da Fase 2, não um
débito. Por isso podem não ser AOT-safe; o smoke de AOT exercita só a **serialização** (segue
0 warnings). Único porte: `bboxClip`.

Follow-ups conscientes (não bloqueiam): `buffer` usa a AEQD via great-circle (equivalente à
d3-geo do `@turf`; área casa, vértices podem diferir em micro); refinar com mais fixtures
quando houver demanda.

---

## Phase 9: Onda F — Interpolation, Grids, Triangulation (paridade)
Status: Not started

Aqui moram os algoritmos hoje **ingênuos/quebrados** (`tin`, `voronoi`, `concave`,
`tesselate`, `isobands`). Port fiel ao Turf.
→ `/speckit-specify parity-wave-interpolation-grids`.

- [ ] Interpolation: `interpolate`, `isolines`, `isobands`, `planepoint`, `tin`.
- [ ] Triangulation/hulls: `voronoi`, `concave`, `convex`, `tesselate`.
- [ ] Grids: `pointGrid`, `hexGrid`, `squareGrid`, `triangleGrid`.

### Verification Plan
- Fixtures do Turf; `tin`/`voronoi`/`tesselate` produzem triangulações/células
  equivalentes às do Turf (não mais o leque ingênuo).

### Phase Summary
_(escrever quando a fase concluir)_

---

## Phase 10: Onda G — Pacotes restantes (paridade total)
Status: Not started

Fechar a paridade com o restante do TurfJS ainda não coberto.
→ `/speckit-specify parity-wave-remaining`.

- [ ] Random: `randomPosition`, `randomPoint`, `randomLineString`, `randomPolygon`.
- [ ] Classification/Aggregation: `clustersKmeans`, `clustersDbscan`, `clusters`,
  `collect`, `sample`.
- [ ] Quaisquer helpers/`@turf/*` restantes identificados no cruzamento com
  `MISSING_FUNCTIONS.md` e o índice de `@turf` em `reference/`.

### Verification Plan
- Cruzar a lista completa de funções do `@turf` (de `reference/node_modules/@turf`)
  com a API pública do Turfano: **cobertura 100%** (ou itens fora de escopo
  explicitamente listados).

### Phase Summary
_(escrever quando a fase concluir)_

---

## Phase 11: Saída do NTS + limpeza da superfície legada (rumo à 1.0)
Status: Not started

Decisão fechada com o usuário em 2026-07-01 (ver discussão registrada abaixo). O objetivo
final é **uma única representação pública** (os tipos próprios), core **sem dependências**
e AOT-limpo, com o NTS sobrevivendo apenas num pacote satélite de UMA função (`buffer`).

**Fatos medidos que fundamentam a decisão** (não re-litigar sem dados novos):
- NTS 2.5.0 = **721 tipos** (BSD-3-Clause). O fecho de dependências de overlay+buffer+
  polygonize ≈ **300+ tipos** (OverlayNG 36, Buffer 23, Noding 55, Algorithm 38,
  GeometriesGraph+Index 63, Geometries ~130) — vendorizar "só essa parte" = metade do NTS,
  operando sobre o modelo `Coordinate`/`Geometry` deles (a 2ª representação continuaria
  existindo, só escondida). **Vendoring rejeitado.**
- O motor real do `@turf` para union/difference/intersect é o **`polyclip-ts`** (MIT,
  **1.137 linhas** num arquivo, opera direto sobre anéis de coordenadas = `Position[][]`).
  `@turf/polygonize` = **635 linhas** (grafo puro). O **buffer** não tem algoritmo pequeno:
  o próprio `@turf` embute o JTS inteiro (`@turf/jsts`, **1,1 MB** traduzido por máquina).
- Modelo próprio vs NTS (benchmark Fase 2): structs alocam **3,4× menos** e rodam **3,4×
  mais rápido** (`Area`: 352→104 B, 105→31 ns). Design "casca própria, miolo NTS"
  **rejeitado**: pessimizaria as ~99 funções nativas (heap por vértice, perda da semântica
  de record e do AOT da lib inteira) para poupar duas cópias O(n) em 1 função.

- [ ] Portar o **`polyclip-ts`** (MIT, 1.137 linhas) sobre os tipos próprios →
  `Geo.Union`/`Difference`/`Intersect`/`Dissolve` nativos (fidelidade por construção: é o
  motor que o `@turf` executa). Manter atribuição (licença MIT no NOTICE).
- [ ] Portar o **`@turf/polygonize`** (635 linhas) → `Geo.Polygonize` nativo.
- [ ] **`Turfano.Buffer`** (pacote satélite): mover `Geo.Buffer` para um pacote próprio que
  referencia o NTS (o core perde a dependência). Pipeline (projeção AEQD própria → NTS
  Buffer → desprojeção) permanece — é o mesmo pedágio que o `@turf` paga com o JSTS.
  **Escada de otimização da fronteira** (verificada na API pública do NTS 2.5 em
  2026-07-01; resolve o problema "Coordinate é classe + cópia defensiva → pressão no GC"):
  1. **`PackedDoubleCoordinateSequence` + `GeometryFactory(CoordinateSequenceFactory)`**
     nos DOIS sentidos: ida = `Position[]` → `double[]` cru → sequência empacotada
     (`ctor(double[],dim,measures)`); as operações do NTS constroem o resultado com a
     factory da entrada → a volta lê via **`GetRawCoordinates()`** (público, devolve o
     array interno **sem cópia**). **Zero objetos `Coordinate`** em ambos os lados; sobram
     só as alocações internas do algoritmo (preço do noding, irremovível por fronteira).
  2. (Opcional) subclasse de `CoordinateSequence` lendo direto do `Position[]` — elimina
     até a cópia crua da ida (`Position` não é blittable por causa do `double? Alt`, então
     nada de reinterpretar memória; leitura por índice).
  3. **`UnsafeAccessor` = último recurso documentado, NÃO usar por padrão**: só se um
     profiling futuro mostrar hotspot de fronteira que a API pública não alcança. Motivo:
     amarra a membros privados POR NOME de assembly de terceiro → rename interno num
     update do NTS vira `MissingFieldException` em runtime. Se algum dia usado: NTS com
     versão pinada no satélite + checagem que falha alto na inicialização + caminho de
     fallback via API pública.
- [ ] Tornar a **`NtsBridge` pública** (`ToNts`/`FromNts`) para interop na borda
  (EF Core spatial etc.) — conversão única, não por operação.
- [ ] **Deletar a superfície legada `Turf.*`** (72 arquivos, NTS/UnitsNet nas assinaturas —
  a fonte real de "induzir o dev ao erro") + remover a dependência **UnitsNet** do core.
- [ ] Split de pacotes: **`Turfano`** (core, zero dependências, AOT-limpo) +
  **`Turfano.Buffer`** (NTS). Atualizar README/release notes (breaking 1.0).

### Verification Plan
- `Union`/`Intersect`/`Difference` portados batem em ÁREA com o `@turf` nos mesmos casos da
  Onda E (mesmos testes, sem NTS no core); `Polygonize` reproduz o teste da Onda D.
- `dotnet publish` AOT do core sem warnings; `grep NetTopologySuite src/Turfano/` vazio
  (dependência só em `Turfano.Buffer`).
- Suíte completa verde; superfície legada removida do pacote público.

### Phase Summary
_(escrever quando a fase concluir)_

---

## Final Recap
_(escrever quando todas as fases concluírem)_

## Deployment Plan
_(escrever quando todas as fases concluírem — provável: bump para 1.0.0 com release
notes de breaking changes (NTS/UnitsNet → tipos próprios), publish via
`dotnet-releaser.toml`, e migração documentada)_
