namespace Turfano.GeoJson.Polyclip;

/// <summary>Porte de `src/sweep-line.ts`: o status da varredura (árvore de segmentos).</summary>
internal sealed class SweepLine
{
    private readonly SplayTreeSet<SweepEvent> queue;
    private readonly SplayTreeSet<Segment> tree;
    public readonly List<Segment> Segments = new();

    public SweepLine(SplayTreeSet<SweepEvent> queue)
    {
        this.queue = queue;
        tree = new SplayTreeSet<Segment>(Segment.Compare);
    }

    public List<SweepEvent> Process(SweepEvent sweepEvent)
    {
        var segment = sweepEvent.Segment;
        var newEvents = new List<SweepEvent>();

        if (sweepEvent.ConsumedBy is not null)
        {
            if (sweepEvent.IsLeft)
                queue.Delete(sweepEvent.OtherEvent);
            else
                tree.Delete(segment);
            return newEvents;
        }

        if (sweepEvent.IsLeft)
            tree.Add(segment);

        var prevSegment = segment;
        var nextSegment = segment;
        do
        {
            prevSegment = tree.LastBefore(prevSegment!)!;
        } while (prevSegment is not null && prevSegment.ConsumedBy is not null);
        do
        {
            nextSegment = tree.FirstAfter(nextSegment!)!;
        } while (nextSegment is not null && nextSegment.ConsumedBy is not null);

        if (sweepEvent.IsLeft)
        {
            // interseções com os vizinhos (podem exigir splits)
            SweepPoint? prevMySplitter = null;
            if (prevSegment is not null)
            {
                var prevIntersection = prevSegment.GetIntersection(segment);
                if (prevIntersection is not null)
                {
                    if (!segment.IsAnEndpoint(prevIntersection))
                        prevMySplitter = prevIntersection;
                    if (!prevSegment.IsAnEndpoint(prevIntersection))
                        newEvents.AddRange(SplitSafely(prevSegment, prevIntersection));
                }
            }

            SweepPoint? nextMySplitter = null;
            if (nextSegment is not null)
            {
                var nextIntersection = nextSegment.GetIntersection(segment);
                if (nextIntersection is not null)
                {
                    if (!segment.IsAnEndpoint(nextIntersection))
                        nextMySplitter = nextIntersection;
                    if (!nextSegment.IsAnEndpoint(nextIntersection))
                        newEvents.AddRange(SplitSafely(nextSegment, nextIntersection));
                }
            }

            if (prevMySplitter is not null || nextMySplitter is not null)
            {
                SweepPoint? mySplitter;
                if (prevMySplitter is null)
                    mySplitter = nextMySplitter;
                else if (nextMySplitter is null)
                    mySplitter = prevMySplitter;
                else
                {
                    var splitterComparison = SweepEvent.ComparePoints(prevMySplitter, nextMySplitter);
                    mySplitter = splitterComparison <= 0 ? prevMySplitter : nextMySplitter;
                }

                // o rightSE volta à fila para ser reprocessado depois do split
                queue.Delete(segment.RightEvent);
                newEvents.Add(segment.RightEvent);
                newEvents.AddRange(segment.Split(mySplitter!));
            }

            if (newEvents.Count > 0)
            {
                // sai da árvore e volta à fila como evento novo (a fonte re-enfileira)
                tree.Delete(segment);
                newEvents.Add(sweepEvent);
            }
            else
            {
                Segments.Add(segment);
                segment.Previous = prevSegment;
            }
        }
        else
        {
            // evento direito: os vizinhos ficam adjacentes — checa interseção entre eles
            if (prevSegment is not null && nextSegment is not null)
            {
                var intersection = prevSegment.GetIntersection(nextSegment);
                if (intersection is not null)
                {
                    if (!prevSegment.IsAnEndpoint(intersection))
                        newEvents.AddRange(SplitSafely(prevSegment, intersection));
                    if (!nextSegment.IsAnEndpoint(intersection))
                        newEvents.AddRange(SplitSafely(nextSegment, intersection));
                }
            }

            tree.Delete(segment);
        }

        return newEvents;
    }

    /// <summary>Split seguro de um segmento que está nas estruturas (≠ o em processamento).</summary>
    private List<SweepEvent> SplitSafely(Segment segment, SweepPoint point)
    {
        tree.Delete(segment);
        var rightEvent = segment.RightEvent;
        queue.Delete(rightEvent);
        var newEvents = segment.Split(point);
        newEvents.Add(rightEvent);
        if (segment.ConsumedBy is null)
            tree.Add(segment);
        return newEvents;
    }
}
