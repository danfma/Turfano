namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Grade de pontos sobre uma bbox — `@turf/point-grid`. O passo em graus vem da RAZÃO
    /// entre o lado da célula e as distâncias great-circle das bordas da bbox (semântica da
    /// fonte — diferente do rectangle-grid). <paramref name="mask"/> filtra por `within`.
    /// </summary>
    public static FeatureCollection PointGrid(BBox bbox, Units.Length cellSide, Geometry? mask = null)
    {
        var results = new List<Feature>();
        var west = bbox.Values[0];
        var south = bbox.Values[1];
        var east = bbox.Values[2];
        var north = bbox.Values[3];

        var cellSideKm = cellSide.Kilometers;
        var xFraction = cellSideKm / Distance(new Position(west, south), new Position(east, south)).Kilometers;
        var cellWidth = xFraction * (east - west);
        var yFraction = cellSideKm / Distance(new Position(west, south), new Position(west, north)).Kilometers;
        var cellHeight = yFraction * (north - south);

        var bboxWidth = east - west;
        var bboxHeight = north - south;
        var columns = Math.Floor(bboxWidth / cellWidth);
        var rows = Math.Floor(bboxHeight / cellHeight);
        var deltaX = (bboxWidth - columns * cellWidth) / 2;
        var deltaY = (bboxHeight - rows * cellHeight) / 2;

        var currentX = west + deltaX;
        while (currentX <= east)
        {
            var currentY = south + deltaY;
            while (currentY <= north)
            {
                var cellPoint = new Point(new Position(currentX, currentY));
                if (mask is null || BooleanWithin(cellPoint, mask))
                    results.Add(new Feature(cellPoint));
                currentY += cellHeight;
            }
            currentX += cellWidth;
        }

        return new FeatureCollection(results.ToArray());
    }
}
