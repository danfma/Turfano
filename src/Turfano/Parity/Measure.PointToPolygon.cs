using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Signed distance from a point to a polygon — `@turf/point-to-polygon-distance`.
    /// Positive outside, negative inside. Minimum of `PointToLineDistance` over each ring, signed
    /// via point-in-polygon. (@turf's hole-aware path is simplified here to the direct minimum.)
    /// </summary>
    public static Units.Length PointToPolygonDistance(Point point, Polygon polygon)
    {
        var minDistance = double.PositiveInfinity;
        foreach (var ring in polygon.Coordinates)
        {
            var d = PointToLineDistance(point, new LineString(ring)).Kilometers;
            if (d < minDistance)
                minDistance = d;
        }

        var inside = PointInPolygon(point.Coordinates, polygon);
        return Units.Length.FromKilometers(inside ? -minDistance : minDistance);
    }

    /// <summary>
    /// A point guaranteed to lie on the feature's surface — `@turf/point-on-feature`. For
    /// `Polygon`: the bbox center if it lies inside; otherwise the nearest point on the boundary.
    /// </summary>
    public static Point PointOnFeature(Geometry geometry)
    {
        if (geometry is Point p)
            return p;

        var center = Center(geometry);

        if (geometry is Polygon poly)
            return PointInPolygon(center.Coordinates, poly)
                ? center
                : NearestPointOnLine(new LineString(poly.Coordinates[0]), center).Point;

        if (geometry is LineString ls)
            return NearestPointOnLine(ls, center).Point;

        return center;
    }

    /// <summary>Ray-casting: point inside the outer ring and outside all holes.</summary>
    private static bool PointInPolygon(Position pt, Polygon polygon)
    {
        var rings = polygon.Coordinates;
        if (rings.Length == 0 || !InRing(pt, rings[0]))
            return false;
        for (var i = 1; i < rings.Length; i++)
            if (InRing(pt, rings[i]))
                return false; // dentro de um furo
        return true;
    }

    private static bool InRing(Position pt, Position[] ring)
    {
        var inside = false;
        double x = pt.Lon,
            y = pt.Lat;
        for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
        {
            double xi = ring[i].Lon,
                yi = ring[i].Lat,
                xj = ring[j].Lon,
                yj = ring[j].Lat;
            var intersect = ((yi > y) != (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
            if (intersect)
                inside = !inside;
        }
        return inside;
    }
}
