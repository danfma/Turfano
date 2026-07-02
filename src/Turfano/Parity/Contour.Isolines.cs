using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Isolinhas de uma grade de pontos com valores — porte fiel do `@turf/isolines`
    /// (marching squares embutido no bundle; um `MultiLineString` por break, reescalado
    /// do espaço da matriz para a bbox da grade).
    /// </summary>
    public static FeatureCollection Isolines(
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

        // reescala do espaço da matriz para a bbox da grade
        var (west, south, east, north) = PointsBounds(pointGrid);
        var scaleX = (east - west) / (dx - 1);
        var scaleY = (north - south) / (matrix.Length - 1);

        var results = new Feature[breaks.Length];
        for (var i = 0; i < breaks.Length; i++)
        {
            var threshold = breaks[i];
            var contours = ChainContourSegments(MarchingSquaresSegments(matrix, threshold));
            var lines = contours
                .Select(contour =>
                    contour.Select(vertex => new Position(vertex.X * scaleX + west, vertex.Y * scaleY + south)).ToArray()
                )
                .ToArray();
            var properties = new JsonObject { [zProperty] = threshold };
            results[i] = new Feature(new MultiLineString(lines), properties);
        }

        return new FeatureCollection(results);
    }
}
