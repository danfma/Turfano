namespace Turfano.GeoJson;

/// <summary>
/// Porte 1:1 do d3-voronoi 1.1.2 (BSD-3 © Mike Bostock) — algoritmo de Fortune com beach
/// line em red-black tree e células cortadas pelo extent. Escopo: o caminho que o
/// `@turf/voronoi` executa (`polygons()`); triangles/links/find ficam de fora. Os globais de
/// módulo do d3 (`beaches`/`circles`/`edges`/`cells`/`firstCircle`) viram campos desta
/// instância (mesma adaptação do `OperationRun` da Fase 11). `delete edges[i]`/`delete
/// cells[i]` do JS (arrays esparsos) viram entradas nulas nas listas.
/// </summary>
internal sealed class FortuneVoronoi
{
    private const double Epsilon = 1e-6;
    private const double EpsilonSquared = 1e-12;

    internal sealed class VoronoiSite(double x, double y, int index)
    {
        public readonly double X = x;
        public readonly double Y = y;
        public readonly int Index = index;
    }

    /// <summary>Nó de red-black tree com lista encadeada (P/N) embutida, como o d3.</summary>
    internal class RedBlackNode<TNode>
        where TNode : RedBlackNode<TNode>
    {
        public TNode? Parent; // U
        public bool IsRed; // C
        public TNode? Left; // L
        public TNode? Right; // R
        public TNode? Previous; // P
        public TNode? Next; // N

        public void ResetLinks()
        {
            Parent = null;
            IsRed = false;
            Left = null;
            Right = null;
            Previous = null;
            Next = null;
        }
    }

    private sealed class Beach : RedBlackNode<Beach>
    {
        public VoronoiEdge? Edge;
        public VoronoiSite Site = null!;
        public Circle? CircleEvent;
    }

    private sealed class Circle : RedBlackNode<Circle>
    {
        public double X;
        public double Y;
        public Beach Arc = null!;
        public VoronoiSite Site = null!;
        public double CenterY; // cy
    }

    internal sealed class VoronoiEdge(VoronoiSite left, VoronoiSite? right)
    {
        public double[]? V0;
        public double[]? V1;
        public VoronoiSite Left = left;
        public VoronoiSite? Right = right;
    }

    internal sealed class VoronoiCell(VoronoiSite site)
    {
        public readonly VoronoiSite Site = site;
        public readonly List<int> Halfedges = new();
    }

