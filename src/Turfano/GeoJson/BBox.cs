namespace Turfano.GeoJson;

/// <summary>
/// Bounding box GeoJSON (RFC 7946): array de 2×n números (4 para 2D
/// `[west, south, east, north]`, 6 para 3D `[west, south, minAlt, east, north, maxAlt]`).
/// </summary>
public readonly struct BBox
{
    public double[] Values { get; }

    public BBox(params double[] values) => Values = values;

    /// <summary>2 (2D) ou 3 (3D), conforme o número de valores (4 ou 6).</summary>
    public int Dimension => (Values?.Length ?? 0) == 6 ? 3 : 2;
}
