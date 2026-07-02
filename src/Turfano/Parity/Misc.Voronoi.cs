using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Diagrama de Voronoi dos pontos, cortado pela bbox — `@turf/voronoi` (porte do
    /// d3-voronoi). Uma célula por ponto, na ordem de entrada, com as propriedades do ponto
    /// clonadas; células totalmente fora do extent são omitidas (no @turf viram buracos
    /// `undefined` na coleção).
    /// </summary>
    public static FeatureCollection Voronoi(FeatureCollection points, BBox bbox)
    {
        // Math.round do JS (half-up em direção a +∞), não o Math.Round (banker's) do C#
        static double RoundToEpsilon(double value) => Math.Floor(value / 1e-6 + 0.5) * 1e-6;

        var sites = new List<FortuneVoronoi.VoronoiSite>(points.Features.Length);
        for (var i = 0; i < points.Features.Length; i++)
        {
            var coordinates = ((Point)points.Features[i].Geometry!).Coordinates;
            sites.Add(new FortuneVoronoi.VoronoiSite(RoundToEpsilon(coordinates.Lon), RoundToEpsilon(coordinates.Lat), i));
        }

        var diagram = new FortuneVoronoi(
            sites,
            (bbox.Values[0], bbox.Values[1], bbox.Values[2], bbox.Values[3])
        );
        var polygons = diagram.Polygons();

        var features = new List<Feature>();
        for (var i = 0; i < polygons.Length; i++)
        {
            if (polygons[i] is not { } ring)
                continue;
            var positions = ring.Select(vertex => new Position(vertex[0], vertex[1])).ToList();
            positions.Add(positions[0]);
            var properties = (JsonObject?)points.Features[i].Properties?.DeepClone();
            features.Add(new Feature(new Polygon(new[] { positions.ToArray() }), properties));
        }
        return new FeatureCollection(features.ToArray());
    }
}