    private sealed class RedBlackTree<TNode>
        where TNode : RedBlackNode<TNode>
    {
        public TNode? Root; // _

        public void Insert(TNode? after, TNode node)
        {
            TNode? parent;

            if (after is not null)
            {
                node.Previous = after;
                node.Next = after.Next;
                if (after.Next is not null)
                    after.Next.Previous = node;
                after.Next = node;
                if (after.Right is not null)
                {
                    after = after.Right;
                    while (after.Left is not null)
                        after = after.Left;
                    after.Left = node;
                }
                else
                {
                    after.Right = node;
                }
                parent = after;
            }
            else if (Root is not null)
            {
                after = First(Root);
                node.Previous = null;
                node.Next = after;
                after.Previous = after.Left = node;
                parent = after;
            }
            else
            {
                node.Previous = node.Next = null;
                Root = node;
                parent = null;
            }
            node.Left = node.Right = null;
            node.Parent = parent;
            node.IsRed = true;

            var current = node;
            while (parent is not null && parent.IsRed)
            {
                var grandpa = parent.Parent!;
                if (parent == grandpa.Left)
                {
                    var uncle = grandpa.Right;
                    if (uncle is not null && uncle.IsRed)
                    {
                        parent.IsRed = uncle.IsRed = false;
                        grandpa.IsRed = true;
                        current = grandpa;
                    }
                    else
                    {
                        if (current == parent.Right)
                        {
                            RotateLeft(parent);
                            current = parent;
                            parent = current.Parent!;
                        }
                        parent.IsRed = false;
                        grandpa.IsRed = true;
                        RotateRight(grandpa);
                    }
                }
                else
                {
                    var uncle = grandpa.Left;
                    if (uncle is not null && uncle.IsRed)
                    {
                        parent.IsRed = uncle.IsRed = false;
                        grandpa.IsRed = true;
                        current = grandpa;
                    }
                    else
                    {
                        if (current == parent.Left)
                        {
                            RotateRight(parent);
                            current = parent;
                            parent = current.Parent!;
                        }
                        parent.IsRed = false;
                        grandpa.IsRed = true;
                        RotateLeft(grandpa);
                    }
                }
                parent = current.Parent;
            }
            Root!.IsRed = false;
        }

        public void Remove(TNode node)
        {
            if (node.Next is not null)
                node.Next.Previous = node.Previous;
            if (node.Previous is not null)
                node.Previous.Next = node.Next;
            node.Next = node.Previous = null;

            var parent = node.Parent;
            var left = node.Left;
            var right = node.Right;
            TNode? next;

            if (left is null)
                next = right;
            else if (right is null)
                next = left;
            else
                next = First(right);

            if (parent is not null)
            {
                if (parent.Left == node)
                    parent.Left = next;
                else
                    parent.Right = next;
            }
            else
            {
                Root = next;
            }

            bool red;
            TNode? current;
            if (left is not null && right is not null)
            {
                red = next!.IsRed;
                next.IsRed = node.IsRed;
                next.Left = left;
                left.Parent = next;
                if (next != right)
                {
                    parent = next.Parent;
                    next.Parent = node.Parent;
                    current = next.Right;
                    parent!.Left = current;
                    next.Right = right;
                    right.Parent = next;
                }
                else
                {
                    next.Parent = parent;
                    parent = next;
                    current = next.Right;
                }
            }
            else
            {
                red = node.IsRed;
                current = next;
            }

            if (current is not null)
                current.Parent = parent;
            if (red)
                return;
            if (current is not null && current.IsRed)
            {
                current.IsRed = false;
                return;
            }

            do
            {
                if (current == Root)
                    break;
                if (current == parent!.Left)
                {
                    var sibling = parent.Right!;
                    if (sibling.IsRed)
                    {
                        sibling.IsRed = false;
                        parent.IsRed = true;
                        RotateLeft(parent);
                        sibling = parent.Right!;
                    }
                    if ((sibling.Left is not null && sibling.Left.IsRed) || (sibling.Right is not null && sibling.Right.IsRed))
                    {
                        if (sibling.Right is null || !sibling.Right.IsRed)
                        {
                            sibling.Left!.IsRed = false;
                            sibling.IsRed = true;
                            RotateRight(sibling);
                            sibling = parent.Right!;
                        }
                        sibling.IsRed = parent.IsRed;
                        parent.IsRed = sibling.Right!.IsRed = false;
                        RotateLeft(parent);
                        current = Root;
                        break;
                    }
                    sibling.IsRed = true;
                }
                else
                {
                    var sibling = parent.Left!;
                    if (sibling.IsRed)
                    {
                        sibling.IsRed = false;
                        parent.IsRed = true;
                        RotateRight(parent);
                        sibling = parent.Left!;
                    }
                    if ((sibling.Left is not null && sibling.Left.IsRed) || (sibling.Right is not null && sibling.Right.IsRed))
                    {
                        if (sibling.Left is null || !sibling.Left.IsRed)
                        {
                            sibling.Right!.IsRed = false;
                            sibling.IsRed = true;
                            RotateLeft(sibling);
                            sibling = parent.Left!;
                        }
                        sibling.IsRed = parent.IsRed;
                        parent.IsRed = sibling.Left!.IsRed = false;
                        RotateRight(parent);
                        current = Root;
                        break;
                    }
                    sibling.IsRed = true;
                }
                current = parent;
                parent = parent.Parent;
            } while (!current!.IsRed);

            if (current is not null)
                current.IsRed = false;
        }

        private void RotateLeft(TNode node)
        {
            var p = node;
            var q = node.Right!;
            var parent = p.Parent;

            if (parent is not null)
            {
                if (parent.Left == p)
                    parent.Left = q;
                else
                    parent.Right = q;
            }
            else
            {
                Root = q;
            }

            q.Parent = parent;
            p.Parent = q;
            p.Right = q.Left;
            if (p.Right is not null)
                p.Right.Parent = p;
            q.Left = p;
        }

        private void RotateRight(TNode node)
        {
            var p = node;
            var q = node.Left!;
            var parent = p.Parent;

            if (parent is not null)
            {
                if (parent.Left == p)
                    parent.Left = q;
                else
                    parent.Right = q;
            }
            else
            {
                Root = q;
            }

            q.Parent = parent;
            p.Parent = q;
            p.Left = q.Right;
            if (p.Left is not null)
                p.Left.Parent = p;
            q.Right = p;
        }

        private static TNode First(TNode node)
        {
            while (node.Left is not null)
                node = node.Left;
            return node;
        }
    }

