> Leia em outro idioma: [English](README.md)

# Turfano

[![CI](https://github.com/danfma/Turfano/actions/workflows/ci.yml/badge.svg)](https://github.com/danfma/Turfano/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Turfano.svg?label=Turfano)](https://www.nuget.org/packages/Turfano/)
[![NuGet](https://img.shields.io/nuget/vpre/Turfano.NetTopologySuite.svg?label=Turfano.NetTopologySuite)](https://www.nuget.org/packages/Turfano.NetTopologySuite/)

Um port fiel do [TurfJS](https://turfjs.org) para .NET — a biblioteca de análise
geoespacial — com tipos GeoJSON próprios, quantidades tipadas próprias e **zero
dependências externas** no pacote principal.

## Pacotes

- **`Turfano`** (núcleo): a fachada `Geo` (namespace `Turfano.GeoJson`), um port fiel do
  índice de funções `@turf/*` — `Distance`, `Bearing`, `Area`, `Union`, `Buffer` (via
  satélite), `Voronoi`, `Isolines` e dezenas de outras — sobre tipos GeoJSON em forma de
  record próprios (`Point`, `Polygon`, `Position`, ...) e quantidades tipadas próprias
  (`Turfano.Units.Length`/`Area`/`Angle`). Sem NetTopologySuite, sem UnitsNet — amigável
  a AOT e trimming.
- **`Turfano.NetTopologySuite`** (satélite): faz a ponte entre os tipos GeoJSON do
  Turfano e o [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite)
  via `NtsConvert.ToNts`/`FromNts` (uma fronteira sem `Coordinate` por vértice, com
  sequências empacotadas), além de um método de extensão `Buffer` implementado sobre o
  NTS (a única operação que realmente precisa de um motor de geometria planar completo).

## Por que um port do zero em vez de construir sobre o NetTopologySuite?

O NTS é um motor de topologia excelente, mas a superfície de funções e a ergonomia do
TurfJS (`bearing`, `destination`, `along`, `midpoint`, `area`, `union`, `voronoi`,
`isolines`/`isobands`, ...) não se encaixam de forma limpa nele, e portar *em cima* do
NTS significava carregar NTS + UnitsNet como dependências obrigatórias só para obter
APIs no formato do TurfJS. O Turfano hoje porta cada função `@turf` fielmente — mesmo
algoritmo, mesmos casos de borda, mesmos resultados esperados, conferidos contra o
código-fonte real do TurfJS em `reference/` — diretamente sobre tipos GeoJSON simples
que você pode serializar como estão.

## Início rápido

```csharp
using Turfano.GeoJson;

var origem = new Position(-122.4194, 37.7749);
var destino = new Position(-122.4094, 37.7849);

var distancia = Geo.Distance(origem, destino); // Turfano.Units.Length
Console.WriteLine($"{distancia.Kilometers:F3} km");

var quadrado = Geo.Polygon(
    [new Position(0, 0), new Position(1, 0), new Position(1, 1), new Position(0, 1), new Position(0, 0)]
);
var area = Geo.Area(quadrado); // Turfano.Units.Area
Console.WriteLine($"{area.SquareMeters:F1} m²");

var quadradoSobreposto = Geo.Polygon(
    [new Position(0.5, 0.5), new Position(1.5, 0.5), new Position(1.5, 1.5), new Position(0.5, 1.5), new Position(0.5, 0.5)]
);
var uniao = Geo.Union(quadrado, quadradoSobreposto); // Turfano.GeoJson.Geometry?
```

## Interoperabilidade com NetTopologySuite

Precisa de geometrias NTS de verdade — para interoperar com uma stack baseada em NTS,
ou para rodar `Buffer` — adicione o pacote `Turfano.NetTopologySuite`:

```csharp
using Turfano.NetTopologySuite;

var comBuffer = quadrado.Buffer(Turfano.Units.Length.FromKilometers(10)); // @turf/buffer

var geometriaNts = NtsConvert.ToNts(quadrado);
var deVoltaAoTurfano = NtsConvert.FromNts(geometriaNts);
```

## Status

Pré-1.0 (`1.0.0-rc.1`). A fachada `Geo` cobre todo o índice de funções `@turf`; a API
ainda pode evoluir antes da `1.0.0`. Suporta `net8.0`, `net9.0` e `net10.0`.

## Atribuições de terceiros

O Turfano porta algoritmos do TurfJS, polyclip-ts, splaytree-ts, earcut e d3-voronoi.
Veja [`NOTICE`](NOTICE) para as atribuições completas e licenças.

## Desenvolvimento

Veja [`CLAUDE.md`](CLAUDE.md) para os comandos de build/teste e as convenções do
repositório.

## Documentação

- Site de documentação: [Introdução](docs/articles-pt-br/introducao.md) ·
  [Conceitos](docs/articles-pt-br/conceitos.md) ·
  [Interoperabilidade com NTS](docs/articles-pt-br/interop-nts.md)
- Também disponível em inglês: [README.md](README.md)
