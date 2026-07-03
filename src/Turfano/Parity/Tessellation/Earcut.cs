namespace Turfano.GeoJson;

/// <summary>
/// Porte 1:1 do earcut 3.x (ISC © Mapbox) — triangulação por ear clipping com lista circular
/// duplamente ligada, eliminação de furos por pontes (David Eberly) e aceleração por curva
/// z-order quando o polígono é grande (> 80 vértices). Entrada achatada (`data`,
/// `holeIndices`, `dim`), saída índices de triângulos. Slots z ausentes chegam como NaN
/// (o JS usa `undefined`) e não participam da geometria.
/// </summary>
internal static class Earcut
{
    internal sealed class EarcutNode(int i, double x, double y)
    {
        public readonly int I = i; // índice do vértice no array de dados
        public readonly double X = x;
        public readonly double Y = y;
        public EarcutNode Prev = null!;
        public EarcutNode Next = null!;
        public int Z; // valor na curva z-order
        public EarcutNode? PrevZ;
        public EarcutNode? NextZ;
        public bool Steiner;
    }

    public static List<int> Tessellate(double[] data, int[]? holeIndices, int dim)
    {
        var hasHoles = holeIndices is { Length: > 0 };
        var outerLen = hasHoles ? holeIndices![0] * dim : data.Length;
        var outerNode = LinkedList(data, 0, outerLen, dim, clockwise: true);
        var triangles = new List<int>();

        if (outerNode is null || outerNode.Next == outerNode.Prev)
            return triangles;

        double minX = 0,
            minY = 0,
            invSize = 0;

        if (hasHoles)
            outerNode = EliminateHoles(data, holeIndices!, outerNode, dim);

        // polígonos grandes ganham o hash z-order
        if (data.Length > 80 * dim)
        {
            minX = data[0];
            minY = data[1];
            var maxX = minX;
            var maxY = minY;
            for (var i = dim; i < outerLen; i += dim)
            {
                var x = data[i];
                var y = data[i + 1];
                if (x < minX)
                    minX = x;
                if (y < minY)
                    minY = y;
                if (x > maxX)
                    maxX = x;
                if (y > maxY)
                    maxY = y;
            }
            invSize = Math.Max(maxX - minX, maxY - minY);
            invSize = invSize != 0 ? 32767 / invSize : 0;
        }

        EarcutLinked(outerNode, triangles, dim, minX, minY, invSize, 0);
        return triangles;
    }

    /// <summary>Lista circular a partir dos pontos, na orientação pedida.</summary>
    private static EarcutNode? LinkedList(double[] data, int start, int end, int dim, bool clockwise)
    {
        EarcutNode? last = null;

        if (clockwise == SignedArea(data, start, end, dim) > 0)
        {
            for (var i = start; i < end; i += dim)
                last = InsertNode(i, data[i], data[i + 1], last);
        }
        else
        {
            for (var i = end - dim; i >= start; i -= dim)
                last = InsertNode(i, data[i], data[i + 1], last);
        }

        if (last is not null && NodesEqual(last, last.Next))
        {
            RemoveNode(last);
            last = last.Next;
        }
        return last;
    }

    /// <summary>Remove pontos colineares ou duplicados.</summary>
    private static EarcutNode? FilterPoints(EarcutNode? start, EarcutNode? end = null)
    {
        if (start is null)
            return start;
        end ??= start;

        var p = start;
        bool again;
        do
        {
            again = false;
            if (!p.Steiner && (NodesEqual(p, p.Next) || TriangleArea(p.Prev, p, p.Next) == 0))
            {
                RemoveNode(p);
                p = end = p.Prev;
                if (p == p.Next)
                    break;
                again = true;
            }
            else
            {
                p = p.Next;
            }
        } while (again || p != end);

        return end;
    }

