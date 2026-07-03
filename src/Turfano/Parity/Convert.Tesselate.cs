namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Tessellates a polygon into triangles — `@turf/tesselate` (ported earcut). Accepts a
    /// Polygon or MultiPolygon; the source flattens with dim=3 (a missing z becomes an empty
    /// slot — here, NaN) and builds one triangle per index triple.
    /// </summary>
    public static FeatureCollection Tesselate(Geometry polygon)
    {
        var features = polygon switch
        {
            Polygon single => TesselateRings(single.Coordinates),
            MultiPolygon multi => multi.Coordinates.SelectMany(TesselateRings).ToList(),
            _ => throw new ArgumentException("input must be a Polygon or MultiPolygon", nameof(polygon)),
        };
        return new FeatureCollection(features.ToArray());
    }

    private static List<Feature> TesselateRings(Position[][] rings)
    {
        // flattenCoords da fonte: dim=3, z ausente → slot "vazio" (NaN como sentinela)
        const int dim = 3;
        var vertexCount = rings.Sum(ring => ring.Length);
        var vertices = new double[vertexCount * dim];
        var holes = new List<int>();
        var offset = 0;
        var holeIndex = 0;
        for (var i = 0; i < rings.Length; i++)
        {
            foreach (var position in rings[i])
            {
                vertices[offset++] = position.Lon;
                vertices[offset++] = position.Lat;
                vertices[offset++] = position.Alt ?? double.NaN;
            }
            if (i > 0)
            {
                holeIndex += rings[i - 1].Length;
                holes.Add(holeIndex);
            }
        }

        var indices = Earcut.Tessellate(vertices, holes.ToArray(), dim);

        var features = new List<Feature>();
        for (var i = 0; i + 2 < indices.Count; i += 3)
        {
            var ring = new Position[4];
            for (var k = 0; k < 3; k++)
            {
                var index = indices[i + k];
                var z = vertices[index * dim + 2];
                ring[k] = new Position(vertices[index * dim], vertices[index * dim + 1], double.IsNaN(z) ? null : z);
            }
            ring[3] = ring[0];
            features.Add(new Feature(new Polygon(new[] { ring })));
        }
        return features;
    }
}
