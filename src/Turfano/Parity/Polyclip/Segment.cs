namespace Turfano.GeoJson.Polyclip;

/// <summary>Estado antes/depois de um segmento (rings/windings/multipolys) — `segment.ts`.</summary>
internal sealed class SegmentState
{
    public List<RingIn> Rings = new();
    public List<int> Windings = new();
    public List<MultiPolyIn> MultiPolys = new();
}

/// <summary>
/// Porte de `src/segment.ts` do polyclip-ts. Adaptação estrutural registrada no plano: o
/// singleton global mutável `operation` (tipo/numMultiPolys/segmentId) vira a instância
/// <see cref="OperationRun"/> recebida no construtor — comportamento idêntico (os ids só
/// servem de tiebreak dentro de uma execução), sem estado global.
/// </summary>
internal sealed class Segment
{
    public readonly int Id;
    public SweepEvent LeftEvent; // leftSE
    public SweepEvent RightEvent; // rightSE
    public List<RingIn>? Rings;
    public List<int>? Windings;
    public RingOut? RingOut;
    public Segment? ConsumedBy;
    public Segment? Previous; // prev

    private readonly OperationRun operation;
    private bool prevInResultComputed;
    private Segment? prevInResult;
    private SegmentState? beforeState;
    private SegmentState? afterState;
    private bool? isInResult;

    /* Ordena segmentos na árvore do sweep: qual dos dois uma linha vertical um passo
     * infinitesimal à direita do left-endpoint mais à direita intersecta primeiro,
     * subindo de y = -infinito. (Comentário completo na fonte, linhas 617-629.) */
    public static int Compare(Segment a, Segment b)
    {
        var alx = a.LeftEvent.Point.X;
        var blx = b.LeftEvent.Point.X;
        var arx = a.RightEvent.Point.X;
        var brx = b.RightEvent.Point.X;

        if (brx.IsLessThan(alx))
            return 1;
        if (arx.IsLessThan(blx))
            return -1;

        var aly = a.LeftEvent.Point.Y;
        var bly = b.LeftEvent.Point.Y;
        var ary = a.RightEvent.Point.Y;
        var bry = b.RightEvent.Point.Y;

        if (alx.IsLessThan(blx))
        {
            if (bly.IsLessThan(aly) && bly.IsLessThan(ary))
                return 1;
            if (bly.IsGreaterThan(aly) && bly.IsGreaterThan(ary))
                return -1;

            var aCompareBLeft = a.ComparePoint(b.LeftEvent.Point);
            if (aCompareBLeft < 0)
                return 1;
            if (aCompareBLeft > 0)
                return -1;
            var bCompareARight = b.ComparePoint(a.RightEvent.Point);
            if (bCompareARight != 0)
                return bCompareARight;
            return -1;
        }

        if (alx.IsGreaterThan(blx))
        {
            if (aly.IsLessThan(bly) && aly.IsLessThan(bry))
                return -1;
            if (aly.IsGreaterThan(bly) && aly.IsGreaterThan(bry))
                return 1;

            var bCompareALeft = b.ComparePoint(a.LeftEvent.Point);
            if (bCompareALeft != 0)
                return bCompareALeft;
            var aCompareBRight = a.ComparePoint(b.RightEvent.Point);
            if (aCompareBRight < 0)
                return 1;
            if (aCompareBRight > 0)
                return -1;
            return 1;
        }

        if (aly.IsLessThan(bly))
            return -1;
        if (aly.IsGreaterThan(bly))
            return 1;

        if (arx.IsLessThan(brx))
        {
            var bCompareARight = b.ComparePoint(a.RightEvent.Point);
            if (bCompareARight != 0)
                return bCompareARight;
        }
        if (arx.IsGreaterThan(brx))
        {
            var aCompareBRight = a.ComparePoint(b.RightEvent.Point);
            if (aCompareBRight < 0)
                return 1;
            if (aCompareBRight > 0)
                return -1;
        }

        if (!arx.Eq(brx))
        {
            var ay = ary.Minus(aly);
            var ax = arx.Minus(alx);
            var by = bry.Minus(bly);
            var bx = brx.Minus(blx);
            if (ay.IsGreaterThan(ax) && by.IsLessThan(bx))
                return 1;
            if (ay.IsLessThan(ax) && by.IsGreaterThan(bx))
                return -1;
        }

        if (arx.IsGreaterThan(brx))
            return 1;
        if (arx.IsLessThan(brx))
            return -1;
        if (ary.IsLessThan(bry))
            return -1;
        if (ary.IsGreaterThan(bry))
            return 1;

        if (a.Id < b.Id)
            return -1;
        if (a.Id > b.Id)
            return 1;
        return 0;
    }

