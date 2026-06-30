using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Divide a linha em pedaços de comprimento `segmentLength` — `@turf/line-chunk`.</summary>
    public static FeatureCollection LineChunk(LineString line, Units.Length segmentLength)
    {
        var results = new List<Feature>();
        var lineLength = Length(line).Kilometers;
        var segment = segmentLength.Kilometers;

        var numberOfSegments = lineLength / segment;
        var count =
            numberOfSegments == Math.Floor(numberOfSegments)
                ? (int)numberOfSegments
                : (int)Math.Floor(numberOfSegments) + 1;

        for (var i = 0; i < count; i++)
        {
            var outline = LineSliceAlong(
                line,
                Units.Length.FromKilometers(segment * i),
                Units.Length.FromKilometers(segment * (i + 1))
            );
            results.Add(new Feature(outline));
        }
        return new FeatureCollection(results.ToArray());
    }

    /// <summary>Pontos de auto-interseção de uma linha/polígono — `@turf/kinks`.</summary>
    public static FeatureCollection Kinks(Geometry geometry)
    {
        var results = new List<Feature>();
        foreach (var lineCoords in KinkRings(geometry))
        {
            var n = lineCoords.Length;
            for (var i = 0; i < n - 1; i++)
                for (var k = i + 1; k < n - 1; k++)
                {
                    if (k == i + 1)
                        continue; // segmentos adjacentes compartilham vértice
                    if (i == 0 && k == n - 2 && lineCoords[0].Equals(lineCoords[^1]))
                        continue; // anel fechado: par que compartilha o vértice de fechamento
                    var point = SegmentIntersectionPoint(
                        lineCoords[i],
                        lineCoords[i + 1],
                        lineCoords[k],
                        lineCoords[k + 1]
                    );
                    if (point is { } p)
                        results.Add(new Feature(new Point(p)));
                }
        }
        return new FeatureCollection(results.ToArray());
    }

    private static IEnumerable<Position[]> KinkRings(Geometry g)
    {
        switch (g)
        {
            case LineString ls:
                yield return ls.Coordinates;
                break;
            case MultiLineString mls:
                foreach (var l in mls.Coordinates)
                    yield return l;
                break;
            case Polygon poly:
                foreach (var r in poly.Coordinates)
                    yield return r;
                break;
            case MultiPolygon mpoly:
                foreach (var pol in mpoly.Coordinates)
                    foreach (var r in pol)
                        yield return r;
                break;
        }
    }

    private static Position? SegmentIntersectionPoint(Position a1, Position a2, Position b1, Position b2)
    {
        var denominator = (b2.Lat - b1.Lat) * (a2.Lon - a1.Lon) - (b2.Lon - b1.Lon) * (a2.Lat - a1.Lat);
        if (denominator == 0)
            return null;
        var uA = ((b2.Lon - b1.Lon) * (a1.Lat - b1.Lat) - (b2.Lat - b1.Lat) * (a1.Lon - b1.Lon)) / denominator;
        var uB = ((a2.Lon - a1.Lon) * (a1.Lat - b1.Lat) - (a2.Lat - a1.Lat) * (a1.Lon - b1.Lon)) / denominator;
        if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
            return new Position(a1.Lon + uA * (a2.Lon - a1.Lon), a1.Lat + uA * (a2.Lat - a1.Lat));
        return null;
    }
}
