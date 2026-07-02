namespace Turfano.GeoJson;

// Porte 1:1 do rbush 3.x + quickselect (MIT © Vladimir Agafonkin). A ORDEM dos resultados
// de Search/All é a da travessia da árvore original e é observável pelos consumidores
// (dbscan/line-overlap/...) — por isso o porte preserva: bulk-load OMT com multiSelect/
// quickselect, splits R*-like com sort ESTÁVEL (Array.sort do JS) e pilha de busca LIFO.

/// <summary>Retângulo (bbox) — base de itens e nós.</summary>
internal class SpatialBox
{
    public double MinX = double.PositiveInfinity;
    public double MinY = double.PositiveInfinity;
    public double MaxX = double.NegativeInfinity;
    public double MaxY = double.NegativeInfinity;
}

/// <summary>Item indexado: bbox própria + payload.</summary>
internal sealed class RBushItem<T> : SpatialBox
{
    public readonly T Item;

    public RBushItem(double minX, double minY, double maxX, double maxY, T item)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        Item = item;
    }
}

internal sealed class RBushIndex<T>
{
    private sealed class Node : SpatialBox
    {
        public List<SpatialBox> Children = new();
        public int Height = 1;
        public bool Leaf = true;
    }

    private readonly int maxEntries;
    private readonly int minEntries;
    private Node root = null!;

    public RBushIndex(int maxEntries = 9)
    {
        this.maxEntries = Math.Max(4, maxEntries);
        minEntries = Math.Max(2, (int)Math.Ceiling(this.maxEntries * 0.4));
        Clear();
    }

    public void Clear() => root = new Node();

    public List<RBushItem<T>> All()
    {
        var result = new List<RBushItem<T>>();
        CollectAll(root, result);
        return result;
    }

    /// <summary>Itens cuja bbox intersecta a consulta — NA ORDEM da travessia original.</summary>
    public List<RBushItem<T>> Search(SpatialBox bbox)
    {
        var result = new List<RBushItem<T>>();
        Node? node = root;

        if (!Intersects(bbox, node))
            return result;

        var nodesToSearch = new List<Node>();
        while (node is not null)
        {
            foreach (var child in node.Children)
            {
                if (!Intersects(bbox, child))
                    continue;
                if (node.Leaf)
                    result.Add((RBushItem<T>)child);
                else if (Contains(bbox, child))
                    CollectAll((Node)child, result);
                else
                    nodesToSearch.Add((Node)child);
            }
            if (nodesToSearch.Count > 0)
            {
                node = nodesToSearch[^1];
                nodesToSearch.RemoveAt(nodesToSearch.Count - 1);
            }
            else
            {
                node = null;
            }
        }
        return result;
    }

    public void Load(List<RBushItem<T>> data)
    {
        if (data.Count == 0)
            return;

        if (data.Count < minEntries)
        {
            foreach (var item in data)
                Insert(item);
            return;
        }

        // OMT bulk-load
        var node = Build(new List<SpatialBox>(data), 0, data.Count - 1, 0);

        if (root.Children.Count == 0)
        {
            root = node;
        }
        else if (root.Height == node.Height)
        {
            SplitRoot(root, node);
        }
        else
        {
            if (root.Height < node.Height)
                (root, node) = (node, root);
            InsertInternal(node, root.Height - node.Height - 1, isNode: true);
        }
    }

    public void Insert(RBushItem<T> item) => InsertInternal(item, root.Height - 1, isNode: false);

