# Contrato: API pública das funções de measurement (Fase 1)

Novas sobrecargas na fachada `Turf` (namespace `Turfano`), recebendo `Turfano.GeoJson` e
devolvendo `Turfano.Units`/`Turfano.GeoJson`. As `Turf.*` NTS-based permanecem (overload
por tipo de parâmetro). Esboço:

```csharp
using GeoJson = Turfano.GeoJson;
using Units = Turfano.Units;

public static partial class Turf
{
    // medições escalares
    public static Units.Area Area(GeoJson.Geometry geometry);            // esférica
    public static GeoJson.BBox Bbox(GeoJson.Geometry geometry);
    public static GeoJson.Polygon BboxPolygon(GeoJson.BBox bbox);
    public static GeoJson.BBox Square(GeoJson.BBox bbox);
    public static GeoJson.Polygon Envelope(GeoJson.Geometry geometry);
    public static Units.Length Distance(GeoJson.Position from, GeoJson.Position to);
    public static Units.Angle Bearing(GeoJson.Position from, GeoJson.Position to, bool final = false);
    public static Units.Length Length(GeoJson.LineString line);

    // pontos derivados (centroid CONSERTADO)
    public static GeoJson.Point Centroid(GeoJson.Geometry geometry);     // exclui vértice de fechamento
    public static GeoJson.Point Center(GeoJson.Geometry geometry);
    public static GeoJson.Point CenterOfMass(GeoJson.Geometry geometry);
    public static GeoJson.Point Midpoint(GeoJson.Point a, GeoJson.Point b);
    public static GeoJson.Point Destination(GeoJson.Position origin, Units.Length distance, Units.Angle bearing);
    public static GeoJson.Point Along(GeoJson.LineString line, Units.Length distance);

    // rumo + distâncias a geometrias
    public static Units.Angle RhumbBearing(GeoJson.Position from, GeoJson.Position to, bool final = false);
    public static Units.Length RhumbDistance(GeoJson.Position from, GeoJson.Position to);
    public static GeoJson.Point RhumbDestination(GeoJson.Position origin, Units.Length distance, Units.Angle bearing);
    public static Units.Length PointToLineDistance(GeoJson.Point point, GeoJson.LineString line);
    public static Units.Length PointToPolygonDistance(GeoJson.Point point, GeoJson.Polygon polygon);
    public static GeoJson.Point PointOnFeature(GeoJson.Geometry geometry);
    public static GeoJson.LineString GreatCircle(GeoJson.Position start, GeoJson.Position end);
    // nearestPointOnLine, polygonTangents → tipos de retorno compostos (definir na impl)

    // conversões de unidade (reuso de Turfano.Units)
    public static Units.Angle BearingToAzimuth(Units.Angle bearing);
    public static double ConvertLength(double value, Units.LengthUnit from, Units.LengthUnit to);
    public static double ConvertArea(double value, Units.AreaUnit from, Units.AreaUnit to);
    // degreesToRadians/radiansToDegrees/lengthToRadians/radiansToLength/lengthToDegrees ...
}
```

## Invariantes de verificação
- Cada função = `@turf` real dentro de tolerância apertada (SC-001).
- `Centroid([[0,0],[0,2],[1,1],[2,2],[2,0],[0,0]])` = `[1,1]` (SC-002).
- Assinaturas só com `Turfano.GeoJson`/`Turfano.Units` (SC-003).
- Suíte existente 177 verde; build net8/9/10; 0 warnings AOT (SC-004/005).
