using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Isobandas de uma grade de pontos com valores — porte fiel do `@turf/isobands`: um
    /// `MultiPolygon` por faixa `[breaks[i-1], breaks[i])`, montado do marching squares dos
    /// dois limiares (o superior invertido), com fechamento pelas bordas da matriz e
    /// agrupamento de anéis aninhados (furos) por contenção.
    /// </summary>
    public static FeatureCollection Isobands(
        FeatureCollection pointGrid,
        double[] breaks,
        string zProperty = "elevation"
    )
    {
        var matrix = GridToMatrix(pointGrid, zProperty);
        var dx = matrix[0].Length;
        if (matrix.Length < 2 || dx < 2)
            throw new ArgumentException("Matrix of points must be at least 2x2");
        for (var i = 1; i < matrix.Length; i++)
        {
            if (matrix[i].Length != dx)
                throw new ArgumentException("Matrix of points is not uniform in the x dimension");
        }

        var (west, south, east, north) = PointsBounds(pointGrid);
        var scaleX = (east - west) / (dx - 1);
        var scaleY = (north - south) / (matrix.Length - 1);

        var results = new List<Feature>();
        List<ContourVertex[]>? prevSegments = null;
        for (var i = 1; i < breaks.Length; i++)
        {
            prevSegments ??= MarchingSquaresSegments(matrix, breaks[0]);

            var upperBand = breaks[i];
            var lowerBand = breaks[i - 1];
            var segments = MarchingSquaresSegments(matrix, upperBand);
            var reversedSegments = segments.Select(s => new[] { s[1], s[0] }).ToList();

            var combined = new List<ContourVertex[]>(prevSegments.Count + reversedSegments.Count);
            combined.AddRange(prevSegments);
            combined.AddRange(reversedSegments);

            var rings = AssembleContourRings(combined, matrix);
            var orderedRings = OrderRingsByArea(rings);
            var polygons = GroupNestedRings(orderedRings);

            // faixa "cheia" sem contornos: a matriz inteira pertence à banda
            if (polygons.Count == 0 && matrix[0][0] < upperBand && matrix[0][0] >= lowerBand)
            {
                polygons.Add(
                    new List<List<ContourVertex>>
                    {
                        new()
                        {
                            new ContourVertex(0, 0),
                            new ContourVertex(dx - 1, 0),
                            new ContourVertex(dx - 1, matrix.Length - 1),
                            new ContourVertex(0, matrix.Length - 1),
                            new ContourVertex(0, 0),
                        },
                    }
                );
            }

            var coordinates = polygons
                .Select(group =>
                    group
                        .Select(ring =>
                            ring.Select(v => new Position(v.X * scaleX + west, v.Y * scaleY + south)).ToArray()
                        )
                        .ToArray()
                )
                .ToArray();
            var properties = new JsonObject { [zProperty] = lowerBand + "-" + upperBand };
            results.Add(new Feature(new MultiPolygon(coordinates), properties));

            prevSegments = segments;
        }

        return new FeatureCollection(results.ToArray());
    }

    /// <summary>`assembleRings` da fonte: encadeia e FECHA contornos abertos andando pelas
    /// bordas da matriz (cada lado com sua preferência), descartando anéis com &lt; 4 pontos.</summary>
    private static List<List<ContourVertex>> AssembleContourRings(List<ContourVertex[]> segments, double[][] matrix)
    {
        var dy = matrix.Length;
        var dx = matrix[0].Length;
        var contours = ChainContourSegments(segments);
        var result = new List<List<ContourVertex>>();

        while (contours.Count > 0)
        {
            var contour = contours[0];
            if (contour[0] == contour[^1])
            {
                result.Add(contour);
                contours.RemoveAt(0);
                continue;
            }

            var end = contour[^1];
            int match;
            ContourVertex corner;
            if (end.X == 0 && end.Y != 0)
            {
                // borda esquerda: começa na esquerda, abaixo do fim; prefere o mais alto
                match = FindAdjacentContour(contours, c => c[0].X == 0 && c[0].Y < end.Y, (a, b) => b[0].Y - a[0].Y);
                corner = new ContourVertex(0, 0);
            }
            else if (end.Y == 0 && end.X != dx - 1)
            {
                // borda de baixo: à direita do fim; prefere o mais à esquerda
                match = FindAdjacentContour(contours, c => c[0].Y == 0 && c[0].X > end.X, (a, b) => a[0].X - b[0].X);
                corner = new ContourVertex(dx - 1, 0);
            }
            else if (end.X == dx - 1 && end.Y != dy - 1)
            {
                // borda direita: acima do fim; prefere o mais baixo
                match = FindAdjacentContour(contours, c => c[0].X == dx - 1 && c[0].Y > end.Y, (a, b) => a[0].Y - b[0].Y);
                corner = new ContourVertex(dx - 1, dy - 1);
            }
            else if (end.Y == dy - 1 && end.X != 0)
            {
                // borda de cima: à esquerda do fim; prefere o mais à direita
                match = FindAdjacentContour(contours, c => c[0].Y == dy - 1 && c[0].X < end.X, (a, b) => b[0].X - a[0].X);
                corner = new ContourVertex(0, dy - 1);
            }
            else
            {
                throw new InvalidOperationException("Contour not closed but is not along an edge");
            }

            if (match == -1)
            {
                contour.Add(corner);
            }
            else if (match == 0)
            {
                contour.Add(contour[0]);
                result.Add(contour);
                contours.RemoveAt(0);
            }
            else
            {
                var matchedContour = contours[match];
                contours.RemoveAt(match);
                contour.AddRange(matchedContour);
            }
        }

        for (var i = 0; i < result.Count; i++)
        {
            if (result[i].Count < 4)
            {
                result.RemoveAt(i);
                i--;
            }
        }
        return result;
    }

    private static int FindAdjacentContour(
        List<List<ContourVertex>> contours,
        Func<List<ContourVertex>, bool> test,
        Func<List<ContourVertex>, List<ContourVertex>, double> sort
    )
    {
        var match = -1;
        for (var j = 0; j < contours.Count; j++)
        {
            if (test(contours[j]) && (match == -1 || sort(contours[match], contours[j]) > 0))
                match = j;
        }
        return match;
    }

    /// <summary>Ordena anéis por área (esférica, no espaço da matriz) decrescente — a fonte
    /// usa `turf.area`.</summary>
    private static List<List<ContourVertex>> OrderRingsByArea(List<List<ContourVertex>> rings) =>
        rings
            .OrderByDescending(ring => Area(MatrixSpacePolygon(ring)).SquareMeters)
            .ToList();

    /// <summary>Agrupa anéis aninhados: o maior vira exterior; contidos (e não dentro de um
    /// furo já agrupado) viram furos — `groupNestedRings` da fonte.</summary>
    private static List<List<List<ContourVertex>>> GroupNestedRings(List<List<ContourVertex>> orderedRings)
    {
        var entries = orderedRings.Select(r => (Ring: r, Grouped: false)).ToArray();
        var groups = new List<List<List<ContourVertex>>>();

        while (entries.Any(e => !e.Grouped))
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Grouped)
                    continue;
                var group = new List<List<ContourVertex>> { entries[i].Ring };
                entries[i].Grouped = true;
                var outerMost = MatrixSpacePolygon(entries[i].Ring);

                for (var j = i + 1; j < entries.Length; j++)
                {
                    if (entries[j].Grouped)
                        continue;
                    var candidate = MatrixSpacePolygon(entries[j].Ring);
                    if (!RingIsInside(candidate, outerMost))
                        continue;
                    var insideExistingHole = false;
                    for (var k = 1; k < group.Count; k++)
                    {
                        if (RingIsInside(candidate, MatrixSpacePolygon(group[k])))
                        {
                            insideExistingHole = true;
                            break;
                        }
                    }
                    if (insideExistingHole)
                        continue;
                    group.Add(entries[j].Ring);
                    entries[j].Grouped = true;
                }
                groups.Add(group);
            }
        }
        return groups;
    }

    private static Polygon MatrixSpacePolygon(List<ContourVertex> ring) =>
        new(new[] { ring.Select(v => new Position(v.X, v.Y)).ToArray() });

    /// <summary>Todos os vértices de `test` dentro de `target` (`isInside` da fonte).</summary>
    private static bool RingIsInside(Polygon test, Polygon target)
    {
        foreach (var vertex in test.Coordinates[0])
        {
            if (!BooleanPointInPolygon(new Point(vertex), target))
                return false;
        }
        return true;
    }
}
