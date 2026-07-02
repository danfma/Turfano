namespace Turfano.GeoJson.Polyclip;

/// <summary>Porte de `src/geom-in.ts`: anel de entrada normalizado para o sweep.</summary>
internal sealed class RingIn
{
    public readonly PolyIn Poly;
    public readonly bool IsExterior;
    public readonly List<Segment> Segments = new();
    public readonly ExactBounds Bounds;

    public RingIn(Position[] geometryRing, PolyIn poly, bool isExterior)
    {
        if (geometryRing.Length == 0)
            throw new ArgumentException("Input geometry is not a valid Polygon or MultiPolygon");

        Poly = poly;
        IsExterior = isExterior;

        var firstPoint = PolyclipPrecision.Exact.Snap(
            new SweepPoint(ExactDecimal.FromDouble(geometryRing[0].Lon), ExactDecimal.FromDouble(geometryRing[0].Lat))
        );
        Bounds = new ExactBounds
        {
            LowerX = firstPoint.X,
            LowerY = firstPoint.Y,
            UpperX = firstPoint.X,
            UpperY = firstPoint.Y,
        };

        var prevPoint = firstPoint;
        for (int i = 1, iMax = geometryRing.Length; i < iMax; i++)
        {
            var point = PolyclipPrecision.Exact.Snap(
                new SweepPoint(ExactDecimal.FromDouble(geometryRing[i].Lon), ExactDecimal.FromDouble(geometryRing[i].Lat))
            );
            // pula pontos consecutivos idênticos
            if (point.X.Eq(prevPoint.X) && point.Y.Eq(prevPoint.Y))
                continue;
            Segments.Add(Segment.FromRing(prevPoint, point, this));
            if (point.X.IsLessThan(Bounds.LowerX))
                Bounds.LowerX = point.X;
            if (point.Y.IsLessThan(Bounds.LowerY))
                Bounds.LowerY = point.Y;
            if (point.X.IsGreaterThan(Bounds.UpperX))
                Bounds.UpperX = point.X;
            if (point.Y.IsGreaterThan(Bounds.UpperY))
                Bounds.UpperY = point.Y;
            prevPoint = point;
        }

        // fecha o anel implicitamente, se preciso
        if (!firstPoint.X.Eq(prevPoint.X) || !firstPoint.Y.Eq(prevPoint.Y))
            Segments.Add(Segment.FromRing(prevPoint, firstPoint, this));
    }

    public List<SweepEvent> GetSweepEvents()
    {
        var sweepEvents = new List<SweepEvent>();
        for (int i = 0, iMax = Segments.Count; i < iMax; i++)
        {
            var segment = Segments[i];
            sweepEvents.Add(segment.LeftEvent);
            sweepEvents.Add(segment.RightEvent);
        }
        return sweepEvents;
    }
}

/// <summary>Porte de `src/geom-in.ts`: polígono de entrada (exterior + furos).</summary>
internal sealed class PolyIn
{
    public readonly MultiPolyIn MultiPoly;
    public readonly RingIn ExteriorRing;
    public readonly List<RingIn> InteriorRings = new();
    public readonly ExactBounds Bounds;

    public PolyIn(Position[][] geometryPolygon, MultiPolyIn multiPoly)
    {
        MultiPoly = multiPoly;
        ExteriorRing = new RingIn(geometryPolygon[0], this, isExterior: true);
        Bounds = new ExactBounds
        {
            LowerX = ExteriorRing.Bounds.LowerX,
            LowerY = ExteriorRing.Bounds.LowerY,
            UpperX = ExteriorRing.Bounds.UpperX,
            UpperY = ExteriorRing.Bounds.UpperY,
        };

        for (int i = 1, iMax = geometryPolygon.Length; i < iMax; i++)
        {
            var ring = new RingIn(geometryPolygon[i], this, isExterior: false);
            if (ring.Bounds.LowerX.IsLessThan(Bounds.LowerX))
                Bounds.LowerX = ring.Bounds.LowerX;
            if (ring.Bounds.LowerY.IsLessThan(Bounds.LowerY))
                Bounds.LowerY = ring.Bounds.LowerY;
            if (ring.Bounds.UpperX.IsGreaterThan(Bounds.UpperX))
                Bounds.UpperX = ring.Bounds.UpperX;
            if (ring.Bounds.UpperY.IsGreaterThan(Bounds.UpperY))
                Bounds.UpperY = ring.Bounds.UpperY;
            InteriorRings.Add(ring);
        }
    }

    public List<SweepEvent> GetSweepEvents()
    {
        var sweepEvents = ExteriorRing.GetSweepEvents();
        for (int i = 0, iMax = InteriorRings.Count; i < iMax; i++)
            sweepEvents.AddRange(InteriorRings[i].GetSweepEvents());
        return sweepEvents;
    }
}

/// <summary>Porte de `src/geom-in.ts`: multipolígono de entrada (a entrada do Geo já chega
/// normalizada como `Position[][][]`, então o auto-embrulho do JS não é necessário).</summary>
internal sealed class MultiPolyIn
{
    public readonly bool IsSubject;
    public readonly List<PolyIn> Polys = new();
    public readonly ExactBounds Bounds;
    public readonly OperationRun Operation;

    public MultiPolyIn(Position[][][] geometry, bool isSubject, OperationRun operation)
    {
        Operation = operation;
        Bounds = new ExactBounds
        {
            LowerX = ExactDecimal.PositiveInfinity,
            LowerY = ExactDecimal.PositiveInfinity,
            UpperX = ExactDecimal.NegativeInfinity,
            UpperY = ExactDecimal.NegativeInfinity,
        };

        for (int i = 0, iMax = geometry.Length; i < iMax; i++)
        {
            var poly = new PolyIn(geometry[i], this);
            if (poly.Bounds.LowerX.IsLessThan(Bounds.LowerX))
                Bounds.LowerX = poly.Bounds.LowerX;
            if (poly.Bounds.LowerY.IsLessThan(Bounds.LowerY))
                Bounds.LowerY = poly.Bounds.LowerY;
            if (poly.Bounds.UpperX.IsGreaterThan(Bounds.UpperX))
                Bounds.UpperX = poly.Bounds.UpperX;
            if (poly.Bounds.UpperY.IsGreaterThan(Bounds.UpperY))
                Bounds.UpperY = poly.Bounds.UpperY;
            Polys.Add(poly);
        }

        IsSubject = isSubject;
    }

    public List<SweepEvent> GetSweepEvents()
    {
        var sweepEvents = new List<SweepEvent>();
        for (int i = 0, iMax = Polys.Count; i < iMax; i++)
            sweepEvents.AddRange(Polys[i].GetSweepEvents());
        return sweepEvents;
    }
}
