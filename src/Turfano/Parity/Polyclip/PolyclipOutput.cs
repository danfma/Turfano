namespace Turfano.GeoJson.Polyclip;

/// <summary>Porte de `src/geom-out.ts`: reconstrução dos anéis de saída.</summary>
internal sealed class RingOut
{
    public readonly List<SweepEvent> Events;
    public PolyOut? Poly;

    private bool? isExteriorRing;
    private bool enclosingRingComputed;
    private RingOut? enclosingRingValue;

    private readonly record struct IntersectionEntry(int Index, SweepPoint Point);

    /// <summary>Monta os anéis fechados a partir dos segmentos marcados para o resultado.</summary>
    public static List<RingOut> Factory(List<Segment> allSegments)
    {
        var ringsOut = new List<RingOut>();

        for (int i = 0, iMax = allSegments.Count; i < iMax; i++)
        {
            var segment = allSegments[i];
            if (!segment.IsInResult() || segment.RingOut is not null)
                continue;

            SweepEvent? prevEvent = null;
            var sweepEvent = segment.LeftEvent;
            var nextEvent = segment.RightEvent;
            var events = new List<SweepEvent> { sweepEvent };
            var startingPoint = sweepEvent.Point;
            var intersectionEntries = new List<IntersectionEntry>();

            while (true)
            {
                prevEvent = sweepEvent;
                sweepEvent = nextEvent;
                events.Add(sweepEvent);

                if (ReferenceEquals(sweepEvent.Point, startingPoint))
                    break;

                while (true)
                {
                    var availableEvents = sweepEvent.GetAvailableLinkedEvents();

                    if (availableEvents.Count == 0)
                    {
                        var firstPoint = events[0].Point;
                        var lastPoint = events[^1].Point;
                        throw new InvalidOperationException(
                            $"Unable to complete output ring starting at [{firstPoint.X}, {firstPoint.Y}]. "
                                + $"Last matching segment found ends at [{lastPoint.X}, {lastPoint.Y}]."
                        );
                    }

                    if (availableEvents.Count == 1)
                    {
                        nextEvent = availableEvents[0].OtherEvent;
                        break;
                    }

                    // já passamos por este ponto de interseção? → fecha um anel interno
                    int? entryIndex = null;
                    for (int j = 0, jMax = intersectionEntries.Count; j < jMax; j++)
                    {
                        if (ReferenceEquals(intersectionEntries[j].Point, sweepEvent.Point))
                        {
                            entryIndex = j;
                            break;
                        }
                    }

                    if (entryIndex is not null)
                    {
                        // splice(indexLE) do JS: remove do índice ao FIM; [0] é a entrada
                        var entry = intersectionEntries[entryIndex.Value];
                        intersectionEntries.RemoveRange(entryIndex.Value, intersectionEntries.Count - entryIndex.Value);

                        // splice(entry.Index): o rabo de `events` vira o anel interno
                        var ringEvents = events.GetRange(entry.Index, events.Count - entry.Index);
                        events.RemoveRange(entry.Index, events.Count - entry.Index);

                        ringEvents.Insert(0, ringEvents[0].OtherEvent);
                        ringEvents.Reverse();
                        ringsOut.Add(new RingOut(ringEvents));
                        continue;
                    }

                    intersectionEntries.Add(new IntersectionEntry(events.Count, sweepEvent.Point));

                    var comparator = sweepEvent.GetLeftmostComparator(prevEvent);
                    // Array.prototype.sort é ESTÁVEL (ES2019) → OrderBy, não List.Sort
                    nextEvent = availableEvents.OrderBy(e => e, Comparer<SweepEvent>.Create(comparator)).First().OtherEvent;
                    break;
                }
            }

            ringsOut.Add(new RingOut(events));
        }

        return ringsOut;
    }

    public RingOut(List<SweepEvent> events)
    {
        Events = events;
        for (int i = 0, iMax = events.Count; i < iMax; i++)
            events[i].Segment.RingOut = this;
        Poly = null;
    }

