using System.Globalization;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Trechos sobrepostos entre duas linhas — porte fiel do `@turf/line-overlap`: os
    /// segmentos de `line1` vão para o índice espacial; cada segmento de `line2` procura
    /// matches (na ORDEM da árvore) e a sobreposição é encadeada segmento a segmento.
    /// </summary>
    public static FeatureCollection LineOverlap(Geometry line1, Geometry line2, Units.Length? tolerance = null)
    {
        var toleranceKm = tolerance?.Kilometers ?? 0;
        var features = new List<Feature>();
        var tree = new GeoJsonSpatialIndex();
        tree.Load(LineSegment(line1).Features);

        List<Position>? overlapSegment = null;
        var additionalSegments = new List<List<Position>>();

        foreach (var linePart in LinearParts(line2))
        {
            for (var s = 1; s < linePart.Length; s++)
            {
                var segment = new LineString(new[] { linePart[s - 1], linePart[s] });
                var doesOverlap = false;

                foreach (var match in tree.Search(segment))
                {
                    if (doesOverlap)
                        continue;

                    var matchLine = (LineString)match.Geometry!;
                    var coordsSegment = JsDefaultSort(segment.Coordinates);
                    var coordsMatch = JsDefaultSort(matchLine.Coordinates);

                    if (coordsSegment[0] == coordsMatch[0] && coordsSegment[1] == coordsMatch[1])
                    {
                        doesOverlap = true;
                        overlapSegment = overlapSegment is not null
                            ? ConcatSegment(overlapSegment, segment.Coordinates) ?? overlapSegment
                            : new List<Position>(segment.Coordinates);
                    }
                    else if (
                        toleranceKm == 0
                            ? BooleanPointOnLine(new Point(coordsSegment[0]), matchLine)
                                && BooleanPointOnLine(new Point(coordsSegment[1]), matchLine)
                            : NearestPointOnLine(matchLine, new Point(coordsSegment[0])).Distance.Kilometers <= toleranceKm
                                && NearestPointOnLine(matchLine, new Point(coordsSegment[1])).Distance.Kilometers <= toleranceKm
                    )
                    {
                        doesOverlap = true;
                        overlapSegment = overlapSegment is not null
                            ? ConcatSegment(overlapSegment, segment.Coordinates) ?? overlapSegment
                            : new List<Position>(segment.Coordinates);
                    }
                    else if (
                        toleranceKm == 0
                            ? BooleanPointOnLine(new Point(coordsMatch[0]), segment)
                                && BooleanPointOnLine(new Point(coordsMatch[1]), segment)
                            : NearestPointOnLine(segment, new Point(coordsMatch[0])).Distance.Kilometers <= toleranceKm
                                && NearestPointOnLine(segment, new Point(coordsMatch[1])).Distance.Kilometers <= toleranceKm
                    )
                    {
                        if (overlapSegment is not null)
                        {
                            var combined = ConcatSegment(overlapSegment, matchLine.Coordinates);
                            if (combined is not null)
                                overlapSegment = combined;
                            else
                                additionalSegments.Add(new List<Position>(matchLine.Coordinates));
                        }
                        else
                        {
                            overlapSegment = new List<Position>(matchLine.Coordinates);
                        }
                    }
                }

                if (!doesOverlap && overlapSegment is not null)
                {
                    features.Add(new Feature(new LineString(overlapSegment.ToArray())));
                    foreach (var extra in additionalSegments)
                        features.Add(new Feature(new LineString(extra.ToArray())));
                    additionalSegments.Clear();
                    overlapSegment = null;
                }
            }
        }

        if (overlapSegment is not null)
            features.Add(new Feature(new LineString(overlapSegment.ToArray())));

        return new FeatureCollection(features.ToArray());
    }

    // `getCoords(x).sort()` da fonte: o sort DEFAULT do JS compara os pares como STRING
    private static Position[] JsDefaultSort(Position[] pair)
    {
        static string Key(Position p) =>
            p.Lon.ToString("R", CultureInfo.InvariantCulture) + "," + p.Lat.ToString("R", CultureInfo.InvariantCulture);
        var sorted = (Position[])pair.Clone();
        if (string.CompareOrdinal(Key(sorted[0]), Key(sorted[1])) > 0)
            (sorted[0], sorted[1]) = (sorted[1], sorted[0]);
        return sorted;
    }

    /// <summary>Anexa o segmento à ponta compatível; null se não toca as pontas.</summary>
    private static List<Position>? ConcatSegment(List<Position> line, Position[] segment)
    {
        var start = line[0];
        var end = line[^1];

        if (segment[0] == start)
            line.Insert(0, segment[1]);
        else if (segment[0] == end)
            line.Add(segment[1]);
        else if (segment[1] == start)
            line.Insert(0, segment[0]);
        else if (segment[1] == end)
            line.Add(segment[0]);
        else
            return null;
        return line;
    }
}
