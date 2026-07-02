# Public API Contract: Onda G — Paridade total

**Data**: 2026-07-02 | Namespace: `Turfano.GeoJson` (fachada `Geo`, partials)

## US1 — Linhas

```csharp
public static FeatureCollection LineSegment(Geometry geojson);            // @turf/line-segment
public static FeatureCollection LineIntersect(Geometry line1, Geometry line2); // @turf/line-intersect
public static FeatureCollection LineOverlap(Geometry line1, Geometry line2, Length? tolerance = null); // @turf/line-overlap
public static FeatureCollection LineSplit(Feature line, Feature splitter); // @turf/line-split
public static LineString LineArc(Point center, Length radius, Angle bearing1, Angle bearing2, int steps = 64); // @turf/line-arc
public static Feature? ShortestPath(Point start, Point end, FeatureCollection? obstacles = null, Length? resolution = null); // @turf/shortest-path
public static Feature NearestPointToLine(FeatureCollection points, Geometry line); // @turf/nearest-point-to-line
public static double Angle(Position startPoint, Position midPoint, Position endPoint, bool explementary = false, bool mercator = false); // @turf/angle
```

## US2 — Formas e projeção

```csharp
public static Polygon Ellipse(Point center, Length xSemiAxis, Length ySemiAxis, Angle? angle = null, int steps = 64); // @turf/ellipse
public static Polygon Sector(Point center, Length radius, Angle bearing1, Angle bearing2, int steps = 64); // @turf/sector
public static Polygon Mask(Geometry polygon, Polygon? mask = null); // @turf/mask (motor polyclip nativo)
public static FeatureCollection UnkinkPolygon(Geometry polygon);    // @turf/unkink-polygon
public static Geometry ToMercator(Geometry geojson);                // @turf/projection
public static Geometry ToWgs84(Geometry geojson);
```

## US3 — Estatística espacial

```csharp
public static Feature CenterMean(FeatureCollection features, string? weightProperty = null);
public static Feature CenterMedian(FeatureCollection features, string? weightProperty = null, int counter = 10);
public static Feature DirectionalMean(FeatureCollection lines, bool planar = true, bool segment = false);
public static double[][] DistanceWeight(FeatureCollection points, double? threshold = null, double? bandwidth = null, ...);
public static MoranIndexResult MoranIndex(FeatureCollection points, string inputField, ...);
public static NearestNeighborResult NearestNeighborAnalysis(FeatureCollection dataset, ...);
public static QuadratAnalysisResult QuadratAnalysis(FeatureCollection dataset, ...);
public static Feature StandardDeviationalEllipse(FeatureCollection points, ...);
```

(Resultados compostos viram `record`s com os campos exatos do `@turf` — detalhados na
implementação a partir das fontes/GT.)

## US4 — Aleatórios, clusters e agregação

```csharp
public static Position RandomPosition(BBox? bbox = null);
public static FeatureCollection RandomPoint(int count = 1, BBox? bbox = null);
public static FeatureCollection RandomLineString(int count = 1, BBox? bbox = null, int numVertices = 10, double maxLength = 0.0001, double maxRotation = Math.PI / 8);
public static FeatureCollection RandomPolygon(int count = 1, BBox? bbox = null, int numVertices = 10, double maxRadialLength = 10);
public static FeatureCollection Sample(FeatureCollection features, int num);
public static FeatureCollection ClustersKmeans(FeatureCollection points, int? numberOfClusters = null); // determinístico (R3)
public static FeatureCollection ClustersDbscan(FeatureCollection points, Length maxDistance, int minPoints = 3);
public static FeatureCollection GetCluster(FeatureCollection clustered, ...); // + ClusterEach/ClusterReduce
public static FeatureCollection Collect(FeatureCollection polygons, FeatureCollection points, string inProperty, string outProperty);
```

## Invariantes

- Propriedades de cluster/estatística em `JsonObject` com os MESMOS nomes do `@turf`
  (`cluster`, `centroid`, `dbscan`, etc.). `Parity/` sem NTS; nomes .NET.