    // os globais de módulo do d3, por instância
    private RedBlackTree<Beach> beaches = null!;
    private RedBlackTree<Circle> circles = null!;
    private List<VoronoiEdge?> edges = null!;
    private VoronoiCell?[] cells = null!;
    private Circle? firstCircle;

    private readonly List<VoronoiEdge?> resultEdges;
    private readonly VoronoiCell?[] resultCells;

    public FortuneVoronoi(List<VoronoiSite> sites, (double X0, double Y0, double X1, double Y1)? extent)
    {
        // sort lexicográfico do d3 (y desc, depois x desc) + pop do fim (menor y primeiro)
        sites.Sort((a, b) =>
        {
            var byY = b.Y.CompareTo(a.Y);
            return byY != 0 ? byY : b.X.CompareTo(a.X);
        });

        edges = new List<VoronoiEdge?>();
        cells = new VoronoiCell?[sites.Count];
        beaches = new RedBlackTree<Beach>();
        circles = new RedBlackTree<Circle>();

        var siteIndex = sites.Count - 1;
        var site = siteIndex >= 0 ? sites[siteIndex--] : null;
        var x = double.NaN;
        var y = double.NaN;

        while (true)
        {
            var circle = firstCircle;
            if (site is not null && (circle is null || site.Y < circle.Y || (site.Y == circle.Y && site.X < circle.X)))
            {
                if (site.X != x || site.Y != y)
                {
                    AddBeach(site);
                    x = site.X;
                    y = site.Y;
                }
                site = siteIndex >= 0 ? sites[siteIndex--] : null;
            }
            else if (circle is not null)
            {
                RemoveBeach(circle.Arc);
            }
            else
            {
                break;
            }
        }

        SortCellHalfedges();

        if (extent is { } box)
        {
            ClipEdges(box.X0, box.Y0, box.X1, box.Y1);
            ClipCells(box.X0, box.Y0, box.X1, box.Y1);
        }

        resultEdges = edges;
        resultCells = cells;
        beaches = null!;
        circles = null!;
        edges = null!;
        cells = null!;
    }

    /// <summary>Anéis das células (sem o ponto de fechamento), indexados pelo site de
    /// entrada; null para células totalmente cortadas.</summary>
    public List<double[]>?[] Polygons()
    {
        var polygons = new List<double[]>?[resultCells.Length];
        for (var i = 0; i < resultCells.Length; i++)
        {
            if (resultCells[i] is not { } cell)
                continue;
            var ring = new List<double[]>(cell.Halfedges.Count);
            foreach (var edgeIndex in cell.Halfedges)
                ring.Add(CellHalfedgeStart(cell, resultEdges[edgeIndex]!));
            polygons[cell.Site.Index] = ring;
        }
        return polygons;
    }

    // ---- Beach.js ----

