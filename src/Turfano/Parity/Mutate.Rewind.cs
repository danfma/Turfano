namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Orients the rings (`@turf/rewind`): by default the outer ring becomes **counterclockwise**
    /// and holes **clockwise** (RFC 7946); `reverse` flips this. LineStrings are left unchanged.
    /// </summary>
    public static Geometry Rewind(Geometry geometry, bool reverse = false) =>
        geometry switch
        {
            Polygon poly => new Polygon(RewindRings(poly.Coordinates, reverse)),
            MultiPolygon mpoly => new MultiPolygon(
                mpoly.Coordinates.Select(p => RewindRings(p, reverse)).ToArray()
            ),
            GeometryCollection gc => new GeometryCollection(
                gc.Geometries.Select(sub => Rewind(sub, reverse)).ToArray()
            ),
            _ => geometry,
        };

    private static Position[][] RewindRings(Position[][] rings, bool reverse)
    {
        var result = new Position[rings.Length][];
        for (var i = 0; i < rings.Length; i++)
        {
            var ring = (Position[])rings[i].Clone();
            var clockwise = BooleanClockwise(new LineString(ring));
            // externo (i==0): anti-horário; furos: horário.
            var shouldReverse = i == 0 ? clockwise != reverse : clockwise == reverse;
            if (shouldReverse)
                Array.Reverse(ring);
            result[i] = ring;
        }
        return result;
    }
}
