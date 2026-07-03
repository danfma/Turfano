# Turfano

[![CI](https://github.com/danfma/Turfano/actions/workflows/ci.yml/badge.svg)](https://github.com/danfma/Turfano/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Turfano.svg?label=Turfano)](https://www.nuget.org/packages/Turfano/)
[![NuGet](https://img.shields.io/nuget/vpre/Turfano.NetTopologySuite.svg?label=Turfano.NetTopologySuite)](https://www.nuget.org/packages/Turfano.NetTopologySuite/)

A faithful .NET port of [TurfJS](https://turfjs.org) — the geospatial analysis library —
with its own GeoJSON types, its own typed quantities, and **zero external dependencies**
in the core package.

## Packages

- **`Turfano`** (core): the `Geo` facade (namespace `Turfano.GeoJson`), a faithful port of
  the `@turf/*` function index — `Distance`, `Bearing`, `Area`, `Union`, `Buffer`
  (via satellite), `Voronoi`, `Isolines`, and dozens more — over its own GeoJSON record
  types (`Point`, `Polygon`, `Position`, ...) and its own typed quantities
  (`Turfano.Units.Length`/`Area`/`Angle`). No NetTopologySuite, no UnitsNet — AOT and
  trimming friendly.
- **`Turfano.NetTopologySuite`** (satellite): bridges Turfano's GeoJSON types to
  [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite)'s via
  `NtsConvert.ToNts`/`FromNts` (a zero-`Coordinate`, packed-sequence boundary), plus a
  `Buffer` extension implemented on top of NTS (the one operation that genuinely needs a
  full planar geometry engine).

## Why a from-scratch port instead of building on NetTopologySuite?

NTS is a superb topology engine, but TurfJS's function surface and ergonomics
(`bearing`, `destination`, `along`, `midpoint`, `area`, `union`, `voronoi`,
`isolines`/`isobands`, ...) don't map cleanly onto it, and porting *on top of* NTS meant
carrying NTS + UnitsNet as mandatory dependencies just to get TurfJS-shaped APIs. Turfano
now ports each `@turf` function faithfully — same algorithm, same edge cases, same
expected outputs, checked against the real TurfJS sources in `reference/` — directly over
plain GeoJSON types you can serialize as-is.

## Quick start

```csharp
using Turfano.GeoJson;

var origin = new Position(-122.4194, 37.7749);
var destination = new Position(-122.4094, 37.7849);

var distance = Geo.Distance(origin, destination); // Turfano.Units.Length
Console.WriteLine($"{distance.Kilometers:F3} km");

var square = Geo.Polygon(
    [new Position(0, 0), new Position(1, 0), new Position(1, 1), new Position(0, 1), new Position(0, 0)]
);
var area = Geo.Area(square); // Turfano.Units.Area
Console.WriteLine($"{area.SquareMeters:F1} m²");

var overlappingSquare = Geo.Polygon(
    [new Position(0.5, 0.5), new Position(1.5, 0.5), new Position(1.5, 1.5), new Position(0.5, 1.5), new Position(0.5, 0.5)]
);
var union = Geo.Union(square, overlappingSquare); // Turfano.GeoJson.Geometry?
```

## Interop with NetTopologySuite

Need actual NTS geometries — to interoperate with an NTS-based stack, or to run
`Buffer` — add the `Turfano.NetTopologySuite` package:

```csharp
using Turfano.NetTopologySuite;

var buffered = square.Buffer(Turfano.Units.Length.FromKilometers(10)); // @turf/buffer

var ntsGeometry = NtsConvert.ToNts(square);
var backToTurfano = NtsConvert.FromNts(ntsGeometry);
```

## Status

Pre-1.0 (`1.0.0-rc.1`). The `Geo` facade covers the full `@turf` function index; the API
may still evolve before `1.0.0`. Targets `net8.0`, `net9.0` and `net10.0`.

## Third-party attributions

Turfano ports algorithms from TurfJS, polyclip-ts, splaytree-ts, earcut and d3-voronoi.
See [`NOTICE`](NOTICE) for the full attributions and licenses.

## Development

See [`CLAUDE.md`](CLAUDE.md) for build/test commands and repository conventions.
