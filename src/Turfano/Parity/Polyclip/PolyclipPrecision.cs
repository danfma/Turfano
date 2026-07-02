namespace Turfano.GeoJson.Polyclip;

/// <summary>
/// Porte de `src/{constant,compare,orient,snap,identity,precision}.ts` do polyclip-ts.
/// Default (eps nulo) = comparações EXATAS, `Orient` exato e `Snap` identidade — é assim
/// que o `@turf` executa. O caminho com eps é portado por fidelidade, mas não há
/// `setPrecision` público.
/// </summary>
internal sealed class PolyclipPrecision
{
    /// <summary>A instância default (eps `undefined` na fonte).</summary>
    public static readonly PolyclipPrecision Exact = new(null);

    private sealed class CoordinateBox(ExactDecimal value)
    {
        public readonly ExactDecimal Value = value;
    }

    private readonly ExactDecimal? epsilon;
    private readonly SplayTreeSet<CoordinateBox>? snapXTree;
    private readonly SplayTreeSet<CoordinateBox>? snapYTree;

    public PolyclipPrecision(ExactDecimal? epsilon)
    {
        this.epsilon = epsilon;
        if (epsilon is not null)
        {
            snapXTree = new SplayTreeSet<CoordinateBox>((a, b) => Compare(a.Value, b.Value));
            snapYTree = new SplayTreeSet<CoordinateBox>((a, b) => Compare(a.Value, b.Value));
            // a fonte "aquece" as árvores com o ponto (0,0)
            Snap(new SweepPoint(ExactDecimal.Zero, ExactDecimal.Zero));
        }
    }

    // src/compare.ts
    public int Compare(ExactDecimal a, ExactDecimal b)
    {
        if (epsilon is { } eps && b.Minus(a).Abs().IsLessThanOrEqualTo(eps))
            return 0;
        return a.ComparedTo(b);
    }

    // src/orient.ts — sinal da área dupla do triângulo (a, b, c)
    public int Orient(SweepPoint a, SweepPoint b, SweepPoint c)
    {
        var ax = a.X;
        var ay = a.Y;
        var cx = c.X;
        var cy = c.Y;
        var doubleArea = ay.Minus(cy).Times(b.X.Minus(cx)).Minus(ax.Minus(cx).Times(b.Y.Minus(cy)));

        if (epsilon is { } eps)
        {
            var almostCollinear = doubleArea
                .Square()
                .IsLessThanOrEqualTo(cx.Minus(ax).Square().Plus(cy.Minus(ay).Square()).Times(eps));
            if (almostCollinear)
                return 0;
        }

        return doubleArea.ComparedTo(ExactDecimal.Zero);
    }

    // src/snap.ts — com eps, deduplica coordenadas pelas árvores; sem eps, identidade
    public SweepPoint Snap(SweepPoint point)
    {
        if (epsilon is null)
            return point;

        return new SweepPoint(
            snapXTree!.AddAndReturn(new CoordinateBox(point.X)).Value,
            snapYTree!.AddAndReturn(new CoordinateBox(point.Y)).Value
        );
    }
}