    public Segment(SweepEvent leftEvent, SweepEvent rightEvent, List<RingIn> rings, List<int> windings, OperationRun operation)
    {
        this.operation = operation;
        Id = operation.NextSegmentId();
        LeftEvent = leftEvent;
        leftEvent.Segment = this;
        leftEvent.OtherEvent = rightEvent;
        RightEvent = rightEvent;
        rightEvent.Segment = this;
        rightEvent.OtherEvent = leftEvent;
        Rings = rings;
        Windings = windings;
    }

    public static Segment FromRing(SweepPoint pt1, SweepPoint pt2, RingIn ring)
    {
        SweepPoint leftPoint,
            rightPoint;
        int winding;

        var pointComparison = SweepEvent.ComparePoints(pt1, pt2);
        if (pointComparison < 0)
        {
            leftPoint = pt1;
            rightPoint = pt2;
            winding = 1;
        }
        else if (pointComparison > 0)
        {
            leftPoint = pt2;
            rightPoint = pt1;
            winding = -1;
        }
        else
        {
            throw new InvalidOperationException($"Tried to create degenerate segment at [{pt1.X}, {pt1.Y}]");
        }

        var leftEvent = new SweepEvent(leftPoint, true);
        var rightEvent = new SweepEvent(rightPoint, false);
        return new Segment(leftEvent, rightEvent, new List<RingIn> { ring }, new List<int> { winding }, ring.Poly.MultiPoly.Operation);
    }

    /// <summary>No split, o rightSE é substituído por um novo sweep event.</summary>
    public void ReplaceRightEvent(SweepEvent newRightEvent)
    {
        RightEvent = newRightEvent;
        RightEvent.Segment = this;
        RightEvent.OtherEvent = LeftEvent;
        LeftEvent.OtherEvent = RightEvent;
    }

    public ExactBounds Bounds()
    {
        var y1 = LeftEvent.Point.Y;
        var y2 = RightEvent.Point.Y;
        return new ExactBounds
        {
            LowerX = LeftEvent.Point.X,
            LowerY = y1.IsLessThan(y2) ? y1 : y2,
            UpperX = RightEvent.Point.X,
            UpperY = y1.IsGreaterThan(y2) ? y1 : y2,
        };
    }

    public ExactVector Vector() =>
        new(RightEvent.Point.X.Minus(LeftEvent.Point.X), RightEvent.Point.Y.Minus(LeftEvent.Point.Y));

    public bool IsAnEndpoint(SweepPoint pt) =>
        (pt.X.Eq(LeftEvent.Point.X) && pt.Y.Eq(LeftEvent.Point.Y))
        || (pt.X.Eq(RightEvent.Point.X) && pt.Y.Eq(RightEvent.Point.Y));

    /// <summary>1 = ponto acima do segmento; 0 = colinear; -1 = abaixo.</summary>
    public int ComparePoint(SweepPoint point) =>
        PolyclipPrecision.Exact.Orient(LeftEvent.Point, point, RightEvent.Point);

