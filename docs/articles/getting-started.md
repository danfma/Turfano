# Getting Started

Turfano is a faithful .NET port of [TurfJS](https://turfjs.org). This guide gets you from
zero to your first measurements and boolean operation.

## Install

Add the core package to your project:

```bash
dotnet add package Turfano
```

That is the only package you need for the vast majority of `@turf` functions. It has
**zero external dependencies** — no NetTopologySuite, no UnitsNet — which makes it a good
fit for trimmed and Native AOT deployments (`<IsAotCompatible>true</IsAotCompatible>`
in the package itself). Serialization is powered by `System.Text.Json` source generators,
so no reflection is required at runtime either.

If you also need to interoperate with [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite)
geometries, or need `Geo.Buffer` (which genuinely requires a planar geometry engine),
add the satellite package too — see [NetTopologySuite interop](nts-interop.md).

```bash
dotnet add package Turfano.NetTopologySuite
```

## The namespace

Everything you need for day-to-day use lives under `Turfano.GeoJson`:

```csharp
using Turfano.GeoJson;
```

This brings in the `Geo` facade (every `@turf` function, as a static method), the GeoJSON
record types (`Position`, `Point`, `Polygon`, `LineString`, `Feature`, `FeatureCollection`,
...), and `BBox`. Typed quantities (`Length`, `Area`, `Angle`) live in `Turfano.Units` and
are returned directly by the relevant `Geo` methods, so you rarely need to `using` that
namespace explicitly.

## Your first example

Create two positions and measure the distance and bearing between them — mirrors
`@turf/distance` and `@turf/bearing`:

```csharp
using Turfano.GeoJson;

var origin = new Position(-122.4194, 37.7749); // San Francisco
var destination = new Position(-122.4094, 37.7849);

var distance = Geo.Distance(origin, destination); // Turfano.Units.Length
var bearing = Geo.Bearing(origin, destination); // Turfano.Units.Angle

Console.WriteLine($"{distance.Kilometers:F3} km");
Console.WriteLine($"{bearing.Degrees:F1}°");
```

`Length` and `Angle` are typed quantities, not raw `double`s — you read them out in
whichever unit you need (`.Kilometers`, `.Miles`, `.Meters`, `.Degrees`, `.Radians`, ...)
without juggling conversion constants yourself.

## Building geometries and measuring area

Use the `Geo` constructors — they mirror `@turf/helpers` (`point`, `polygon`,
`lineString`, ...) — to build geometries, then measure them with `Geo.Area`
(`@turf/area`):

```csharp
var square = Geo.Polygon(
    [
        new Position(0, 0),
        new Position(1, 0),
        new Position(1, 1),
        new Position(0, 1),
        new Position(0, 0), // rings must close
    ]
);

var area = Geo.Area(square); // Turfano.Units.Area
Console.WriteLine($"{area.SquareMeters:F1} m²");
```

## A boolean operation: Union

Overlay operations (`Geo.Union`, `Geo.Intersect`, `Geo.Difference`) work directly on
`Geometry` values, no NetTopologySuite required:

```csharp
var overlappingSquare = Geo.Polygon(
    [
        new Position(0.5, 0.5),
        new Position(1.5, 0.5),
        new Position(1.5, 1.5),
        new Position(0.5, 1.5),
        new Position(0.5, 0.5),
    ]
);

Geometry? union = Geo.Union(square, overlappingSquare); // @turf/union
```

`union` is `null` only when the operation legitimately produces no geometry (for
example, unioning two empty inputs) — check TurfJS parity notes in the XML docs of each
function for the exact edge-case behavior.

## Serializing to GeoJSON

Because the GeoJSON types are plain `System.Text.Json`-annotated records, serializing a
`Feature` or `FeatureCollection` is a direct `JsonSerializer.Serialize` call; no custom
converters are required on your end (the RFC 7946 `type` discriminator and coordinate
array layout are already baked into the types themselves):

```csharp
using System.Text.Json;

var feature = Geo.Feature(square);
var json = JsonSerializer.Serialize<GeoJsonObject>(feature);
```

## Where to next

- [Concepts](concepts.md) — how the `Geo` facade, the GeoJSON types, and the typed units
  fit together, and how they relate to TurfJS.
- [NetTopologySuite interop](nts-interop.md) — when and how to use the satellite package.
- [API Reference](../api/Turfano.GeoJson.Geo.yml) — the full list of `Geo` methods.
