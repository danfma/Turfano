namespace Turfano.GeoJson.Polyclip;

internal enum PolyclipOperationType
{
    Union,
    Intersection,
    Xor,
    Difference,
}

/// <summary>
/// Porte de `src/operation.ts` do polyclip-ts. Adaptação registrada no plano: o singleton
/// global mutável `operation` vira UMA INSTÂNCIA por execução (thread-safe; os ids de
/// segmento por run são equivalentes — só desempatam o `Segment.Compare` dentro do run).
/// </summary>
internal sealed class OperationRun
{
    public PolyclipOperationType Type { get; private set; }
    public int NumMultiPolys { get; private set; }

    private int segmentCounter;

    public int NextSegmentId() => ++segmentCounter;

    /// <summary>Executa a boolean op sobre multipolígonos (`Position[][][]`).</summary>
    public Position[][][] Run(
        PolyclipOperationType type,
        Position[][][] geometry,
        IReadOnlyList<Position[][][]> moreGeometries
    )
    {
        Type = type;
        var multipolys = new List<MultiPolyIn> { new(geometry, isSubject: true, this) };
        for (int i = 0, iMax = moreGeometries.Count; i < iMax; i++)
            multipolys.Add(new MultiPolyIn(moreGeometries[i], isSubject: false, this));
        NumMultiPolys = multipolys.Count;

        // otimização da fonte p/ difference: descarta clippers sem overlap de bbox
        if (Type == PolyclipOperationType.Difference)
        {
            var subject = multipolys[0];
            var i = 1;
            while (i < multipolys.Count)
            {
                if (ExactBounds.Overlap(multipolys[i].Bounds, subject.Bounds) is not null)
                    i++;
                else
                    multipolys.RemoveAt(i);
            }
        }

        // otimização da fonte p/ intersection: sem overlap entre algum par → vazio
        if (Type == PolyclipOperationType.Intersection)
        {
            for (int i = 0, iMax = multipolys.Count; i < iMax; i++)
            {
                var multiPolyA = multipolys[i];
                for (int j = i + 1, jMax = multipolys.Count; j < jMax; j++)
                {
                    if (ExactBounds.Overlap(multiPolyA.Bounds, multipolys[j].Bounds) is null)
                        return Array.Empty<Position[][]>();
                }
            }
        }

        // enfileira todos os sweep events
        var queue = new SplayTreeSet<SweepEvent>(SweepEvent.Compare);
        for (int i = 0, iMax = multipolys.Count; i < iMax; i++)
        {
            var sweepEvents = multipolys[i].GetSweepEvents();
            for (int j = 0, jMax = sweepEvents.Count; j < jMax; j++)
                queue.Add(sweepEvents[j]);
        }

        // varredura
        var sweepLine = new SweepLine(queue);
        SweepEvent? sweepEvent = null;
        if (queue.Count != 0)
        {
            sweepEvent = queue.First();
            queue.Delete(sweepEvent);
        }
        while (sweepEvent is not null)
        {
            var newEvents = sweepLine.Process(sweepEvent);
            for (int i = 0, iMax = newEvents.Count; i < iMax; i++)
            {
                var newEvent = newEvents[i];
                if (newEvent.ConsumedBy is null)
                    queue.Add(newEvent);
            }
            if (queue.Count != 0)
            {
                sweepEvent = queue.First();
                queue.Delete(sweepEvent);
            }
            else
            {
                sweepEvent = null;
            }
        }

        // (precision.reset() da fonte é no-op no modo exato)
        var ringsOut = RingOut.Factory(sweepLine.Segments);
        var result = new MultiPolyOut(ringsOut);
        return result.GetGeometry();
    }
}
