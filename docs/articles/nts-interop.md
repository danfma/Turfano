# NetTopologySuite interop

The core `Turfano` package is intentionally dependency-free: it defines its own GeoJSON
types and its own `Length`/`Area`/`Angle` units instead of pulling in NetTopologySuite or
UnitsNet, so it stays lightweight and Native AOT / trimming friendly. Most applications
never need anything beyond it.

The `Turfano.NetTopologySuite` satellite package exists for the two situations where you
do need [NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite) (NTS):

1. Your application already stores or queries geometries as NTS types — for example, EF
   Core spatial columns, or another library that speaks NTS — and you need to move data
   between that world and Turfano's.
2. You need `Geo.Buffer` (`@turf/buffer`), the one Turfano operation that genuinely
   requires a full planar geometry engine to expand or contract a shape by a radius; it
   is implemented on top of NTS's buffering and lives in the satellite for that reason.

Everything else in `Geo` works on plain Turfano geometries with no NTS dependency at
all — treat this package as strictly opt-in.

## Install

```bash
dotnet add package Turfano.NetTopologySuite
```

## Converting between Turfano and NTS: `NtsConvert`

`NtsConvert` (namespace `Turfano.NetTopologySuite`) converts both ways, at both the
coordinate level and the geometry level:

```csharp
using Turfano.GeoJson;
using Turfano.NetTopologySuite;

Polygon square = Geo.Polygon(
    [
        new Position(0, 0),
        new Position(1, 0),
        new Position(1, 1),
        new Position(0, 1),
        new Position(0, 0),
    ]
);

// Turfano -> NTS
NetTopologySuite.Geometries.Geometry ntsGeometry = NtsConvert.ToNts(square);

// NTS -> Turfano
Turfano.GeoJson.Geometry backToTurfano = NtsConvert.FromNts(ntsGeometry);

// Single coordinates convert too
NetTopologySuite.Geometries.Coordinate coordinate = NtsConvert.ToNts(new Position(-122.4194, 37.7749));
Position position = NtsConvert.FromNts(coordinate);
```

Altitude round-trips through NTS `Z` ordinates: a `Position` with `Alt` set becomes a
`CoordinateZ`, and a `Coordinate` with a `NaN` `Z` converts back to a `Position` with
`Alt: null`.

Under the hood the conversion is boundary-conscious about allocation: coordinates move
through NTS's *packed* coordinate sequences (`PackedDoubleCoordinateSequence`) rather
than materializing one `Coordinate` object per vertex, with a fast path that reads the
packed sequence's internal array directly when reading a geometry back out.

## `Geo.Buffer`

`Buffer` is an extension method on `Turfano.GeoJson.Geometry`, added by the satellite
package (it needs NTS, so it can't live in the dependency-free core):

```csharp
using Turfano.GeoJson;
using Turfano.NetTopologySuite;

Geometry? buffered = square.Buffer(Turfano.Units.Length.FromKilometers(10)); // @turf/buffer
```

It mirrors `@turf/buffer`'s approach: project the geometry onto an azimuthal-equidistant
plane centered on the geometry (via `Geo.Distance`/`Geo.Bearing`/`Geo.Destination`, so the
projection itself needs no NTS), run NTS's planar buffer in meters on the projected
shape, then unproject back to longitude/latitude. The result is `null` when buffering
produces empty geometry (for example, a large negative buffer that erases the input
entirely).

## Keep the boundary narrow

Because `Turfano`'s `Geo` is `public static partial class Geo`, and partial classes
cannot span assemblies, the satellite cannot add its own `Geo.*` methods — that's why
`Buffer` is exposed as an extension method rather than `Geo.Buffer(geometry, radius)`.
In practice this keeps the dependency direction honest: everything that needs NTS is
visibly marked (`.Buffer(...)`, `NtsConvert.*`) and everything else keeps working with
zero NTS on the classpath.

## Where to next

- [Getting Started](getting-started.md) — install and first calls with the core package.
- [Concepts](concepts.md) — the `Geo` facade, Turfano's GeoJSON types, and typed units.
- [API Reference](../api/Turfano.NetTopologySuite.NtsConvert.yml) — full `NtsConvert` and
  `NtsGeometryExtensions` reference.
