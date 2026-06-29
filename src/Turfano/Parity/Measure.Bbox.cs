namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Bounding box `[west, south, east, north]` — `@turf/bbox`.</summary>
    public static BBox Bbox(Geometry geometry)
    {
        double west = double.MaxValue,
            south = double.MaxValue,
            east = double.MinValue,
            north = double.MinValue;

        EachPosition(
            geometry,
            excludeWrapCoord: false,
            p =>
            {
                if (p.Lon < west)
                    west = p.Lon;
                if (p.Lat < south)
                    south = p.Lat;
                if (p.Lon > east)
                    east = p.Lon;
                if (p.Lat > north)
                    north = p.Lat;
            }
        );

        return new BBox(west, south, east, north);
    }

    /// <summary>Polígono retangular a partir de uma bbox — `@turf/bbox-polygon`.</summary>
    public static Polygon BboxPolygon(BBox bbox)
    {
        var v = bbox.Values;
        double west = v[0],
            south = v[1],
            east = v[2],
            north = v[3];
        var ring = new[]
        {
            new Position(west, south),
            new Position(east, south),
            new Position(east, north),
            new Position(west, north),
            new Position(west, south),
        };
        return new Polygon(new[] { ring });
    }

    /// <summary>Polígono envelope (bbox como polígono) — `@turf/envelope`.</summary>
    public static Polygon Envelope(Geometry geometry) => BboxPolygon(Bbox(geometry));

    /// <summary>Quadra a bbox em torno do centro do lado maior — `@turf/square`.</summary>
    public static BBox Square(BBox bbox)
    {
        var v = bbox.Values;
        double west = v[0],
            south = v[1],
            east = v[2],
            north = v[3];

        var horizontal = Distance(new Position(west, south), new Position(east, south)).Meters;
        var vertical = Distance(new Position(west, south), new Position(west, north)).Meters;

        if (horizontal >= vertical)
        {
            var midY = (south + north) / 2;
            var halfW = (east - west) / 2;
            return new BBox(west, midY - halfW, east, midY + halfW);
        }
        else
        {
            var midX = (west + east) / 2;
            var halfH = (north - south) / 2;
            return new BBox(midX - halfH, south, midX + halfH, north);
        }
    }
}
