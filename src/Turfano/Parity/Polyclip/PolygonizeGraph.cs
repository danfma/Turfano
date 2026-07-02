using System.Globalization;

namespace Turfano.GeoJson;

// Porte fiel do @turf/polygonize (grafo de arestas dirigidas estilo GEOS): nós/arestas
// simétricas → remove dangles → remove cut-edges → extrai anéis (CW com correção CCW nos
// nós de interseção) → classifica shells/furos. Opera em double puro, como a fonte.

internal sealed class PolygonizeNode(Position coordinates)
{
    public static string BuildId(Position coordinates) =>
        coordinates.Lon.ToString("R", CultureInfo.InvariantCulture)
        + ","
        + coordinates.Lat.ToString("R", CultureInfo.InvariantCulture);

    public readonly string Id = BuildId(coordinates);
    public readonly Position Coordinates = coordinates;
    public List<PolygonizeEdge> InnerEdges = new();
    private List<PolygonizeEdge> outerEdges = new();
    private bool outerEdgesSorted;

    public void RemoveInnerEdge(PolygonizeEdge edge) =>
        InnerEdges = InnerEdges.Where(e => e.From.Id != edge.From.Id).ToList();

    public void RemoveOuterEdge(PolygonizeEdge edge) =>
        outerEdges = outerEdges.Where(e => e.To.Id != edge.To.Id).ToList();

    public void AddOuterEdge(PolygonizeEdge edge)
    {
        outerEdges.Add(edge);
        outerEdgesSorted = false;
    }

    public void AddInnerEdge(PolygonizeEdge edge) => InnerEdges.Add(edge);

    /// <summary>Arestas de saída ordenadas em CCW (comparator da fonte).</summary>
    public List<PolygonizeEdge> GetOuterEdges()
    {
        SortOuterEdges();
        return outerEdges;
    }

    public PolygonizeEdge GetOuterEdge(int index)
    {
        SortOuterEdges();
        return outerEdges[index];
    }

    private void SortOuterEdges()
    {
        if (outerEdgesSorted)
            return;

        var center = Coordinates;
        outerEdges = outerEdges
            .OrderBy(e => e, Comparer<PolygonizeEdge>.Create((a, b) =>
            {
                var aNode = a.To;
                var bNode = b.To;
                var aDx = aNode.Coordinates.Lon - center.Lon;
                var bDx = bNode.Coordinates.Lon - center.Lon;

                if (aDx >= 0 && bDx < 0)
                    return 1;
                if (aDx < 0 && bDx >= 0)
                    return -1;
                if (aDx == 0 && bDx == 0)
                {
                    if (aNode.Coordinates.Lat - center.Lat >= 0 || bNode.Coordinates.Lat - center.Lat >= 0)
                        return Math.Sign(aNode.Coordinates.Lat - bNode.Coordinates.Lat);
                    return Math.Sign(bNode.Coordinates.Lat - aNode.Coordinates.Lat);
                }

                var det = PolygonizeGraph.OrientationIndex(center, aNode.Coordinates, bNode.Coordinates);
                if (det < 0)
                    return 1;
                if (det > 0)
                    return -1;

                var d1 = Math.Pow(aNode.Coordinates.Lon - center.Lon, 2) + Math.Pow(aNode.Coordinates.Lat - center.Lat, 2);
                var d2 = Math.Pow(bNode.Coordinates.Lon - center.Lon, 2) + Math.Pow(bNode.Coordinates.Lat - center.Lat, 2);
                return Math.Sign(d1 - d2);
            }))
            .ToList();
        outerEdgesSorted = true;
    }
}

internal sealed class PolygonizeEdge
{
    public readonly PolygonizeNode From;
    public readonly PolygonizeNode To;
    public PolygonizeEdge? Next;
    public int? Label;
    public PolygonizeEdgeRing? Ring;
    private PolygonizeEdge? symmetric;

