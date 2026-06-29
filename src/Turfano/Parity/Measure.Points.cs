using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>Ponto de destino (great-circle) a partir de origem, distância e rumo — `@turf/destination`.</summary>
    public static Point Destination(Position origin, Units.Length distance, Units.Angle bearing)
    {
        var lon1 = origin.Lon * RadiansPerDegree;
        var lat1 = origin.Lat * RadiansPerDegree;
        var rad = distance.Radians;
        var br = bearing.Radians;

        var lat2 = Math.Asin(
            Math.Sin(lat1) * Math.Cos(rad) + Math.Cos(lat1) * Math.Sin(rad) * Math.Cos(br)
        );
        var lon2 =
            lon1
            + Math.Atan2(
                Math.Sin(br) * Math.Sin(rad) * Math.Cos(lat1),
                Math.Cos(rad) - Math.Sin(lat1) * Math.Sin(lat2)
            );

        return new Point(new Position(lon2 / RadiansPerDegree, lat2 / RadiansPerDegree));
    }

    /// <summary>Ponto médio geodésico entre dois pontos — `@turf/midpoint`.</summary>
    public static Point Midpoint(Point a, Point b)
    {
        var dist = Distance(a.Coordinates, b.Coordinates);
        var bearing = Bearing(a.Coordinates, b.Coordinates);
        return Destination(a.Coordinates, dist / 2, bearing);
    }

    /// <summary>Centro da bounding box de uma geometria — `@turf/center`.</summary>
    public static Point Center(Geometry geometry)
    {
        var b = Bbox(geometry).Values;
        return new Point(new Position((b[0] + b[2]) / 2, (b[1] + b[3]) / 2));
    }

    /// <summary>
    /// Centro de massa — `@turf/center-of-mass`. Para `Point`/`Polygon` reproduz o algoritmo
    /// do @turf (shoelace ponderado em torno do `centroid`); para outros tipos cai em
    /// `Centroid` (o caminho de convex hull do @turf fica para a onda de transformação).
    /// </summary>
    public static Point CenterOfMass(Geometry geometry)
    {
        if (geometry is Point p)
            return p;
        if (geometry is not Polygon poly)
            return Centroid(geometry);

        var coords = new List<Position>();
        EachPosition(poly, excludeWrapCoord: false, c => coords.Add(c));

        var centre = Centroid(poly).Coordinates;
        double tx = centre.Lon,
            ty = centre.Lat;
        double sx = 0,
            sy = 0,
            sArea = 0;

        for (var i = 0; i < coords.Count - 1; i++)
        {
            double xi = coords[i].Lon - tx,
                yi = coords[i].Lat - ty;
            double xj = coords[i + 1].Lon - tx,
                yj = coords[i + 1].Lat - ty;
            var a = xi * yj - xj * yi;
            sArea += a;
            sx += (xi + xj) * a;
            sy += (yi + yj) * a;
        }

        if (sArea == 0)
            return new Point(centre);

        var areaFactor = 1 / (6 * (sArea * 0.5));
        return new Point(new Position(tx + areaFactor * sx, ty + areaFactor * sy));
    }

    /// <summary>Ponto a uma distância ao longo de uma linha — `@turf/along`.</summary>
    public static Point Along(LineString line, Units.Length distance)
    {
        var coords = line.Coordinates;
        var travelled = Units.Length.Zero;

        for (var i = 0; i < coords.Length; i++)
        {
            if (distance.Meters >= travelled.Meters && i == coords.Length - 1)
                break;

            if (travelled.Meters >= distance.Meters)
            {
                var overshot = distance - travelled;
                if (overshot.Meters == 0)
                    return new Point(coords[i]);

                var direction = Bearing(coords[i], coords[i - 1]) - Units.Angle.FromDegrees(180);
                return Destination(coords[i], overshot, direction);
            }

            travelled += Distance(coords[i], coords[i + 1]);
        }

        return new Point(coords[^1]);
    }

    /// <summary>Destino ao longo de uma linha de rumo constante — `@turf/rhumb-destination`.</summary>
    public static Point RhumbDestination(Position origin, Units.Length distance, Units.Angle bearing)
    {
        var phi1 = origin.Lat * RadiansPerDegree;
        var lambda1 = origin.Lon * RadiansPerDegree;
        var br = bearing.Radians;
        var delta = distance.Meters / EarthRadiusMeters;

        var phi2 = phi1 + delta * Math.Cos(br);
        if (Math.Abs(phi2) > Math.PI / 2)
            phi2 = phi2 > 0 ? Math.PI - phi2 : -Math.PI - phi2;

        // @turf: q = (phi2 - phi1) / deltaPsi  (a impl NTS existente usa deltaPsi/sin(br),
        // que diverge — por isso validamos contra o @turf e não a replicamos aqui).
        var deltaPsi = Math.Log(Math.Tan(phi2 / 2 + Math.PI / 4) / Math.Tan(phi1 / 2 + Math.PI / 4));
        var q = Math.Abs(deltaPsi) > 1e-12 ? (phi2 - phi1) / deltaPsi : Math.Cos(phi1);
        var lambda2 = lambda1 + delta * Math.Sin(br) / q;
        lambda2 = (lambda2 + 3 * Math.PI) % (2 * Math.PI) - Math.PI;

        return new Point(new Position(lambda2 / RadiansPerDegree, phi2 / RadiansPerDegree));
    }
}
