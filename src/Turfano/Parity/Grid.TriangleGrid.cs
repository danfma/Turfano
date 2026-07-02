namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Grade de triângulos retos alternados sobre uma bbox — `@turf/triangle-grid`
    /// (a orientação alterna pela paridade de coluna/linha, exatamente como a fonte).
    /// <paramref name="mask"/> filtra por interseção (overlay nativo).
    /// </summary>
    public static FeatureCollection TriangleGrid(BBox bbox, Units.Length cellSide, Geometry? mask = null)
    {
        var west = bbox.Values[0];
        var south = bbox.Values[1];
        var east = bbox.Values[2];
        var north = bbox.Values[3];

        var cellSideKm = cellSide.Kilometers;
        var xFraction = cellSideKm / Distance(new Position(west, south), new Position(east, south)).Kilometers;
        var cellWidth = xFraction * (east - west);
        var yFraction = cellSideKm / Distance(new Position(west, south), new Position(west, north)).Kilometers;
        var cellHeight = yFraction * (north - south);

        var results = new List<Feature>();
        var xi = 0;
        var currentX = west;
        while (currentX <= east)
        {
            var yi = 0;
            var currentY = south;
            while (currentY <= north)
            {
                Polygon? cellTriangle1 = null;
                Polygon? cellTriangle2 = null;

                if (xi % 2 == 0 && yi % 2 == 0)
                {
                    cellTriangle1 = TriangleCell(
                        (currentX, currentY),
                        (currentX, currentY + cellHeight),
                        (currentX + cellWidth, currentY)
                    );
                    cellTriangle2 = TriangleCell(
                        (currentX, currentY + cellHeight),
                        (currentX + cellWidth, currentY + cellHeight),
                        (currentX + cellWidth, currentY)
                    );
                }
                else if (xi % 2 == 0 && yi % 2 == 1)
                {
                    cellTriangle1 = TriangleCell(
                        (currentX, currentY),
                        (currentX + cellWidth, currentY + cellHeight),
                        (currentX + cellWidth, currentY)
                    );
                    cellTriangle2 = TriangleCell(
                        (currentX, currentY),
                        (currentX, currentY + cellHeight),
                        (currentX + cellWidth, currentY + cellHeight)
                    );
                }
                else if (yi % 2 == 0 && xi % 2 == 1)
                {
                    cellTriangle1 = TriangleCell(
                        (currentX, currentY),
                        (currentX, currentY + cellHeight),
                        (currentX + cellWidth, currentY + cellHeight)
                    );
                    cellTriangle2 = TriangleCell(
                        (currentX, currentY),
                        (currentX + cellWidth, currentY + cellHeight),
                        (currentX + cellWidth, currentY)
                    );
                }
                else if (yi % 2 == 1 && xi % 2 == 1)
                {
                    cellTriangle1 = TriangleCell(
                        (currentX, currentY),
                        (currentX, currentY + cellHeight),
                        (currentX + cellWidth, currentY)
                    );
                    cellTriangle2 = TriangleCell(
                        (currentX, currentY + cellHeight),
                        (currentX + cellWidth, currentY + cellHeight),
                        (currentX + cellWidth, currentY)
                    );
                }

                if (mask is not null)
                {
                    if (Intersect(mask, cellTriangle1!) is not null)
                        results.Add(new Feature(cellTriangle1));
                    if (Intersect(mask, cellTriangle2!) is not null)
                        results.Add(new Feature(cellTriangle2));
                }
                else
                {
                    results.Add(new Feature(cellTriangle1));
                    results.Add(new Feature(cellTriangle2));
                }

                currentY += cellHeight;
                yi++;
            }
            xi++;
            currentX += cellWidth;
        }

        return new FeatureCollection(results.ToArray());
    }

    private static Polygon TriangleCell(
        (double X, double Y) a,
        (double X, double Y) b,
        (double X, double Y) c
    ) =>
        new(
            new[]
            {
                new[]
                {
                    new Position(a.X, a.Y),
                    new Position(b.X, b.Y),
                    new Position(c.X, c.Y),
                    new Position(a.X, a.Y),
                },
            }
        );
}