    private void RemoveBeach(Beach beach)
    {
        var circle = beach.CircleEvent!;
        var x = circle.X;
        var y = circle.CenterY;
        var vertex = new[] { x, y };
        var previous = beach.Previous;
        var next = beach.Next;
        var disappearing = new List<Beach> { beach };

        DetachBeach(beach);

        var lArc = previous!;
        while (
            lArc.CircleEvent is not null
            && Math.Abs(x - lArc.CircleEvent.X) < Epsilon
            && Math.Abs(y - lArc.CircleEvent.CenterY) < Epsilon
        )
        {
            previous = lArc.Previous;
            disappearing.Insert(0, lArc);
            DetachBeach(lArc);
            lArc = previous!;
        }

        disappearing.Insert(0, lArc);
        DetachCircle(lArc);

        var rArc = next!;
        while (
            rArc.CircleEvent is not null
            && Math.Abs(x - rArc.CircleEvent.X) < Epsilon
            && Math.Abs(y - rArc.CircleEvent.CenterY) < Epsilon
        )
        {
            next = rArc.Next;
            disappearing.Add(rArc);
            DetachBeach(rArc);
            rArc = next!;
        }

        disappearing.Add(rArc);
        DetachCircle(rArc);

        var arcCount = disappearing.Count;
        for (var iArc = 1; iArc < arcCount; ++iArc)
        {
            rArc = disappearing[iArc];
            lArc = disappearing[iArc - 1];
            SetEdgeEnd(rArc.Edge!, lArc.Site, rArc.Site, vertex);
        }

        lArc = disappearing[0];
        rArc = disappearing[arcCount - 1];
        rArc.Edge = CreateEdge(lArc.Site, rArc.Site, null, vertex);

        AttachCircle(lArc);
        AttachCircle(rArc);
    }

    private void AddBeach(VoronoiSite site)
    {
        var x = site.X;
        var directrix = site.Y;
        Beach? lArc = null;
        Beach? rArc = null;
        var node = beaches.Root;

        while (node is not null)
        {
            var dxl = LeftBreakPoint(node, directrix) - x;
            if (dxl > Epsilon)
            {
                node = node.Left;
            }
            else
            {
                var dxr = x - RightBreakPoint(node, directrix);
                if (dxr > Epsilon)
                {
                    if (node.Right is null)
                    {
                        lArc = node;
                        break;
                    }
                    node = node.Right;
                }
                else
                {
                    if (dxl > -Epsilon)
                    {
                        lArc = node.Previous;
                        rArc = node;
                    }
                    else if (dxr > -Epsilon)
                    {
                        lArc = node;
                        rArc = node.Next;
                    }
                    else
                    {
                        lArc = rArc = node;
                    }
                    break;
                }
            }
        }

        cells[site.Index] = new VoronoiCell(site);
        var newArc = new Beach { Site = site };
        beaches.Insert(lArc, newArc);

        if (lArc is null && rArc is null)
            return;

        if (lArc == rArc)
        {
            DetachCircle(lArc!);
            rArc = new Beach { Site = lArc!.Site };
            beaches.Insert(newArc, rArc);
            newArc.Edge = rArc.Edge = CreateEdge(lArc.Site, newArc.Site, null, null);
            AttachCircle(lArc);
            AttachCircle(rArc);
            return;
        }

        if (rArc is null)
        {
            newArc.Edge = CreateEdge(lArc!.Site, newArc.Site, null, null);
            return;
        }

        DetachCircle(lArc!);
        DetachCircle(rArc);

        var lSite = lArc!.Site;
        var ax = lSite.X;
        var ay = lSite.Y;
        var bx = site.X - ax;
        var by = site.Y - ay;
        var rSite = rArc.Site;
        var cx = rSite.X - ax;
        var cy = rSite.Y - ay;
        var d = 2 * (bx * cy - by * cx);
        var hb = bx * bx + by * by;
        var hc = cx * cx + cy * cy;
        var vertex = new[] { (cy * hb - by * hc) / d + ax, (bx * hc - cx * hb) / d + ay };

        SetEdgeEnd(rArc.Edge!, lSite, rSite, vertex);
        newArc.Edge = CreateEdge(lSite, site, null, vertex);
        rArc.Edge = CreateEdge(site, rSite, null, vertex);
        AttachCircle(lArc);
        AttachCircle(rArc);
    }

