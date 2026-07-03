namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Circle arc between two bearings — `@turf/line-arc`. Equal bearings (mod 360) become
    /// the full circle as a line.
    /// </summary>
    public static LineString LineArc(
        Point center,
        Units.Length radius,
        Units.Angle bearing1,
        Units.Angle bearing2,
        int steps = 64
    )
    {
        var angle1 = ConvertAngleTo360(bearing1.Degrees);
        var angle2 = ConvertAngleTo360(bearing2.Degrees);

        if (angle1 == angle2)
            return new LineString(Circle(center, radius, steps).Coordinates[0]);

        var arcStartDegree = angle1;
        var arcEndDegree = angle1 < angle2 ? angle2 : angle2 + 360;

        var alpha = arcStartDegree;
        var coordinates = new List<Position>();
        var i = 0;
        var arcStep = (arcEndDegree - arcStartDegree) / steps;
        while (alpha <= arcEndDegree)
        {
            coordinates.Add(
                Destination(center.Coordinates, radius, Units.Angle.FromDegrees(alpha)).Coordinates
            );
            i++;
            alpha = arcStartDegree + i * arcStep;
        }
        return new LineString(coordinates.ToArray());
    }

    private static double ConvertAngleTo360(double alpha)
    {
        var beta = alpha % 360;
        if (beta < 0)
            beta += 360;
        return beta;
    }
}