    public void Remove(RBushItem<T> item)
    {
        Node? node = root;
        var bbox = item;
        var path = new List<Node>();
        var indexes = new List<int>();
        var i = 0;
        Node? parent = null;
        var goingUp = false;

        while (node is not null || path.Count > 0)
        {
            if (node is null)
            {
                node = path[^1];
                path.RemoveAt(path.Count - 1);
                parent = path.Count > 0 ? path[^1] : null;
                i = indexes[^1];
                indexes.RemoveAt(indexes.Count - 1);
                goingUp = true;
            }

            if (node.Leaf)
            {
                var index = node.Children.IndexOf(item);
                if (index != -1)
                {
                    node.Children.RemoveAt(index);
                    path.Add(node);
                    Condense(path);
                    return;
                }
            }

            if (!goingUp && !node.Leaf && Contains(node, bbox))
            {
                path.Add(node);
                indexes.Add(i);
                i = 0;
                parent = node;
                node = (Node)node.Children[0];
            }
            else if (parent is not null)
            {
                i++;
                node = i < parent.Children.Count ? (Node?)parent.Children[i] : null;
                if (node is null)
                {
                    // fim dos irmãos: sobe (o JS indexa fora do array → undefined → sobe)
                    goingUp = false;
                    continue;
                }
                goingUp = false;
            }
            else
            {
                node = null;
            }
        }
    }

    private static void CollectAll(Node node, List<RBushItem<T>> result)
    {
        var nodesToSearch = new List<Node>();
        Node? current = node;
        while (current is not null)
        {
            if (current.Leaf)
            {
                foreach (var child in current.Children)
                    result.Add((RBushItem<T>)child);
            }
            else
            {
                foreach (var child in current.Children)
                    nodesToSearch.Add((Node)child);
            }
            if (nodesToSearch.Count > 0)
            {
                current = nodesToSearch[^1];
                nodesToSearch.RemoveAt(nodesToSearch.Count - 1);
            }
            else
            {
                current = null;
            }
        }
    }

    private Node Build(List<SpatialBox> items, int left, int right, int height)
    {
        var n = right - left + 1;
        var m = maxEntries;
        Node node;

        if (n <= m)
        {
            node = new Node { Children = items.GetRange(left, right - left + 1) };
            CalcBBox(node);
            return node;
        }

        if (height == 0)
        {
            height = (int)Math.Ceiling(Math.Log(n) / Math.Log(m));
            m = (int)Math.Ceiling(n / Math.Pow(maxEntries, height - 1));
        }

        node = new Node { Leaf = false, Height = height };

        var n2 = (int)Math.Ceiling((double)n / m);
        var n1 = n2 * (int)Math.Ceiling(Math.Sqrt(m));

        MultiSelect(items, left, right, n1, CompareMinX);

        for (var i = left; i <= right; i += n1)
        {
            var right2 = Math.Min(i + n1 - 1, right);
            MultiSelect(items, i, right2, n2, CompareMinY);

            for (var j = i; j <= right2; j += n2)
            {
                var right3 = Math.Min(j + n2 - 1, right2);
                node.Children.Add(Build(items, j, right3, height - 1));
            }
        }

        CalcBBox(node);
        return node;
    }

    private Node ChooseSubtree(SpatialBox bbox, Node node, int level, List<Node> path)
    {
        while (true)
        {
            path.Add(node);

            if (node.Leaf || path.Count - 1 == level)
                break;

            var minArea = double.PositiveInfinity;
            var minEnlargement = double.PositiveInfinity;
            Node? targetNode = null;

            foreach (var childBox in node.Children)
            {
                var child = (Node)childBox;
                var area = BBoxArea(child);
                var enlargement = EnlargedArea(bbox, child) - area;

                if (enlargement < minEnlargement)
                {
                    minEnlargement = enlargement;
                    minArea = area < minArea ? area : minArea;
                    targetNode = child;
                }
                else if (enlargement == minEnlargement && area < minArea)
                {
                    minArea = area;
                    targetNode = child;
                }
            }

            node = targetNode ?? (Node)node.Children[0];
        }

        return node;
    }

    private void InsertInternal(SpatialBox item, int level, bool isNode)
    {
        var bbox = item;
        var insertPath = new List<Node>();

        var node = ChooseSubtree(bbox, root, level, insertPath);

        node.Children.Add(item);
        Extend(node, bbox);

        while (level >= 0)
        {
            if (insertPath[level].Children.Count > maxEntries)
            {
                Split(insertPath, level);
                level--;
            }
            else
            {
                break;
            }
        }

        for (var i = level; i >= 0; i--)
            Extend(insertPath[i], bbox);
    }

