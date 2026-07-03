namespace Turfano.GeoJson;

/// <summary>Grid type used by `Geo.Interpolate` — `@turf/interpolate` (gridType).</summary>
public enum GridType
{
    /// <summary>Grid of square polygons.</summary>
    Square,

    /// <summary>Grid of points.</summary>
    Point,

    /// <summary>Grid of hexagons.</summary>
    Hex,

    /// <summary>Grid of triangles.</summary>
    Triangle,
}
