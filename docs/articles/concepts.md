# Concepts

This page explains the four ideas that shape every corner of Turfano: the `Geo` facade,
Turfano's own GeoJSON types, its typed units, and how all of that relates back to TurfJS.

## The `Geo` facade

`Turfano.GeoJson.Geo` is a single `public static partial class` with one method per
`@turf/*` function — `Geo.Distance`, `Geo.Bearing`, `Geo.Area`, `Geo.Union`,
`Geo.Voronoi`, `Geo.ClustersKmeans`, `Geo.Isolines`, and so on. There is no per-function
class or namespace to hunt through: if TurfJS has `turf.distance(a, b)`, Turfano has
`Geo.Distance(a, b)`, with the argument order kept as close to the original as C#
overloads allow.

Internally the class is split across many partial files (one file per ported function,
under `src/Turfano/Parity/`), but that is purely an organizational detail — from the
caller's side it is one type, one entry point, discoverable via IntelliSense by typing
`Geo.` and reading the XML doc summaries, which cite the originating `@turf` package
(e.g. `// @turf/buffer`) so you can cross-reference the reference implementation.

`Geo` also exposes the `@turf/helpers` constructors (`Geo.Point`, `Geo.Polygon`,
`Geo.LineString`, `Geo.Feature`, `Geo.FeatureCollection`, ...) and the
`@turf/invariant` accessors (`Geo.GetGeoJsonType`, `Geo.GetGeom`, `Geo.GetCoord`), for
the same reason TurfJS keeps them next to everything else: they're the first calls you
make before doing any real analysis.

## Turfano's own GeoJSON types

TurfJS operates on plain GeoJSON objects because JavaScript doesn't have a strong type
system to model RFC 7946 (Point, LineString, Polygon, MultiPoint, MultiLineString,
MultiPolygon, GeometryCollection, Feature, FeatureCollection). Turfano models the same
shapes as immutable C# records under `Turfano.GeoJson`:

```csharp
public readonly record struct Position(double Lon, double Lat, double? Alt = null);

public sealed record Point(Position Coordinates) : Geometry;
public sealed record Polygon(Position[][] Coordinates) : Geometry;
// ... LineString, MultiPoint, MultiLineString, MultiPolygon, GeometryCollection
public sealed record Feature(Geometry? Geometry, JsonObject? Properties) : GeoJsonObject;
public sealed record FeatureCollection(Feature[] Features) : GeoJsonObject;
```

Deliberate design choices worth knowing:

- **`Position` is a value-type record struct** (`Lon`, `Lat`, optional `Alt`) — cheap to
  copy, cheap to compare, no allocation on hot paths that iterate coordinate arrays.
- **Geometries are immutable records**, so equality and `with`-expressions work the way
  you'd expect, and there is no shared mutable state to worry about when the same
  geometry is passed into multiple `Geo` calls.
- **No custom types "just because"**: `Geometry` and `GeoJsonObject` are abstract records
  that mirror the RFC 7946 type hierarchy exactly, so a `Polygon` really is-a `Geometry`
  really is-a `GeoJsonObject`, and pattern matching (`geometry switch { Polygon p => ...,
  MultiPolygon mp => ..., _ => ... }`) reads like the spec.
- **Serialization is `System.Text.Json` source-generated and AOT-safe.** The RFC 7946
  `type` discriminator is wired up with `[JsonPolymorphic]`/`[JsonDerivedType]` on
  `GeoJsonObject`/`Geometry`, coordinate ordinals are pinned with `[JsonPropertyName]` so
  they always emit `"coordinates"`, `"type"`, `"properties"`, etc. regardless of a
  consumer's naming policy, and a single custom `Feature[]` converter fills the one gap
  the built-in polymorphism doesn't reach. None of this depends on runtime reflection,
  so it works unmodified when the app is trimmed or published as Native AOT.
- **`BBox`** wraps the RFC 7946 bounding box array (4 values for 2D, 6 for 3D) and
  serializes as a plain JSON array, matching `@turf`'s `bbox` handling.

## Typed units instead of raw numbers

TurfJS functions that take a distance, area, or angle accept a plain `number` plus a
`units` string (`"kilometers"`, `"miles"`, `"degrees"`, ...) and it's on you to remember
which unit a given value is in. Turfano replaces that with three small, immutable value
types under `Turfano.Units`: `Length`, `Area`, and `Angle`.

```csharp
Turfano.Units.Length radius = Turfano.Units.Length.FromKilometers(50);
Console.WriteLine(radius.Miles); // read out in whatever unit you need

var area = Geo.Area(polygon); // Turfano.Units.Area
Console.WriteLine(area.SquareKilometers);
```

Each type stores a `Value` and a `Unit` and exposes `From*` factory methods plus `.As(unit)`
and per-unit read-only properties (`.Meters`, `.Kilometers`, `.SquareMeters`, `.Degrees`,
`.Radians`, ...), with conversion factors that intentionally match `@turf/helpers`
exactly (down to `earthRadius = 6371008.8` meters) — a `Length` round-tripped through
Turfano converts to the same numbers `@turf/helpers`' `convertLength` would produce.
Arithmetic operators (`+`, `-`, `*`, `/`) are defined so you can accumulate lengths (as
`Geo.Length` does when summing a `LineString`'s segments) without manually normalizing
units first.

This is a deliberate trade against reusing a general-purpose quantities library: Turfano
only ever needs three quantities, so it defines exactly those three, with zero
additional package weight and TurfJS-identical conversion tables.

## Relationship to TurfJS

Turfano is a *faithful port*, not a reimagining. For each `@turf/*` package covered, the
implementation is checked line-by-line against the actual TypeScript source vendored
under `reference/` in the repository — same algorithm, same edge cases (empty inputs,
degenerate polygons, antimeridian handling where TurfJS handles it), same expected
numeric outputs. The test suite frequently embeds the equivalent TurfJS snippet and its
expected output directly in a comment, specifically to pin that parity over time.

What Turfano does *not* do is expand the surface beyond what TurfJS offers, or bolt on
unrelated abstractions — the goal (see the project README) is to expose the functions
that complement geometry libraries in the .NET ecosystem, under names and argument
orders you'd recognize immediately if you already know `@turf`.

## Where to next

- [Getting Started](getting-started.md) — install and first calls, if you haven't yet.
- [NetTopologySuite interop](nts-interop.md) — bridging to NTS geometries and using
  `Geo.Buffer`.
- [API Reference](../api/Turfano.GeoJson.Geo.yml) — the full `Geo` method list.
