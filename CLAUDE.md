# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Turfano is a .NET port of the [TurfJS](https://turfjs.org) geospatial library (formerly named `DotTerritory`).
It deliberately does **not** reimplement geometry from scratch: it builds on
[NetTopologySuite](https://github.com/NetTopologySuite/NetTopologySuite) for geometry types and
[UnitsNet](https://github.com/angularsen/UnitsNet) for typed quantities (`Area`, `Length`, `Angle`),
so units are explicit and safe. The goal (see `README.md`) is to cover the TurfJS functions that
*complement* NetTopologySuite and expose them with TurfJS-style, discoverable signatures — not to copy
all of TurfJS. It is currently a pre-1.0 toy project (`<Version>0.8.0</Version>`).

## Solution layout

The solution is `Turfano.slnx` (the XML-based solution format — there is no `.sln`). The .NET SDK is
pinned by `global.json` to `10.0.301` (`rollForward: latestMinor`).

- `src/Turfano` — the library. Multi-targets `net8.0;net9.0;net10.0`, packable as a NuGet package.
- `tests/Turfano.Tests` — TUnit test suite (an `Exe` on `net10.0`).
- `samples/Turfano.Playground` — console scratchpad for trying the API.
- `benchmark/TimeAndMemoryUsage` — BenchmarkDotNet benchmarks (run in Release).
- `reference/` — the actual TurfJS (`@turf`) TypeScript source, vendored as a **porting reference**. It is a
  Bun project, not part of the .NET build. When porting a function, read its TurfJS implementation here.

## Architecture

- **One public static partial class `Turf` (namespace `Turfano`), one file per TurfJS function.** Files are
  named `Turf.<Feature>.cs` (e.g. `Turf.Area.cs`, `Turf.Distance.cs`, `Turf.BooleanPointInPolygon.cs`).
  ~72 partial files compose the single `Turf` class. To add a function, add a new `Turf.<Name>.cs` partial.
- **Entry point is the function name**, mirroring TurfJS: `Turf.Area(geometry)`, `Turf.Distance(a, b)`,
  `Turf.Destination(origin, distance, bearing)`. Keep names and argument order close to TurfJS.
- **Geometry types come from NetTopologySuite** (`Polygon`, `LinearRing`, `Coordinate`, `Geometry`,
  `IFeature`, `FeatureCollection`) — reuse them, do not introduce parallel types.
- **Quantities come from UnitsNet.** Because some `Turf` methods share a name with a UnitsNet type
  (e.g. `Turf.Area(...)` returns `UnitsNet.Area`), disambiguate with the fully-qualified `UnitsNet.Area`
  inside those files (see `Turf.Area.cs`).
- `Turf.Configuration.cs` holds mutable global config such as `EarthRadius` (a `Length`).
- `TerritoryUtils` (in `TerritoryUtils.cs`) is an internal helper used by the marching-squares functions
  (`Turf.Isolines.cs`, `Turf.Isobands.cs`). Note it still carries the legacy "Territory" name.
- `GlobalUsings.cs` globally imports `NetTopologySuite.Features`, `NetTopologySuite.Geometries`, `UnitsNet`,
  and `UnitsNet.Units` — do not re-`using` these in individual files.

## Build & test commands

```bash
# Build everything (or a single project)
dotnet build
dotnet build src/Turfano/Turfano.csproj

# Run the full test suite (TUnit runs on Microsoft.Testing.Platform; the test project is an Exe)
dotnet run --project tests/Turfano.Tests

# Run a single test class or method — TUnit uses --treenode-filter, NOT xUnit's --filter:
#   pattern is /Assembly/Namespace/Class/Method, with * as a wildcard
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/AreaTest/*"
dotnet run --project tests/Turfano.Tests -- --treenode-filter "/*/*/AreaTest/AreaCalculationShouldMatchTurfArea"

# Playground and benchmarks
dotnet run --project samples/Turfano.Playground
dotnet run --project benchmark/TimeAndMemoryUsage -c Release
```

`dotnet test` also works, but the project is built to run as an executable, so `dotnet run` is the primary path.

## Conventions

- Nullable reference types and `ImplicitUsings` are enabled. No explicit `LangVersion` is set, so the C#
  version defaults to each target framework's SDK default.
- Match TurfJS method signatures and naming where reasonable; prefer pure functions and immutable data.
- **Evite acrônimos crípticos** em nomes (variáveis, métodos, propriedades). Prefira nomes
  por extenso e legíveis (ex.: `RadiansPerDegree` em vez de `D2R`, `RightTangent` em vez de
  `Rtan`). Nomes de domínio bem conhecidos podem ser mantidos (ex.: `BBox`, `lon`/`lat`, e a
  notação geodésica `phi`/`lambda` das fórmulas de rumo).
- XML doc comments on public methods.
- Tests use **TUnit** (not xUnit/Shouldly). The pattern is `[Test] public async Task` with
  `await Assert.That(actual).IsEqualTo(expected).Within(tolerance)`. Tests commonly embed the equivalent
  TurfJS snippet and its expected numeric output in a comment to pin parity with TurfJS — preserve this habit.
- The `reference/` project follows a "use Bun, not Node/npm" rule (`reference/.cursor/rules/`): use
  `bun install`, `bun run <file>`, `bun test`.

<!-- SPECKIT START -->
Plano da feature ativa: `specs/006-parity-transformation/plan.md` (Fase 6 / Onda C —
Transformation & Mutation; ver o plano-mãe `plans/turfjs-parity-redesign.md`; Fases 1–5 já
na `main`, incl. as Ondas A — measurement e B — booleans, na fachada `Geo`). Consulte esse
plano e os artefatos da feature (`spec.md`, `research.md`, `data-model.md`, `contracts/`,
`quickstart.md`) para contexto.
<!-- SPECKIT END -->
