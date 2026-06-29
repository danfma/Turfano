using GeoJson = Turfano.GeoJson;

namespace Turfano;

public static partial class Turf
{
    /// <summary>Bounding box `[west, south, east, north]` de uma geometria (`@turf/bbox`).</summary>
    public static GeoJson.BBox Bbox(GeoJson.Geometry geometry)
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

        return new GeoJson.BBox(west, south, east, north);
    }

    /// <summary>Polígono retangular a partir de uma bbox (`@turf/bbox-polygon`).</summary>
    public static GeoJson.Polygon BboxPolygon(GeoJson.BBox bbox)
    {
        var v = bbox.Values;
        double west = v[0],
            south = v[1],
            east = v[2],
            north = v[3];
        var ring = new[]
        {
            new GeoJson.Position(west, south),
            new GeoJson.Position(east, south),
            new GeoJson.Position(east, north),
            new GeoJson.Position(west, north),
            new GeoJson.Position(west, south),
        };
        return new GeoJson.Polygon(new[] { ring });
    }

    /// <summary>Polígono envelope (bbox como polígono) de uma geometria (`@turf/envelope`).</summary>
    public static GeoJson.Polygon Envelope(GeoJson.Geometry geometry) => BboxPolygon(Bbox(geometry));

    /// <summary>Quadra a bbox em torno do centro do lado maior (`@turf/square`).</summary>
    public static GeoJson.BBox Square(GeoJson.BBox bbox)
    {
        var v = bbox.Values;
        double west = v[0],
            south = v[1],
            east = v[2],
            north = v[3];

        var horizontal = Distance(new GeoJson.Position(west, south), new GeoJson.Position(east, south)).Meters;
        var vertical = Distance(new GeoJson.Position(west, south), new GeoJson.Position(west, north)).Meters;

        if (horizontal >= vertical)
        {
            var midY = (south + north) / 2;
            var halfW = (east - west) / 2;
            return new GeoJson.BBox(west, midY - halfW, east, midY + halfW);
        }
        else
        {
            var midX = (west + east) / 2;
            var halfH = (north - south) / 2;
            return new GeoJson.BBox(midX - halfH, south, midX + halfH, north);
        }
    }
}
