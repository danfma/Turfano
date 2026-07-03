# Interoperabilidade com NetTopologySuite

O pacote principal `Turfano` é propositalmente livre de dependências: ele define seus
próprios tipos GeoJSON e suas próprias unidades `Length`/`Area`/`Angle` em vez de trazer
o NetTopologySuite ou o UnitsNet, para se manter leve e amigável a Native AOT e
trimming. A maioria das aplicações nunca precisa de nada além dele.

O pacote satélite `Turfano.NetTopologySuite` existe para as duas situações em que você
realmente precisa do [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite)
(NTS):

1. Sua aplicação já armazena ou consulta geometrias como tipos NTS — por exemplo,
   colunas espaciais do EF Core, ou outra biblioteca que fala NTS — e você precisa
   mover dados entre esse mundo e o do Turfano.
2. Você precisa de `Geo.Buffer` (`@turf/buffer`), a única operação do Turfano que
   realmente exige um motor de geometria planar completo para expandir ou contrair uma
   forma por um raio; ela é implementada sobre o buffer do NTS e vive no satélite por
   esse motivo.

Todo o resto em `Geo` funciona sobre geometrias próprias do Turfano sem nenhuma
dependência de NTS — trate este pacote como estritamente opcional.

## Instalação

```bash
dotnet add package Turfano.NetTopologySuite
```

## Convertendo entre Turfano e NTS: `NtsConvert`

`NtsConvert` (namespace `Turfano.NetTopologySuite`) converte nos dois sentidos, tanto no
nível de coordenada quanto no de geometria:

```csharp
using Turfano.GeoJson;
using Turfano.NetTopologySuite;

Polygon quadrado = Geo.Polygon(
    [
        new Position(0, 0),
        new Position(1, 0),
        new Position(1, 1),
        new Position(0, 1),
        new Position(0, 0),
    ]
);

// Turfano -> NTS
NetTopologySuite.Geometries.Geometry geometriaNts = NtsConvert.ToNts(quadrado);

// NTS -> Turfano
Turfano.GeoJson.Geometry deVoltaAoTurfano = NtsConvert.FromNts(geometriaNts);

// Coordenadas isoladas também convertem
NetTopologySuite.Geometries.Coordinate coordenada = NtsConvert.ToNts(new Position(-122.4194, 37.7749));
Position posicao = NtsConvert.FromNts(coordenada);
```

A altitude passa intacta pela ordenada `Z` do NTS: uma `Position` com `Alt` definido
vira uma `CoordinateZ`, e uma `Coordinate` com `Z` igual a `NaN` volta como uma
`Position` com `Alt: null`.

Por baixo dos panos, a conversão é cuidadosa quanto à alocação na fronteira: as
coordenadas passam pelas sequências *empacotadas* do NTS
(`PackedDoubleCoordinateSequence`) em vez de materializar um objeto `Coordinate` por
vértice, com um caminho rápido que lê o array interno da sequência empacotada
diretamente ao converter uma geometria de volta.

## `Geo.Buffer`

`Buffer` é um método de extensão sobre `Turfano.GeoJson.Geometry`, adicionado pelo
pacote satélite (ele precisa do NTS, então não pode viver no núcleo livre de
dependências):

```csharp
using Turfano.GeoJson;
using Turfano.NetTopologySuite;

Geometry? comBuffer = quadrado.Buffer(Turfano.Units.Length.FromKilometers(10)); // @turf/buffer
```

Ele reproduz a abordagem do `@turf/buffer`: projeta a geometria em um plano
azimutal-equidistante centrado nela mesma (via `Geo.Distance`/`Geo.Bearing`/
`Geo.Destination`, então a própria projeção não precisa de NTS), roda o buffer planar
do NTS em metros sobre a forma projetada, e depois desprojeta de volta para
longitude/latitude. O resultado é `null` quando o buffer produz geometria vazia (por
exemplo, um buffer negativo grande o suficiente para apagar a entrada por completo).

## Mantendo a fronteira estreita

Como o `Geo` do `Turfano` é uma `public static partial class Geo`, e classes parciais
não atravessam assemblies, o satélite não pode adicionar seus próprios métodos
`Geo.*` — por isso `Buffer` é exposto como método de extensão em vez de
`Geo.Buffer(geometria, raio)`. Na prática, isso mantém a direção da dependência
honesta: tudo o que precisa de NTS fica visivelmente marcado (`.Buffer(...)`,
`NtsConvert.*`), e todo o resto continua funcionando com zero NTS no classpath.

## Para onde ir agora

- [Introdução](introducao.md) — instalação e primeiras chamadas com o pacote principal.
- [Conceitos](conceitos.md) — a fachada `Geo`, os tipos GeoJSON do Turfano e as
  unidades tipadas.
- [Referência de API](../api/Turfano.NetTopologySuite.NtsConvert.yml) — referência
  completa de `NtsConvert` e `NtsGeometryExtensions`.
