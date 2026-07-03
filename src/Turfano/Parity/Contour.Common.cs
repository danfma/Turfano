namespace Turfano.GeoJson;

// Núcleo compartilhado de isolines/isobands — porte fiel do marching squares EMBUTIDO nos
// bundles do @turf (lib/grid-to-matrix + getSegments/isoContours). Coordenadas em espaço de
// MATRIZ (célula = 1) até a reescala final para a bbox da grade.
public static partial class Geo
{
    private readonly record struct ContourVertex(double X, double Y);

    /// <summary>Sorts the point grid into a z[y][x] matrix (flip=true: ascending latitude).</summary>
    private static double[][] GridToMatrix(FeatureCollection pointGrid, string zProperty)
    {
        // agrupa por latitude EXATA; linhas por lat crescente; colunas por lon crescente
        var rows = pointGrid
            .Features.GroupBy(f => ((Point)f.Geometry!).Coordinates.Lat)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(f => ((Point)f.Geometry!).Coordinates.Lon).ToArray())
            .ToArray();

        var matrix = new double[rows.Length][];
        for (var r = 0; r < rows.Length; r++)
        {
            var row = new double[rows[r].Length];
            for (var c = 0; c < rows[r].Length; c++)
            {
                // quirk truthy da fonte: z ausente, 0 ou NaN viram 0
                var value = NumberOrNull(rows[r][c].Properties?[zProperty]);
                row[c] = value is { } z && z != 0 && !double.IsNaN(z) ? z : 0;
            }
            matrix[r] = row;
        }
        return matrix;
    }

    /// <summary>The 16 marching squares cases (`getSegments`/`isoContours` in the source).</summary>
    private static List<ContourVertex[]> MarchingSquaresSegments(double[][] matrix, double threshold)
    {
        var segments = new List<ContourVertex[]>();
        var dy = matrix.Length;
        var dx = matrix[0].Length;

        double Frac(double z0, double z1)
        {
            if (z0 == z1)
                return 0.5;
            var t = (threshold - z0) / (z1 - z0);
            return t > 1 ? 1 : t < 0 ? 0 : t;
        }

        for (var y = 0; y < dy - 1; y++)
        {
            for (var x = 0; x < dx - 1; x++)
            {
                var topRight = matrix[y + 1][x + 1];
                var bottomRight = matrix[y][x + 1];
                var bottomLeft = matrix[y][x];
                var topLeft = matrix[y + 1][x];
                var grid =
                    (topLeft >= threshold ? 8 : 0)
                    | (topRight >= threshold ? 4 : 0)
                    | (bottomRight >= threshold ? 2 : 0)
                    | (bottomLeft >= threshold ? 1 : 0);

                switch (grid)
                {
                    case 0:
                    case 15:
                        continue;
                    case 1:
                        segments.Add(new[] { new ContourVertex(x + Frac(bottomLeft, bottomRight), y), new ContourVertex(x, y + Frac(bottomLeft, topLeft)) });
                        break;
                    case 2:
                        segments.Add(new[] { new ContourVertex(x + 1, y + Frac(bottomRight, topRight)), new ContourVertex(x + Frac(bottomLeft, bottomRight), y) });
                        break;
                    case 3:
                        segments.Add(new[] { new ContourVertex(x + 1, y + Frac(bottomRight, topRight)), new ContourVertex(x, y + Frac(bottomLeft, topLeft)) });
                        break;
                    case 4:
                        segments.Add(new[] { new ContourVertex(x + Frac(topLeft, topRight), y + 1), new ContourVertex(x + 1, y + Frac(bottomRight, topRight)) });
                        break;
                    case 5:
                    {
                        var above = (topLeft + topRight + bottomRight + bottomLeft) / 4 >= threshold;
                        if (above)
                        {
                            segments.Add(new[] { new ContourVertex(x + Frac(topLeft, topRight), y + 1), new ContourVertex(x, y + Frac(bottomLeft, topLeft)) });
                            segments.Add(new[] { new ContourVertex(x + Frac(bottomLeft, bottomRight), y), new ContourVertex(x + 1, y + Frac(bottomRight, topRight)) });
                        }
                        else
                        {
                            segments.Add(new[] { new ContourVertex(x + Frac(topLeft, topRight), y + 1), new ContourVertex(x + 1, y + Frac(bottomRight, topRight)) });
                            segments.Add(new[] { new ContourVertex(x + Frac(bottomLeft, bottomRight), y), new ContourVertex(x, y + Frac(bottomLeft, topLeft)) });
                        }
                        break;
                    }
                    case 6:
                        segments.Add(new[] { new ContourVertex(x + Frac(topLeft, topRight), y + 1), new ContourVertex(x + Frac(bottomLeft, bottomRight), y) });
                        break;
                    case 7:
                        segments.Add(new[] { new ContourVertex(x + Frac(topLeft, topRight), y + 1), new ContourVertex(x, y + Frac(bottomLeft, topLeft)) });
                        break;
                    case 8:
                        segments.Add(new[] { new ContourVertex(x, y + Frac(bottomLeft, topLeft)), new ContourVertex(x + Frac(topLeft, topRight), y + 1) });
                        break;
                    case 9:
                        segments.Add(new[] { new ContourVertex(x + Frac(bottomLeft, bottomRight), y), new ContourVertex(x + Frac(topLeft, topRight), y + 1) });
                        break;
                    case 10:
                    {
                        var above = (topLeft + topRight + bottomRight + bottomLeft) / 4 >= threshold;
                        if (above)
                        {
                            segments.Add(new[] { new ContourVertex(x, y + Frac(bottomLeft, topLeft)), new ContourVertex(x + Frac(bottomLeft, bottomRight), y) });
                            segments.Add(new[] { new ContourVertex(x + 1, y + Frac(bottomRight, topRight)), new ContourVertex(x + Frac(topLeft, topRight), y + 1) });
                        }
                        else
                        {
                            segments.Add(new[] { new ContourVertex(x, y + Frac(bottomLeft, topLeft)), new ContourVertex(x + Frac(topLeft, topRight), y + 1) });
                            segments.Add(new[] { new ContourVertex(x + 1, y + Frac(bottomRight, topRight)), new ContourVertex(x + Frac(bottomLeft, bottomRight), y) });
                        }
                        break;
                    }
                    case 11:
                        segments.Add(new[] { new ContourVertex(x + 1, y + Frac(bottomRight, topRight)), new ContourVertex(x + Frac(topLeft, topRight), y + 1) });
                        break;
                    case 12:
                        segments.Add(new[] { new ContourVertex(x, y + Frac(bottomLeft, topLeft)), new ContourVertex(x + 1, y + Frac(bottomRight, topRight)) });
                        break;
                    case 13:
                        segments.Add(new[] { new ContourVertex(x + Frac(bottomLeft, bottomRight), y), new ContourVertex(x + 1, y + Frac(bottomRight, topRight)) });
                        break;
                    case 14:
                        segments.Add(new[] { new ContourVertex(x, y + Frac(bottomLeft, topLeft)), new ContourVertex(x + Frac(bottomLeft, bottomRight), y) });
                        break;
                }
            }
        }
        return segments;
    }

    /// <summary>Chains segments into contours by exact endpoint equality (the source uses ===).</summary>
    private static List<List<ContourVertex>> ChainContourSegments(List<ContourVertex[]> segments)
    {
        var contours = new List<List<ContourVertex>>();
        while (segments.Count > 0)
        {
            var contour = new List<ContourVertex>(segments[0]);
            segments.RemoveAt(0);
            contours.Add(contour);
            bool found;
            do
            {
                found = false;
                for (var i = 0; i < segments.Count; i++)
                {
                    var segment = segments[i];
                    if (segment[0] == contour[^1])
                    {
                        found = true;
                        contour.Add(segment[1]);
                        segments.RemoveAt(i);
                        break;
                    }
                    if (segment[1] == contour[0])
                    {
                        found = true;
                        contour.Insert(0, segment[0]);
                        segments.RemoveAt(i);
                        break;
                    }
                }
            } while (found);
        }
        return contours;
    }

    /// <summary>Bbox (min/max lon/lat) of a point collection.</summary>
    private static (double West, double South, double East, double North) PointsBounds(FeatureCollection points)
    {
        double west = double.PositiveInfinity,
            south = double.PositiveInfinity,
            east = double.NegativeInfinity,
            north = double.NegativeInfinity;
        foreach (var feature in points.Features)
        {
            var position = ((Point)feature.Geometry!).Coordinates;
            if (position.Lon < west)
                west = position.Lon;
            if (position.Lat < south)
                south = position.Lat;
            if (position.Lon > east)
                east = position.Lon;
            if (position.Lat > north)
                north = position.Lat;
        }
        return (west, south, east, north);
    }
}