    private void DetachBeach(Beach beach)
    {
        DetachCircle(beach);
        beaches.Remove(beach);
        beach.ResetLinks();
    }

    private static double LeftBreakPoint(Beach arc, double directrix)
    {
        var site = arc.Site;
        var rfocx = site.X;
        var rfocy = site.Y;
        var pby2 = rfocy - directrix;

        if (pby2 == 0)
            return rfocx;

        var lArc = arc.Previous;
        if (lArc is null)
            return double.NegativeInfinity;

        site = lArc.Site;
        var lfocx = site.X;
        var lfocy = site.Y;
        var plby2 = lfocy - directrix;

        if (plby2 == 0)
            return lfocx;

        var hl = lfocx - rfocx;
        var aby2 = 1 / pby2 - 1 / plby2;
        var b = hl / plby2;

        if (aby2 != 0)
            return (-b + Math.Sqrt(b * b - 2 * aby2 * (hl * hl / (-2 * plby2) - lfocy + plby2 / 2 + rfocy - pby2 / 2))) / aby2 + rfocx;

        return (rfocx + lfocx) / 2;
    }

    private static double RightBreakPoint(Beach arc, double directrix)
    {
        var rArc = arc.Next;
        if (rArc is not null)
            return LeftBreakPoint(rArc, directrix);
        var site = arc.Site;
        return site.Y == directrix ? site.X : double.PositiveInfinity;
    }

    // ---- Circle.js ----

    private void AttachCircle(Beach arc)
    {
        var lArc = arc.Previous;
        var rArc = arc.Next;

        if (lArc is null || rArc is null)
            return;

        var lSite = lArc.Site;
        var cSite = arc.Site;
        var rSite = rArc.Site;

        if (lSite == rSite)
            return;

        var bx = cSite.X;
        var by = cSite.Y;
        var ax = lSite.X - bx;
        var ay = lSite.Y - by;
        var cx = rSite.X - bx;
        var cy = rSite.Y - by;

        var d = 2 * (ax * cy - ay * cx);
        if (d >= -EpsilonSquared)
            return;

        var ha = ax * ax + ay * ay;
        var hc = cx * cx + cy * cy;
        var x = (cy * ha - ay * hc) / d;
        var y = (ax * hc - cx * ha) / d;

        var circle = new Circle
        {
            Arc = arc,
            Site = cSite,
            X = x + bx,
        };
        circle.CenterY = y + by;
        circle.Y = circle.CenterY + Math.Sqrt(x * x + y * y);

        arc.CircleEvent = circle;

        Circle? before = null;
        var node = circles.Root;
        while (node is not null)
        {
            if (circle.Y < node.Y || (circle.Y == node.Y && circle.X <= node.X))
            {
                if (node.Left is not null)
                {
                    node = node.Left;
                }
                else
                {
                    before = node.Previous;
                    break;
                }
            }
            else
            {
                if (node.Right is not null)
                {
                    node = node.Right;
                }
                else
                {
                    before = node;
                    break;
                }
            }
        }

        circles.Insert(before, circle);
        if (before is null)
            firstCircle = circle;
    }

    private void DetachCircle(Beach arc)
    {
        var circle = arc.CircleEvent;
        if (circle is not null)
        {
            if (circle.Previous is null)
                firstCircle = circle.Next;
            circles.Remove(circle);
            circle.ResetLinks();
            arc.CircleEvent = null;
        }
    }

    // ---- Edge.js ----