    private void Split(List<Node> insertPath, int level)
    {
        var node = insertPath[level];
        var total = node.Children.Count;
        var m = minEntries;

        ChooseSplitAxis(node, m, total);

        var splitIndex = ChooseSplitIndex(node, m, total);

        var newNode = new Node
        {
            Children = node.Children.GetRange(splitIndex, node.Children.Count - splitIndex),
            Height = node.Height,
            Leaf = node.Leaf,
        };
        node.Children.RemoveRange(splitIndex, node.Children.Count - splitIndex);

        CalcBBox(node);
        CalcBBox(newNode);

        if (level > 0)
            insertPath[level - 1].Children.Add(newNode);
        else
            SplitRoot(node, newNode);
    }

    private void SplitRoot(Node node, Node newNode)
    {
        root = new Node
        {
            Children = new List<SpatialBox> { node, newNode },
            Height = node.Height + 1,
            Leaf = false,
        };
        CalcBBox(root);
    }

    private int ChooseSplitIndex(Node node, int m, int total)
    {
        var index = 0;
        var minOverlap = double.PositiveInfinity;
        var minArea = double.PositiveInfinity;

        for (var i = m; i <= total - m; i++)
        {
            var bbox1 = DistBBox(node, 0, i);
            var bbox2 = DistBBox(node, i, total);

            var overlap = IntersectionArea(bbox1, bbox2);
            var area = BBoxArea(bbox1) + BBoxArea(bbox2);

            if (overlap < minOverlap)
            {
                minOverlap = overlap;
                index = i;
                minArea = area < minArea ? area : minArea;
            }
            else if (overlap == minOverlap && area < minArea)
            {
                minArea = area;
                index = i;
            }
        }

        return index != 0 ? index : total - m; // `index || M - m` da fonte
    }

    private void ChooseSplitAxis(Node node, int m, int total)
    {
        var xMargin = AllDistMargin(node, m, total, CompareMinX);
        var yMargin = AllDistMargin(node, m, total, CompareMinY);

        if (xMargin < yMargin)
            StableSortChildren(node, CompareMinX);
    }

    private double AllDistMargin(Node node, int m, int total, Comparison<SpatialBox> compare)
    {
        StableSortChildren(node, compare);

        var leftBBox = DistBBox(node, 0, m);
        var rightBBox = DistBBox(node, total - m, total);
        var margin = BBoxMargin(leftBBox) + BBoxMargin(rightBBox);

        for (var i = m; i < total - m; i++)
        {
            Extend(leftBBox, node.Children[i]);
            margin += BBoxMargin(leftBBox);
        }

        for (var i = total - m - 1; i >= m; i--)
        {
            Extend(rightBBox, node.Children[i]);
            margin += BBoxMargin(rightBBox);
        }

        return margin;
    }

    private void Condense(List<Node> path)
    {
        for (var i = path.Count - 1; i >= 0; i--)
        {
            if (path[i].Children.Count == 0)
            {
                if (i > 0)
                    path[i - 1].Children.Remove(path[i]);
                else
                    Clear();
            }
            else
            {
                CalcBBox(path[i]);
            }
        }
    }

    // sort ESTÁVEL, como o Array.sort do JS (a ordem alimenta os splits → a saída)
    private static void StableSortChildren(Node node, Comparison<SpatialBox> compare)
    {
        var ordered = node.Children.OrderBy(c => c, Comparer<SpatialBox>.Create(compare)).ToList();
        node.Children.Clear();
        node.Children.AddRange(ordered);
    }

    private static int CompareMinX(SpatialBox a, SpatialBox b) => Math.Sign(a.MinX - b.MinX);

    private static int CompareMinY(SpatialBox a, SpatialBox b) => Math.Sign(a.MinY - b.MinY);

    private void CalcBBox(Node node) => DistBBox(node, 0, node.Children.Count, node);

    private SpatialBox DistBBox(Node node, int k, int p, SpatialBox? destNode = null)
    {
        destNode ??= new SpatialBox();
        destNode.MinX = double.PositiveInfinity;
        destNode.MinY = double.PositiveInfinity;
        destNode.MaxX = double.NegativeInfinity;
        destNode.MaxY = double.NegativeInfinity;

        for (var i = k; i < p; i++)
            Extend(destNode, node.Children[i]);

        return destNode;
    }