    /// <summary>Primeira interseção não-trivial (em ordem de varredura) com outro segmento,
    /// ou null (regras completas no comentário da fonte, linhas 761-775).</summary>
    public SweepPoint? GetIntersection(Segment other)
    {
        var thisBounds = Bounds();
        var otherBounds = other.Bounds();
        var boundsOverlap = ExactBounds.Overlap(thisBounds, otherBounds);
        if (boundsOverlap is null)
            return null;

        var thisLeftPoint = LeftEvent.Point;
        var thisRightPoint = RightEvent.Point;
        var otherLeftPoint = other.LeftEvent.Point;
        var otherRightPoint = other.RightEvent.Point;

        var touchesOtherLeft = thisBounds.Contains(otherLeftPoint.X, otherLeftPoint.Y) && ComparePoint(otherLeftPoint) == 0;
        var touchesThisLeft = otherBounds.Contains(thisLeftPoint.X, thisLeftPoint.Y) && other.ComparePoint(thisLeftPoint) == 0;
        var touchesOtherRight = thisBounds.Contains(otherRightPoint.X, otherRightPoint.Y) && ComparePoint(otherRightPoint) == 0;
        var touchesThisRight = otherBounds.Contains(thisRightPoint.X, thisRightPoint.Y) && other.ComparePoint(thisRightPoint) == 0;

        if (touchesThisLeft && touchesOtherLeft)
        {
            if (touchesThisRight && !touchesOtherRight)
                return thisRightPoint;
            if (!touchesThisRight && touchesOtherRight)
                return otherRightPoint;
            return null;
        }

        if (touchesThisLeft)
        {
            if (touchesOtherRight)
            {
                if (thisLeftPoint.X.Eq(otherRightPoint.X) && thisLeftPoint.Y.Eq(otherRightPoint.Y))
                    return null;
            }
            return thisLeftPoint;
        }

        if (touchesOtherLeft)
        {
            if (touchesThisRight)
            {
                if (thisRightPoint.X.Eq(otherLeftPoint.X) && thisRightPoint.Y.Eq(otherLeftPoint.Y))
                    return null;
            }
            return otherLeftPoint;
        }

        if (touchesThisRight && touchesOtherRight)
            return null;
        if (touchesThisRight)
            return thisRightPoint;
        if (touchesOtherRight)
            return otherRightPoint;

        var intersectionPoint = ExactVectorMath.Intersection(thisLeftPoint, Vector(), otherLeftPoint, other.Vector());
        if (intersectionPoint is null)
            return null;
        if (!boundsOverlap.Contains(intersectionPoint.X, intersectionPoint.Y))
            return null;
        return PolyclipPrecision.Exact.Snap(intersectionPoint);
    }

    /// <summary>Divide o segmento no ponto dado; devolve os novos sweep events gerados.</summary>
    public List<SweepEvent> Split(SweepPoint point)
    {
        var newEvents = new List<SweepEvent>();
        var alreadyLinked = point.Events is not null;

        var newLeftEvent = new SweepEvent(point, true);
        var newRightEvent = new SweepEvent(point, false);
        var oldRightEvent = RightEvent;
        ReplaceRightEvent(newRightEvent);
        newEvents.Add(newRightEvent);
        newEvents.Add(newLeftEvent);

        var newSegment = new Segment(newLeftEvent, oldRightEvent, new List<RingIn>(Rings!), new List<int>(Windings!), operation);

        if (SweepEvent.ComparePoints(newSegment.LeftEvent.Point, newSegment.RightEvent.Point) > 0)
            newSegment.SwapEvents();
        if (SweepEvent.ComparePoints(LeftEvent.Point, RightEvent.Point) > 0)
            SwapEvents();

        if (alreadyLinked)
        {
            newLeftEvent.CheckForConsuming();
            newRightEvent.CheckForConsuming();
        }

        return newEvents;
    }

    private void SwapEvents()
    {
        (LeftEvent, RightEvent) = (RightEvent, LeftEvent);
        LeftEvent.IsLeft = true;
        RightEvent.IsLeft = false;
        for (int i = 0, iMax = Windings!.Count; i < iMax; i++)
            Windings[i] *= -1;
    }

    /// <summary>Consome outro segmento (sobreposição perfeita): herda os anéis dele.</summary>
    public void Consume(Segment other)
    {
        var consumer = this;
        var consumee = other;
        while (consumer.ConsumedBy is not null)
            consumer = consumer.ConsumedBy;
        while (consumee.ConsumedBy is not null)
            consumee = consumee.ConsumedBy;

        var comparison = Compare(consumer, consumee);
        if (comparison == 0)
            return; // já é o mesmo
        if (comparison > 0)
            (consumer, consumee) = (consumee, consumer);
        if (consumer.Previous == consumee)
            (consumer, consumee) = (consumee, consumer);

        for (int i = 0, iMax = consumee.Rings!.Count; i < iMax; i++)
        {
            var ring = consumee.Rings[i];
            var winding = consumee.Windings![i];
            var index = consumer.Rings!.IndexOf(ring);
            if (index == -1)
            {
                consumer.Rings.Add(ring);
                consumer.Windings!.Add(winding);
            }
            else
            {
                consumer.Windings![index] += winding;
            }
        }

        consumee.Rings = null;
        consumee.Windings = null;
        consumee.ConsumedBy = consumer;
        consumee.LeftEvent.ConsumedBy = consumer.LeftEvent;
        consumee.RightEvent.ConsumedBy = consumer.RightEvent;
    }