    /// <summary>Laço principal de ear clipping.</summary>
    private static void EarcutLinked(EarcutNode? ear, List<int> triangles, int dim, double minX, double minY, double invSize, int pass)
    {
        if (ear is null)
            return;

        if (pass == 0 && invSize != 0)
            IndexCurve(ear, minX, minY, invSize);

        var stop = ear;

        while (ear!.Prev != ear.Next)
        {
            var prev = ear.Prev;
            var next = ear.Next;

            if (invSize != 0 ? IsEarHashed(ear, minX, minY, invSize) : IsEar(ear))
            {
                triangles.Add(prev.I / dim);
                triangles.Add(ear.I / dim);
                triangles.Add(next.I / dim);

                RemoveNode(ear);

                // pular o próximo vértice gera menos triângulos-lasca
                ear = next.Next;
                stop = next.Next;
                continue;
            }

            ear = next;

            if (ear == stop)
            {
                if (pass == 0)
                {
                    EarcutLinked(FilterPoints(ear), triangles, dim, minX, minY, invSize, 1);
                }
                else if (pass == 1)
                {
                    ear = CureLocalIntersections(FilterPoints(ear)!, triangles, dim);
                    EarcutLinked(ear, triangles, dim, minX, minY, invSize, 2);
                }
                else if (pass == 2)
                {
                    SplitEarcut(ear, triangles, dim, minX, minY, invSize);
                }
                break;
            }
        }
    }

    private static bool IsEar(EarcutNode ear)
    {
        var a = ear.Prev;
        var b = ear;
        var c = ear.Next;

        if (TriangleArea(a, b, c) >= 0)
            return false; // reflexo

        var ax = a.X;
        var bx = b.X;
        var cx = c.X;
        var ay = a.Y;
        var by = b.Y;
        var cy = c.Y;

        var x0 = ax < bx ? (ax < cx ? ax : cx) : (bx < cx ? bx : cx);
        var y0 = ay < by ? (ay < cy ? ay : cy) : (by < cy ? by : cy);
        var x1 = ax > bx ? (ax > cx ? ax : cx) : (bx > cx ? bx : cx);
        var y1 = ay > by ? (ay > cy ? ay : cy) : (by > cy ? by : cy);

        var p = c.Next;
        while (p != a)
        {
            if (
                p.X >= x0
                && p.X <= x1
                && p.Y >= y0
                && p.Y <= y1
                && PointInTriangle(ax, ay, bx, by, cx, cy, p.X, p.Y)
                && TriangleArea(p.Prev, p, p.Next) >= 0
            )
                return false;
            p = p.Next;
        }
        return true;
    }

    private static bool IsEarHashed(EarcutNode ear, double minX, double minY, double invSize)
    {
        var a = ear.Prev;
        var b = ear;
        var c = ear.Next;

        if (TriangleArea(a, b, c) >= 0)
            return false;

        var ax = a.X;
        var bx = b.X;
        var cx = c.X;
        var ay = a.Y;
        var by = b.Y;
        var cy = c.Y;

        var x0 = ax < bx ? (ax < cx ? ax : cx) : (bx < cx ? bx : cx);
        var y0 = ay < by ? (ay < cy ? ay : cy) : (by < cy ? by : cy);
        var x1 = ax > bx ? (ax > cx ? ax : cx) : (bx > cx ? bx : cx);
        var y1 = ay > by ? (ay > cy ? ay : cy) : (by > cy ? by : cy);

        var minZ = ZOrder(x0, y0, minX, minY, invSize);
        var maxZ = ZOrder(x1, y1, minX, minY, invSize);

        var p = ear.PrevZ;
        var n = ear.NextZ;

        while (p is not null && p.Z >= minZ && n is not null && n.Z <= maxZ)
        {
            if (
                p.X >= x0 && p.X <= x1 && p.Y >= y0 && p.Y <= y1 && p != a && p != c
                && PointInTriangle(ax, ay, bx, by, cx, cy, p.X, p.Y)
                && TriangleArea(p.Prev, p, p.Next) >= 0
            )
                return false;
            p = p.PrevZ;

            if (
                n.X >= x0 && n.X <= x1 && n.Y >= y0 && n.Y <= y1 && n != a && n != c
                && PointInTriangle(ax, ay, bx, by, cx, cy, n.X, n.Y)
                && TriangleArea(n.Prev, n, n.Next) >= 0
            )
                return false;
            n = n.NextZ;
        }

        while (p is not null && p.Z >= minZ)
        {
            if (
                p.X >= x0 && p.X <= x1 && p.Y >= y0 && p.Y <= y1 && p != a && p != c
                && PointInTriangle(ax, ay, bx, by, cx, cy, p.X, p.Y)
                && TriangleArea(p.Prev, p, p.Next) >= 0
            )
                return false;
            p = p.PrevZ;
        }

        while (n is not null && n.Z <= maxZ)
        {
            if (
                n.X >= x0 && n.X <= x1 && n.Y >= y0 && n.Y <= y1 && n != a && n != c
                && PointInTriangle(ax, ay, bx, by, cx, cy, n.X, n.Y)
                && TriangleArea(n.Prev, n, n.Next) >= 0
            )
                return false;
            n = n.NextZ;
        }

        return true;
    }

