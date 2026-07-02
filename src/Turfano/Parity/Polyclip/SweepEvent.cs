namespace Turfano.GeoJson.Polyclip;

/// <summary>
/// Porte de `src/sweep-event.ts` do polyclip-ts. ATENÇÃO: o comparator estático tem o
/// EFEITO COLATERAL da fonte — quando dois eventos empatam em coordenadas mas apontam para
/// objetos de ponto distintos, ele os "linka" (funde as listas de eventos). Isso acontece
/// dentro das operações da árvore splay, exatamente como no original.
/// </summary>
internal sealed class SweepEvent
{
    public SweepPoint Point;
    public bool IsLeft;
    public Segment Segment = null!;
    public SweepEvent OtherEvent = null!; // otherSE na fonte
    public SweepEvent? ConsumedBy;

    /// <summary>Ordenação da fila de eventos do sweep.</summary>
    public static int Compare(SweepEvent a, SweepEvent b)
    {
        var pointComparison = ComparePoints(a.Point, b.Point);
        if (pointComparison != 0)
            return pointComparison;

        // efeito colateral da fonte: pontos iguais em valor mas objetos distintos → link
        if (!ReferenceEquals(a.Point, b.Point))
            a.Link(b);

        if (a.IsLeft != b.IsLeft)
            return a.IsLeft ? 1 : -1;

        return Polyclip.Segment.Compare(a.Segment, b.Segment);
    }

    /// <summary>Ordenação de pontos em ordem de varredura (comparação EXATA, sem precision).</summary>
    public static int ComparePoints(SweepPoint aPoint, SweepPoint bPoint)
    {
        if (aPoint.X.IsLessThan(bPoint.X))
            return -1;
        if (aPoint.X.IsGreaterThan(bPoint.X))
            return 1;
        if (aPoint.Y.IsLessThan(bPoint.Y))
            return -1;
        if (aPoint.Y.IsGreaterThan(bPoint.Y))
            return 1;
        return 0;
    }

    public SweepEvent(SweepPoint point, bool isLeft)
    {
        if (point.Events is null)
            point.Events = new List<SweepEvent> { this };
        else
            point.Events.Add(this);
        Point = point;
        IsLeft = isLeft;
    }

    public void Link(SweepEvent other)
    {
        if (ReferenceEquals(other.Point, Point))
            throw new InvalidOperationException("Tried to link already linked events");

        var otherEvents = other.Point.Events!;
        for (int i = 0, iMax = otherEvents.Count; i < iMax; i++)
        {
            var evt = otherEvents[i];
            Point.Events!.Add(evt);
            evt.Point = Point;
        }
        CheckForConsuming();
    }

    /// <summary>Passa pelos eventos linkados e consome pares de segmentos idênticos.</summary>
    public void CheckForConsuming()
    {
        var numEvents = Point.Events!.Count;
        for (var i = 0; i < numEvents; i++)
        {
            var evt1 = Point.Events[i];
            if (evt1.Segment.ConsumedBy is not null)
                continue;
            for (var j = i + 1; j < numEvents; j++)
            {
                var evt2 = Point.Events[j];
                if (evt2.ConsumedBy is not null)
                    continue;
                // na fonte: comparação de REFERÊNCIA das listas de eventos dos pontos opostos
                if (!ReferenceEquals(evt1.OtherEvent.Point.Events, evt2.OtherEvent.Point.Events))
                    continue;
                evt1.Segment.Consume(evt2.Segment);
            }
        }
    }

    public List<SweepEvent> GetAvailableLinkedEvents()
    {
        var events = new List<SweepEvent>();
        for (int i = 0, iMax = Point.Events!.Count; i < iMax; i++)
        {
            var evt = Point.Events[i];
            if (evt != this && evt.Segment.RingOut is null && evt.Segment.IsInResult())
                events.Add(evt);
        }
        return events;
    }

    /// <summary>
    /// Comparator que favorece o evento com o menor ângulo à esquerda (construção de anéis
    /// virando sempre o máximo à esquerda). Cache de seno/cosseno como na fonte.
    /// </summary>
    public Comparison<SweepEvent> GetLeftmostComparator(SweepEvent baseEvent)
    {
        var cache = new Dictionary<SweepEvent, (ExactDecimal Sine, ExactDecimal Cosine)>();

        void FillCache(SweepEvent linkedEvent)
        {
            var nextEvent = linkedEvent.OtherEvent;
            cache[linkedEvent] = (
                ExactVectorMath.SineOfAngle(Point, baseEvent.Point, nextEvent.Point),
                ExactVectorMath.CosineOfAngle(Point, baseEvent.Point, nextEvent.Point)
            );
        }

        return (a, b) =>
        {
            if (!cache.ContainsKey(a))
                FillCache(a);
            if (!cache.ContainsKey(b))
                FillCache(b);

            var (aSine, aCosine) = cache[a];
            var (bSine, bCosine) = cache[b];

            if (aSine.IsGreaterThanOrEqualTo(0) && bSine.IsGreaterThanOrEqualTo(0))
            {
                if (aCosine.IsLessThan(bCosine))
                    return 1;
                if (aCosine.IsGreaterThan(bCosine))
                    return -1;
                return 0;
            }
            if (aSine.IsLessThan(0) && bSine.IsLessThan(0))
            {
                if (aCosine.IsLessThan(bCosine))
                    return -1;
                if (aCosine.IsGreaterThan(bCosine))
                    return 1;
                return 0;
            }
            if (bSine.IsLessThan(aSine))
                return -1;
            if (bSine.IsGreaterThan(aSine))
                return 1;
            return 0;
        };
    }
}
