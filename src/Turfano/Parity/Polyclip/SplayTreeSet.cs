namespace Turfano.GeoJson.Polyclip;

/// <summary>
/// Conjunto ordenado por árvore splay — porte do `splaytree-ts` (BSD-3, © 2022 Luiz Felipe
/// Machado Barboza; por sua vez um porte do SplayTreeSet do Dart). O polyclip usa:
/// `Add`, `AddAndReturn` (devolve o elemento JÁ EXISTENTE quando o comparator empata —
/// essencial para o `snap`), `Delete`, `First`, `LastBefore` (maior estritamente menor),
/// `FirstAfter` (menor estritamente maior) e `Count`. Splay top-down clássico
/// (Sleator–Tarjan), o mesmo do Dart SDK.
/// </summary>
internal sealed class SplayTreeSet<T>
    where T : class
{
    private sealed class Node(T value)
    {
        public T Value = value;
        public Node? Left;
        public Node? Right;
    }

    private readonly Comparison<T> comparator;
    private Node? root;

    public SplayTreeSet(Comparison<T> comparator) => this.comparator = comparator;

    public int Count { get; private set; }

    public bool Add(T value)
    {
        if (root is null)
        {
            root = new Node(value);
            Count = 1;
            return true;
        }

        var comparison = Splay(value);
        if (comparison == 0)
            return false;

        InsertAtRoot(value, comparison);
        return true;
    }

    /// <summary>Como o `addAndReturn` do splaytree-ts: devolve o elemento existente se o
    /// comparator empatar; senão insere e devolve o novo.</summary>
    public T AddAndReturn(T value)
    {
        if (root is null)
        {
            root = new Node(value);
            Count = 1;
            return value;
        }

        var comparison = Splay(value);
        if (comparison == 0)
            return root.Value;

        InsertAtRoot(value, comparison);
        return value;
    }

    public bool Delete(T value)
    {
        if (root is null)
            return false;

        var comparison = Splay(value);
        if (comparison != 0)
            return false;

        if (root.Left is null)
        {
            root = root.Right;
        }
        else
        {
            var detachedRight = root.Right;
            root = root.Left;
            Splay(value); // leva o máximo da subárvore esquerda à raiz (tudo < value)
            root.Right = detachedRight;
        }
        Count--;
        return true;
    }

    /// <summary>Menor elemento (o chamador garante não-vazio, como no polyclip).</summary>
    public T First()
    {
        var node = root ?? throw new InvalidOperationException("Conjunto vazio.");
        while (node.Left is not null)
            node = node.Left;
        return node.Value;
    }

    /// <summary>Maior elemento estritamente MENOR que `value`; null se não houver.</summary>
    public T? LastBefore(T value)
    {
        if (root is null)
            return null;

        var comparison = Splay(value);
        if (comparison > 0) // raiz < value
            return root.Value;

        var node = root.Left;
        if (node is null)
            return null;
        while (node.Right is not null)
            node = node.Right;
        return node.Value;
    }

    /// <summary>Menor elemento estritamente MAIOR que `value`; null se não houver.</summary>
    public T? FirstAfter(T value)
    {
        if (root is null)
            return null;

        var comparison = Splay(value);
        if (comparison < 0) // raiz > value
            return root.Value;

        var node = root.Right;
        if (node is null)
            return null;
        while (node.Left is not null)
            node = node.Left;
        return node.Value;
    }

    private void InsertAtRoot(T value, int comparison)
    {
        var node = new Node(value);
        // comparison = comparator(value, root.Value) — a raiz é o nó mais próximo pós-splay
        if (comparison < 0)
        {
            node.Left = root!.Left;
            node.Right = root;
            root.Left = null;
        }
        else
        {
            node.Right = root!.Right;
            node.Left = root;
            root.Right = null;
        }
        root = node;
        Count++;
    }

    /// <summary>
    /// Splay top-down: traz o nó mais próximo de `value` para a raiz. Retorna
    /// `comparator(value, root.Value)` após o splay (0 = elemento equivalente na raiz).
    /// </summary>
    private int Splay(T value)
    {
        var current = root!;
        // cabeçalho falso: Left acumula a árvore direita, Right a esquerda
        var header = new Node(current.Value);
        var leftTail = header;
        var rightTail = header;
        int comparison;

        while (true)
        {
            comparison = comparator(value, current.Value);
            if (comparison < 0)
            {
                if (current.Left is null)
                    break;
                if (comparator(value, current.Left.Value) < 0)
                {
                    // rotação à direita
                    var child = current.Left;
                    current.Left = child.Right;
                    child.Right = current;
                    current = child;
                    if (current.Left is null)
                    {
                        comparison = comparator(value, current.Value);
                        break;
                    }
                }
                // liga à árvore direita
                rightTail.Left = current;
                rightTail = current;
                current = current.Left;
            }
            else if (comparison > 0)
            {
                if (current.Right is null)
                    break;
                if (comparator(value, current.Right.Value) > 0)
                {
                    // rotação à esquerda
                    var child = current.Right;
                    current.Right = child.Left;
                    child.Left = current;
                    current = child;
                    if (current.Right is null)
                    {
                        comparison = comparator(value, current.Value);
                        break;
                    }
                }
                // liga à árvore esquerda
                leftTail.Right = current;
                leftTail = current;
                current = current.Right;
            }
            else
            {
                break;
            }
        }

        leftTail.Right = current.Left;
        rightTail.Left = current.Right;
        current.Left = header.Right;
        current.Right = header.Left;
        root = current;
        return comparison;
    }
}