    private VoronoiEdge CreateEdge(VoronoiSite left, VoronoiSite right, double[]? v0, double[]? v1)
    {
        var edge = new VoronoiEdge(left, right);
        edges.Add(edge);
        var index = edges.Count - 1;
        if (v0 is not null)
            SetEdgeEnd(edge, left, right, v0);
        if (v1 is not null)
            SetEdgeEnd(edge, right, left, v1);
        cells[left.Index]!.Halfedges.Add(index);
        cells[right.Index]!.Halfedges.Add(index);
        return edge;
    }

    private static VoronoiEdge CreateBorderEdge(VoronoiSite left, double[] v0, double[]? v1)
    {
        var edge = new VoronoiEdge(left, null) { V0 = v0, V1 = v1 };
        return edge;
    }

    private static void SetEdgeEnd(VoronoiEdge edge, VoronoiSite left, VoronoiSite right, double[] vertex)
    {
        if (edge.V0 is null && edge.V1 is null)
        {
            edge.V0 = vertex;
            edge.Left = left;
            edge.Right = right;
        }
        else if (edge.Left == right)
        {
            edge.V1 = vertex;
        }
        else
        {
            edge.V0 = vertex;
        }
    }

    /// <summary>Recorte Liang–Barsky.</summary>
    private static bool ClipEdge(VoronoiEdge edge, double x0, double y0, double x1, double y1)
    {
        var a = edge.V0!;
        var b = edge.V1!;
        var ax = a[0];
        var ay = a[1];
        var bx = b[0];
        var by = b[1];
        var t0 = 0.0;
        var t1 = 1.0;
        var dx = bx - ax;
        var dy = by - ay;

        var r = x0 - ax;
        if (dx == 0 && r > 0)
            return false;
        r /= dx;
        if (dx < 0)
        {
            if (r < t0)
                return false;
            if (r < t1)
                t1 = r;
        }
        else if (dx > 0)
        {
            if (r > t1)
                return false;
            if (r > t0)
                t0 = r;
        }

        r = x1 - ax;
        if (dx == 0 && r < 0)
            return false;
        r /= dx;
        if (dx < 0)
        {
            if (r > t1)
                return false;
            if (r > t0)
                t0 = r;
        }
        else if (dx > 0)
        {
            if (r < t0)
                return false;
            if (r < t1)
                t1 = r;
        }

        r = y0 - ay;
        if (dy == 0 && r > 0)
            return false;
        r /= dy;
        if (dy < 0)
        {
            if (r < t0)
                return false;
            if (r < t1)
                t1 = r;
        }
        else if (dy > 0)
        {
            if (r > t1)
                return false;
            if (r > t0)
                t0 = r;
        }

        r = y1 - ay;
        if (dy == 0 && r < 0)
            return false;
        r /= dy;
        if (dy < 0)
        {
            if (r > t1)
                return false;
            if (r > t0)
                t0 = r;
        }
        else if (dy > 0)
        {
            if (r < t0)
                return false;
            if (r < t1)
                t1 = r;
        }

        if (!(t0 > 0) && !(t1 < 1))
            return true;

        if (t0 > 0)
            edge.V0 = new[] { ax + t0 * dx, ay + t0 * dy };
        if (t1 < 1)
            edge.V1 = new[] { ax + t1 * dx, ay + t1 * dy };
        return true;
    }

