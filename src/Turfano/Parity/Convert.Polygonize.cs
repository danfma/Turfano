namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Monta polígonos a partir de uma rede de linhas — porte fiel do `@turf/polygonize`
    /// (grafo de arestas estilo GEOS: remove dangles e cut-edges, extrai anéis e classifica
    /// shells/furos). Nativo — sem NTS.
    /// </summary>
    public static FeatureCollection Polygonize(FeatureCollection lines)
    {
        var lineCoordinates = new List<Position[]>();
        foreach (var feature in lines.Features)
        {
            switch (feature.Geometry)
            {
                case LineString lineString:
                    lineCoordinates.Add(lineString.Coordinates);
                    break;
                case MultiLineString multiLineString:
                    lineCoordinates.AddRange(multiLineString.Coordinates);
                    break;
            }
        }

        var graph = PolygonizeGraph.FromLines(lineCoordinates);
        graph.DeleteDangles();
        graph.DeleteCutEdges();

        var holes = new List<PolygonizeEdgeRing>();
        var shells = new List<PolygonizeEdgeRing>();
        foreach (var edgeRing in graph.GetEdgeRings())
        {
            if (edgeRing.IsHole())
                holes.Add(edgeRing);
            else
                shells.Add(edgeRing);
        }

        // furos contidos num shell viram polígonos próprios (comportamento do @turf)
        foreach (var hole in holes)
        {
            if (PolygonizeEdgeRing.FindEdgeRingContaining(hole, shells) is not null)
                shells.Add(hole);
        }

        return new FeatureCollection(shells.Select(shell => new Feature(shell.ToPolygon())).ToArray());
    }
}
