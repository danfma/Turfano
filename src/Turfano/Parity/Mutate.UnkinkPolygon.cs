using System.Globalization;
using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Decompõe polígonos com auto-interseção em polígonos simples — porte fiel do
    /// `@turf/unkink-polygon` (simplepolygon de M. Fraeye embutido no bundle): grafo de
    /// pseudo-vértices nas interseções (achadas via rbush) e caminhada pelos anéis.
    /// As propriedades de cada feature de entrada são preservadas nas saídas.
    /// </summary>
    public static FeatureCollection UnkinkPolygon(Geometry geojson)
    {
        var features = new List<Feature>();
        foreach (var (polygon, properties) in FlattenPolygons(geojson, null))
        {
            foreach (var ring in SimplePolygonRings(polygon))
                features.Add(new Feature(new Polygon(new[] { ring }), (JsonObject?)properties?.DeepClone()));
        }
        return new FeatureCollection(features.ToArray());
    }

    /// <summary>Sobrecarga para coleções (propriedades preservadas por feature).</summary>
    public static FeatureCollection UnkinkPolygon(FeatureCollection collection)
    {
        var features = new List<Feature>();
        foreach (var feature in collection.Features)
        {
            if (feature.Geometry is null)
                continue;
            foreach (var (polygon, properties) in FlattenPolygons(feature.Geometry, feature.Properties))
            {
                foreach (var ring in SimplePolygonRings(polygon))
                    features.Add(new Feature(new Polygon(new[] { ring }), (JsonObject?)properties?.DeepClone()));
            }
        }
        return new FeatureCollection(features.ToArray());
    }

    private static IEnumerable<(Polygon Polygon, JsonObject? Properties)> FlattenPolygons(
        Geometry geometry,
        JsonObject? properties
    )
    {
        switch (geometry)
        {
            case Polygon polygon:
                yield return (polygon, properties);
                break;
            case MultiPolygon multiPolygon:
                foreach (var coords in multiPolygon.Coordinates)
                    yield return (new Polygon(coords), properties);
                break;
        }
    }

    private static string CoordKey(Position p) =>
        p.Lon.ToString("R", CultureInfo.InvariantCulture) + "," + p.Lat.ToString("R", CultureInfo.InvariantCulture);

    private sealed class PseudoVertex(Position coord, double param, (int Ring, int Edge) ringAndEdgeIn, (int Ring, int Edge) ringAndEdgeOut)
    {
        public readonly Position Coord = coord;
        public readonly double Param = param;
        public readonly (int Ring, int Edge) RingAndEdgeIn = ringAndEdgeIn;
        public readonly (int Ring, int Edge) RingAndEdgeOut = ringAndEdgeOut;
        public int NextIsectAlongEdgeIn;
    }

    private sealed class IntersectionNode(
        Position coord,
        (int Ring, int Edge) ringAndEdge1,
        (int Ring, int Edge) ringAndEdge2,
        bool ringAndEdge1Walkable,
        bool ringAndEdge2Walkable
    )
    {
        public readonly Position Coord = coord;
        public readonly (int Ring, int Edge) RingAndEdge1 = ringAndEdge1;
        public readonly (int Ring, int Edge) RingAndEdge2 = ringAndEdge2;
        public int NextIsectAlongRingAndEdge1;
        public int NextIsectAlongRingAndEdge2;
        public bool RingAndEdge1Walkable = ringAndEdge1Walkable;
        public bool RingAndEdge2Walkable = ringAndEdge2Walkable;
    }

    private readonly record struct SelfIntersection(
        Position Point,
        int Ring0,
        int Edge0,
        double Frac0,
        int Ring1,
        int Edge1,
        bool Unique
    );

    /// <summary>simplepolygon: anéis simples resultantes (só as coordenadas — as
    /// propriedades parent/winding do lib são descartadas pelo @turf/unkink-polygon).</summary>
    private static List<Position[]> SimplePolygonRings(Polygon polygonInput)
    {
        // fecha anéis se necessário (cópia, sem mutar a entrada)
        var rings = polygonInput
            .Coordinates.Select(r => r[0] == r[^1] ? (Position[])r.Clone() : r.Append(r[0]).ToArray())
            .ToArray();
        var numRings = rings.Length;

        var vertexKeys = new HashSet<string>();
        var numVertices = 0;
        foreach (var ring in rings)
        {
            for (var j = 0; j < ring.Length - 1; j++)
            {
                if (!vertexKeys.Add(CoordKey(ring[j])))
                    throw new ArgumentException(
                        "The input polygon may not have duplicate vertices (except for the first and last vertex of each ring)"
                    );
                numVertices++;
            }
        }

        var selfIntersections = FindSelfIntersections(rings);

        if (selfIntersections.Count == 0)
            return rings.ToList();

        // pseudo-vértices por (anel, aresta) + lista de interseções (vértices do anel primeiro)
        var pseudoByRingAndEdge = new List<List<List<PseudoVertex>>>();
        var isectList = new List<IntersectionNode>();
        for (var i = 0; i < numRings; i++)
        {
            pseudoByRingAndEdge.Add(new List<List<PseudoVertex>>());
            var ringLength = rings[i].Length - 1;
            for (var j = 0; j < ringLength; j++)
            {
                pseudoByRingAndEdge[i].Add(
                    new List<PseudoVertex>
                    {
                        new(rings[i][Modulo(j + 1, ringLength)], 1, (i, j), (i, Modulo(j + 1, ringLength))),
                    }
                );
                isectList.Add(
                    new IntersectionNode(rings[i][j], (i, Modulo(j - 1, ringLength)), (i, j), false, true)
                );
            }
        }

        foreach (var isect in selfIntersections)
        {
            pseudoByRingAndEdge[isect.Ring0][isect.Edge0].Add(
                new PseudoVertex(isect.Point, isect.Frac0, (isect.Ring0, isect.Edge0), (isect.Ring1, isect.Edge1))
            );
            if (isect.Unique)
                isectList.Add(
                    new IntersectionNode(isect.Point, (isect.Ring0, isect.Edge0), (isect.Ring1, isect.Edge1), true, true)
                );
        }

        foreach (var perRing in pseudoByRingAndEdge)
        {
            for (var j = 0; j < perRing.Count; j++)
                perRing[j] = perRing[j].OrderBy(v => v.Param).ToList();
        }

        // índice das interseções por coordenada (a fonte usa rbush pontual; coords são únicas)
        var isectIndexByCoord = new Dictionary<string, int>();
        for (var i = 0; i < isectList.Count; i++)
            isectIndexByCoord[CoordKey(isectList[i].Coord)] = i;

        // liga cada pseudo-vértice à próxima interseção pela aresta de entrada
        for (var i = 0; i < pseudoByRingAndEdge.Count; i++)
        {
            var ringLength = rings[i].Length - 1;
            for (var j = 0; j < pseudoByRingAndEdge[i].Count; j++)
            {
                for (var k = 0; k < pseudoByRingAndEdge[i][j].Count; k++)
                {
                    var coordToFind =
                        k == pseudoByRingAndEdge[i][j].Count - 1
                            ? pseudoByRingAndEdge[i][Modulo(j + 1, ringLength)][0].Coord
                            : pseudoByRingAndEdge[i][j][k + 1].Coord;
                    pseudoByRingAndEdge[i][j][k].NextIsectAlongEdgeIn = isectIndexByCoord[CoordKey(coordToFind)];
                }
            }
        }

        // transfere os links para as interseções
        for (var i = 0; i < pseudoByRingAndEdge.Count; i++)
        {
            for (var j = 0; j < pseudoByRingAndEdge[i].Count; j++)
            {
                foreach (var pseudo in pseudoByRingAndEdge[i][j])
                {
                    var l = isectIndexByCoord[CoordKey(pseudo.Coord)];
                    if (l < numVertices)
                    {
                        isectList[l].NextIsectAlongRingAndEdge2 = pseudo.NextIsectAlongEdgeIn;
                    }
                    else if (isectList[l].RingAndEdge1 == pseudo.RingAndEdgeIn)
                    {
                        isectList[l].NextIsectAlongRingAndEdge1 = pseudo.NextIsectAlongEdgeIn;
                    }
                    else
                    {
                        isectList[l].NextIsectAlongRingAndEdge2 = pseudo.NextIsectAlongEdgeIn;
                    }
                }
            }
        }

        // fila inicial: interseção mais à esquerda de cada anel, com o winding local
        var queue = new List<(int Isect, int Parent, int Winding)>();
        var vertexCursor = 0;
        for (var j = 0; j < numRings; j++)
        {
            var leftIsect = vertexCursor;
            var ringLength = rings[j].Length - 1;
            for (var k = 0; k < ringLength; k++)
            {
                if (isectList[vertexCursor].Coord.Lon < isectList[leftIsect].Coord.Lon)
                    leftIsect = vertexCursor;
                vertexCursor++;
            }
            var isectAfterLeft = isectList[leftIsect].NextIsectAlongRingAndEdge2;
            var isectBeforeLeft = 0;
            for (var k = 0; k < isectList.Count; k++)
            {
                if (isectList[k].NextIsectAlongRingAndEdge1 == leftIsect || isectList[k].NextIsectAlongRingAndEdge2 == leftIsect)
                {
                    isectBeforeLeft = k;
                    break;
                }
            }
            var windingAtIsect = IsConvexTriple(
                isectList[isectBeforeLeft].Coord,
                isectList[leftIsect].Coord,
                isectList[isectAfterLeft].Coord,
                righthanded: true
            )
                ? 1
                : -1;
            queue.Add((leftIsect, -1, windingAtIsect));
        }

        // sort da fonte: coords comparadas COMO STRING, decrescente (o pop pega a menor)
        queue = queue
            .OrderByDescending(entry => CoordKey(isectList[entry.Isect].Coord), StringComparer.Ordinal)
            .ToList();

        var outputRings = new List<Position[]>();
        while (queue.Count > 0)
        {
            var popped = queue[^1];
            queue.RemoveAt(queue.Count - 1);
            var startIsect = popped.Isect;
            var currentWinding = popped.Winding;
            var currentRingCoords = new List<Position> { isectList[startIsect].Coord };
            var currentIsect = startIsect;

            (int Ring, int Edge) walkingRingAndEdge;
            int nxtIsect;
            if (isectList[startIsect].RingAndEdge1Walkable)
            {
                walkingRingAndEdge = isectList[startIsect].RingAndEdge1;
                nxtIsect = isectList[startIsect].NextIsectAlongRingAndEdge1;
            }
            else
            {
                walkingRingAndEdge = isectList[startIsect].RingAndEdge2;
                nxtIsect = isectList[startIsect].NextIsectAlongRingAndEdge2;
            }

            while (isectList[startIsect].Coord != isectList[nxtIsect].Coord)
            {
                currentRingCoords.Add(isectList[nxtIsect].Coord);

                var inQueue = queue.FindIndex(entry => entry.Isect == nxtIsect);
                if (inQueue >= 0)
                    queue.RemoveAt(inQueue);

                if (walkingRingAndEdge == isectList[nxtIsect].RingAndEdge1)
                {
                    walkingRingAndEdge = isectList[nxtIsect].RingAndEdge2;
                    isectList[nxtIsect].RingAndEdge2Walkable = false;
                    if (isectList[nxtIsect].RingAndEdge1Walkable)
                    {
                        var convex = IsConvexTriple(
                            isectList[currentIsect].Coord,
                            isectList[nxtIsect].Coord,
                            isectList[isectList[nxtIsect].NextIsectAlongRingAndEdge2].Coord,
                            currentWinding == 1
                        );
                        queue.Add((nxtIsect, 0, convex ? -currentWinding : currentWinding));
                    }
                    currentIsect = nxtIsect;
                    nxtIsect = isectList[nxtIsect].NextIsectAlongRingAndEdge2;
                }
                else
                {
                    walkingRingAndEdge = isectList[nxtIsect].RingAndEdge1;
                    isectList[nxtIsect].RingAndEdge1Walkable = false;
                    if (isectList[nxtIsect].RingAndEdge2Walkable)
                    {
                        var convex = IsConvexTriple(
                            isectList[currentIsect].Coord,
                            isectList[nxtIsect].Coord,
                            isectList[isectList[nxtIsect].NextIsectAlongRingAndEdge1].Coord,
                            currentWinding == 1
                        );
                        queue.Add((nxtIsect, 0, convex ? -currentWinding : currentWinding));
                    }
                    currentIsect = nxtIsect;
                    nxtIsect = isectList[nxtIsect].NextIsectAlongRingAndEdge1;
                }
            }

            currentRingCoords.Add(isectList[nxtIsect].Coord);
            outputRings.Add(currentRingCoords.ToArray());
        }

        return outputRings;
    }

    /// <summary>Auto-interseções (interiores) por aresta, na ordem da varredura da fonte
    /// (busca rbush por aresta; dedup por coordenada-string).</summary>
    private static List<SelfIntersection> FindSelfIntersections(Position[][] rings)
    {
        var output = new List<SelfIntersection>();
        var seen = new HashSet<string>();

        var edges = new List<RBushItem<(int Ring, int Edge)>>();
        for (var ring = 0; ring < rings.Length; ring++)
        {
            for (var edge = 0; edge < rings[ring].Length - 1; edge++)
                edges.Add(EdgeItem(rings, ring, edge));
        }
        var tree = new RBushIndex<(int Ring, int Edge)>();
        tree.Load(edges);

        for (var ringA = 0; ringA < rings.Length; ringA++)
        {
            for (var edgeA = 0; edgeA < rings[ringA].Length - 1; edgeA++)
            {
                foreach (var match in tree.Search(EdgeItem(rings, ringA, edgeA)))
                {
                    var (ringB, edgeB) = match.Item;
                    var start0 = rings[ringA][edgeA];
                    var end0 = rings[ringA][edgeA + 1];
                    var start1 = rings[ringB][edgeB];
                    var end1 = rings[ringB][edgeB + 1];

                    var isect = InfiniteLineIntersect(start0, end0, start1, end1);
                    if (isect is not { } point)
                        continue;

                    var frac0 =
                        end0.Lon != start0.Lon
                            ? (point.Lon - start0.Lon) / (end0.Lon - start0.Lon)
                            : (point.Lat - start0.Lat) / (end0.Lat - start0.Lat);
                    var frac1 =
                        end1.Lon != start1.Lon
                            ? (point.Lon - start1.Lon) / (end1.Lon - start1.Lon)
                            : (point.Lat - start1.Lat) / (end1.Lat - start1.Lat);
                    if (frac0 >= 1 || frac0 <= 0 || frac1 >= 1 || frac1 <= 0)
                        continue;

                    var unique = seen.Add(CoordKey(point));
                    output.Add(new SelfIntersection(point, ringA, edgeA, frac0, ringB, edgeB, unique));
                }
            }
        }
        return output;
    }

    private static RBushItem<(int Ring, int Edge)> EdgeItem(Position[][] rings, int ring, int edge)
    {
        var start = rings[ring][edge];
        var end = rings[ring][edge + 1];
        return new RBushItem<(int, int)>(
            Math.Min(start.Lon, end.Lon),
            Math.Min(start.Lat, end.Lat),
            Math.Max(start.Lon, end.Lon),
            Math.Max(start.Lat, end.Lat),
            (ring, edge)
        );
    }

    /// <summary>Interseção das RETAS (frac filtra para o interior depois); null se paralelas
    /// ou compartilham endpoint (as MESMAS checagens da fonte, incluindo a assimétrica).</summary>
    private static Position? InfiniteLineIntersect(Position start0, Position end0, Position start1, Position end1)
    {
        if (start0 == start1 || start0 == end1 || end0 == start1 || end1 == start1)
            return null;
        var x0 = start0.Lon;
        var y0 = start0.Lat;
        var x1 = end0.Lon;
        var y1 = end0.Lat;
        var x2 = start1.Lon;
        var y2 = start1.Lat;
        var x3 = end1.Lon;
        var y3 = end1.Lat;
        var denom = (x0 - x1) * (y2 - y3) - (y0 - y1) * (x2 - x3);
        if (denom == 0)
            return null;
        var x4 = ((x0 * y1 - y0 * x1) * (x2 - x3) - (x0 - x1) * (x2 * y3 - y2 * x3)) / denom;
        var y4 = ((x0 * y1 - y0 * x1) * (y2 - y3) - (y0 - y1) * (x2 * y3 - y2 * x3)) / denom;
        return new Position(x4, y4);
    }

    private static bool IsConvexTriple(Position a, Position b, Position c, bool righthanded)
    {
        var d = (b.Lon - a.Lon) * (c.Lat - a.Lat) - (b.Lat - a.Lat) * (c.Lon - a.Lon);
        return d >= 0 == righthanded;
    }

    private static int Modulo(int n, int m) => (n % m + m) % m;
}
