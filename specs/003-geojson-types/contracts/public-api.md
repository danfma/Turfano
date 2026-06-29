# Contrato: API pública dos tipos (Fase 3)

Superfície pública nova (namespace `Turfano`). A fachada `Turf` atual é preservada.

## Tipos (esboço de assinatura)

```csharp
public readonly record struct Position(double Lon, double Lat, double? Alt = null);
public readonly record struct BBox(/* 2D/3D */);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Point), "Point")]
[JsonDerivedType(typeof(LineString), "LineString")]
[JsonDerivedType(typeof(Polygon), "Polygon")]
[JsonDerivedType(typeof(MultiPoint), "MultiPoint")]
[JsonDerivedType(typeof(MultiLineString), "MultiLineString")]
[JsonDerivedType(typeof(MultiPolygon), "MultiPolygon")]
[JsonDerivedType(typeof(GeometryCollection), "GeometryCollection")]
[JsonDerivedType(typeof(Feature), "Feature")]
[JsonDerivedType(typeof(FeatureCollection), "FeatureCollection")]
public abstract record GeoJsonObject { public BBox? Bbox { get; init; } }

public abstract record Geometry : GeoJsonObject;
public sealed record Point(Position Coordinates) : Geometry;
public sealed record Polygon(Position[][] Coordinates) : Geometry;
// ... demais geometrias ...
public sealed record Feature(Geometry? Geometry, JsonObject? Properties = null) : GeoJsonObject
{ public object? Id { get; init; } }
public sealed record FeatureCollection(Feature[] Features) : GeoJsonObject;
```

## Serialização (forma de fio — RFC 7946)

- Geometria: `{ "type": "<Tipo>", "coordinates": <arrays> [, "bbox": [...]] }`.
- `GeometryCollection`: `{ "type": "GeometryCollection", "geometries": [...] }`.
- `Feature`: `{ "type": "Feature", "id"?, "geometry": <geom|null>, "properties": <obj|null> [, "bbox"] }`.
- `FeatureCollection`: `{ "type": "FeatureCollection", "features": [...] [, "bbox"] }`.
- `Position` → array `[lon, lat]`/`[lon, lat, alt]`; `BBox` → array de 4/6 números.
- Via `GeoJsonSerializerContext` (source-generated); sem reflexão.

## Helpers/factory (estilo Turf)

```csharp
Point point(double lon, double lat, ...);
LineString lineString(IEnumerable<Position> coords, ...);
Polygon polygon(Position[][] rings, ...);
Feature feature(Geometry? geom, JsonObject? props = null, ...);
FeatureCollection featureCollection(IEnumerable<Feature> features, ...);
// multiPoint/multiLineString/multiPolygon/geometryCollection ...
Position getCoord(...); Position[] getCoords(...); string getType(...); Geometry? getGeom(...);
```

## Unidades (3 structs) — esboço

```csharp
public readonly record struct Length(double Value, LengthUnit Unit) { /* From*/As*, operadores */ }
public readonly record struct Angle(...);
public readonly record struct Area(...);
public enum LengthUnit { Kilometers, Meters, Miles, ... , Degrees, Radians }
// + conversões: ConvertLength/ConvertArea/LengthToRadians/RadiansToLength/...
```

## Ponte interna (NÃO pública)

```csharp
internal static class NtsBridge {
    internal static NetTopologySuite.Geometries.Geometry ToNts(Geometry g);
    internal static Geometry FromNts(NetTopologySuite.Geometries.Geometry g);
}
```

## Invariantes de verificação
- Round-trip de fixtures = forma do TurfJS (SC-001).
- AOT/trimming sem warnings nos tipos do Turfano (SC-002).
- Conversões de unidade = `@turf` (SC-003).
- Suíte atual 156/0 e build net8/9/10 (SC-004). Ponte NTS round-trip exato (SC-005).
