namespace Turfano.Interop;

// Ponte INTERNA entre os tipos GeoJSON próprios (Turfano.GeoJson) e o NetTopologySuite.
// Permite que as funções `Turf.*` ainda baseadas em NTS operem sobre os novos tipos
// durante a transição das ondas de paridade (NTS é o motor interino). Interop público
// segue fora de escopo. Nomes simples (Coordinate, Geometry, ...) = NTS (global using);
// prefixo `GeoJson.` = nossos tipos.
internal static class NtsBridge
{
    private static readonly GeometryFactory Factory = new();

    internal static Coordinate ToNts(GeoJson.Position p) =>
        p.Alt is { } alt ? new CoordinateZ(p.Lon, p.Lat, alt) : new Coordinate(p.Lon, p.Lat);

    internal static GeoJson.Position FromNts(Coordinate c) =>
        new(c.X, c.Y, double.IsNaN(c.Z) ? null : c.Z);

    internal static Geometry ToNts(GeoJson.Geometry g) =>
        g switch
        {
            GeoJson.Point p => Factory.CreatePoint(ToNts(p.Coordinates)),
            GeoJson.MultiPoint mp => Factory.CreateMultiPointFromCoords(
                mp.Coordinates.Select(ToNts).ToArray()
            ),
            GeoJson.LineString ls => Factory.CreateLineString(ls.Coordinates.Select(ToNts).ToArray()),
            GeoJson.MultiLineString mls => Factory.CreateMultiLineString(
                mls.Coordinates.Select(line => Factory.CreateLineString(line.Select(ToNts).ToArray())).ToArray()
            ),
            GeoJson.Polygon poly => ToNtsPolygon(poly.Coordinates),
            GeoJson.MultiPolygon mpoly => Factory.CreateMultiPolygon(
                mpoly.Coordinates.Select(ToNtsPolygon).ToArray()
            ),
            GeoJson.GeometryCollection gc => Factory.CreateGeometryCollection(
                gc.Geometries.Select(ToNts).ToArray()
            ),
            _ => throw new ArgumentException($"Geometria não suportada: {g.Type}", nameof(g)),
        };

    private static Polygon ToNtsPolygon(GeoJson.Position[][] rings)
    {
        var shell = Factory.CreateLinearRing(rings[0].Select(ToNts).ToArray());
        var holes = rings.Skip(1)
            .Select(r => Factory.CreateLinearRing(r.Select(ToNts).ToArray()))
            .ToArray();
        return Factory.CreatePolygon(shell, holes);
    }

    internal static GeoJson.Geometry FromNts(Geometry g) =>
        g switch
        {
            Point p => new GeoJson.Point(FromNts(p.Coordinate)),
            MultiPoint mp => new GeoJson.MultiPoint(mp.Coordinates.Select(FromNts).ToArray()),
            LineString ls => new GeoJson.LineString(ls.Coordinates.Select(FromNts).ToArray()),
            MultiLineString mls => new GeoJson.MultiLineString(
                Enumerable.Range(0, mls.NumGeometries)
                    .Select(i => ((LineString)mls.GetGeometryN(i)).Coordinates.Select(FromNts).ToArray())
                    .ToArray()
            ),
            Polygon poly => new GeoJson.Polygon(RingsOf(poly)),
            MultiPolygon mpoly => new GeoJson.MultiPolygon(
                Enumerable.Range(0, mpoly.NumGeometries)
                    .Select(i => RingsOf((Polygon)mpoly.GetGeometryN(i)))
                    .ToArray()
            ),
            GeometryCollection gc => new GeoJson.GeometryCollection(
                Enumerable.Range(0, gc.NumGeometries).Select(i => FromNts(gc.GetGeometryN(i))).ToArray()
            ),
            _ => throw new ArgumentException($"Geometria NTS não suportada: {g.GeometryType}", nameof(g)),
        };

    private static GeoJson.Position[][] RingsOf(Polygon poly)
    {
        var rings = new List<GeoJson.Position[]>
        {
            poly.ExteriorRing.Coordinates.Select(FromNts).ToArray(),
        };
        for (var i = 0; i < poly.NumInteriorRings; i++)
            rings.Add(poly.GetInteriorRingN(i).Coordinates.Select(FromNts).ToArray());
        return rings.ToArray();
    }
}
