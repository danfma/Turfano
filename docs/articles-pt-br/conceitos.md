# Conceitos

Esta página explica as quatro ideias que moldam cada canto do Turfano: a fachada `Geo`,
os tipos GeoJSON próprios, as unidades tipadas, e como tudo isso se relaciona com o
TurfJS.

## A fachada `Geo`

`Turfano.GeoJson.Geo` é uma única `public static partial class` com um método para cada
função `@turf/*` — `Geo.Distance`, `Geo.Bearing`, `Geo.Area`, `Geo.Union`, `Geo.Voronoi`,
`Geo.ClustersKmeans`, `Geo.Isolines`, e por aí vai. Não há uma classe ou namespace por
função para você procurar: se o TurfJS tem `turf.distance(a, b)`, o Turfano tem
`Geo.Distance(a, b)`, com a ordem dos argumentos mantida o mais próxima possível do
original dentro do que os overloads do C# permitem.

Internamente a classe é dividida em vários arquivos parciais (um arquivo por função
portada, em `src/Turfano/Parity/`), mas isso é puramente um detalhe organizacional — do
lado de quem chama, é um único tipo, um único ponto de entrada, descobrível pelo
IntelliSense digitando `Geo.` e lendo os resumos dos comentários XML, que citam o
pacote `@turf` de origem (ex.: `// @turf/buffer`) para que você possa cruzar com a
implementação de referência.

`Geo` também expõe os construtores do `@turf/helpers` (`Geo.Point`, `Geo.Polygon`,
`Geo.LineString`, `Geo.Feature`, `Geo.FeatureCollection`, ...) e os acessores do
`@turf/invariant` (`Geo.GetGeoJsonType`, `Geo.GetGeom`, `Geo.GetCoord`), pelo mesmo
motivo que o TurfJS os mantém junto de tudo o mais: são as primeiras chamadas que você
faz antes de fazer qualquer análise de verdade.

## Os tipos GeoJSON próprios

O TurfJS opera sobre objetos GeoJSON simples porque o JavaScript não tem um sistema de
tipos forte para modelar o RFC 7946 (Point, LineString, Polygon, MultiPoint,
MultiLineString, MultiPolygon, GeometryCollection, Feature, FeatureCollection). O
Turfano modela as mesmas formas como records imutáveis em C#, sob `Turfano.GeoJson`:

```csharp
public readonly record struct Position(double Lon, double Lat, double? Alt = null);

public sealed record Point(Position Coordinates) : Geometry;
public sealed record Polygon(Position[][] Coordinates) : Geometry;
// ... LineString, MultiPoint, MultiLineString, MultiPolygon, GeometryCollection
public sealed record Feature(Geometry? Geometry, JsonObject? Properties) : GeoJsonObject;
public sealed record FeatureCollection(Feature[] Features) : GeoJsonObject;
```

Decisões de projeto deliberadas que vale a pena conhecer:

- **`Position` é um record struct (tipo valor)** (`Lon`, `Lat`, `Alt` opcional) — barato
  de copiar, barato de comparar, sem alocação nos caminhos quentes que percorrem arrays
  de coordenadas.
- **Geometrias são records imutáveis**, então igualdade e expressões `with` funcionam
  como esperado, e não há estado mutável compartilhado para se preocupar quando a mesma
  geometria é passada para várias chamadas de `Geo`.
- **Nenhum tipo próprio "só porque sim"**: `Geometry` e `GeoJsonObject` são records
  abstratos que espelham exatamente a hierarquia de tipos do RFC 7946, então um
  `Polygon` realmente é-um `Geometry` que realmente é-um `GeoJsonObject`, e o
  pattern matching (`geometry switch { Polygon p => ..., MultiPolygon mp => ..., _ =>
  ... }`) se lê como a própria especificação.
