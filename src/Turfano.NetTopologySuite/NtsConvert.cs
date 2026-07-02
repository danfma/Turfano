using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using GeoJson = Turfano.GeoJson;

namespace Turfano.NetTopologySuite;

/// <summary>
/// Conversão na borda entre os tipos GeoJSON do Turfano e o NetTopologySuite (ex.: EF Core
/// spatial). A fronteira usa sequências EMPACOTADAS (`PackedDoubleCoordinateSequence`):
/// nenhum objeto `Coordinate` é materializado por vértice — na ida os doubles crus viram a
/// sequência; na volta o fast-path lê `GetRawCoordinates()` (o array interno, sem cópia),
/// com fallback por ordinal para geometrias de terceiros com qualquer factory.
/// </summary>
public static class NtsConvert
{
    private static readonly GeometryFactory Factory = new(
        new PrecisionModel(),
        srid: 0,
        PackedCoordinateSequenceFactory.DoubleFactory
    );

    /// <summary>Posição GeoJSON → `Coordinate` (com Z quando houver altitude).</summary>
    public static Coordinate ToNts(GeoJson.Position position) =>
        position.Alt is { } alt
            ? new CoordinateZ(position.Lon, position.Lat, alt)
            : new Coordinate(position.Lon, position.Lat);

    /// <summary>`Coordinate` → posição GeoJSON (Z NaN vira ausência de altitude).</summary>
    public static GeoJson.Position FromNts(Coordinate coordinate) =>
        new(coordinate.X, coordinate.Y, double.IsNaN(coordinate.Z) ? null : coordinate.Z);

    /// <summary>Geometria GeoJSON do Turfano → geometria NTS (sequências empacotadas).</summary>
    public static Geometry ToNts(GeoJson.Geometry geometry) =>
        geometry switch
        {
            GeoJson.Point point => Factory.CreatePoint(PackPositions(new[] { point.Coordinates })),
            GeoJson.MultiPoint multiPoint => Factory.CreateMultiPoint(PackPositions(multiPoint.Coordinates)),
            GeoJson.LineString lineString => Factory.CreateLineString(PackPositions(lineString.Coordinates)),
            GeoJson.MultiLineString multiLineString => Factory.CreateMultiLineString(
                multiLineString.Coordinates.Select(line => Factory.CreateLineString(PackPositions(line))).ToArray()
            ),
            GeoJson.Polygon polygon => ToNtsPolygon(polygon.Coordinates),
            GeoJson.MultiPolygon multiPolygon => Factory.CreateMultiPolygon(
                multiPolygon.Coordinates.Select(ToNtsPolygon).ToArray()
            ),
            GeoJson.GeometryCollection collection => Factory.CreateGeometryCollection(
                collection.Geometries.Select(ToNts).ToArray()
            ),
            _ => throw new ArgumentException($"Geometria não suportada: {geometry.Type}", nameof(geometry)),
        };

    /// <summary>Geometria NTS → geometria GeoJSON do Turfano.</summary>
    public static GeoJson.Geometry FromNts(Geometry geometry) =>
        geometry switch
        {
            Point point => new GeoJson.Point(UnpackPositions(point.CoordinateSequence)[0]),
            MultiPoint multiPoint => new GeoJson.MultiPoint(
                Enumerable
                    .Range(0, multiPoint.NumGeometries)
                    .Select(i => UnpackPositions(((Point)multiPoint.GetGeometryN(i)).CoordinateSequence)[0])
                    .ToArray()
            ),
            LineString lineString => new GeoJson.LineString(UnpackPositions(lineString.CoordinateSequence)),
            MultiLineString multiLineString => new GeoJson.MultiLineString(
                Enumerable
                    .Range(0, multiLineString.NumGeometries)
                    .Select(i => UnpackPositions(((LineString)multiLineString.GetGeometryN(i)).CoordinateSequence))
                    .ToArray()
            ),
            Polygon polygon => new GeoJson.Polygon(RingsOf(polygon)),
            MultiPolygon multiPolygon => new GeoJson.MultiPolygon(
                Enumerable
                    .Range(0, multiPolygon.NumGeometries)
                    .Select(i => RingsOf((Polygon)multiPolygon.GetGeometryN(i)))
                    .ToArray()
            ),
            GeometryCollection collection => new GeoJson.GeometryCollection(
                Enumerable.Range(0, collection.NumGeometries).Select(i => FromNts(collection.GetGeometryN(i))).ToArray()
            ),
            _ => throw new ArgumentException($"Geometria NTS não suportada: {geometry.GeometryType}", nameof(geometry)),
        };

    private static Polygon ToNtsPolygon(GeoJson.Position[][] rings)
    {
        var shell = Factory.CreateLinearRing(PackPositions(rings[0]));
        var holes = rings.Skip(1).Select(ring => Factory.CreateLinearRing(PackPositions(ring))).ToArray();
        return Factory.CreatePolygon(shell, holes);
    }

    private static GeoJson.Position[][] RingsOf(Polygon polygon)
    {
        var rings = new List<GeoJson.Position[]>
        {
            UnpackPositions(((LineString)polygon.ExteriorRing).CoordinateSequence),
        };
        for (var i = 0; i < polygon.NumInteriorRings; i++)
            rings.Add(UnpackPositions(((LineString)polygon.GetInteriorRingN(i)).CoordinateSequence));
        return rings.ToArray();
    }

    /// <summary>Ida: `Position[]` → sequência empacotada (doubles crus; dimensão 3 se houver Z).</summary>
    private static CoordinateSequence PackPositions(GeoJson.Position[] positions)
    {
        var hasAltitude = positions.Any(p => p.Alt is not null);
        var dimension = hasAltitude ? 3 : 2;
        var raw = new double[positions.Length * dimension];
        for (var i = 0; i < positions.Length; i++)
        {
            raw[i * dimension] = positions[i].Lon;
            raw[i * dimension + 1] = positions[i].Lat;
            if (hasAltitude)
                raw[i * dimension + 2] = positions[i].Alt ?? Coordinate.NullOrdinate;
        }
        return new PackedDoubleCoordinateSequence(raw, dimension, 0);
    }

    /// <summary>Volta: fast-path pelo array interno quando empacotada; fallback por ordinal.</summary>
    private static GeoJson.Position[] UnpackPositions(CoordinateSequence sequence)
    {
        var count = sequence.Count;
        var positions = new GeoJson.Position[count];

        if (sequence is PackedDoubleCoordinateSequence packed)
        {
            var raw = packed.GetRawCoordinates(); // array interno, sem cópia
            var dimension = packed.Dimension;
            var hasZ = packed.HasZ;
            for (var i = 0; i < count; i++)
            {
                double? alt = null;
                if (hasZ)
                {
                    var z = raw[i * dimension + 2];
                    if (!double.IsNaN(z))
                        alt = z;
                }
                positions[i] = new GeoJson.Position(raw[i * dimension], raw[i * dimension + 1], alt);
            }
            return positions;
        }

        var sequenceHasZ = sequence.HasZ;
        for (var i = 0; i < count; i++)
        {
            double? alt = null;
            if (sequenceHasZ)
            {
                var z = sequence.GetZ(i);
                if (!double.IsNaN(z))
                    alt = z;
            }
            positions[i] = new GeoJson.Position(sequence.GetX(i), sequence.GetY(i), alt);
        }
        return positions;
    }
}
