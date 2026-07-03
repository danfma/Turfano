namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Clips a geometry by a bounding box — `@turf/bbox-clip` (port of `lineclip`:
    /// Cohen-Sutherland for lines, Sutherland-Hodgman for polygons). No NTS.
    /// </summary>
    public static Geometry BboxClip(Geometry geometry, BBox bbox)
    {
        var box = bbox.Values;
        switch (geometry)
        {
            case LineString ls:
            {
                var lines = new List<Position[]>();
                LineClip(ls.Coordinates, box, lines);
                return lines.Count == 1
                    ? new LineString(lines[0])
                    : new MultiLineString(lines.ToArray());
            }
            case MultiLineString mls:
            {
                var lines = new List<Position[]>();
                foreach (var line in mls.Coordinates)
                    LineClip(line, box, lines);
                return lines.Count == 1
                    ? new LineString(lines[0])
                    : new MultiLineString(lines.ToArray());
            }
            case Polygon poly:
                return new Polygon(ClipPolygonRings(poly.Coordinates, box));
            case MultiPolygon mpoly:
                return new MultiPolygon(
                    mpoly.Coordinates.Select(p => ClipPolygonRings(p, box)).ToArray()
                );
            default:
                throw new ArgumentException(
                    "BboxClip suporta LineString/MultiLineString/Polygon/MultiPolygon",
                    nameof(geometry)
                );
        }
    }

    private static int BitCode(Position p, double[] bbox)
    {
        var code = 0;
        if (p.Lon < bbox[0])
            code |= 1; // esquerda
        else if (p.Lon > bbox[2])
            code |= 2; // direita
        if (p.Lat < bbox[1])
            code |= 4; // baixo
        else if (p.Lat > bbox[3])
            code |= 8; // cima
        return code;
    }

    private static Position ClipIntersect(Position a, Position b, int edge, double[] bbox)
    {
        if ((edge & 8) != 0)
            return new Position(
                a.Lon + (b.Lon - a.Lon) * (bbox[3] - a.Lat) / (b.Lat - a.Lat),
                bbox[3]
            );
        if ((edge & 4) != 0)
            return new Position(
                a.Lon + (b.Lon - a.Lon) * (bbox[1] - a.Lat) / (b.Lat - a.Lat),
                bbox[1]
            );
        if ((edge & 2) != 0)
            return new Position(
                bbox[2],
                a.Lat + (b.Lat - a.Lat) * (bbox[2] - a.Lon) / (b.Lon - a.Lon)
            );
        return new Position(bbox[0], a.Lat + (b.Lat - a.Lat) * (bbox[0] - a.Lon) / (b.Lon - a.Lon));
    }

    private static void LineClip(Position[] points, double[] bbox, List<Position[]> result)
    {
        var len = points.Length;
        var codeA = BitCode(points[0], bbox);
        var part = new List<Position>();

        for (var i = 1; i < len; i++)
        {
            var a = points[i - 1];
            var b = points[i];
            var codeB = BitCode(b, bbox);
            var lastCode = codeB;

            while (true)
            {
                if ((codeA | codeB) == 0)
                {
                    part.Add(a);
                    if (codeB != lastCode)
                    {
                        part.Add(b);
                        if (i < len - 1)
                        {
                            result.Add(part.ToArray());
                            part = new List<Position>();
                        }
                    }
                    else if (i == len - 1)
                    {
                        part.Add(b);
                    }
                    break;
                }
                else if ((codeA & codeB) != 0)
                {
                    break;
                }
                else if (codeA != 0)
                {
                    a = ClipIntersect(a, b, codeA, bbox);
                    codeA = BitCode(a, bbox);
                }
                else
                {
                    b = ClipIntersect(a, b, codeB, bbox);
                    codeB = BitCode(b, bbox);
                }
            }

            codeA = lastCode;
        }

        if (part.Count > 0)
            result.Add(part.ToArray());
    }

    private static Position[][] ClipPolygonRings(Position[][] rings, double[] bbox)
    {
        var outRings = new List<Position[]>();
        foreach (var ring in rings)
        {
            var clipped = PolygonClip(ring, bbox);
            if (clipped.Count > 0)
            {
                if (!clipped[0].Equals(clipped[^1]))
                    clipped.Add(clipped[0]);
                if (clipped.Count >= 4)
                    outRings.Add(clipped.ToArray());
            }
        }
        return outRings.ToArray();
    }

    private static List<Position> PolygonClip(Position[] points, double[] bbox)
    {
        var result = new List<Position>(points);
        for (var edge = 1; edge <= 8; edge *= 2)
        {
            if (result.Count == 0)
                break;
            var clipped = new List<Position>();
            var prev = result[^1];
            var prevInside = (BitCode(prev, bbox) & edge) == 0;
            foreach (var p in result)
            {
                var inside = (BitCode(p, bbox) & edge) == 0;
                if (inside != prevInside)
                    clipped.Add(ClipIntersect(prev, p, edge, bbox));
                if (inside)
                    clipped.Add(p);
                prev = p;
                prevInside = inside;
            }
            result = clipped;
        }
        return result;
    }
}