    public PolygonizeEdge(PolygonizeNode from, PolygonizeNode to)
    {
        From = from;
        To = to;
        from.AddOuterEdge(this);
        to.AddInnerEdge(this);
    }

    public PolygonizeEdge GetSymmetric()
    {
        if (symmetric is null)
        {
            symmetric = new PolygonizeEdge(To, From) { symmetric = this };
        }
        return symmetric;
    }

    public PolygonizeEdge Symmetric => GetSymmetric();

    public void DeleteEdge()
    {
        From.RemoveOuterEdge(this);
        To.RemoveInnerEdge(this);
    }

    public bool IsEqual(PolygonizeEdge edge) => From.Id == edge.From.Id && To.Id == edge.To.Id;
}

internal sealed class PolygonizeEdgeRing
{
    public readonly List<PolygonizeEdge> Edges = new();
    private Polygon? polygon;
    private Polygon? envelope;

    public void Push(PolygonizeEdge edge)
    {
        Edges.Add(edge);
        polygon = null;
        envelope = null;
    }

    /// <summary>Anel é furo se orientado CCW (teste do vértice mais alto, como na fonte).</summary>
    public bool IsHole()
    {
        var highIndex = 0;
        for (var i = 0; i < Edges.Count; i++)
        {
            if (Edges[i].From.Coordinates.Lat > Edges[highIndex].From.Coordinates.Lat)
                highIndex = i;
        }

        var previousIndex = (highIndex == 0 ? Edges.Count : highIndex) - 1;
        var nextIndex = (highIndex + 1) % Edges.Count;
        var disc = PolygonizeGraph.OrientationIndex(
            Edges[previousIndex].From.Coordinates,
            Edges[highIndex].From.Coordinates,
            Edges[nextIndex].From.Coordinates
        );

        if (disc == 0)
            return Edges[previousIndex].From.Coordinates.Lon > Edges[nextIndex].From.Coordinates.Lon;
        return disc > 0;
    }

    public Polygon ToPolygon()
    {
        if (polygon is not null)
            return polygon;
        var coordinates = Edges.Select(edge => edge.From.Coordinates).ToList();
        coordinates.Add(Edges[0].From.Coordinates);
        return polygon = new Polygon(new[] { coordinates.ToArray() });
    }

    public Polygon GetEnvelope()
    {
        if (envelope is not null)
            return envelope;
        return envelope = Geo.Envelope(ToPolygon());
    }

    public bool Inside(Position point) => Geo.BooleanPointInPolygon(new Point(point), ToPolygon());

    private static bool EnvelopeIsEqual(Polygon env1, Polygon env2)
    {
        var ring1 = env1.Coordinates[0];
        var ring2 = env2.Coordinates[0];
        return ring1.Max(c => c.Lon) == ring2.Max(c => c.Lon)
            && ring1.Max(c => c.Lat) == ring2.Max(c => c.Lat)
            && ring1.Min(c => c.Lon) == ring2.Min(c => c.Lon)
            && ring1.Min(c => c.Lat) == ring2.Min(c => c.Lat);
    }

    private static bool EnvelopeContains(Polygon container, Polygon envelope) =>
        envelope.Coordinates[0].All(c => Geo.BooleanPointInPolygon(new Point(c), container));