- **A serialização é source-generated pelo `System.Text.Json` e é AOT-safe.** O
  discriminador `type` do RFC 7946 é resolvido com `[JsonPolymorphic]`/
  `[JsonDerivedType]` em `GeoJsonObject`/`Geometry`, os nomes das propriedades de
  coordenadas são fixados com `[JsonPropertyName]` para sempre emitirem
  `"coordinates"`, `"type"`, `"properties"` etc. independente da política de nomes do
  consumidor, e um único conversor personalizado de `Feature[]` cobre a única lacuna
  que o polimorfismo embutido não alcança. Nada disso depende de reflexão em tempo de
  execução, então funciona sem modificação quando a aplicação é publicada com trimming
  ou como Native AOT.
- **`BBox`** encapsula o array de bounding box do RFC 7946 (4 valores em 2D, 6 em 3D) e
  serializa como um array JSON simples, igual ao tratamento de `bbox` do `@turf`.

## Unidades tipadas em vez de números crus

Funções do TurfJS que recebem uma distância, área ou ângulo aceitam um `number` simples
mais uma string `units` (`"kilometers"`, `"miles"`, `"degrees"`, ...), e cabe a você
lembrar em qual unidade um dado valor está. O Turfano substitui isso por três tipos de
valor pequenos e imutáveis em `Turfano.Units`: `Length`, `Area` e `Angle`.

```csharp
Turfano.Units.Length raio = Turfano.Units.Length.FromKilometers(50);
Console.WriteLine(raio.Miles); // leia na unidade que precisar

var area = Geo.Area(poligono); // Turfano.Units.Area
Console.WriteLine(area.SquareKilometers);
```

Cada tipo guarda um `Value` e um `Unit`, e expõe métodos de fábrica `From*` além de
`.As(unit)` e propriedades somente-leitura por unidade (`.Meters`, `.Kilometers`,
`.SquareMeters`, `.Degrees`, `.Radians`, ...), com fatores de conversão que
intencionalmente batem exatamente com os do `@turf/helpers` (inclusive
`earthRadius = 6371008.8` metros) — um `Length` que passa pelo Turfano converte para os
mesmos números que o `convertLength` do `@turf/helpers` produziria. Operadores
aritméticos (`+`, `-`, `*`, `/`) são definidos para que você possa acumular
comprimentos (como `Geo.Length` faz ao somar os segmentos de uma `LineString`) sem
normalizar unidades manualmente antes.

Essa é uma escolha deliberada em vez de reaproveitar uma biblioteca genérica de
quantidades: o Turfano só precisa de três grandezas, então ele define exatamente essas
três, sem peso adicional de pacotes e com tabelas de conversão idênticas às do TurfJS.

## Relação com o TurfJS

O Turfano é um *port fiel*, não uma reinvenção. Para cada pacote `@turf/*` coberto, a
implementação é conferida linha a linha contra o código-fonte TypeScript real, vendorizado
em `reference/` no repositório — mesmo algoritmo, mesmos casos de borda (entradas
vazias, polígonos degenerados, tratamento de antimeridiano onde o TurfJS trata), mesmos
resultados numéricos esperados. A suíte de testes frequentemente embute o trecho
equivalente do TurfJS e sua saída esperada diretamente em um comentário, especificamente
para fixar essa paridade ao longo do tempo.

O que o Turfano *não* faz é expandir a superfície além do que o TurfJS oferece, ou
acoplar abstrações não relacionadas — o objetivo (veja o README do projeto) é expor as
funções que complementam bibliotecas de geometria no ecossistema .NET, com nomes e
ordem de argumentos que você reconheceria imediatamente se já conhece o `@turf`.

## Para onde ir agora

- [Introdução](introducao.md) — instalação e primeiras chamadas, se ainda não viu.
- [Interoperabilidade com NetTopologySuite](interop-nts.md) — a ponte com geometrias
  NTS e o uso de `Geo.Buffer`.
- [Referência de API](../api/Turfano.GeoJson.Geo.yml) — a lista completa de métodos de
  `Geo`.
