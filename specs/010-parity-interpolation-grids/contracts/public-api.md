# Public API Contract: Onda F — Interpolation, Grids & Triangulation

**Data**: 2026-07-02 | Namespace: `Turfano.GeoJson` (fachada `Geo`, partials)

Assinaturas espelham o `@turf` (nome/ordem), tipos da Fase 3 + `Turfano.Units`.

## US1 — Grades

```csharp
// @turf/point-grid — pontos no centro de células quadradas; mask opcional (within)
public static FeatureCollection PointGrid(BBox bbox, Length cellSide, Geometry? mask = null);

// @turf/square-grid (= rectangleGrid(cellSide, cellSide)); mask por intersects
public static FeatureCollection SquareGrid(BBox bbox, Length cellSide, Geometry? mask = null);

// @turf/rectangle-grid — células retangulares
public static FeatureCollection RectangleGrid(BBox bbox, Length cellWidth, Length cellHeight, Geometry? mask = null);

// @turf/hex-grid — hexágonos (ou triângulos com triangles: true); mask por intersect
public static FeatureCollection HexGrid(BBox bbox, Length cellSide, Geometry? mask = null, bool triangles = false);

// @turf/triangle-grid
public static FeatureCollection TriangleGrid(BBox bbox, Length cellSide, Geometry? mask = null);
```

## US2 — Interpolação

```csharp
// @turf/planepoint — valor interpolado no plano do triângulo (props a/b/c ou 3ª coord)
public static double Planepoint(Point point, Feature triangle);

// @turf/tin — Delaunay; z de properties[z] (ou 3ª coordenada quando z == null)
public static FeatureCollection Tin(FeatureCollection points, string? z = null);

// @turf/interpolate — IDW sobre grade (gridType: square|point|hex|triangle)
public static FeatureCollection Interpolate(
    FeatureCollection points, Length cellSize,
    string gridType = "square", string property = "elevation", double weight = 1);
```

## US3 — Contornos

```csharp
// @turf/isolines — multilinhas por break
public static FeatureCollection Isolines(
    FeatureCollection pointGrid, double[] breaks, string zProperty = "elevation");

// @turf/isobands — multipolígonos por faixa [breaks[i], breaks[i+1]]
public static FeatureCollection Isobands(
    FeatureCollection pointGrid, double[] breaks, string zProperty = "elevation");
```

(`commonProperties`/`breaksProperties` do `@turf` ficam de fora nesta onda — o chamador
compõe propriedades no resultado; registrado como limitação consciente.)

## US4 — Hulls e tesselação

```csharp
// @turf/convex — hull convexo (concaveman com concavity=Infinity); null se degenerado
public static Polygon? Convex(Geometry geojson);
public static Polygon? Convex(FeatureCollection features);

// @turf/concave — tin + maxEdge + união; null quando não há solução
public static Geometry? Concave(FeatureCollection points, Length maxEdge);

// @turf/voronoi — células na ordem dos sites (d3-voronoi)
public static FeatureCollection Voronoi(FeatureCollection points, BBox bbox);

// @turf/tesselate — triângulos do earcut
public static FeatureCollection Tesselate(Polygon polygon);
```

## Invariantes

- Saídas `FeatureCollection` com `properties` `JsonObject` (valores `z`, `a/b/c` etc. como
  no `@turf`).
- `Parity/` livre de NTS; legado intocado; nomes sem acrônimos crípticos.
