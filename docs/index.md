---
_layout: landing
---

# Turfano

**A faithful .NET port of [TurfJS](https://turfjs.org)** — the geospatial analysis
library — over its own GeoJSON types, with typed quantities and **zero external
dependencies** in the core package.

- **`Geo` facade** (namespace `Turfano.GeoJson`): a faithful port of the `@turf/*`
  function index — `Distance`, `Bearing`, `Area`, `Union`, `Voronoi`, `Isolines`,
  clustering, and dozens more — over its own GeoJSON record types and typed quantities
  (`Turfano.Units.Length`/`Area`/`Angle`).
- **AOT & trimming friendly**: no NetTopologySuite, no UnitsNet in the core.
- **`Turfano.NetTopologySuite`** (satellite): opt-in bridge to NTS geometry plus an
  NTS-backed `Buffer`.

## Where to next

- [Getting Started](articles/getting-started.md) — install and first calls.
- [Concepts](articles/concepts.md) — the `Geo` facade, own GeoJSON types, and units.
- [NetTopologySuite interop](articles/nts-interop.md) — the satellite package.
- [API Reference](api/Turfano.GeoJson.Geo.yml) — every public function and type.
- Documentação em **português**: [Introdução](articles-pt-br/introducao.md),
  [Conceitos](articles-pt-br/conceitos.md),
  [Interoperabilidade com NTS](articles-pt-br/interop-nts.md).
