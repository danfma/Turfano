# Data Model: Onda D — Feature Conversion, Joins & Meta (Fase 1)

Sem entidades de dados novas — a onda adiciona **funções** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) e `Turfano.Units.*` na fachada `Geo`. Mapa função → assinatura (esboço):

| Categoria | Função | Assinatura (esboço) | Retorno |
|---|---|---|---|
| Conversão | Explode | `Geo.Explode(Geometry g)` | `FeatureCollection` (de Point) |
| Conversão | Flatten | `Geo.Flatten(Geometry g)` | `FeatureCollection` |
| Conversão | Combine | `Geo.Combine(FeatureCollection fc)` | `FeatureCollection` (Multi*) |
| Conversão | PolygonToLine | `Geo.PolygonToLine(Geometry poly)` | `LineString`/`MultiLineString` |
| Conversão | LineToPolygon | `Geo.LineToPolygon(Geometry line)` | `Polygon`/`MultiPolygon` |
| Conversão | Polygonize | `Geo.Polygonize(Geometry lines)` | `FeatureCollection` (de Polygon) |
| Joins | PointsWithinPolygon | `Geo.PointsWithinPolygon(FeatureCollection points, Geometry polygon)` | `FeatureCollection` |
| Joins | Tag | `Geo.Tag(FeatureCollection points, FeatureCollection polygons, string field, string outField)` | `FeatureCollection` |
| Misc | NearestPoint | `Geo.NearestPoint(Point target, FeatureCollection points)` | `Feature` (de Point) |
| Misc | LineSlice | `Geo.LineSlice(Point start, Point stop, LineString line)` | `LineString` |
| Misc | LineSliceAlong | `Geo.LineSliceAlong(LineString line, Units.Length start, Units.Length stop)` | `LineString` |
| Misc | LineChunk | `Geo.LineChunk(LineString line, Units.Length length)` | `FeatureCollection` (de LineString) |
| Misc | Kinks | `Geo.Kinks(Geometry g)` | `FeatureCollection` (de Point) |
| Meta | CoordEach | `Geo.CoordEach(Geometry g, Action<Position,int,...> cb, bool excludeWrapCoord = false)` | void |
| Meta | CoordReduce | `Geo.CoordReduce<T>(Geometry g, Func<...> cb, T initial)` | `T` |
| Meta | FeatureEach | `Geo.FeatureEach(FeatureCollection fc, Action<Feature,int> cb)` | void |
| Meta | GeomEach | `Geo.GeomEach(GeoJsonObject g, Action<Geometry,...> cb)` | void |
| Meta | PropEach | `Geo.PropEach(FeatureCollection fc, Action<JsonObject,int> cb)` | void |
| Meta | SegmentEach | `Geo.SegmentEach(Geometry g, Action<(Position,Position),...> cb)` | void |
| Meta | FlattenEach | `Geo.FlattenEach(GeoJsonObject g, Action<Geometry,...> cb)` | void |

**Regras/invariantes**
- Cada função bate com o `@turf` (FR-002).
- Meta: ordem e índices iguais aos do `@turf` (FR-003, SC-002).
- `PointsWithinPolygon` respeita a fronteira (FR-004, SC-003).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units`; nomes .NET (FR-005).
