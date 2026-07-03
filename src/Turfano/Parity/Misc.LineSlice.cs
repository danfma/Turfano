using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Slices the line between the points (projected onto it) `start` and `stop` — `@turf/line-slice`.</summary>
    public static LineString LineSlice(Point start, Point stop, LineString line)
    {
        var startVertex = NearestPointOnLine(line, start);
        var stopVertex = NearestPointOnLine(line, stop);
        var (first, last) =
            startVertex.Index <= stopVertex.Index ? (startVertex, stopVertex) : (stopVertex, startVertex);

        var clip = new List<Position> { first.Point.Coordinates };
        for (var i = first.Index + 1; i < last.Index + 1; i++)
            clip.Add(line.Coordinates[i]);
        clip.Add(last.Point.Coordinates);
        return new LineString(clip.ToArray());
    }

    /// <summary>Slices the line between two distances along it — `@turf/line-slice-along`.</summary>
    public static LineString LineSliceAlong(LineString line, Units.Length start, Units.Length stop)
    {
        var coords = line.Coordinates;
        var slice = new List<Position>();
        var startDist = start.Kilometers;
        var stopDist = stop.Kilometers;
        var travelled = 0.0;

        for (var i = 0; i < coords.Length; i++)
        {
            if (startDist >= travelled && i == coords.Length - 1)
            {
                break;
            }
            else if (travelled > startDist && slice.Count == 0)
            {
                var overshot = startDist - travelled;
                if (overshot == 0)
                {
                    slice.Add(coords[i]);
                    return new LineString(slice.ToArray());
                }
                slice.Add(Interpolate(coords[i], coords[i - 1], overshot));
            }

            if (travelled >= stopDist)
            {
                var overshot = stopDist - travelled;
                if (overshot == 0)
                {
                    slice.Add(coords[i]);
                    return new LineString(slice.ToArray());
                }
                slice.Add(Interpolate(coords[i], coords[i - 1], overshot));
                return new LineString(slice.ToArray());
            }

            if (travelled >= startDist)
                slice.Add(coords[i]);

            if (i < coords.Length - 1)
                travelled += Distance(coords[i], coords[i + 1]).Kilometers;
        }

        var lastCoord = coords[^1];
        return new LineString(new[] { lastCoord, lastCoord });
    }

    private static Position Interpolate(Position current, Position previous, double overshotKm)
    {
        var direction = Bearing(current, previous).Degrees - 180;
        return Destination(
            current,
            Units.Length.FromKilometers(overshotKm),
            Units.Angle.FromDegrees(direction)
        ).Coordinates;
    }
}