    private static EarcutNode? CureLocalIntersections(EarcutNode start, List<int> triangles, int dim)
    {
        var p = start;
        do
        {
            var a = p.Prev;
            var b = p.Next.Next;

            if (!NodesEqual(a, b) && SegmentsIntersect(a, p, p.Next, b) && LocallyInside(a, b) && LocallyInside(b, a))
            {
                triangles.Add(a.I / dim);
                triangles.Add(p.I / dim);
                triangles.Add(b.I / dim);

                RemoveNode(p);
                RemoveNode(p.Next);

                p = start = b;
            }
            p = p.Next;
        } while (p != start);

        return FilterPoints(p);
    }

    private static void SplitEarcut(EarcutNode start, List<int> triangles, int dim, double minX, double minY, double invSize)
    {
        var a = start;
        do
        {
            var b = a.Next.Next;
            while (b != a.Prev)
            {
                if (a.I != b.I && IsValidDiagonal(a, b))
                {
                    var c = SplitPolygon(a, b);

                    var aFiltered = FilterPoints(a, a.Next);
                    var cFiltered = FilterPoints(c, c.Next);

                    EarcutLinked(aFiltered, triangles, dim, minX, minY, invSize, 0);
                    EarcutLinked(cFiltered, triangles, dim, minX, minY, invSize, 0);
                    return;
                }
                b = b.Next;
            }
            a = a.Next;
        } while (a != start);
    }

    /// <summary>Liga cada furo ao anel externo (vira um único anel sem furos).</summary>
    private static EarcutNode? EliminateHoles(double[] data, int[] holeIndices, EarcutNode outerNode, int dim)
    {
        var queue = new List<EarcutNode>();

        for (int i = 0, len = holeIndices.Length; i < len; i++)
        {
            var start = holeIndices[i] * dim;
            var end = i < len - 1 ? holeIndices[i + 1] * dim : data.Length;
            var list = LinkedList(data, start, end, dim, clockwise: false);
            if (list is not null)
            {
                if (list == list.Next)
                    list.Steiner = true;
                queue.Add(GetLeftmost(list));
            }
        }

        queue = queue.OrderBy(node => node.X).ToList(); // compareX; estável

        var result = outerNode;
        foreach (var hole in queue)
            result = EliminateHole(hole, result)!;

        return result;
    }

    private static EarcutNode? EliminateHole(EarcutNode hole, EarcutNode outerNode)
    {
        var bridge = FindHoleBridge(hole, outerNode);
        if (bridge is null)
            return outerNode;

        var bridgeReverse = SplitPolygon(bridge, hole);

        FilterPoints(bridgeReverse, bridgeReverse.Next);
        return FilterPoints(bridge, bridge.Next);
    }

    /// <summary>Ponte furo→anel externo (algoritmo de David Eberly).</summary>
    private static EarcutNode? FindHoleBridge(EarcutNode hole, EarcutNode outerNode)
    {
        var p = outerNode;
        var hx = hole.X;
        var hy = hole.Y;
        var qx = double.NegativeInfinity;
        EarcutNode? m = null;

        // raio para a esquerda a partir do ponto mais à esquerda do furo
        do
        {
            if (hy <= p.Y && hy >= p.Next.Y && p.Next.Y != p.Y)
            {
                var x = p.X + (hy - p.Y) * (p.Next.X - p.X) / (p.Next.Y - p.Y);
                if (x <= hx && x > qx)
                {
                    qx = x;
                    m = p.X < p.Next.X ? p : p.Next;
                    if (x == hx)
                        return m; // o furo toca o segmento
                }
            }
            p = p.Next;
        } while (p != outerNode);

        if (m is null)
            return null;

        var stop = m;
        var mx = m.X;
        var my = m.Y;
        var tanMin = double.PositiveInfinity;

        p = m;
        do
        {
            if (
                hx >= p.X
                && p.X >= mx
                && hx != p.X
                && PointInTriangle(hy < my ? hx : qx, hy, mx, my, hy < my ? qx : hx, hy, p.X, p.Y)
            )
            {
                var tan = Math.Abs(hy - p.Y) / (hx - p.X);

                if (
                    LocallyInside(p, hole)
                    && (tan < tanMin || (tan == tanMin && (p.X > m.X || (p.X == m.X && SectorContainsSector(m, p)))))
                )
                {
                    m = p;
                    tanMin = tan;
                }
            }
            p = p.Next;
        } while (p != stop);

        return m;
    }