    /// <summary>`geos::operation::polygonize::EdgeRing::findEdgeRingContaining`.</summary>
    public static PolygonizeEdgeRing? FindEdgeRingContaining(
        PolygonizeEdgeRing testRing,
        List<PolygonizeEdgeRing> shellList
    )
    {
        var testEnvelope = testRing.GetEnvelope();
        Polygon? minEnvelope = null;
        PolygonizeEdgeRing? minShell = null;

        foreach (var shell in shellList)
        {
            var tryEnvelope = shell.GetEnvelope();
            if (minShell is not null)
                minEnvelope = minShell.GetEnvelope();
            if (EnvelopeIsEqual(tryEnvelope, testEnvelope))
                continue;
            if (EnvelopeContains(tryEnvelope, testEnvelope))
            {
                var testCoordinates = testRing.Edges.Select(edge => edge.From.Coordinates).ToList();
                Position? testPoint = null;
                foreach (var pt in testCoordinates)
                {
                    if (!shell.Edges.Any(edge => pt.Lon == edge.From.Coordinates.Lon && pt.Lat == edge.From.Coordinates.Lat))
                        testPoint = pt;
                }
                if (testPoint is { } inside && shell.Inside(inside))
                {
                    if (minShell is null || EnvelopeContains(minEnvelope!, tryEnvelope))
                        minShell = shell;
                }
            }
        }

        return minShell;
    }
}

internal sealed class PolygonizeGraph
{
    private List<PolygonizeEdge> edges = new();
    private readonly Dictionary<string, PolygonizeNode> nodes = new();

    /// <summary>`mathSign(orientationIndex)` da fonte: sinal do cross product.</summary>
    public static int OrientationIndex(Position p1, Position p2, Position q)
    {
        var dx1 = p2.Lon - p1.Lon;
        var dy1 = p2.Lat - p1.Lat;
        var dx2 = q.Lon - p2.Lon;
        var dy2 = q.Lat - p2.Lat;
        return Math.Sign(dx1 * dy2 - dx2 * dy1);
    }

    public static PolygonizeGraph FromLines(IEnumerable<Position[]> lines)
    {
        var graph = new PolygonizeGraph();
        foreach (var line in lines)
        {
            for (var i = 1; i < line.Length; i++)
            {
                var start = graph.GetNode(line[i - 1]);
                var end = graph.GetNode(line[i]);
                graph.AddEdge(start, end);
            }
        }
        return graph;
    }

    private PolygonizeNode GetNode(Position coordinates)
    {
        var id = PolygonizeNode.BuildId(coordinates);
        if (!nodes.TryGetValue(id, out var node))
        {
            node = new PolygonizeNode(coordinates);
            nodes[id] = node;
        }
        return node;
    }

    private void AddEdge(PolygonizeNode from, PolygonizeNode to)
    {
        var edge = new PolygonizeEdge(from, to);
        edges.Add(edge);
        edges.Add(edge.GetSymmetric());
    }

    /// <summary>Remove nós pendurados (grau 1), recursivamente.</summary>
    public void DeleteDangles()
    {
        foreach (var node in nodes.Values.ToList())
            RemoveIfDangle(node);
    }

    private void RemoveIfDangle(PolygonizeNode node)
    {
        if (node.InnerEdges.Count <= 1)
        {
            var outerNodes = node.GetOuterEdges().Select(e => e.To).ToList();
            RemoveNode(node);
            foreach (var outer in outerNodes)
                RemoveIfDangle(outer);
        }
    }

    /// <summary>Remove cut-edges (pontes): arestas cujo simétrico recebe o mesmo label de anel.</summary>
    public void DeleteCutEdges()
    {
        ComputeNextClockwiseEdges();
        FindLabeledEdgeRings();
        foreach (var edge in edges.ToList())
        {
            if (edge.Label == edge.Symmetric.Label)
            {
                RemoveEdge(edge.Symmetric);
                RemoveEdge(edge);
            }
        }
    }

    private void ComputeNextClockwiseEdges()
    {
        foreach (var node in nodes.Values.ToList())
            ComputeNextClockwiseEdges(node);
    }

    private static void ComputeNextClockwiseEdges(PolygonizeNode node)
    {
        var outerEdges = node.GetOuterEdges();
        for (var i = 0; i < outerEdges.Count; i++)
        {
            var edge = outerEdges[i];
            node.GetOuterEdge((i == 0 ? node.GetOuterEdges().Count : i) - 1).Symmetric.Next = edge;
        }
    }

