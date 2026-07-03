namespace Turfano.GeoJson;

// US4 — `@turf/random`, com `Random.Shared` no lugar do `Math.random` (sem estado global
// próprio, sem seed público — mesmo contrato do @turf). A aritmética de `randomPolygon` e
// `randomLineString` é copiada tal e qual da fonte, incluindo o que parecem peculiaridades
// dela (ex.: `randomLineString` usa `Math.tan` sobre a INCLINAÇÃO bruta ao computar o
// próximo ângulo, não `Math.atan`/`atan2` — preservado por fidelidade).
public static partial class Geo
{
    /// <summary>Random position inside a bbox (the whole globe, if omitted) — `@turf/random randomPosition`.</summary>
    public static Position RandomPosition(BBox? bbox = null)
    {
        CheckRandomBBox(bbox);
        return RandomPositionUnchecked(bbox);
    }

    /// <summary>`count` random points inside a bbox — `@turf/random randomPoint`.</summary>
    public static FeatureCollection RandomPoint(int count = 1, BBox? bbox = null)
    {
        CheckRandomBBox(bbox);
        var features = new Feature[count];
        for (var i = 0; i < count; i++)
            features[i] = new Feature(Point(RandomPositionUnchecked(bbox)));
        return new FeatureCollection(features);
    }

    /// <summary>
    /// `count` random lines with <paramref name="numVertices"/> vertices — `@turf/random
    /// randomLineString`. Each vertex follows the previous one by up to <paramref name="maxLength"/>
    /// (decimal degrees) at an angle derived from the previous segment, rotated by up to
    /// ±<paramref name="maxRotation"/> radians.
    /// </summary>
    public static FeatureCollection RandomLineString(
        int count = 1,
        BBox? bbox = null,
        int numVertices = 10,
        double maxLength = 0.0001,
        double maxRotation = Math.PI / 8
    )
    {
        CheckRandomBBox(bbox);
        if (numVertices < 2)
            numVertices = 10;

        var features = new Feature[count];
        for (var i = 0; i < count; i++)
        {
            var vertices = new Position[numVertices];
            vertices[0] = RandomPositionUnchecked(bbox);

            for (var j = 0; j < numVertices - 1; j++)
            {
                double priorAngle;
                if (j == 0)
                {
                    priorAngle = Random.Shared.NextDouble() * 2 * Math.PI;
                }
                else
                {
                    var current = vertices[j];
                    var previous = vertices[j - 1];
                    // Fiel à fonte: `Math.tan` sobre a inclinação bruta (Δlat/Δlon), não o
                    // ângulo real do segmento (que seria `Math.atan`).
                    priorAngle = Math.Tan((current.Lat - previous.Lat) / (current.Lon - previous.Lon));
                }

                var angle = priorAngle + (Random.Shared.NextDouble() - 0.5) * maxRotation * 2;
                var distance = Random.Shared.NextDouble() * maxLength;
                vertices[j + 1] = new Position(
                    vertices[j].Lon + distance * Math.Cos(angle),
                    vertices[j].Lat + distance * Math.Sin(angle)
                );
            }

            features[i] = new Feature(LineString(vertices));
        }
        return new FeatureCollection(features);
    }

    /// <summary>
    /// `count` random polygons (rings with <paramref name="numVertices"/> + 1 positions,
    /// already closed) — `@turf/random randomPolygon`. Each vertex is at most
    /// <paramref name="maxRadialLength"/> decimal degrees from the center (drawn inside the
    /// bbox shrunk by that radius, so the whole polygon fits inside it).
    /// </summary>
    public static FeatureCollection RandomPolygon(
        int count = 1,
        BBox? bbox = null,
        int numVertices = 10,
        double maxRadialLength = 10
    )
    {
        CheckRandomBBox(bbox);
        var effectiveBBox = bbox ?? new BBox(-180, -90, 180, 90);
        var v = effectiveBBox.Values;

        var bboxWidth = Math.Abs(v[0] - v[2]);
        var bboxHeight = Math.Abs(v[1] - v[3]);
        var maxRadius = Math.Min(bboxWidth / 2, bboxHeight / 2);
        if (maxRadialLength > maxRadius)
            throw new ArgumentException("max_radial_length is greater than the radius of the bbox");

        var paddedBBox = new BBox(
            v[0] + maxRadialLength,
            v[1] + maxRadialLength,
            v[2] - maxRadialLength,
            v[3] - maxRadialLength
        );

        var features = new Feature[count];
        for (var i = 0; i < count; i++)
        {
            // `num_vertices + 1` ângulos cumulativos aleatórios, normalizados por
            // `2π / (soma total)`; o ÚLTIMO vértice gerado é depois SOBRESCRITO pelo
            // primeiro (fecha o anel sem alterar seu tamanho: numVertices+1 posições).
            var circleOffsets = new double[numVertices + 1];
            for (var j = 0; j < circleOffsets.Length; j++)
                circleOffsets[j] = Random.Shared.NextDouble();
            for (var j = 1; j < circleOffsets.Length; j++)
                circleOffsets[j] += circleOffsets[j - 1];

            var total = circleOffsets[^1];
            var vertices = new Position[circleOffsets.Length];
            for (var j = 0; j < circleOffsets.Length; j++)
            {
                var cur = circleOffsets[j] * 2 * Math.PI / total;
                var radialScaler = Random.Shared.NextDouble();
                vertices[j] = new Position(
                    radialScaler * maxRadialLength * Math.Sin(cur),
                    radialScaler * maxRadialLength * Math.Cos(cur)
                );
            }
            vertices[^1] = vertices[0];
            Array.Reverse(vertices);

            var hub = RandomPositionUnchecked(paddedBBox);
            for (var j = 0; j < vertices.Length; j++)
                vertices[j] = new Position(vertices[j].Lon + hub.Lon, vertices[j].Lat + hub.Lat);

            features[i] = new Feature(Polygon(vertices));
        }
        return new FeatureCollection(features);
    }

    private static Position RandomPositionUnchecked(BBox? bbox) =>
        bbox is { } box ? CoordInBBox(box) : new Position(RandomLon(), RandomLat());

    private static void CheckRandomBBox(BBox? bbox)
    {
        if (bbox is not { } box)
            return;
        var length = box.Values.Length;
        if (length != 4 && length != 6)
            throw new ArgumentException("bbox must be an Array of 4 or 6 numbers", nameof(bbox));
    }

    private static double RandomSigned() => Random.Shared.NextDouble() - 0.5;

    private static double RandomLon() => RandomSigned() * 360;

    private static double RandomLat() => RandomSigned() * 180;

    private static Position CoordInBBox(BBox bbox)
    {
        var v = bbox.Values;
        return new Position(
            Random.Shared.NextDouble() * (v[2] - v[0]) + v[0],
            Random.Shared.NextDouble() * (v[3] - v[1]) + v[1]
        );
    }
}
