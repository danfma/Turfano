namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Shortest path between two points avoiding obstacle polygons — faithful port of
    /// `@turf/shortest-path`: grid over the expanded bbox (×1.15), cells inside an obstacle
    /// become walls, A* (manhattan heuristic, diagonals with cost 1.41421), and `CleanCoords`
    /// at the end. <paramref name="resolution"/> is the cell size (default 100 km, as in the
    /// source).
    /// </summary>
    public static Feature ShortestPath(
        Point start,
        Point end,
        FeatureCollection? obstacles = null,
        Units.Length? resolution = null
    )
    {
        var resolutionKm = resolution?.Kilometers ?? 100;
        var startCoord = start.Coordinates;
        var endCoord = end.Coordinates;

        if (obstacles is null || obstacles.Features.Length == 0)
            return new Feature(new LineString(new[] { startCoord, endCoord }));

        // bbox da coleção obstáculos+pontas, expandida 15%
        var boundsFeatures = obstacles
            .Features.Select(f => f.Geometry!)
            .Append(new Point(startCoord))
            .Append((Geometry)new Point(endCoord))
            .ToList();
        double west = double.PositiveInfinity,
            south = double.PositiveInfinity,
            east = double.NegativeInfinity,
            north = double.NegativeInfinity;
        foreach (var geometry in boundsFeatures)
        {
            var b = Bbox(geometry);
            west = Math.Min(west, b.Values[0]);
            south = Math.Min(south, b.Values[1]);
            east = Math.Max(east, b.Values[2]);
            north = Math.Max(north, b.Values[3]);
        }
        var scaled = Bbox(TransformScale(BboxPolygon(new BBox(west, south, east, north)), 1.15));
        west = scaled.Values[0];
        south = scaled.Values[1];
        east = scaled.Values[2];
        north = scaled.Values[3];

        var columnsWithFraction = Distance(new Position(west, south), new Position(east, south)).Kilometers / resolutionKm;
        var cellWidth = (east - west) / columnsWithFraction;
        var rowsWithFraction = Distance(new Position(west, south), new Position(west, north)).Kilometers / resolutionKm;
        var cellHeight = (north - south) / rowsWithFraction;

        var deltaX = columnsWithFraction % 1 * cellWidth / 2;
        var deltaY = rowsWithFraction % 1 * cellHeight / 2;

        var pointMatrix = new List<List<Position>>();
        var walkableMatrix = new List<List<int>>();
        var closestToStart = (Column: 0, Row: 0);
        var closestToEnd = (Column: 0, Row: 0);
        var minDistStart = double.PositiveInfinity;
        var minDistEnd = double.PositiveInfinity;

        var currentY = north - deltaY;
        var row = 0;
        while (currentY >= south)
        {
            var matrixRow = new List<int>();
            var pointRow = new List<Position>();
            var currentX = west + deltaX;
            var column = 0;
            while (currentX <= east)
            {
                var cell = new Position(currentX, currentY);
                var insideObstacle = obstacles.Features.Any(f =>
                    f.Geometry is { } g && BooleanPointInPolygon(new Point(cell), g)
                );
                matrixRow.Add(insideObstacle ? 0 : 1);
                pointRow.Add(cell);

                var distStart = Distance(cell, startCoord).Kilometers;
                if (!insideObstacle && distStart < minDistStart)
                {
                    minDistStart = distStart;
                    closestToStart = (column, row);
                }
                var distEnd = Distance(cell, endCoord).Kilometers;
                if (!insideObstacle && distEnd < minDistEnd)
                {
                    minDistEnd = distEnd;
                    closestToEnd = (column, row);
                }
                currentX += cellWidth;
                column++;
            }
            walkableMatrix.Add(matrixRow);
            pointMatrix.Add(pointRow);
            currentY -= cellHeight;
            row++;
        }

        var grid = BuildAStarGrid(walkableMatrix);
        var startNode = grid[closestToStart.Row][closestToStart.Column];
        var endNode = grid[closestToEnd.Row][closestToEnd.Column];
        var result = AStarSearch(grid, startNode, endNode);

        var path = new List<Position> { startCoord };
        foreach (var node in result)
            path.Add(pointMatrix[node.X][node.Y]);
        path.Add(endCoord);

        return new Feature((LineString)CleanCoords(new LineString(path.ToArray())));
    }

    // ---- A* (porte do javascript-astar embutido na fonte) ----

    private sealed class AStarNode(int x, int y, int weight)
    {
        public readonly int X = x; // índice de LINHA (como a fonte)
        public readonly int Y = y; // índice de COLUNA
        public readonly int Weight = weight;
        public double F;
        public double G;
        public double H;
        public bool Visited;
        public bool Closed;
        public AStarNode? Parent;

        public bool IsWall => Weight == 0;

        public double GetCost(AStarNode fromNeighbor) =>
            fromNeighbor.X != X && fromNeighbor.Y != Y ? Weight * 1.41421 : Weight;
    }

    private static List<List<AStarNode>> BuildAStarGrid(List<List<int>> walkableMatrix)
    {
        var grid = new List<List<AStarNode>>();
        for (var x = 0; x < walkableMatrix.Count; x++)
        {
            var gridRow = new List<AStarNode>();
            for (var y = 0; y < walkableMatrix[x].Count; y++)
                gridRow.Add(new AStarNode(x, y, walkableMatrix[x][y]));
            grid.Add(gridRow);
        }
        return grid;
    }

    private static List<AStarNode> AStarSearch(List<List<AStarNode>> grid, AStarNode start, AStarNode end)
    {
        static double Manhattan(AStarNode a, AStarNode b) => Math.Abs(b.X - a.X) + Math.Abs(b.Y - a.Y);

        var openHeap = new AStarHeap();
        start.H = Manhattan(start, end);
        openHeap.Push(start);

        while (openHeap.Size > 0)
        {
            var currentNode = openHeap.Pop();
            if (currentNode == end)
            {
                // pathTo
                var path = new List<AStarNode>();
                var current = currentNode;
                while (current.Parent is not null)
                {
                    path.Insert(0, current);
                    current = current.Parent;
                }
                return path;
            }

            currentNode.Closed = true;

            foreach (var neighbor in NeighborsOf(grid, currentNode))
            {
                if (neighbor.Closed || neighbor.IsWall)
                    continue;

                var gScore = currentNode.G + neighbor.GetCost(currentNode);
                var beenVisited = neighbor.Visited;
                if (!beenVisited || gScore < neighbor.G)
                {
                    neighbor.Visited = true;
                    neighbor.Parent = currentNode;
                    neighbor.H = neighbor.H != 0 ? neighbor.H : Manhattan(neighbor, end);
                    neighbor.G = gScore;
                    neighbor.F = neighbor.G + neighbor.H;

                    if (!beenVisited)
                        openHeap.Push(neighbor);
                    else
                        openHeap.Rescore(neighbor);
                }
            }
        }

        return new List<AStarNode>();
    }

    private static IEnumerable<AStarNode> NeighborsOf(List<List<AStarNode>> grid, AStarNode node)
    {
        var x = node.X;
        var y = node.Y;

        AStarNode? At(int gx, int gy) =>
            gx >= 0 && gx < grid.Count && gy >= 0 && gy < grid[gx].Count ? grid[gx][gy] : null;

        // a ordem da fonte: W, E, S, N e depois as diagonais SW, SE, NW, NE
        var candidates = new[]
        {
            At(x - 1, y),
            At(x + 1, y),
            At(x, y - 1),
            At(x, y + 1),
            At(x - 1, y - 1),
            At(x + 1, y - 1),
            At(x - 1, y + 1),
            At(x + 1, y + 1),
        };
        foreach (var candidate in candidates)
        {
            if (candidate is not null)
                yield return candidate;
        }
    }

    /// <summary>Binary heap from javascript-astar (score = F).</summary>
    private sealed class AStarHeap
    {
        private readonly List<AStarNode> content = new();

        public int Size => content.Count;

        public void Push(AStarNode element)
        {
            content.Add(element);
            SinkDown(content.Count - 1);
        }

        public AStarNode Pop()
        {
            var result = content[0];
            var end = content[^1];
            content.RemoveAt(content.Count - 1);
            if (content.Count > 0)
            {
                content[0] = end;
                BubbleUp(0);
            }
            return result;
        }

        public void Rescore(AStarNode node) => SinkDown(content.IndexOf(node));

        private void SinkDown(int n)
        {
            var element = content[n];
            while (n > 0)
            {
                var parentIndex = ((n + 1) >> 1) - 1;
                var parent = content[parentIndex];
                if (element.F < parent.F)
                {
                    content[parentIndex] = element;
                    content[n] = parent;
                    n = parentIndex;
                }
                else
                {
                    break;
                }
            }
        }

        private void BubbleUp(int n)
        {
            var length = content.Count;
            var element = content[n];
            var elementScore = element.F;
            while (true)
            {
                var child2Index = (n + 1) << 1;
                var child1Index = child2Index - 1;
                int? swap = null;
                double child1Score = 0;
                if (child1Index < length)
                {
                    child1Score = content[child1Index].F;
                    if (child1Score < elementScore)
                        swap = child1Index;
                }
                if (child2Index < length)
                {
                    var child2Score = content[child2Index].F;
                    if (child2Score < (swap is null ? elementScore : child1Score))
                        swap = child2Index;
                }
                if (swap is { } target)
                {
                    content[n] = content[target];
                    content[target] = element;
                    n = target;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
