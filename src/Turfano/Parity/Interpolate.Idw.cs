using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Interpolação IDW (inverse distance weighting) sobre uma grade — `@turf/interpolate`.
    /// <paramref name="gridType"/>: `GridType.Square` (default), `Point`, `Hex` ou `Triangle`.
    /// O valor de cada amostra vem de `properties[property]` ou da 3ª coordenada.
    /// </summary>
    public static FeatureCollection Interpolate(
        FeatureCollection points,
        Units.Length cellSize,
        GridType gridType = GridType.Square,
        string property = "elevation",
        double weight = 1
    )
    {
        // bbox da coleção de pontos (bbox(points) da fonte)
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
        var box = new BBox(west, south, east, north);

        var grid = gridType switch
        {
            GridType.Point => PointGrid(box, cellSize),
            GridType.Square => SquareGrid(box, cellSize),
            GridType.Hex => HexGrid(box, cellSize),
            GridType.Triangle => TriangleGrid(box, cellSize),
            _ => throw new ArgumentOutOfRangeException(
                nameof(gridType),
                gridType,
                "gridType inválido"
            ),
        };

        var results = new List<Feature>();
        foreach (var gridFeature in grid.Features)
        {
            var zw = 0.0;
            var sw = 0.0;
            foreach (var sample in points.Features)
            {
                var gridPoint =
                    gridType == GridType.Point
                        ? (Point)gridFeature.Geometry!
                        : Centroid(gridFeature.Geometry!);
                var d = Distance(
                    gridPoint.Coordinates,
                    ((Point)sample.Geometry!).Coordinates
                ).Kilometers;

                var zValue =
                    NumberOrNull(sample.Properties?[property])
                    ?? ((Point)sample.Geometry!).Coordinates.Alt
                    ?? throw new InvalidOperationException("zValue is missing");

                if (d == 0)
                    zw = zValue;
                var w = 1 / Math.Pow(d, weight);
                sw += w;
                zw += w * zValue;
            }

            var properties = new JsonObject { [property] = zw / sw };
            results.Add(new Feature(gridFeature.Geometry, properties));
        }

        return new FeatureCollection(results.ToArray());
    }
}
