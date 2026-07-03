# Introdução

Turfano é um port fiel do [TurfJS](https://turfjs.org) para .NET. Este guia leva você do
zero até suas primeiras medições e uma operação booleana.

## Instalação

Adicione o pacote principal ao seu projeto:

```bash
dotnet add package Turfano
```

Esse é o único pacote necessário para a grande maioria das funções `@turf`. Ele tem
**zero dependências externas** — nada de NetTopologySuite, nada de UnitsNet — o que o
torna adequado para publicações com trimming e Native AOT
(`<IsAotCompatible>true</IsAotCompatible>` já no próprio pacote). A serialização usa
geradores de código-fonte do `System.Text.Json`, então também não há reflexão em tempo
de execução.

Se você também precisar interoperar com geometrias do
[NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite), ou precisar de
`Geo.Buffer` (que realmente exige um motor de geometria planar), adicione também o
pacote satélite — veja [Interoperabilidade com NetTopologySuite](interop-nts.md).

```bash
dotnet add package Turfano.NetTopologySuite
```

## O namespace

Tudo o que você precisa no dia a dia está em `Turfano.GeoJson`:

```csharp
using Turfano.GeoJson;
```

Isso traz a fachada `Geo` (todas as funções `@turf`, como métodos estáticos), os tipos
GeoJSON em forma de record (`Position`, `Point`, `Polygon`, `LineString`, `Feature`,
`FeatureCollection`, ...) e `BBox`. As quantidades tipadas (`Length`, `Area`, `Angle`)
ficam em `Turfano.Units` e são retornadas diretamente pelos métodos de `Geo`
correspondentes, então raramente é preciso importar esse namespace explicitamente.

## Seu primeiro exemplo

Crie duas posições e meça a distância e o rumo entre elas — equivalente a
`@turf/distance` e `@turf/bearing`:

```csharp
using Turfano.GeoJson;

var origem = new Position(-122.4194, 37.7749); // São Francisco
var destino = new Position(-122.4094, 37.7849);

var distancia = Geo.Distance(origem, destino); // Turfano.Units.Length
var rumo = Geo.Bearing(origem, destino); // Turfano.Units.Angle

Console.WriteLine($"{distancia.Kilometers:F3} km");
Console.WriteLine($"{rumo.Degrees:F1}°");
```

`Length` e `Angle` são quantidades tipadas, não `double`s crus — você lê o valor na
unidade que precisar (`.Kilometers`, `.Miles`, `.Meters`, `.Degrees`, `.Radians`, ...)
sem ter que lembrar constantes de conversão.

## Construindo geometrias e medindo área

Use os construtores de `Geo` — que espelham o `@turf/helpers` (`point`, `polygon`,
`lineString`, ...) — para montar geometrias, e depois meça-as com `Geo.Area`
(`@turf/area`):

```csharp
var quadrado = Geo.Polygon(
    [
        new Position(0, 0),
        new Position(1, 0),
        new Position(1, 1),
        new Position(0, 1),
        new Position(0, 0), // o anel precisa fechar
    ]
);

var area = Geo.Area(quadrado); // Turfano.Units.Area
Console.WriteLine($"{area.SquareMeters:F1} m²");
```

## Uma operação booleana: União

Operações de sobreposição (`Geo.Union`, `Geo.Intersect`, `Geo.Difference`) trabalham
diretamente sobre valores `Geometry`, sem precisar de NetTopologySuite:

```csharp
var quadradoSobreposto = Geo.Polygon(
    [
        new Position(0.5, 0.5),
        new Position(1.5, 0.5),
        new Position(1.5, 1.5),
        new Position(0.5, 1.5),
        new Position(0.5, 0.5),
    ]
);

Geometry? uniao = Geo.Union(quadrado, quadradoSobreposto); // @turf/union
```

`uniao` só é `null` quando a operação legitimamente não produz geometria (por exemplo,
ao unir duas entradas vazias) — confira as notas de paridade com o TurfJS nos
comentários XML de cada função para o comportamento exato dos casos de borda.

## Serializando para GeoJSON

Como os tipos GeoJSON são records simples anotados com `System.Text.Json`, serializar
uma `Feature` ou `FeatureCollection` é uma chamada direta a `JsonSerializer.Serialize`;
nenhum conversor personalizado é necessário do seu lado (o discriminador `type` do RFC
7946 e o layout dos arrays de coordenadas já vêm embutidos nos tipos):

```csharp
using System.Text.Json;

var feature = Geo.Feature(quadrado);
var json = JsonSerializer.Serialize<GeoJsonObject>(feature);
```

## Para onde ir agora

- [Conceitos](conceitos.md) — como a fachada `Geo`, os tipos GeoJSON e as unidades
  tipadas se encaixam, e como se relacionam com o TurfJS.
- [Interoperabilidade com NetTopologySuite](interop-nts.md) — quando e como usar o
  pacote satélite.
- [Referência de API](../api/Turfano.GeoJson.Geo.yml) — a lista completa de métodos de
  `Geo`.
