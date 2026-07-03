using System.Text.Json.Nodes;
using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Directional mean angle of a set of lines (trend) — `@turf/directional-mean`. Works
    /// both in the plane (<paramref name="planar"/> = true, Cartesian arithmetic) and
    /// geodesically (default — bearings via <see cref="Bearing"/>); with <paramref name="segment"/>
    /// = true, each line segment is treated in isolation instead of the whole line.
    /// <para>
    /// **Fidelity note**: @turf's documentation (JSDoc) states `planar` defaults to `true`,
    /// but the actual source code uses `!!options.planar` — i.e., the real default is
    /// `false` (geodesic). This port follows the actual behavior (confirmed via ground
    /// truth), not the docs.
    /// </para>
    /// </summary>
    public static Feature DirectionalMean(FeatureCollection lines, bool planar = false, bool segment = false)
    {
        double sigmaSin = 0,
            sigmaCos = 0;
        var countOfLines = 0;
        double sumOfLen = 0;
        var centroidList = new List<Position>();

        void Accumulate(Position[] coordinates)
        {
            var (sin1, cos1) = GetCosAndSin(coordinates, planar);
            if (double.IsNaN(sin1) || double.IsNaN(cos1))
                return;

            sigmaSin += sin1;
            sigmaCos += cos1;
            countOfLines += 1;
            sumOfLen += GetLengthOfCoordinates(coordinates, planar);
            centroidList.Add(Centroid(new LineString(coordinates)).Coordinates);
        }

        if (segment)
        {
            foreach (var feature in lines.Features)
            {
                if (feature.Geometry is not { } geometry)
                    continue;
                SegmentEach(geometry, (seg, _, _, _, _) => Accumulate(new[] { seg.Start, seg.End }));
            }
        }
        else
        {
            foreach (var feature in lines.Features)
            {
                if (feature.Geometry is not LineString lineString)
                    // Erro fiel ao @turf, incl. o typo original da mensagem.
                    throw new InvalidOperationException("shold to support MultiLineString?");
                Accumulate(lineString.Coordinates);
            }
        }

        var cartesianAngle = GetAngleBySinAndCos(sigmaSin, sigmaCos);
        var bearingAngle = BearingToCartesian(cartesianAngle);
        var circularVariance = GetCircularVariance(sigmaSin, sigmaCos, countOfLines);
        var averageLength = sumOfLen / countOfLines;

        var averageX = centroidList.Average(p => p.Lon);
        var averageY = centroidList.Average(p => p.Lat);

        var meanLineString = GetMeanLineString(
            new Position(averageX, averageY),
            planar ? cartesianAngle : bearingAngle,
            averageLength,
            planar
        );

        var properties = new JsonObject
        {
            ["averageLength"] = averageLength,
            ["averageX"] = averageX,
            ["averageY"] = averageY,
            ["bearingAngle"] = bearingAngle,
            ["cartesianAngle"] = cartesianAngle,
            ["circularVariance"] = circularVariance,
            ["countOfLines"] = countOfLines,
        };

        return new Feature(new LineString(meanLineString), properties);
    }

    private static double GetLengthOfCoordinates(Position[] coordinates, bool isPlanar)
    {
        if (isPlanar)
        {
            double total = 0;
            for (var i = 0; i < coordinates.Length - 1; i++)
                total += EuclideanDistance(coordinates[i], coordinates[i + 1]);
            return total;
        }

        return Length(new LineString(coordinates)).Meters;
    }

    private static double EuclideanDistance(Position a, Position b)
    {
        var dx = b.Lon - a.Lon;
        var dy = b.Lat - a.Lat;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static (double Sin, double Cos) GetCosAndSin(Position[] coordinates, bool isPlanar)
    {
        var beginPoint = coordinates[0];
        var endPoint = coordinates[^1];

        if (isPlanar)
        {
            var dx = endPoint.Lon - beginPoint.Lon;
            var dy = endPoint.Lat - beginPoint.Lat;
            var h = Math.Sqrt(dx * dx + dy * dy);
            if (h < 1e-9)
                return (double.NaN, double.NaN);
            return (dy / h, dx / h);
        }

        var angle = BearingToCartesian(Bearing(beginPoint, endPoint).Degrees);
        var radian = angle * RadiansPerDegree;
        return (Math.Sin(radian), Math.Cos(radian));
    }

    private static double BearingToCartesian(double angle)
    {
        var result = 90 - angle;
        if (result > 180)
            result -= 360;
        return result;
    }

    private static double GetAngleBySinAndCos(double sin1, double cos1)
    {
        var angle = Math.Abs(cos1) < 1e-9 ? 90 : Math.Atan2(sin1, cos1) * 180 / Math.PI;

        if (sin1 >= 0)
        {
            if (cos1 < 0)
                angle += 180;
        }
        else
        {
            if (cos1 < 0)
                angle -= 180;
        }
        return angle;
    }

    private static double GetCircularVariance(double sin1, double cos1, int len)
    {
        if (len == 0)
            throw new InvalidOperationException("the size of the features set must be greater than 0");
        return 1 - Math.Sqrt(Math.Pow(sin1, 2) + Math.Pow(cos1, 2)) / len;
    }

    private static Position[] GetMeanLineString(
        Position centroidOfLine,
        double angleDegrees,
        double lengthOfLine,
        bool isPlanar
    )
    {
        if (isPlanar)
        {
            var r = angleDegrees * RadiansPerDegree;
            var sin = Math.Sin(r);
            var cos = Math.Cos(r);
            var beginX = centroidOfLine.Lon - lengthOfLine / 2 * cos;
            var beginY = centroidOfLine.Lat - lengthOfLine / 2 * sin;
            var endX = centroidOfLine.Lon + lengthOfLine / 2 * cos;
            var endY = centroidOfLine.Lat + lengthOfLine / 2 * sin;
            return new[] { new Position(beginX, beginY), new Position(endX, endY) };
        }

        var end = Destination(
            centroidOfLine,
            Units.Length.FromMeters(lengthOfLine / 2),
            Units.Angle.FromDegrees(angleDegrees)
        );
        var begin = Destination(
            centroidOfLine,
            Units.Length.FromMeters(-lengthOfLine / 2),
            Units.Angle.FromDegrees(angleDegrees)
        );
        return new[] { begin.Coordinates, end.Coordinates };
    }
}
