# Data Model: Onda A — Measurement (Fase 1)

Sem entidades de dados novas — a onda adiciona **funções** sobre os tipos da Fase 3
(`Turfano.GeoJson.*`) e `Turfano.Units.*`. Mapa função → assinatura (esboço):

| Função | Assinatura (esboço) | Retorno |
|---|---|---|
| Area | `Turf.Area(GeoJson.Geometry)` | `Units.Area` (esférica) |
| Bbox | `Turf.Bbox(GeoJson.Geometry)` | `GeoJson.BBox` |
| BboxPolygon | `Turf.BboxPolygon(GeoJson.BBox)` | `GeoJson.Polygon` |
| Square | `Turf.Square(GeoJson.BBox)` | `GeoJson.BBox` |
| Envelope | `Turf.Envelope(GeoJson.Geometry)` | `GeoJson.Polygon` |
| Distance | `Turf.Distance(GeoJson.Position a, b)` | `Units.Length` |
| Bearing | `Turf.Bearing(Position from, to, bool final=false)` | `Units.Angle` |
| Length | `Turf.Length(GeoJson.LineString\|MultiLineString)` | `Units.Length` |
| Destination | `Turf.Destination(Position origin, Units.Length, Units.Angle bearing)` | `GeoJson.Point` |
| Along | `Turf.Along(GeoJson.LineString, Units.Length)` | `GeoJson.Point` |
| Midpoint | `Turf.Midpoint(GeoJson.Point a, b)` | `GeoJson.Point` |
| Center | `Turf.Center(GeoJson.Geometry)` | `GeoJson.Point` |
| CenterOfMass | `Turf.CenterOfMass(GeoJson.Geometry)` | `GeoJson.Point` |
| **Centroid** | `Turf.Centroid(GeoJson.Geometry)` | `GeoJson.Point` (**exclui o vértice de fechamento**) |
| RhumbBearing | `Turf.RhumbBearing(Position from, to, bool final=false)` | `Units.Angle` |
| RhumbDistance | `Turf.RhumbDistance(Position from, to)` | `Units.Length` |
| RhumbDestination | `Turf.RhumbDestination(Position origin, Units.Length, Units.Angle)` | `GeoJson.Point` |
| PointToLineDistance | `Turf.PointToLineDistance(GeoJson.Point, GeoJson.LineString)` | `Units.Length` |
| PointToPolygonDistance | `Turf.PointToPolygonDistance(GeoJson.Point, GeoJson.Polygon\|MultiPolygon)` | `Units.Length` |
| NearestPointOnLine | `Turf.NearestPointOnLine(GeoJson.LineString, GeoJson.Point)` | `GeoJson.Point` (+ índice/distância) |
| PointOnFeature | `Turf.PointOnFeature(GeoJson.Geometry\|Feature)` | `GeoJson.Point` |
| GreatCircle | `Turf.GreatCircle(Position start, end, ...)` | `GeoJson.LineString` |
| PolygonTangents | `Turf.PolygonTangents(Position pt, GeoJson.Polygon)` | `(GeoJson.Point, GeoJson.Point)` |

Conversões de unidade (já em `Turfano.Units`, expor na fachada): `BearingToAzimuth`,
`ConvertLength`, `ConvertArea`, `DegreesToRadians`, `RadiansToDegrees`, `LengthToRadians`,
`RadiansToLength`, `LengthToDegrees`.

**Regras/invariantes**
- Cada função bate com o `@turf` (FR-002).
- `Centroid` exclui o vértice de fechamento (FR-003, SC-002).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units` (FR-001/SC-003).