    private static bool SectorContainsSector(EarcutNode m, EarcutNode p) =>
        TriangleArea(m.Prev, m, p.Prev) < 0 && TriangleArea(p.Next, m, m.Next) < 0;

    private static void IndexCurve(EarcutNode start, double minX, double minY, double invSize)
    {
        var p = start;
        do
        {
            if (p.Z == 0)
                p.Z = ZOrder(p.X, p.Y, minX, minY, invSize);
            p.PrevZ = p.Prev;
            p.NextZ = p.Next;
            p = p.Next;
        } while (p != start);

        p.PrevZ!.NextZ = null;
        p.PrevZ = null;

        SortLinked(p);
    }

    /// <summary>Merge sort de lista ligada (Simon Tatham).</summary>
    private static EarcutNode SortLinked(EarcutNode list)
    {
        int inSize = 1;
        int numMerges;

        do
        {
            var p = list;
            EarcutNode? newList = null;
            EarcutNode? tail = null;
            numMerges = 0;

            while (p is not null)
            {
                numMerges++;
                var q = p;
                var pSize = 0;
                for (var i = 0; i < inSize; i++)
                {
                    pSize++;
                    q = q!.NextZ;
                    if (q is null)
                        break;
                }
                var qSize = inSize;

                while (pSize > 0 || (qSize > 0 && q is not null))
                {
                    EarcutNode e;
                    if (pSize != 0 && (qSize == 0 || q is null || p!.Z <= q.Z))
                    {
                        e = p!;
                        p = p!.NextZ;
                        pSize--;
                    }
                    else
                    {
                        e = q!;
                        q = q!.NextZ;
                        qSize--;
                    }

                    if (tail is not null)
                        tail.NextZ = e;
                    else
                        newList = e;

                    e.PrevZ = tail;
                    tail = e;
                }

                p = q!;
            }

            tail!.NextZ = null;
            inSize *= 2;
            list = newList!;
        } while (numMerges > 1);

        return list;
    }

    /// <summary>Curva z-order (coords em inteiros de 15 bits intercalados).</summary>
    private static int ZOrder(double xCoord, double yCoord, double minX, double minY, double invSize)
    {
        var x = (int)((xCoord - minX) * invSize);
        var y = (int)((yCoord - minY) * invSize);

        x = (x | (x << 8)) & 0x00FF00FF;
        x = (x | (x << 4)) & 0x0F0F0F0F;
        x = (x | (x << 2)) & 0x33333333;
        x = (x | (x << 1)) & 0x55555555;

        y = (y | (y << 8)) & 0x00FF00FF;
        y = (y | (y << 4)) & 0x0F0F0F0F;
        y = (y | (y << 2)) & 0x33333333;
        y = (y | (y << 1)) & 0x55555555;

        return x | (y << 1);
    }

    private static EarcutNode GetLeftmost(EarcutNode start)
    {
        var p = start;
        var leftmost = start;
        do
        {
            if (p.X < leftmost.X || (p.X == leftmost.X && p.Y < leftmost.Y))
                leftmost = p;
            p = p.Next;
        } while (p != start);
        return leftmost;
    }

    private static bool PointInTriangle(double ax, double ay, double bx, double by, double cx, double cy, double px, double py) =>
        (cx - px) * (ay - py) >= (ax - px) * (cy - py)
        && (ax - px) * (by - py) >= (bx - px) * (ay - py)
        && (bx - px) * (cy - py) >= (cx - px) * (by - py);

    /// <summary>Diagonal válida (interior do polígono) — precedência `&amp;&amp;` sobre `||` da fonte.</summary>
    private static bool IsValidDiagonal(EarcutNode a, EarcutNode b) =>
        a.Next.I != b.I
        && a.Prev.I != b.I
        && !IntersectsPolygon(a, b)
        && (
            (
                LocallyInside(a, b)
                && LocallyInside(b, a)
                && MiddleInside(a, b)
                && (TriangleArea(a.Prev, a, b.Prev) != 0 || TriangleArea(a, b.Prev, b) != 0)
            )
            || (NodesEqual(a, b) && TriangleArea(a.Prev, a, a.Next) > 0 && TriangleArea(b.Prev, b, b.Next) > 0)
        );

