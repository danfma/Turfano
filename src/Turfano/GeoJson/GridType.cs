namespace Turfano.GeoJson;

/// <summary>Tipo de grade usado por `Geo.Interpolate` — `@turf/interpolate` (gridType).</summary>
public enum GridType
{
    /// <summary>Grade de polígonos quadrados.</summary>
    Square,

    /// <summary>Grade de pontos.</summary>
    Point,

    /// <summary>Grade de hexágonos.</summary>
    Hex,

    /// <summary>Grade de triângulos.</summary>
    Triangle,
}
