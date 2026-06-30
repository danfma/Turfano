namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Cada vértice da geometria como um `Point` numa `FeatureCollection` — `@turf/explode`.</summary>
    public static FeatureCollection Explode(Geometry geometry)
    {
        var features = new List<Feature>();
        EachPosition(geometry, excludeWrapCoord: false, p => features.Add(new Feature(new Point(p))));
        return new FeatureCollection(features.ToArray());
    }

    /// <summary>Achata `Multi*`/`GeometryCollection` em partes simples — `@turf/flatten`.</summary>
    public static FeatureCollection Flatten(Geometry geometry) =>
        new(FlattenGeometry(geometry).Select(g => new Feature(g)).ToArray());

    /// <summary>Agrupa `Point`/`LineString`/`Polygon` da coleção em `Multi*` — `@turf/combine`.</summary>
    public static FeatureCollection Combine(FeatureCollection collection)
    {
        var points = new List<Position>();
        var lines = new List<Position[]>();
        var polygons = new List<Position[][]>();

        foreach (var feature in collection.Features)
        {
            switch (feature.Geometry)
            {
                case Point p:
                    points.Add(p.Coordinates);
                    break;
                case MultiPoint mp:
                    points.AddRange(mp.Coordinates);
                    break;
                case LineString ls:
                    lines.Add(ls.Coordinates);
                    break;
                case MultiLineString mls:
                    lines.AddRange(mls.Coordinates);
                    break;
                case Polygon poly:
                    polygons.Add(poly.Coordinates);
                    break;
                case MultiPolygon mpoly:
                    polygons.AddRange(mpoly.Coordinates);
                    break;
            }
        }

        var result = new List<Feature>();
        if (points.Count > 0)
            result.Add(new Feature(new MultiPoint(points.ToArray())));
        if (lines.Count > 0)
            result.Add(new Feature(new MultiLineString(lines.ToArray())));
        if (polygons.Count > 0)
            result.Add(new Feature(new MultiPolygon(polygons.ToArray())));
        return new FeatureCollection(result.ToArray());
    }

    /// <summary>Anéis de um polígono como linha(s) — `@turf/polygon-to-line`.</summary>
    public static Geometry PolygonToLine(Geometry polygon)
    {
        switch (polygon)
        {
            case Polygon p:
                return p.Coordinates.Length == 1
                    ? new LineString(p.Coordinates[0])
                    : new MultiLineString(p.Coordinates);
            case MultiPolygon mp:
                return new MultiLineString(mp.Coordinates.SelectMany(rings => rings).ToArray());
            default:
                throw new ArgumentException("PolygonToLine espera Polygon ou MultiPolygon", nameof(polygon));
        }
    }

    /// <summary>Linha (fechando se preciso) como polígono — `@turf/line-to-polygon`.</summary>
    public static Geometry LineToPolygon(Geometry line)
    {
        switch (line)
        {
            case LineString ls:
                return new Polygon(new[] { EnsureClosed(ls.Coordinates) });
            case MultiLineString mls:
                return new Polygon(mls.Coordinates.Select(EnsureClosed).ToArray());
            default:
                throw new ArgumentException("LineToPolygon espera LineString ou MultiLineString", nameof(line));
        }
    }

    private static Position[] EnsureClosed(Position[] coords)
    {
        if (coords.Length > 0 && !coords[0].Equals(coords[^1]))
            return coords.Append(coords[0]).ToArray();
        return coords;
    }
}