    private static double TriangleArea(EarcutNode p, EarcutNode q, EarcutNode r) =>
        (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);

    private static bool NodesEqual(EarcutNode p1, EarcutNode p2) => p1.X == p2.X && p1.Y == p2.Y;

    private static bool SegmentsIntersect(EarcutNode p1, EarcutNode q1, EarcutNode p2, EarcutNode q2)
    {
        var o1 = Math.Sign(TriangleArea(p1, q1, p2));
        var o2 = Math.Sign(TriangleArea(p1, q1, q2));
        var o3 = Math.Sign(TriangleArea(p2, q2, p1));
        var o4 = Math.Sign(TriangleArea(p2, q2, q1));

        if (o1 != o2 && o3 != o4)
            return true;

        if (o1 == 0 && OnSegment(p1, p2, q1))
            return true;
        if (o2 == 0 && OnSegment(p1, q2, q1))
            return true;
        if (o3 == 0 && OnSegment(p2, p1, q2))
            return true;
        if (o4 == 0 && OnSegment(p2, q1, q2))
            return true;

        return false;
    }

    private static bool OnSegment(EarcutNode p, EarcutNode q, EarcutNode r) =>
        q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) && q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);

    private static bool IntersectsPolygon(EarcutNode a, EarcutNode b)
    {
        var p = a;
        do
        {
            if (p.I != a.I && p.Next.I != a.I && p.I != b.I && p.Next.I != b.I && SegmentsIntersect(p, p.Next, a, b))
                return true;
            p = p.Next;
        } while (p != a);
        return false;
    }

    private static bool LocallyInside(EarcutNode a, EarcutNode b) =>
        TriangleArea(a.Prev, a, a.Next) < 0
            ? TriangleArea(a, b, a.Next) >= 0 && TriangleArea(a, a.Prev, b) >= 0
            : TriangleArea(a, b, a.Prev) < 0 || TriangleArea(a, a.Next, b) < 0;

    private static bool MiddleInside(EarcutNode a, EarcutNode b)
    {
        var p = a;
        var inside = false;
        var px = (a.X + b.X) / 2;
        var py = (a.Y + b.Y) / 2;
        do
        {
            if (
                p.Y > py != p.Next.Y > py
                && p.Next.Y != p.Y
                && px < (p.Next.X - p.X) * (py - p.Y) / (p.Next.Y - p.Y) + p.X
            )
                inside = !inside;
            p = p.Next;
        } while (p != a);
        return inside;
    }

    /// <summary>Liga dois vértices com uma ponte (divide o anel ou funde furo ao externo).</summary>
    private static EarcutNode SplitPolygon(EarcutNode a, EarcutNode b)
    {
        var a2 = new EarcutNode(a.I, a.X, a.Y);
        var b2 = new EarcutNode(b.I, b.X, b.Y);
        var an = a.Next;
        var bp = b.Prev;

        a.Next = b;
        b.Prev = a;

        a2.Next = an;
        an.Prev = a2;

        b2.Next = a2;
        a2.Prev = b2;

        bp.Next = b2;
        b2.Prev = bp;

        return b2;
    }

    private static EarcutNode InsertNode(int i, double x, double y, EarcutNode? last)
    {
        var p = new EarcutNode(i, x, y);

        if (last is null)
        {
            p.Prev = p;
            p.Next = p;
        }
        else
        {
            p.Next = last.Next;
            p.Prev = last;
            last.Next.Prev = p;
            last.Next = p;
        }
        return p;
    }

    private static void RemoveNode(EarcutNode p)
    {
        p.Next.Prev = p.Prev;
        p.Prev.Next = p.Next;

        if (p.PrevZ is not null)
            p.PrevZ.NextZ = p.NextZ;
        if (p.NextZ is not null)
            p.NextZ.PrevZ = p.PrevZ;
    }

    private static double SignedArea(double[] data, int start, int end, int dim)
    {
        var sum = 0.0;
        for (int i = start, j = end - dim; i < end; i += dim)
        {
            sum += (data[j] - data[i]) * (data[i + 1] + data[j + 1]);
            j = i;
        }
        return sum;
    }
}
