using System.Globalization;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Pontos de interseção entre duas geometrias lineares — `@turf/line-intersect`
    /// (sweepline-intersections portado; auto-interseções ignoradas e duplicatas removidas
    /// por default, como a fonte).
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