    private static bool ConnectEdge(VoronoiEdge edge, double x0, double y0, double x1, double y1)
    {
        var v1 = edge.V1;
        if (v1 is not null)
            return true;

        var v0 = edge.V0;
        var left = edge.Left;
        var right = edge.Right!;
        var lx = left.X;
        var ly = left.Y;
        var rx = right.X;
        var ry = right.Y;
        var fx = (lx + rx) / 2;
        var fy = (ly + ry) / 2;

        if (ry == ly)
        {
            if (fx < x0 || fx >= x1)
                return false;
            if (lx > rx)
            {
                if (v0 is null)
                    v0 = new[] { fx, y0 };
                else if (v0[1] >= y1)
                    return false;
                v1 = new[] { fx, y1 };
            }
            else
            {
                if (v0 is null)
                    v0 = new[] { fx, y1 };
                else if (v0[1] < y0)
                    return false;
                v1 = new[] { fx, y0 };
            }
        }
        else
        {
            var fm = (lx - rx) / (ry - ly);
            var fb = fy - fm * fx;
            if (fm < -1 || fm > 1)
            {
                if (lx > rx)
                {
                    if (v0 is null)
                        v0 = new[] { (y0 - fb) / fm, y0 };
                    else if (v0[1] >= y1)
                        return false;
                    v1 = new[] { (y1 - fb) / fm, y1 };
                }
                else
                {
                    if (v0 is null)
                        v0 = new[] { (y1 - fb) / fm, y1 };
                    else if (v0[1] < y0)
                        return false;
                    v1 = new[] { (y0 - fb) / fm, y0 };
                }
            }
            else
            {
                if (ly < ry)
                {
                    if (v0 is null)
                        v0 = new[] { x0, fm * x0 + fb };
                    else if (v0[0] >= x1)
                        return false;
                    v1 = new[] { x1, fm * x1 + fb };
                }
                else
                {
                    if (v0 is null)
                        v0 = new[] { x1, fm * x1 + fb };
                    else if (v0[0] < x0)
                        return false;
                    v1 = new[] { x0, fm * x0 + fb };
                }
            }
        }

        edge.V0 = v0;
        edge.V1 = v1;
        return true;
    }

    private void ClipEdges(double x0, double y0, double x1, double y1)
    {
        var i = edges.Count;
        while (i-- > 0)
        {
            var edge = edges[i]!;
            if (
                !ConnectEdge(edge, x0, y0, x1, y1)
                || !ClipEdge(edge, x0, y0, x1, y1)
                || !(Math.Abs(edge.V0![0] - edge.V1![0]) > Epsilon || Math.Abs(edge.V0[1] - edge.V1[1]) > Epsilon)
            )
            {
                edges[i] = null; // delete edges[i]
            }
        }
    }

    // ---- Cell.js ----

    private double CellHalfedgeAngle(VoronoiCell cell, VoronoiEdge edge)
    {
        var site = cell.Site;
        var va = edge.Left;
        var vb = edge.Right;
        if (site == vb)
        {
            vb = va;
            va = site;
        }
        if (vb is not null)
            return Math.Atan2(vb.Y - va.Y, vb.X - va.X);

        double[] pa,
            pb;
        if (site == va)
        {
            pa = edge.V1!;
            pb = edge.V0!;
        }
        else
        {
            pa = edge.V0!;
            pb = edge.V1!;
        }
        return Math.Atan2(pa[0] - pb[0], pb[1] - pa[1]);
    }

    private static double[] CellHalfedgeStart(VoronoiCell cell, VoronoiEdge edge) =>
        edge.Left != cell.Site ? edge.V1! : edge.V0!;

    private static double[] CellHalfedgeEnd(VoronoiCell cell, VoronoiEdge edge) =>
        edge.Left == cell.Site ? edge.V1! : edge.V0!;

    private void SortCellHalfedges()
    {
        foreach (var cell in cells)
        {
            if (cell is null || cell.Halfedges.Count == 0)
                continue;
            var halfedges = cell.Halfedges;
            var m = halfedges.Count;
            var index = new int[m];
            var angles = new double[m];
            for (var j = 0; j < m; ++j)
            {
                index[j] = j;
                angles[j] = CellHalfedgeAngle(cell, edges[halfedges[j]]!);
            }
            // sort por ângulo DECRESCENTE (comparator array[j] - array[i]); estável
            var ordered = index.OrderByDescending(j => angles[j]).ToArray();
            var reordered = new int[m];
            for (var j = 0; j < m; ++j)
                reordered[j] = halfedges[ordered[j]];
            for (var j = 0; j < m; ++j)
                halfedges[j] = reordered[j];
        }
    }

