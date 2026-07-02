namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Grade de células retangulares sobre uma bbox — `@turf/rectangle-grid`. As dimensões
    /// viram graus via o equivalente angular (`convertLength(→degrees)` do @turf) e a grade
    /// é centrada na bbox. <paramref name="mask"/> filtra por interseção.
    /// </summary>
    public static FeatureCollection RectangleGrid(
        BBox bbox,
        Units.Length cellWidth,
        Units.Length cellHeight,
        Geometry? mask = null
    )
    {
        var results = new List<Feature>();
        var west = bbox.Values[0];
        var south = bbox.Values[1];
        var east = bbox.Values[2];
        var north = bbox.Values[3];

        var bboxWidth = east - west;
        var cellWidthDeg = cellWidth.Degrees;
        var bboxHeight = north - south;
        var cellHeightDeg = cellHeight.Degrees;

        var columns = (int)Math.Floor(Math.Abs(bboxWidth) / cellWidthDeg);
        var rows = (int)Math.Floor(Math.Abs(bboxHeight) / cellHeightDeg);
        var deltaX = (bboxWidth - columns * cellWidthDeg) / 2;
        var deltaY = (bboxHeight - rows * cellHeightDeg) / 2;

        var currentX = west + deltaX;
        for (var column = 0; column < columns; column++)
        {
            var currentY = south + deltaY;
            for (var row = 0; row < rows; row++)
            {
                var cellPolygon = new Polygon(
                    new[]
                    {
                        new[]
                        {
                            new Position(currentX, currentY),
                            new Position(currentX, currentY + cellHeightDeg),
                            new Position(currentX + cellWidthDeg, currentY + cellHeightDeg),
                            new Position(currentX + cellWidthDeg, currentY),
                            new Position(currentX, currentY),
                        },
                    }
                );
                if (mask is null || BooleanIntersects(mask, cellPolygon))
                    results.Add(new Feature(cellPolygon));
                currentY += cellHeightDeg;
            }
            currentX += cellWidthDeg;
        }

        return new FeatureCollection(results.ToArray());
    }

    /// <summary>Grade de células quadradas — `@turf/square-grid` (= rectangleGrid com lado igual).</summary>
    public static FeatureCollection SquareGrid(BBox bbox, Units.Length cellSide, Geometry? mask = null) =>
        RectangleGrid(bbox, cellSide, cellSide, mask);
}
