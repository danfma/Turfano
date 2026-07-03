namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Point on a line — `@turf/boolean-point-on-line`. `ignoreEndVertices` excludes the
    /// start/end vertices; `epsilon` is the collinearity tolerance.
    /// </summary>
    public static bool BooleanPointOnLine(
        Point point,
        LineString line,
        bool ignoreEndVertices = false,
        double? epsilon = null
    )
    {
        var pt = point.Coordinates;
        var coords = line.Coordinates;

        for (var i = 0; i < coords.Length - 1; i++)
        {
            string? excludeBoundary = null;
            if (ignoreEndVertices)
            {
                if (i == 0)
                    excludeBoundary = "start";
                if (i == coords.Length - 2)
                    excludeBoundary = "end";
                if (i == 0 && i + 1 == coords.Length - 1)
                    excludeBoundary = "both";
            }

            if (IsPointOnLineSegment(coords[i], coords[i + 1], pt, excludeBoundary, epsilon))
                return true;
        }
        return false;
    }

    private static bool IsPointOnLineSegment(
        Position start,
        Position end,
        Position pt,
        string? excludeBoundary,
        double? epsilon
    )
    {
        double x = pt.Lon,
            y = pt.Lat,
            x1 = start.Lon,
            y1 = start.Lat,
            x2 = end.Lon,
            y2 = end.Lat;
        var dxc = x - x1;
        var dyc = y - y1;
        var dxl = x2 - x1;
        var dyl = y2 - y1;
        var cross = dxc * dyl - dyc * dxl;

        if (epsilon is { } eps)
        {
            if (Math.Abs(cross) > eps)
                return false;
        }
        else if (cross != 0)
        {
            return false;
        }

        if (Math.Abs(dxl) == 0 && Math.Abs(dyl) == 0)
        {
            if (excludeBoundary != null)
                return false;
            return x == x1 && y == y1;
        }

        if (excludeBoundary == null)
        {
            if (Math.Abs(dxl) >= Math.Abs(dyl))
                return dxl > 0 ? x1 <= x && x <= x2 : x2 <= x && x <= x1;
            return dyl > 0 ? y1 <= y && y <= y2 : y2 <= y && y <= y1;
        }
        if (excludeBoundary == "start")
        {
            if (Math.Abs(dxl) >= Math.Abs(dyl))
                return dxl > 0 ? x1 < x && x <= x2 : x2 <= x && x < x1;
            return dyl > 0 ? y1 < y && y <= y2 : y2 <= y && y < y1;
        }
        if (excludeBoundary == "end")
        {
            if (Math.Abs(dxl) >= Math.Abs(dyl))
                return dxl > 0 ? x1 <= x && x < x2 : x2 < x && x <= x1;
            return dyl > 0 ? y1 <= y && y < y2 : y2 < y && y <= y1;
        }
        // both
        if (Math.Abs(dxl) >= Math.Abs(dyl))
            return dxl > 0 ? x1 < x && x < x2 : x2 < x && x < x1;
        return dyl > 0 ? y1 < y && y < y2 : y2 < y && y < y1;
    }
}
