using Units = Turfano.Units;

namespace Turfano.GeoJson;

/// <summary>Result of <see cref="Geo.NearestPointOnLine"/>: point, segment index, and distances.</summary>
public sealed record NearestPointOnLineResult(
    Point Point,
    int Index,
    Units.Length Distance,
    Units.Length Location
);

public static partial class Geo
{
    private readonly record struct Vector3(double X, double Y, double Z);

    /// <summary>
    /// Closest point on a line to a given point — faithful port of `@turf/nearest-point-on-line`
    /// (geodesic cross-track via 3D vectors on the unit sphere; NOT the planar projection of
    /// the existing NTS code, which diverges from @turf).
    /// </summary>
    public static NearestPointOnLineResult NearestPointOnLine(LineString line, Point inputPoint)
    {
        var input = inputPoint.Coordinates;
        var coords = line.Coordinates;

        Position closest = default;
        var closestDist = double.PositiveInfinity;
        var closestIndex = -1;
        var closestLocation = 0.0;
        var lineDistance = 0.0;

        for (var i = 0; i < coords.Length - 1; i++)
        {
            var start = coords[i];
            var stop = coords[i + 1];
            var segmentLength = Distance(start, stop).Kilometers;

            Position intersect;
            bool wasEnd;
            if (stop.Lon == input.Lon && stop.Lat == input.Lat)
                (intersect, wasEnd) = (stop, true);
            else if (start.Lon == input.Lon && start.Lat == input.Lat)
                (intersect, wasEnd) = (start, false);
            else
                (intersect, wasEnd) = NearestPointOnSegment(start, stop, input);

            var pointDistance = Distance(input, intersect).Kilometers;
            if (pointDistance < closestDist)
            {
                var segmentDistance = Distance(start, intersect).Kilometers;
                closest = intersect;
                closestIndex = wasEnd ? i + 1 : i;
                closestDist = pointDistance;
                closestLocation = lineDistance + segmentDistance;
            }

            lineDistance += segmentLength;
        }

        return new NearestPointOnLineResult(
            new Point(closest),
            closestIndex,
            Units.Length.FromKilometers(closestDist),
            Units.Length.FromKilometers(closestLocation)
        );
    }

    /// <summary>Minimum distance from a point to a line — `@turf/point-to-line-distance` (geodesic).</summary>
    public static Units.Length PointToLineDistance(Point point, LineString line) =>
        NearestPointOnLine(line, point).Distance;

    private static (Position, bool) NearestPointOnSegment(Position posA, Position posB, Position posC)
    {
        var A = LngLatToVector(posA);
        var B = LngLatToVector(posB);
        var C = LngLatToVector(posC);

        var segmentAxis = Cross(A, B);
        if (segmentAxis is { X: 0, Y: 0, Z: 0 })
            return Dot(A, B) > 0 ? (posB, true) : (posC, false);

        var targetAxis = Cross(segmentAxis, C);
        if (targetAxis is { X: 0, Y: 0, Z: 0 })
            return (posB, true);

        var I1 = Normalize(Cross(targetAxis, segmentAxis));
        var I2 = new Vector3(-I1.X, -I1.Y, -I1.Z);
        var I = Dot(C, I1) > Dot(C, I2) ? I1 : I2;

        var segmentAxisNorm = Normalize(segmentAxis);
        var compareStartToIntersect = Dot(Cross(A, I), segmentAxisNorm);
        var compareIntersectToEnd = Dot(Cross(I, B), segmentAxisNorm);
        if (compareStartToIntersect >= 0 && compareIntersectToEnd >= 0)
            return (VectorToLngLat(I), false);

        return Dot(A, C) > Dot(B, C) ? (posA, false) : (posB, true);
    }

    private static Vector3 LngLatToVector(Position a)
    {
        var lat = a.Lat * RadiansPerDegree;
        var lng = a.Lon * RadiansPerDegree;
        return new Vector3(Math.Cos(lat) * Math.Cos(lng), Math.Cos(lat) * Math.Sin(lng), Math.Sin(lat));
    }

    private static Position VectorToLngLat(Vector3 v)
    {
        var z = Math.Min(Math.Max(v.Z, -1), 1);
        return new Position(Math.Atan2(v.Y, v.X) / RadiansPerDegree, Math.Asin(z) / RadiansPerDegree);
    }

    private static double Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static Vector3 Cross(Vector3 a, Vector3 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static Vector3 Normalize(Vector3 v)
    {
        var m = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return new Vector3(v.X / m, v.Y / m, v.Z / m);
    }
}
