# Research: Sistema de tipos GeoJSON (Fase 0)

Sem `[NEEDS CLARIFICATION]` (o ponto de `Feature.properties` está como assumption no
spec). Aqui ficam as decisões técnicas e **o risco principal** a derrubar com um spike.

## Risco principal (spike obrigatório) — STJ source-gen + polimorfismo multinível + converter de `Position`

**Decisão**: validar, num spike isolado, a combinação:
1. `[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]` com `[JsonDerivedType]`
   usando o **discriminador GeoJSON `type`** (valores `"Point"`, `"Feature"`, ...).
2. **Dois níveis** de polimorfismo: `GeoJsonObject` (base de tudo: geometrias + `Feature`
   + `FeatureCollection`) e `Geometry` (base das 7 geometrias).
3. Um `JsonConverter<Position>` (e `BBox`) que serializa como **array** `[lon,lat,alt?]`.
4. Tudo sob um `JsonSerializerContext` **source-generated** (AOT), sem reflexão.

**Rationale**: é a interação onde o STJ historicamente tem arestas (polimorfismo +
source-gen + converters customizados). Confirmar cedo evita retrabalho. Se o STJ
source-gen não suportar bem o polimorfismo multinível, o fallback é um **converter
polimórfico manual** para `GeoJsonObject`/`Geometry` (lendo a propriedade `type`),
mantendo source-gen para o resto.

**Alternativas**: reflexão STJ (rejeitada — quebra AOT); discriminador `$type` padrão do
STJ (rejeitado — GeoJSON exige `type`).

## Decisão — `Position`/`BBox` como structs serializadas como array

`readonly record struct Position(double Lon, double Lat, double? Alt = null)` com
`[JsonConverter(typeof(PositionConverter))]`; o converter lê/escreve um array JSON de 2
ou 3 números, preservando a dimensão (não inventar `alt`). `BBox` idem (4 ou 6 números).

## Decisão — Hierarquia e selamento

`abstract record GeoJsonObject` → `abstract record Geometry : GeoJsonObject` → selados
`Point`, `MultiPoint`, `LineString`, `MultiLineString`, `Polygon`, `MultiPolygon`,
`GeometryCollection`; e `sealed record Feature : GeoJsonObject`,
`sealed record FeatureCollection : GeoJsonObject`. `coordinates` conforme RFC 7946
(`Position`, `Position[]`, `Position[][]`, `Position[][][]`).

## Decisão — Unidades (3 structs) e fonte das constantes

3 `readonly record struct` (comprimento/distância, ângulo/rumo, área), cada um com `enum`
de unidades alinhado ao Turf. Constantes do Turf: `earthRadius = 6371008.8 m`, fatores de
`@turf/helpers` (`factors`/`areaFactors`). **Validar** cada conversão contra o `@turf`
real (`convertLength`, `convertArea`, `lengthToRadians`, `radiansToLength`,
`lengthToDegrees`, `degreesToRadians`, `radiansToDegrees`, `bearingToAzimuth`).

## Decisão — Ponte interna NTS

`internal static class NtsBridge` com `ToNts`/`FromNts` para `Position↔Coordinate` e cada
geometria↔tipo NTS. **internal** (interop público segue fora de escopo). Permite às
funções `Turf.*` atuais (NTS) operarem sobre os novos tipos durante as ondas de paridade.

## Decisão — Fixtures e validação de round-trip

Conjunto canônico de GeoJSON (do `@turf`/RFC 7946): Point/Line/Polygon/Multi*/GC, Feature
com `properties`/`bbox`/`id`, FeatureCollection, Position 2D e 3D. Round-trip: desserializar
→ reserializar → comparar a forma com a saída do `@turf` (via `reference/`).

## Decisão — Smoke de AOT/trimming

App de teste mínimo que (de)serializa os tipos; `dotnet publish -p:PublishAot=true` (ou
`-p:PublishTrimmed=true`) deve concluir sem warnings de trimming/reflexão nos tipos do
Turfano.

## Riscos / observações

- `Feature.properties` como `JsonObject?` (System.Text.Json.Nodes): confirmar que é
  AOT-safe sob source-gen (Nodes são AOT-friendly). A variante `Feature<TProps>` exige o
  `TProps` no contexto source-gen.
- Polimorfismo STJ exige que o discriminador seja legível na desserialização; com o
  converter de `Position` em arrays e o `type` no objeto, não há conflito.
- net8 vs net10: confirmar que o comportamento de polimorfismo source-gen é igual nos 3
  TFMs (rodar os testes de round-trip em todos via `dotnet test`/`run`).
