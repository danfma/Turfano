using Units = Turfano.Units;

namespace Turfano.GeoJson;

/// <summary>Resultado de <see cref="Geo.NearestPointOnLine"/>: ponto, índice do segmento e distâncias.</summary>
public sealed record NearestPointOnLineResult(
    Point Point,
    int Index,
    Units.Length Distance,
    Units.Length Location
);

public static partial class Geo
{
    private readonly record struct Vec3(double X, double Y, double Z);

    /// <summary>
    /// Ponto mais próximo de uma linha a um ponto — porte fiel do `@turf/nearest-point-on-line`
    /// (cross-track geodésico via vetores 3D na esfera unitária; NÃO a projeção planar do
    /// código NTS existente, que diverge do @turf).
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

    /// <summary>Distância mínima de um ponto a uma linha — `@turf/point-to-line-distance` (geodésico).</summary>
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
        var I2 = new Vec3(-I1.X, -I1.Y, -I1.Z);
        var I = Dot(C, I1) > Dot(C, I2) ? I1 : I2;

        var segmentAxisNorm = Normalize(segmentAxis);
        var cmpAI = Dot(Cross(A, I), segmentAxisNorm);
        var cmpIB = Dot(Cross(I, B), segmentAxisNorm);
        if (cmpAI >= 0 && cmpIB >= 0)
            return (VectorToLngLat(I), false);

        return Dot(A, C) > Dot(B, C) ? (posA, false) : (posB, true);
    }

    private static Vec3 LngLatToVector(Position a)
    {
        var lat = a.Lat * D2R;
        var lng = a.Lon * D2R;
        return new Vec3(Math.Cos(lat) * Math.Cos(lng), Math.Cos(lat) * Math.Sin(lng), Math.Sin(lat));
    }

    private static Position VectorToLngLat(Vec3 v)
    {
        var z = Math.Min(Math.Max(v.Z, -1), 1);
        return new Position(Math.Atan2(v.Y, v.X) / D2R, Math.Asin(z) / D2R);
    }

    private static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static Vec3 Cross(Vec3 a, Vec3 b) =>
        new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static Vec3 Normalize(Vec3 v)
    {
        var m = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return new Vec3(v.X / m, v.Y / m, v.Z / m);
    }
}
