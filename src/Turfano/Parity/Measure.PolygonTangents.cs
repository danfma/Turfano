namespace Turfano.GeoJson;

/// <summary>Result of <see cref="Geo.PolygonTangents"/>: the right and left tangents.</summary>
public sealed record PolygonTangentsResult(Point RightTangent, Point LeftTangent);

public static partial class Geo
{
    /// <summary>
    /// Tangents (right/left) from an external point to a polygon — faithful port of
    /// `@turf/polygon-tangents` (planar, via the `isLeft` cross product). Outer-ring case only;
    /// @turf's hole/MultiPolygon logic is deferred.
    /// </summary>
    public static PolygonTangentsResult PolygonTangents(Position pt, Polygon polygon)
    {
        var ring = polygon.Coordinates[0];
        var bbox = Bbox(polygon).Values;

        var nearestIndex = 0;
        Position? nearest = null;
        if (pt.Lon > bbox[0] && pt.Lon < bbox[2] && pt.Lat > bbox[1] && pt.Lat < bbox[3])
        {
            var minD = double.PositiveInfinity;
            for (var i = 0; i < ring.Length; i++)
            {
                var d = Distance(pt, ring[i]).Kilometers;
                if (d < minD)
                {
                    minD = d;
                    nearestIndex = i;
                    nearest = ring[i];
                }
            }
        }

        var rtan = ring[nearestIndex];
        var ltan = ring[0];
        if (nearest is { } nv && nv.Lat < pt.Lat)
            ltan = ring[nearestIndex];

        var eprev = IsLeft(ring[0], ring[^1], pt);
        (rtan, ltan) = ProcessPolygon(ring, pt, eprev, rtan, ltan);
        return new PolygonTangentsResult(new Point(rtan), new Point(ltan));
    }

    private static (Position, Position) ProcessPolygon(
        Position[] coords,
        Position pt,
        double eprev,
        Position rtan,
        Position ltan
    )
    {
        for (var i = 0; i < coords.Length; i++)
        {
            var current = coords[i];
            var next = i == coords.Length - 1 ? coords[0] : coords[i + 1];
            var enext = IsLeft(current, next, pt);

            if (eprev <= 0 && enext > 0)
            {
                if (!(IsLeft(pt, current, rtan) < 0)) // !isBelow
                    rtan = current;
            }
            else if (eprev > 0 && enext <= 0)
            {
                if (!(IsLeft(pt, current, ltan) > 0)) // !isAbove
                    ltan = current;
            }

            eprev = enext;
        }
        return (rtan, ltan);
    }

    private static double IsLeft(Position p1, Position p2, Position p3) =>
        (p2.Lon - p1.Lon) * (p3.Lat - p1.Lat) - (p3.Lon - p1.Lon) * (p2.Lat - p1.Lat);
}