    /// <summary>Anel como coordenadas (exterior em ordem direta, furo invertido); null se degenerado.</summary>
    public Position[]? GetGeometry()
    {
        var prevPoint = Events[0].Point;
        var points = new List<SweepPoint> { prevPoint };

        for (int i = 1, iMax = Events.Count - 1; i < iMax; i++)
        {
            var point = Events[i].Point;
            var nextPoint = Events[i + 1].Point;
            if (PolyclipPrecision.Exact.Orient(point, prevPoint, nextPoint) == 0)
                continue; // colinear — pula
            points.Add(point);
            prevPoint = point;
        }

        if (points.Count == 1)
            return null;

        var firstPoint = points[0];
        var secondPoint = points[1];
        if (PolyclipPrecision.Exact.Orient(firstPoint, prevPoint, secondPoint) == 0)
            points.RemoveAt(0);

        points.Add(points[0]);

        var step = IsExteriorRing() ? 1 : -1;
        var iStart = IsExteriorRing() ? 0 : points.Count - 1;
        var iEnd = IsExteriorRing() ? points.Count : -1;
        var orderedPoints = new List<Position>();
        for (var i = iStart; i != iEnd; i += step)
            orderedPoints.Add(new Position(points[i].X.ToNumber(), points[i].Y.ToNumber()));
        return orderedPoints.ToArray();
    }

    public bool IsExteriorRing()
    {
        if (isExteriorRing is null)
        {
            var enclosing = EnclosingRing();
            isExteriorRing = enclosing is null || !enclosing.IsExteriorRing();
        }
        return isExteriorRing.Value;
    }

    public RingOut? EnclosingRing()
    {
        if (!enclosingRingComputed)
        {
            enclosingRingValue = CalcEnclosingRing();
            enclosingRingComputed = true;
        }
        return enclosingRingValue;
    }

    /// <summary>O anel que envolve este, se houver.</summary>
    private RingOut? CalcEnclosingRing()
    {
        var leftMostEvent = Events[0];
        for (int i = 1, iMax = Events.Count; i < iMax; i++)
        {
            var sweepEvent = Events[i];
            if (SweepEvent.Compare(leftMostEvent, sweepEvent) > 0)
                leftMostEvent = sweepEvent;
        }

        var prevSegment = leftMostEvent.Segment.PrevInResult();
        var prevPrevSegment = prevSegment?.PrevInResult();

        while (true)
        {
            if (prevSegment is null)
                return null;
            if (prevPrevSegment is null)
                return prevSegment.RingOut;

            if (prevPrevSegment.RingOut != prevSegment.RingOut)
            {
                if (prevPrevSegment.RingOut?.EnclosingRing() != prevSegment.RingOut)
                    return prevSegment.RingOut;
                return prevSegment.RingOut?.EnclosingRing();
            }

            prevSegment = prevPrevSegment.PrevInResult();
            prevPrevSegment = prevSegment?.PrevInResult();
        }
    }
}

/// <summary>Porte de `src/geom-out.ts`: polígono de saída (exterior + furos).</summary>
internal sealed class PolyOut
{
    public readonly RingOut ExteriorRing;
    public readonly List<RingOut> InteriorRings = new();

    public PolyOut(RingOut exteriorRing)
    {
        ExteriorRing = exteriorRing;
        exteriorRing.Poly = this;
    }

    public void AddInterior(RingOut ring)
    {
        InteriorRings.Add(ring);
        ring.Poly = this;
    }

    public Position[][]? GetGeometry()
    {
        var exterior = ExteriorRing.GetGeometry();
        if (exterior is null)
            return null;

        var geometry = new List<Position[]> { exterior };
        for (int i = 0, iMax = InteriorRings.Count; i < iMax; i++)
        {
            var ringGeometry = InteriorRings[i].GetGeometry();
            if (ringGeometry is null)
                continue;
            geometry.Add(ringGeometry);
        }
        return geometry.ToArray();
    }
}

/// <summary>Porte de `src/geom-out.ts`: composição final dos polígonos.</summary>
internal sealed class MultiPolyOut
{
    private readonly List<PolyOut> polys;

    public MultiPolyOut(List<RingOut> rings)
    {
        polys = ComposePolys(rings);
    }

    public Position[][][] GetGeometry()
    {
        var geometry = new List<Position[][]>();
        for (int i = 0, iMax = polys.Count; i < iMax; i++)
        {
            var polyGeometry = polys[i].GetGeometry();
            if (polyGeometry is null)
                continue;
            geometry.Add(polyGeometry);
        }
        return geometry.ToArray();
    }

    private static List<PolyOut> ComposePolys(List<RingOut> rings)
    {
        var polys = new List<PolyOut>();
        for (int i = 0, iMax = rings.Count; i < iMax; i++)
        {
            var ring = rings[i];
            if (ring.Poly is not null)
                continue;
            if (ring.IsExteriorRing())
            {
                polys.Add(new PolyOut(ring));
            }
            else
            {
                var enclosingRing = ring.EnclosingRing();
                if (enclosingRing?.Poly is null)
                    polys.Add(new PolyOut(enclosingRing!));
                enclosingRing?.Poly?.AddInterior(ring);
            }
        }
        return polys;
    }
}
