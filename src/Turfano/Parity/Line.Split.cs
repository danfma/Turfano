namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Splits a line by a splitter (point, multipoint, line, or polygon) — faithful port of
    /// `@turf/line-split`: the splitter is truncated (precision 7), lines/polygons turn into
    /// the points from `LineIntersect`, and the split happens point by point via the closest
    /// segment (spatial index in tree order).
    /// </summary>
    public static FeatureCollection LineSplit(Feature line, Feature splitter)
    {
        if (line.Geometry is not LineString _)
            throw new ArgumentException("line must be LineString", nameof(line));
        if (splitter.Geometry is null)
            throw new ArgumentException("splitter is required", nameof(splitter));

        var truncated = Truncate(splitter.Geometry, precision: 7);

        return truncated switch
        {
            Point point => SplitLineWithPoints((LineString)line.Geometry, new[] { point.Coordinates }),
            MultiPoint multiPoint => SplitLineWithPoints((LineString)line.Geometry, multiPoint.Coordinates),
            LineString _ or MultiLineString _ or Polygon _ or MultiPolygon _ => SplitLineWithPoints(
                (LineString)line.Geometry,
                LineIntersect(line.Geometry, truncated)
                    .Features.Select(f => ((Point)f.Geometry!).Coordinates)
                    .ToArray()
            ),
            _ => throw new ArgumentException("splitter cannot be a GeometryCollection", nameof(splitter)),
        };
    }

    private static FeatureCollection SplitLineWithPoints(LineString line, Position[] splitterPoints)
    {
        var results = new List<LineString>();
        var tree = new GeoJsonSpatialIndex();
        var featureByLine = new Dictionary<LineString, Feature>();

        Feature Wrap(LineString l)
        {
            var f = new Feature(l);
            featureByLine[l] = f;
            return f;
        }

        foreach (var point in splitterPoints)
        {
            if (results.Count == 0)
            {
                results.AddRange(SplitLineWithPoint(line, point));
                tree.Load(results.Select(Wrap).ToList());
            }
            else
            {
                var search = tree.Search(new Point(point));
                if (search.Count == 0)
                    continue;
                var closest = (LineString)FindClosestFeature(point, search).Geometry!;
                results.Remove(closest);
                tree.Remove(featureByLine[closest]);
                foreach (var piece in SplitLineWithPoint(closest, point))
                {
                    results.Add(piece);
                    tree.Insert(Wrap(piece));
                }
            }
        }

        return new FeatureCollection(results.Select(l => new Feature(l)).ToArray());
    }

    private static List<LineString> SplitLineWithPoint(LineString line, Position splitter)
    {
        var results = new List<LineString>();
        var startPoint = line.Coordinates[0];
        var endPoint = line.Coordinates[^1];
        if (startPoint == splitter || endPoint == splitter)
            return new List<LineString> { line };

        // segmentos da linha; o mais próximo do splitter define o corte
        var segments = new List<LineString>();
        for (var i = 1; i < line.Coordinates.Length; i++)
            segments.Add(new LineString(new[] { line.Coordinates[i - 1], line.Coordinates[i] }));

        var tree = new GeoJsonSpatialIndex();
        tree.Load(segments.Select(s => new Feature(s)));
        var search = tree.Search(new Point(splitter));
        if (search.Count == 0)
            return new List<LineString> { line };

        var closestSegment = (LineString)FindClosestFeature(splitter, search).Geometry!;
        var closestIndex = segments.IndexOf(closestSegment);

        var previous = new List<Position> { startPoint };
        for (var index = 0; index < segments.Count; index++)
        {
            var currentCoords = segments[index].Coordinates[1];
            if (index == closestIndex)
            {
                previous.Add(splitter);
                results.Add(new LineString(previous.ToArray()));
                previous = splitter == currentCoords
                    ? new List<Position> { splitter }
                    : new List<Position> { splitter, currentCoords };
            }
            else
            {
                previous.Add(currentCoords);
            }
        }

        if (previous.Count > 1)
            results.Add(new LineString(previous.ToArray()));

        return results;
    }

    private static Feature FindClosestFeature(Position point, List<Feature> lines)
    {
        if (lines.Count == 0)
            throw new InvalidOperationException("lines must contain features");
        if (lines.Count == 1)
            return lines[0];

        Feature? closestFeature = null;
        var closestDistance = double.PositiveInfinity;
        foreach (var segment in lines)
        {
            var nearest = NearestPointOnLine((LineString)segment.Geometry!, new Point(point));
            if (nearest.Distance.Kilometers < closestDistance)
            {
                closestFeature = segment;
                closestDistance = nearest.Distance.Kilometers;
            }
        }
        return closestFeature!;
    }
}