    /// <summary>Primeiro segmento da cadeia `prev` que está no resultado.</summary>
    public Segment? PrevInResult()
    {
        if (prevInResultComputed)
            return prevInResult;
        prevInResultComputed = true;
        if (Previous is null)
            prevInResult = null;
        else if (Previous.IsInResult())
            prevInResult = Previous;
        else
            prevInResult = Previous.PrevInResult();
        return prevInResult;
    }

    public SegmentState BeforeState()
    {
        if (beforeState is not null)
            return beforeState;
        if (Previous is null)
        {
            beforeState = new SegmentState();
        }
        else
        {
            var segment = Previous.ConsumedBy ?? Previous;
            beforeState = segment.AfterState();
        }
        return beforeState;
    }

    public SegmentState AfterState()
    {
        if (afterState is not null)
            return afterState;

        var before = BeforeState();
        afterState = new SegmentState
        {
            Rings = new List<RingIn>(before.Rings),
            Windings = new List<int>(before.Windings),
            MultiPolys = new List<MultiPolyIn>(),
        };
        var ringsAfter = afterState.Rings;
        var windingsAfter = afterState.Windings;
        var multiPolysAfter = afterState.MultiPolys;

        // calcula os windings pós-segmento
        for (int i = 0, iMax = Rings!.Count; i < iMax; i++)
        {
            var ring = Rings[i];
            var winding = Windings![i];
            var index = ringsAfter.IndexOf(ring);
            if (index == -1)
            {
                ringsAfter.Add(ring);
                windingsAfter.Add(winding);
            }
            else
            {
                windingsAfter[index] += winding;
            }
        }

        // calcula os polys interessados (exterior conta, furo exclui o poly)
        var polysAfter = new List<PolyIn>();
        var polysExclude = new List<PolyIn>();
        for (int i = 0, iMax = ringsAfter.Count; i < iMax; i++)
        {
            if (windingsAfter[i] == 0)
                continue; // anel zerado
            var ring = ringsAfter[i];
            var poly = ring.Poly;
            if (polysExclude.Contains(poly))
                continue;
            if (ring.IsExterior)
            {
                polysAfter.Add(poly);
            }
            else
            {
                if (!polysExclude.Contains(poly))
                    polysExclude.Add(poly);
                var index = polysAfter.IndexOf(ring.Poly);
                if (index != -1)
                    polysAfter.RemoveAt(index);
            }
        }

        for (int i = 0, iMax = polysAfter.Count; i < iMax; i++)
        {
            var multiPoly = polysAfter[i].MultiPoly;
            if (!multiPolysAfter.Contains(multiPoly))
                multiPolysAfter.Add(multiPoly);
        }

        return afterState;
    }

    /// <summary>Este segmento faz parte do resultado final?</summary>
    public bool IsInResult()
    {
        if (ConsumedBy is not null)
            return false;
        if (isInResult is not null)
            return isInResult.Value;

        var multiPolysBefore = BeforeState().MultiPolys;
        var multiPolysAfter = AfterState().MultiPolys;

        switch (operation.Type)
        {
            case PolyclipOperationType.Union:
            {
                var noBefores = multiPolysBefore.Count == 0;
                var noAfters = multiPolysAfter.Count == 0;
                isInResult = noBefores != noAfters;
                break;
            }
            case PolyclipOperationType.Intersection:
            {
                int least,
                    most;
                if (multiPolysBefore.Count < multiPolysAfter.Count)
                {
                    least = multiPolysBefore.Count;
                    most = multiPolysAfter.Count;
                }
                else
                {
                    least = multiPolysAfter.Count;
                    most = multiPolysBefore.Count;
                }
                isInResult = most == operation.NumMultiPolys && least < most;
                break;
            }
            case PolyclipOperationType.Xor:
            {
                var difference = Math.Abs(multiPolysBefore.Count - multiPolysAfter.Count);
                isInResult = difference % 2 == 1;
                break;
            }
            case PolyclipOperationType.Difference:
            {
                static bool IsJustSubject(List<MultiPolyIn> multiPolys) =>
                    multiPolys.Count == 1 && multiPolys[0].IsSubject;
                isInResult = IsJustSubject(multiPolysBefore) != IsJustSubject(multiPolysAfter);
                break;
            }
        }

        return isInResult!.Value;
    }
}
