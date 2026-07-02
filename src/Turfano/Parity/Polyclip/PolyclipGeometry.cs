namespace Turfano.GeoJson.Polyclip;

/// <summary>
/// Ponto do sweep — o objeto `point` do polyclip (que em JS ganha a propriedade dinâmica
/// `events`). A identidade REFERENCIAL importa: `a.point !== b.point` e
/// `event.point === startingPoint` são comparações de referência na fonte.
/// </summary>
internal sealed class SweepPoint(ExactDecimal x, ExactDecimal y)
{
    public ExactDecimal X = x;
    public ExactDecimal Y = y;
    public List<SweepEvent>? Events;
}

/// <summary>Vetor 2D em decimal exato (objetos `{x, y}` sem events na fonte).</summary>
internal readonly record struct ExactVector(ExactDecimal X, ExactDecimal Y);

/// <summary>Envelope `{ll, ur}` do polyclip (mutável, como na fonte `geom-in`).</summary>
internal sealed class ExactBounds
{
    public ExactDecimal LowerX;
    public ExactDecimal LowerY;
    public ExactDecimal UpperX;
    public ExactDecimal UpperY;

    // src/bbox.ts isInBbox
    public bool Contains(ExactDecimal x, ExactDecimal y) =>
        LowerX.IsLessThanOrEqualTo(x)
        && x.IsLessThanOrEqualTo(UpperX)
        && LowerY.IsLessThanOrEqualTo(y)
        && y.IsLessThanOrEqualTo(UpperY);

    // src/bbox.ts getBboxOverlap
    public static ExactBounds? Overlap(ExactBounds b1, ExactBounds b2)
    {
        if (
            b2.UpperX.IsLessThan(b1.LowerX)
            || b1.UpperX.IsLessThan(b2.LowerX)
            || b2.UpperY.IsLessThan(b1.LowerY)
            || b1.UpperY.IsLessThan(b2.LowerY)
        )
            return null;

        return new ExactBounds
        {
            LowerX = b1.LowerX.IsLessThan(b2.LowerX) ? b2.LowerX : b1.LowerX,
            UpperX = b1.UpperX.IsLessThan(b2.UpperX) ? b1.UpperX : b2.UpperX,
            LowerY = b1.LowerY.IsLessThan(b2.LowerY) ? b2.LowerY : b1.LowerY,
            UpperY = b1.UpperY.IsLessThan(b2.UpperY) ? b1.UpperY : b2.UpperY,
        };
    }
}

/// <summary>Porte de `src/vector.ts` (cross/dot/length/ângulos/interseções).</summary>
internal static class ExactVectorMath
{
    public static ExactDecimal CrossProduct(ExactVector a, ExactVector b) =>
        a.X.Times(b.Y).Minus(a.Y.Times(b.X));

    public static ExactDecimal DotProduct(ExactVector a, ExactVector b) =>
        a.X.Times(b.X).Plus(a.Y.Times(b.Y));

    public static ExactDecimal Length(ExactVector v) => DotProduct(v, v).Sqrt();

    public static ExactDecimal SineOfAngle(SweepPoint shared, SweepPoint basePoint, SweepPoint anglePoint)
    {
        var vectorBase = new ExactVector(basePoint.X.Minus(shared.X), basePoint.Y.Minus(shared.Y));
        var vectorAngle = new ExactVector(anglePoint.X.Minus(shared.X), anglePoint.Y.Minus(shared.Y));
        return CrossProduct(vectorAngle, vectorBase).Div(Length(vectorAngle)).Div(Length(vectorBase));
    }

    public static ExactDecimal CosineOfAngle(SweepPoint shared, SweepPoint basePoint, SweepPoint anglePoint)
    {
        var vectorBase = new ExactVector(basePoint.X.Minus(shared.X), basePoint.Y.Minus(shared.Y));
        var vectorAngle = new ExactVector(anglePoint.X.Minus(shared.X), anglePoint.Y.Minus(shared.Y));
        return DotProduct(vectorAngle, vectorBase).Div(Length(vectorAngle)).Div(Length(vectorBase));
    }

    private static SweepPoint? HorizontalIntersection(SweepPoint pt, ExactVector v, ExactDecimal y)
    {
        if (v.Y.IsZero())
            return null;
        return new SweepPoint(pt.X.Plus(v.X.Div(v.Y).Times(y.Minus(pt.Y))), y);
    }

    private static SweepPoint? VerticalIntersection(SweepPoint pt, ExactVector v, ExactDecimal x)
    {
        if (v.X.IsZero())
            return null;
        return new SweepPoint(x, pt.Y.Plus(v.Y.Div(v.X).Times(x.Minus(pt.X))));
    }

    /// <summary>Interseção das retas (pt1, v1) × (pt2, v2) — `src/vector.ts intersection`.</summary>
    public static SweepPoint? Intersection(SweepPoint pt1, ExactVector v1, SweepPoint pt2, ExactVector v2)
    {
        if (v1.X.IsZero())
            return VerticalIntersection(pt2, v2, pt1.X);
        if (v2.X.IsZero())
            return VerticalIntersection(pt1, v1, pt2.X);
        if (v1.Y.IsZero())
            return HorizontalIntersection(pt2, v2, pt1.Y);
        if (v2.Y.IsZero())
            return HorizontalIntersection(pt1, v1, pt2.Y);

        var kross = CrossProduct(v1, v2);
        if (kross.IsZero())
            return null;

        var ve = new ExactVector(pt2.X.Minus(pt1.X), pt2.Y.Minus(pt1.Y));
        var d1 = CrossProduct(ve, v1).Div(kross);
        var d2 = CrossProduct(ve, v2).Div(kross);
        var x1 = pt1.X.Plus(d2.Times(v1.X));
        var x2 = pt2.X.Plus(d1.Times(v2.X));
        var y1 = pt1.Y.Plus(d2.Times(v1.Y));
        var y2 = pt2.Y.Plus(d1.Times(v2.Y));
        var x = x1.Plus(x2).Div(2);
        var y = y1.Plus(y2).Div(2);
        return new SweepPoint(x, y);
    }
}
