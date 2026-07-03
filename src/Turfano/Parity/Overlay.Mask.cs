using Turfano.GeoJson.Polyclip;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// "World minus the polygon" — `@turf/mask`: the world ring (or the custom mask)
    /// gains the EXTERIOR rings of the input polygon's union as holes. The union uses the
    /// native polyclip engine (the same one @turf runs).
    /// </summary>
    public static Polygon Mask(Geometry polygon, Polygon? mask = null)
    {
        // createMask da fonte: anel do mundo ou as coordenadas da máscara
        var maskRings = new List<Position[]>();
        if (mask is not null)
        {
            maskRings.AddRange(mask.Coordinates);
        }
        else
        {
            maskRings.Add(
                new[]
                {
                    new Position(180, 90),
                    new Position(-180, 90),
                    new Position(-180, -90),
                    new Position(180, -90),
                    new Position(180, 90),
                }
            );
        }

        // união (normalização) do polígono de entrada, como polyclip.union(coords)
        var unioned = new OperationRun().Run(
            PolyclipOperationType.Union,
            ToMultiPolygonCoordinates(polygon),
            Array.Empty<Position[][][]>()
        );

        // cada exterior da união vira um furo da máscara
        foreach (var contour in unioned)
            maskRings.Add(contour[0]);

        return new Polygon(maskRings.ToArray());
    }
}
