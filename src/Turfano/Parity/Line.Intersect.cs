using System.Globalization;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Intersection points between two linear geometries — `@turf/line-intersect` (ported
    /// sweepline-intersections; self-intersections ignored and duplicates removed by
    /// default, as in the source).
    /// </summary>
    public static FeatureCollection LineIntersect(
        Geometry line1,
        Geometry line2,
        bool removeDuplicates = true,
        bool ignoreSelfIntersections = true
    )
    {
        var intersections = SweeplineIntersections.Run(new[] { line1, line2 }, ignoreSelfIntersections);

        List<double[]> results;
        if (removeDuplicates)
        {
            var unique = new HashSet<string>();
            results = new List<double[]>();
            foreach (var intersection in intersections)
            {
                var key =
                    intersection[0].ToString("R", CultureInfo.InvariantCulture)
                    + ","
                    + intersection[1].ToString("R", CultureInfo.InvariantCulture);
                if (unique.Add(key))
                    results.Add(intersection);
            }
        }
        else
        {
            results = intersections;
        }

        return new FeatureCollection(
            results.Select(r => new Feature(new Point(new Position(r[0], r[1])))).ToArray()
        );
    }
}
