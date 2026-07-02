namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Grade hexagonal (flat-top, offset em colunas ímpares) sobre uma bbox —
    /// `@turf/hex-grid`. Com <paramref name="triangles"/> = true, cada hexágono vira seus
    /// 6 triângulos. <paramref name="mask"/> filtra por interseção (overlay nativo).
    /// </summary>
    public static FeatureCollection HexGrid(
        BBox bbox,
        Units.Length cellSide,
        Geometry? mask = null,
        bool triangles = false
    )
    {
        var west = bbox.Values[0];
        var south = bbox.Values[1];
        var east = bbox.Values[2];
        var north = bbox.Values[3];
        var centerY = (south + north) / 2;
        var centerX = (west + east) / 2;

        var cellSideKm = cellSide.Kilometers;
        var xFraction =
            cellSideKm * 2 / Distance(new Position(west, centerY), new Position(east, centerY)).Kilometers;
        var cellWidth = xFraction * (east - west);
        var yFraction =
            cellSideKm * 2 / Distance(new Position(centerX, south), new Position(centerX, north)).Kilometers;
        var cellHeight = yFraction * (north - south);
        var radius = cellWidth / 2;

        var hexWidth = radius * 2;
        var hexHeight = Math.Sqrt(3) / 2 * cellHeight;

        var boxWidth = east - west;
        var boxHeight = north - south;

        var xInterval = 3.0 / 4 * hexWidth;
        var yInterval = hexHeight;

        var xSpan = (boxWidth - hexWidth) / (hexWidth - radius / 2);
        var xCount = (int)Math.Floor(xSpan);
        var xAdjust = (xCount * xInterval - radius / 2 - boxWidth) / 2 - radius / 2 + xInterval / 2;

        var yCount = (int)Math.Floor((boxHeight - hexHeight) / hexHeight);
        var yAdjust = (boxHeight - yCount * hexHeight) / 2;
        var hasOffsetY = yCount * hexHeight - boxHeight > hexHeight / 2;
        if (hasOffsetY)
            yAdjust -= hexHeight / 4;

        var cosines = new double[6];
        var sines = new double[6];
        for (var i = 0; i < 6; i++)
        {
            var angle = 2 * Math.PI / 6 * i;
            cosines[i] = Math.Cos(angle);
            sines[i] = Math.Sin(angle);
        }

        var results = new List<Feature>();
        for (var x = 0; x <= xCount; x++)
        {
            for (var y = 0; y <= yCount; y++)
            {
                var isOdd = x % 2 == 1;
                if (y == 0 && isOdd)
                    continue;
                if (y == 0 && hasOffsetY)
                    continue;

                var hexCenterX = x * xInterval + west - xAdjust;
                var hexCenterY = y * yInterval + south + yAdjust;
                if (isOdd)
                    hexCenterY -= hexHeight / 2;

                if (triangles)
                {
                    foreach (var triangle in HexTriangles(hexCenterX, hexCenterY, cellWidth / 2, cellHeight / 2, cosines, sines))
                    {
                        if (mask is null || Intersect(mask, triangle) is not null)
                            results.Add(new Feature(triangle));
                    }
                }
                else
                {
                    var hex = Hexagon(hexCenterX, hexCenterY, cellWidth / 2, cellHeight / 2, cosines, sines);
                    if (mask is null || Intersect(mask, hex) is not null)
                        results.Add(new Feature(hex));
                }
            }
        }

        return new FeatureCollection(results.ToArray());
    }

    private static Polygon Hexagon(
        double centerX,
        double centerY,
        double radiusX,
        double radiusY,
        double[] cosines,
        double[] sines
    )
    {
        var vertices = new Position[7];
        for (var i = 0; i < 6; i++)
            vertices[i] = new Position(centerX + radiusX * cosines[i], centerY + radiusY * sines[i]);
        vertices[6] = vertices[0];
        return new Polygon(new[] { vertices });
    }

    private static Polygon[] HexTriangles(
        double centerX,
        double centerY,
        double radiusX,
        double radiusY,
        double[] cosines,
        double[] sines
    )
    {
        var hexagonTriangles = new Polygon[6];
        for (var i = 0; i < 6; i++)
        {
            var vertices = new[]
            {
                new Position(centerX, centerY),
                new Position(centerX + radiusX * cosines[i], centerY + radiusY * sines[i]),
                new Position(centerX + radiusX * cosines[(i + 1) % 6], centerY + radiusY * sines[(i + 1) % 6]),
                new Position(centerX, centerY),
            };
            hexagonTriangles[i] = new Polygon(new[] { vertices });
        }
        return hexagonTriangles;
    }
}