    private static void Extend(SpatialBox a, SpatialBox b)
    {
        a.MinX = Math.Min(a.MinX, b.MinX);
        a.MinY = Math.Min(a.MinY, b.MinY);
        a.MaxX = Math.Max(a.MaxX, b.MaxX);
        a.MaxY = Math.Max(a.MaxY, b.MaxY);
    }

    private static double BBoxArea(SpatialBox a) => (a.MaxX - a.MinX) * (a.MaxY - a.MinY);

    private static double BBoxMargin(SpatialBox a) => (a.MaxX - a.MinX) + (a.MaxY - a.MinY);

    private static double EnlargedArea(SpatialBox a, SpatialBox b) =>
        (Math.Max(b.MaxX, a.MaxX) - Math.Min(b.MinX, a.MinX))
        * (Math.Max(b.MaxY, a.MaxY) - Math.Min(b.MinY, a.MinY));

    private static double IntersectionArea(SpatialBox a, SpatialBox b)
    {
        var minX = Math.Max(a.MinX, b.MinX);
        var minY = Math.Max(a.MinY, b.MinY);
        var maxX = Math.Min(a.MaxX, b.MaxX);
        var maxY = Math.Min(a.MaxY, b.MaxY);
        return Math.Max(0, maxX - minX) * Math.Max(0, maxY - minY);
    }

    private static bool Contains(SpatialBox a, SpatialBox b) =>
        a.MinX <= b.MinX && a.MinY <= b.MinY && b.MaxX <= a.MaxX && b.MaxY <= a.MaxY;

    private static bool Intersects(SpatialBox a, SpatialBox b) =>
        b.MinX <= a.MaxX && b.MinY <= a.MaxY && b.MaxX >= a.MinX && b.MaxY >= a.MinY;

    // multiSelect + quickselect (Floyd–Rivest) da fonte
    private static void MultiSelect(List<SpatialBox> arr, int left, int right, int n, Comparison<SpatialBox> compare)
    {
        var stack = new List<int> { left, right };

        while (stack.Count > 0)
        {
            right = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            left = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            if (right - left <= n)
                continue;

            var mid = left + (int)Math.Ceiling((double)(right - left) / n / 2) * n;
            QuickselectStep(arr, mid, left, right, compare);

            stack.Add(left);
            stack.Add(mid);
            stack.Add(mid);
            stack.Add(right);
        }
    }

    private static void QuickselectStep(List<SpatialBox> arr, int k, int left, int right, Comparison<SpatialBox> compare)
    {
        while (right > left)
        {
            if (right - left > 600)
            {
                var n = right - left + 1;
                var m = k - left + 1;
                var z = Math.Log(n);
                var s = 0.5 * Math.Exp(2 * z / 3);
                var sd = 0.5 * Math.Sqrt(z * s * (n - s) / n) * (m - n / 2.0 < 0 ? -1 : 1);
                var newLeft = Math.Max(left, (int)Math.Floor(k - m * s / n + sd));
                var newRight = Math.Min(right, (int)Math.Floor(k + (n - m) * s / n + sd));
                QuickselectStep(arr, k, newLeft, newRight, compare);
            }

            var t = arr[k];
            var i = left;
            var j = right;

            (arr[left], arr[k]) = (arr[k], arr[left]);
            if (compare(arr[right], t) > 0)
                (arr[left], arr[right]) = (arr[right], arr[left]);

            while (i < j)
            {
                (arr[i], arr[j]) = (arr[j], arr[i]);
                i++;
                j--;
                while (compare(arr[i], t) < 0)
                    i++;
                while (compare(arr[j], t) > 0)
                    j--;
            }

            if (compare(arr[left], t) == 0)
            {
                (arr[left], arr[j]) = (arr[j], arr[left]);
            }
            else
            {
                j++;
                (arr[j], arr[right]) = (arr[right], arr[j]);
            }

            if (j <= k)
                left = j + 1;
            if (k <= j)
                right = j - 1;
        }
    }
}