    private void ClipCells(double x0, double y0, double x1, double y1)
    {
        var cellCount = cells.Length;
        var cover = true;

        for (var iCell = 0; iCell < cellCount; ++iCell)
        {
            if (cells[iCell] is not { } cell)
                continue;
            var site = cell.Site;
            var halfedges = cell.Halfedges;

            // remove referências a arestas recortadas fora
            var iHalfedge = halfedges.Count;
            while (iHalfedge-- > 0)
            {
                if (edges[halfedges[iHalfedge]] is null)
                    halfedges.RemoveAt(iHalfedge);
            }

            // insere as arestas de borda
            iHalfedge = 0;
            var halfedgeCount = halfedges.Count;
            while (iHalfedge < halfedgeCount)
            {
                var end = CellHalfedgeEnd(cell, edges[halfedges[iHalfedge]]!);
                var endX = end[0];
                var endY = end[1];
                iHalfedge++;
                var start = CellHalfedgeStart(cell, edges[halfedges[iHalfedge % halfedgeCount]]!);
                var startX = start[0];
                var startY = start[1];
                if (Math.Abs(endX - startX) > Epsilon || Math.Abs(endY - startY) > Epsilon)
                {
                    var borderEnd =
                        Math.Abs(endX - x0) < Epsilon && y1 - endY > Epsilon
                            ? new[] { x0, Math.Abs(startX - x0) < Epsilon ? startY : y1 }
                        : Math.Abs(endY - y1) < Epsilon && x1 - endX > Epsilon
                            ? new[] { Math.Abs(startY - y1) < Epsilon ? startX : x1, y1 }
                        : Math.Abs(endX - x1) < Epsilon && endY - y0 > Epsilon
                            ? new[] { x1, Math.Abs(startX - x1) < Epsilon ? startY : y0 }
                        : Math.Abs(endY - y0) < Epsilon && endX - x0 > Epsilon
                            ? new[] { Math.Abs(startY - y0) < Epsilon ? startX : x0, y0 }
                        : null;
                    edges.Add(CreateBorderEdge(site, end, borderEnd));
                    halfedges.Insert(iHalfedge, edges.Count - 1);
                    ++halfedgeCount;
                }
            }

            if (halfedgeCount > 0)
                cover = false;
        }

        // nenhum tinha arestas → o site mais próximo cobre o extent inteiro
        if (cover)
        {
            VoronoiCell? coverCell = null;
            var bestDistance = double.PositiveInfinity;
            for (var iCell = 0; iCell < cellCount; ++iCell)
            {
                if (cells[iCell] is not { } cell)
                    continue;
                var site = cell.Site;
                var dx = site.X - x0;
                var dy = site.Y - y0;
                var d2 = dx * dx + dy * dy;
                if (d2 < bestDistance)
                {
                    bestDistance = d2;
                    coverCell = cell;
                }
            }

            if (coverCell is not null)
            {
                var v00 = new[] { x0, y0 };
                var v01 = new[] { x0, y1 };
                var v11 = new[] { x1, y1 };
                var v10 = new[] { x1, y0 };
                var site = coverCell.Site;
                edges.Add(CreateBorderEdge(site, v00, v01));
                coverCell.Halfedges.Add(edges.Count - 1);
                edges.Add(CreateBorderEdge(site, v01, v11));
                coverCell.Halfedges.Add(edges.Count - 1);
                edges.Add(CreateBorderEdge(site, v11, v10));
                coverCell.Halfedges.Add(edges.Count - 1);
                edges.Add(CreateBorderEdge(site, v10, v00));
                coverCell.Halfedges.Add(edges.Count - 1);
            }
        }

        // células totalmente recortadas somem
        for (var iCell = 0; iCell < cellCount; ++iCell)
        {
            if (cells[iCell] is { } cell && cell.Halfedges.Count == 0)
                cells[iCell] = null; // delete cells[iCell]
        }
    }
}
