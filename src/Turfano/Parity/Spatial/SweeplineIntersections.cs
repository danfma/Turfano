namespace Turfano.GeoJson;

/// <summary>
/// Porte 1:1 do sweepline-intersections 1.5 (MIT © Rowan Winsemius) — o motor do
/// `@turf/line-intersect`: fila de eventos em heap binária (TinyQueue), varredura com a
/// fila de segmentos ativos ordenada pelo endpoint direito, interseção segmento-a-segmento.
/// Os contadores globais de módulo do JS (featureId/ringId/eventId) viram campos da
/// instância (só a IGUALDADE dentro de uma execução é observável).
/// </summary>
internal sealed class SweeplineIntersections
{
    private sealed class SweepEvent(double x, double y, int featureId, int ringId)
    {
        public readonly double X = x;
        public readonly double Y = y;
        public readonly int FeatureId = featureId;
        public readonly int RingId = ringId;
        public SweepEvent OtherEvent = null!;
        public bool IsLeftEndpoint;

        public bool IsSamePoint(SweepEvent other) => X == other.X && Y == other.Y;
    }

    private sealed class ActiveSegment(SweepEvent leftEvent)
    {
        public readonly SweepEvent LeftSweepEvent = leftEvent;
        public readonly SweepEvent RightSweepEvent = leftEvent.OtherEvent;
    }

    /// <summary>Heap binária mínima — porte da TinyQueue (a ordem interna do array `data`
    /// é observável: o runCheck itera `outQueue.data` diretamente).</summary>
    private sealed class BinaryHeap<T>(Comparison<T> compare)
    {
        public readonly List<T> Data = new();

        public int Length => Data.Count;

        public void Push(T item)
        {
            Data.Add(item);
            Up(Data.Count - 1);
        }

        public T? Pop()
        {
            if (Data.Count == 0)
                return default;
            var top = Data[0];
            var bottom = Data[^1];
            Data.RemoveAt(Data.Count - 1);
            if (Data.Count > 0)
            {
                Data[0] = bottom;
                Down(0);
            }
            return top;
        }

        private void Up(int pos)
        {
            var item = Data[pos];
            while (pos > 0)
            {
                var parent = (pos - 1) >> 1;
                var current = Data[parent];
                if (compare(item, current) >= 0)
                    break;
                Data[pos] = current;
                pos = parent;
            }
            Data[pos] = item;
        }

        private void Down(int pos)
        {
            var halfLength = Data.Count >> 1;
            var item = Data[pos];
            while (pos < halfLength)
            {
                var left = (pos << 1) + 1;
                var best = Data[left];
                var right = left + 1;
                if (right < Data.Count && compare(Data[right], best) < 0)
                {
                    left = right;
                    best = Data[right];
                }
                if (compare(best, item) >= 0)
                    break;
                Data[pos] = best;
                pos = left;
            }
            Data[pos] = item;
        }
    }

    private static int CheckWhichEventIsLeft(SweepEvent e1, SweepEvent e2)
    {
        if (e1.X > e2.X)
            return 1;
        if (e1.X < e2.X)
            return -1;
        if (e1.Y != e2.Y)
            return e1.Y > e2.Y ? 1 : -1;
        return 1;
    }

    private static int CheckWhichSegmentHasRightEndpointFirst(ActiveSegment seg1, ActiveSegment seg2)
    {
        if (seg1.RightSweepEvent.X > seg2.RightSweepEvent.X)
            return 1;
        if (seg1.RightSweepEvent.X < seg2.RightSweepEvent.X)
            return -1;
        if (seg1.RightSweepEvent.Y != seg2.RightSweepEvent.Y)
            return seg1.RightSweepEvent.Y < seg2.RightSweepEvent.Y ? 1 : -1;
        return 1;
    }

    private int featureId;
    private int ringId;

