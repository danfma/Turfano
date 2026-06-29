# Data Model: Sistema de tipos GeoJSON (Fase 1)

Entidades = os tipos da fundação. Regras do RFC 7946.

## Position (struct de valor)
| Campo | Tipo | Regra |
|---|---|---|
| `Lon` | double | longitude |
| `Lat` | double | latitude |
| `Alt` | double? | altitude opcional (preservar dimensão 2D/3D) |

Serializa como array `[Lon, Lat]` ou `[Lon, Lat, Alt]`.

## BBox (struct de valor)
2D `[west, south, east, north]` ou 3D `[west, south, minAlt, east, north, maxAlt]`.
Serializa como array.

## GeoJsonObject (base abstrata polimórfica — discriminador `type`)
Derivados (valor do `type`): `Point`, `MultiPoint`, `LineString`, `MultiLineString`,
`Polygon`, `MultiPolygon`, `GeometryCollection`, `Feature`, `FeatureCollection`.
Campo comum opcional: `BBox? Bbox`.

## Geometry (abstrata : GeoJsonObject) e geometrias selladas
| Tipo | `coordinates` (RFC 7946) |
|---|---|
| `Point` | `Position` |
| `MultiPoint` | `Position[]` |
| `LineString` | `Position[]` (≥2) |
| `MultiLineString` | `Position[][]` |
| `Polygon` | `Position[][]` (anéis; 1º exterior; cada anel fechado, ≥4) |
| `MultiPolygon` | `Position[][][]` |
| `GeometryCollection` | `Geometry[]` (campo `geometries`) |

## Feature (sellada : GeoJsonObject)
| Campo | Tipo | Regra |
|---|---|---|
| `Id` | string\|número? | RFC 7946 permite ambos; opcional |
| `Geometry` | `Geometry?` | pode ser null |
| `Properties` | `JsonObject?` (default) | + variante `Feature<TProps>` |
| `Bbox` | `BBox?` | opcional |

## FeatureCollection (sellada : GeoJsonObject)
| Campo | Tipo |
|---|---|
| `Features` | `Feature[]` |
| `Bbox` | `BBox?` |

## Unidades (3 structs de valor)
| Struct | Unidades (enum, estilo Turf) | Operações |
|---|---|---|
| Comprimento/Distância | Kilometers, Meters, Miles, NauticalMiles, Feet, Inches, Yards, Centimeters, Degrees, Radians | `From*`/`As*`, operadores, `+ - * /` |
| Ângulo/Rumo | Degrees, Radians | `From*`/`As*`, `bearingToAzimuth`, normalização |
| Área | SquareMeters, SquareKilometers, Acres, SquareMiles, SquareFeet, SquareYards, Hectares | `From*`/`As*` |

Conversões batem com `@turf` (`convertLength`/`convertArea`/`*ToRadians`/`*ToDegrees`).

## Invariantes
- Round-trip preserva `type`, `coordinates`, `properties`, `bbox`, `id` (RFC 7946).
- `Position`/`BBox` preservam dimensão (2D vs 3D).
- Ponte interna NTS: `FromNts(ToNts(g))` preserva coordenadas.
