namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Monta polígonos a partir de uma rede de linhas — `@turf/polygonize`. Usa o `Polygonizer`
    /// do NetTopologySuite (motor interino) via a ponte `Turfano.Interop.NtsBridge`; a forma
    /// casa com o `@turf` (a ordem dos vértices do anel pode diferir).
    /// </summary>
    public static FeatureCollection Polygonize(FeatureCollection lines)
    {
        var polygonizer = new NetTopologySuite.Operation.Polygonize.Polygonizer();

        foreach (var feature in lines.Features)
        {
            switch (feature.Geometry)
            {
                case LineString ls:
                    polygonizer.Add(Turfano.Interop.NtsBridge.ToNts(ls));
                    break;
                case MultiLineString mls:
                    foreach (var line in mls.Coordinates)
                        polygonizer.Add(Turfano.Interop.NtsBridge.ToNts(new LineString(line)));
                    break;
            }
        }

        var features = new List<Feature>();
        foreach (NetTopologySuite.Geometries.Geometry polygon in polygonizer.GetPolygons())
            features.Add(new Feature(Turfano.Interop.NtsBridge.FromNts(polygon)));

        return new FeatureCollection(features.ToArray());
    }
}