    /// <summary>Pontos de interseção (na ordem do algoritmo) entre as geometrias dadas.</summary>
    public static List<double[]> Run(IEnumerable<Geometry> features, bool ignoreSelfIntersections)
    {
        var instance = new SweeplineIntersections();
        var eventQueue = new BinaryHeap<SweepEvent>(CheckWhichEventIsLeft);
        foreach (var geometry in features)
            instance.ProcessFeature(geometry, eventQueue);
        return RunCheck(eventQueue, ignoreSelfIntersections);
    }

    private void ProcessFeature(Geometry geometry, BinaryHeap<SweepEvent> eventQueue)
    {
        foreach (var line in Geo.LinearParts(geometry))
        {
            ringId++;
            for (var i = 0; i < line.Length - 1; i++)
            {
                var current = line[i];
                var next = line[i + 1];

                var e1 = new SweepEvent(current.Lon, current.Lat, featureId, ringId);
                var e2 = new SweepEvent(next.Lon, next.Lat, featureId, ringId);
                e1.OtherEvent = e2;
                e2.OtherEvent = e1;

                if (CheckWhichEventIsLeft(e1, e2) > 0)
                {
                    e2.IsLeftEndpoint = true;
                    e1.IsLeftEndpoint = false;
                }
                else
                {
                    e1.IsLeftEndpoint = true;
                    e2.IsLeftEndpoint = false;
                }
                eventQueue.Push(e1);
                eventQueue.Push(e2);
            }
        }
        featureId++;
    }

    private static double[]? TestSegmentIntersect(ActiveSegment seg1, ActiveSegment seg2)
    {
        if (
            seg1.LeftSweepEvent.RingId == seg2.LeftSweepEvent.RingId
            && (
                seg1.RightSweepEvent.IsSamePoint(seg2.LeftSweepEvent)
                || seg1.RightSweepEvent.IsSamePoint(seg2.RightSweepEvent)
                || seg1.LeftSweepEvent.IsSamePoint(seg2.LeftSweepEvent)
                || seg1.LeftSweepEvent.IsSamePoint(seg2.RightSweepEvent)
            )
        )
            return null;

        var x1 = seg1.LeftSweepEvent.X;
        var y1 = seg1.LeftSweepEvent.Y;
        var x2 = seg1.RightSweepEvent.X;
        var y2 = seg1.RightSweepEvent.Y;
        var x3 = seg2.LeftSweepEvent.X;
        var y3 = seg2.LeftSweepEvent.Y;
        var x4 = seg2.RightSweepEvent.X;
        var y4 = seg2.RightSweepEvent.Y;

        var denom = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);
        var numeA = (x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3);
        var numeB = (x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3);

        if (denom == 0)
            return null; // paralelos/colineares: a fonte devolve false nos dois ramos

        var uA = numeA / denom;
        var uB = numeB / denom;

        if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
            return new[] { x1 + uA * (x2 - x1), y1 + uA * (y2 - y1) };
        return null;
    }

    private static List<double[]> RunCheck(BinaryHeap<SweepEvent> eventQueue, bool ignoreSelfIntersections)
    {
        var intersectionPoints = new List<double[]>();
        var outQueue = new BinaryHeap<ActiveSegment>(CheckWhichSegmentHasRightEndpointFirst);

        while (eventQueue.Length > 0)
        {
            var sweepEvent = eventQueue.Pop()!;
            if (sweepEvent.IsLeftEndpoint)
            {
                var segment = new ActiveSegment(sweepEvent);
                // itera o ARRAY interno da heap, como a fonte
                for (var i = 0; i < outQueue.Data.Count; i++)
                {
                    var otherSegment = outQueue.Data[i];
                    if (ignoreSelfIntersections && otherSegment.LeftSweepEvent.FeatureId == sweepEvent.FeatureId)
                        continue;
                    var intersection = TestSegmentIntersect(segment, otherSegment);
                    if (intersection is not null)
                        intersectionPoints.Add(intersection);
                }
                outQueue.Push(segment);
            }
            else
            {
                outQueue.Pop();
            }
        }
        return intersectionPoints;
    }
}