    /// <summary>Transcrição de `geos::...::computeNextCCWEdges` (via a fonte do @turf).</summary>
    private static void ComputeNextCounterClockwiseEdges(PolygonizeNode node, int label)
    {
        var outerEdges = node.GetOuterEdges();
        PolygonizeEdge? firstOutgoing = null;
        PolygonizeEdge? prevIncoming = null;

        for (var i = outerEdges.Count - 1; i >= 0; --i)
        {
            var directedEdge = outerEdges[i];
            var symmetric = directedEdge.Symmetric;
            PolygonizeEdge? outgoing = null;
            PolygonizeEdge? incoming = null;
            if (directedEdge.Label == label)
                outgoing = directedEdge;
            if (symmetric.Label == label)
                incoming = symmetric;
            if (outgoing is null || incoming is null)
                continue; // como na fonte: exige ambos

            prevIncoming = incoming;
            if (prevIncoming is not null)
            {
                prevIncoming.Next = outgoing;
                prevIncoming = null;
            }
            if (firstOutgoing is null)
                firstOutgoing = outgoing;
        }
        if (prevIncoming is not null)
            prevIncoming.Next = firstOutgoing;
    }

    /// <summary>Rotula anéis (número crescente); devolve as arestas iniciais de cada anel.</summary>
    private List<PolygonizeEdge> FindLabeledEdgeRings()
    {
        var edgeRingStarts = new List<PolygonizeEdge>();
        var label = 0;
        foreach (var edge in edges)
        {
            if (edge.Label is not null)
                continue;
            edgeRingStarts.Add(edge);
            var e = edge;
            do
            {
                e.Label = label;
                e = e.Next!;
            } while (!edge.IsEqual(e));
            label++;
        }
        return edgeRingStarts;
    }

    public List<PolygonizeEdgeRing> GetEdgeRings()
    {
        ComputeNextClockwiseEdges();
        foreach (var edge in edges)
            edge.Label = null;
        foreach (var startEdge in FindLabeledEdgeRings())
        {
            foreach (var node in FindIntersectionNodes(startEdge))
                ComputeNextCounterClockwiseEdges(node, startEdge.Label!.Value);
        }

        var edgeRings = new List<PolygonizeEdgeRing>();
        foreach (var edge in edges)
        {
            if (edge.Ring is not null)
                continue;
            edgeRings.Add(FindEdgeRing(edge));
        }
        return edgeRings;
    }

    private static List<PolygonizeNode> FindIntersectionNodes(PolygonizeEdge startEdge)
    {
        var intersectionNodes = new List<PolygonizeNode>();
        var edge = startEdge;
        do
        {
            var degree = 0;
            foreach (var outer in edge.From.GetOuterEdges())
            {
                if (outer.Label == startEdge.Label)
                    ++degree;
            }
            if (degree > 1)
                intersectionNodes.Add(edge.From);
            edge = edge.Next!;
        } while (!startEdge.IsEqual(edge));
        return intersectionNodes;
    }

    private static PolygonizeEdgeRing FindEdgeRing(PolygonizeEdge startEdge)
    {
        var edge = startEdge;
        var edgeRing = new PolygonizeEdgeRing();
        do
        {
            edgeRing.Push(edge);
            edge.Ring = edgeRing;
            edge = edge.Next!;
        } while (!startEdge.IsEqual(edge));
        return edgeRing;
    }

    private void RemoveNode(PolygonizeNode node)
    {
        foreach (var edge in node.GetOuterEdges().ToList())
            RemoveEdge(edge);
        foreach (var edge in node.InnerEdges.ToList())
            RemoveEdge(edge);
        nodes.Remove(node.Id);
    }

    private void RemoveEdge(PolygonizeEdge edge)
    {
        edges = edges.Where(e => !e.IsEqual(edge)).ToList();
        edge.DeleteEdge();
    }
}
