using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Desloca uma linha por uma distância (perpendicular) — `@turf/line-offset`. Cada segmento
    /// é deslocado e as junções são resolvidas pela interseção dos segmentos deslocados.
    /// </summary>
    public static LineString LineOffset(LineString line, Units.Length distance)
    {
        var coords = line.Coordinates;
        var offsetDegrees = distance.Degrees;
        var segments = new List<Position[]>();
        var finalCoords = new List<Position>();

        for (var index = 0; index < coords.Length - 1; index++)
        {
            var segment = ProcessOffsetSegment(coords[index], coords[index + 1], offsetDegrees);
            segments.Add(segment);

            if (index > 0)
            {
                var previous = segments[index - 1];
                var intersects = OffsetIntersection(segment, previous);
                if (intersects is { } pt)
                {
                    previous[1] = pt;
                    segment[0] = pt;
                }
                finalCoords.Add(previous[0]);
                if (index == coords.Length - 2)
                {
                    finalCoords.Add(segment[0]);
                    finalCoords.Add(segment[1]);
                }
            }

            if (coords.Length == 2)
            {
                finalCoords.Add(segment[0]);
                finalCoords.Add(segment[1]);
            }
        }

        return new LineString(finalCoords.ToArray());
    }

    private static Position[] ProcessOffsetSegment(Position p1, Position p2, double offset)
    {
        var length = Math.Sqrt(
            (p1.Lon - p2.Lon) * (p1.Lon - p2.Lon) + (p1.Lat - p2.Lat) * (p1.Lat - p2.Lat)
        );
        return new[]
        {
            new Position(p1.Lon + offset * (p2.Lat - p1.Lat) / length, p1.Lat + offset * (p1.Lon - p2.Lon) / length),
            new Position(p2.Lon + offset * (p2.Lat - p1.Lat) / length, p2.Lat + offset * (p1.Lon - p2.Lon) / length),
        };
    }

    private static Position? OffsetIntersection(Position[] a, Position[] b)
    {
        var rx = a[1].Lon - a[0].Lon;
        var ry = a[1].Lat - a[0].Lat;
        var sx = b[1].Lon - b[0].Lon;
        var sy = b[1].Lat - b[0].Lat;
        var cross = rx * sy - sx * ry;
        if (cross == 0)
            return null; // paralelos
        var qpx = b[0].Lon - a[0].Lon;
        var qpy = b[0].Lat - a[0].Lat;
        var t = (qpx * sy - qpy * sx) / cross;
        return new Position(a[0].Lon + t * rx, a[0].Lat + t * ry);
    }
}
