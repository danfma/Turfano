using System.Text.Json.Nodes;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// TIN (Delaunay) from points — faithful port of `@turf/tin` (incremental algorithm with
    /// a super-triangle and circumcircles; replaces the legacy's naive "fan"). The `z` value
    /// of each vertex comes from `properties[z]` or, when <paramref name="z"/> is null, from
    /// the 3rd coordinate.
    /// </summary>
    public static FeatureCollection Tin(FeatureCollection points, string? z = null)
    {
        var isPointZ = false;
        var vertices = points
            .Features.Select(feature =>
            {
                var coordinates = ((Point)feature.Geometry!).Coordinates;
                var vertex = new TinVertex(coordinates.Lon, coordinates.Lat);
                if (z is not null)
                {
                    vertex.Z = NumberOrNull(feature.Properties?[z]);
                }
                else if (coordinates.Alt is { } altitude)
                {
                    isPointZ = true;
                    vertex.Z = altitude;
                }
                return vertex;
            })
            .ToList();

        var features = Triangulate(vertices)
            .Select(triangle =>
            {
                JsonObject? properties = null;
                Position a,
                    b,
                    c;
                if (isPointZ)
                {
                    a = new Position(triangle.A.X, triangle.A.Y, triangle.A.Z);
                    b = new Position(triangle.B.X, triangle.B.Y, triangle.B.Z);
                    c = new Position(triangle.C.X, triangle.C.Y, triangle.C.Z);
                }
                else
                {
                    a = new Position(triangle.A.X, triangle.A.Y);
                    b = new Position(triangle.B.X, triangle.B.Y);
                    c = new Position(triangle.C.X, triangle.C.Y);
                    properties = new JsonObject
                    {
                        ["a"] = triangle.A.Z,
                        ["b"] = triangle.B.Z,
                        ["c"] = triangle.C.Z,
                    };
                }
                return new Feature(new Polygon(new[] { new[] { a, b, c, a } }), properties);
            })
            .ToArray();

        return new FeatureCollection(features);
    }

    private sealed class TinVertex(double x, double y)
    {
        public readonly double X = x;
        public readonly double Y = y;
        public double? Z;
        public bool Sentinel;
    }

    private sealed class TinTriangle
    {
        public readonly TinVertex A;
        public readonly TinVertex B;
        public readonly TinVertex C;
        public readonly double X; // circumcentro
        public readonly double Y;
        public readonly double RadiusSquared;

        public TinTriangle(TinVertex a, TinVertex b, TinVertex c)
        {
            A = a;
            B = b;
            C = c;
            var abX = b.X - a.X;
            var abY = b.Y - a.Y;
            var acX = c.X - a.X;
            var acY = c.Y - a.Y;
            var abMid = abX * (a.X + b.X) + abY * (a.Y + b.Y);
            var acMid = acX * (a.X + c.X) + acY * (a.Y + c.Y);
            var determinant = 2 * (abX * (c.Y - b.Y) - abY * (c.X - b.X));
            X = (acY * abMid - abY * acMid) / determinant;
            Y = (abX * acMid - acX * abMid) / determinant;
            var dx = X - a.X;
            var dy = Y - a.Y;
            RadiusSquared = dx * dx + dy * dy;
        }
    }

    /// <summary>Removes pairs of duplicate edges (compared by REFERENCE, as in the source).</summary>
    private static void DedupTinEdges(List<TinVertex> edges)
    {
        var j = edges.Count;
        while (j > 0)
        {
            var b = edges[--j];
            var a = edges[--j];
            var i = j;
            var removed = false;
            while (i > 0)
            {
                var n = edges[--i];
                var m = edges[--i];
                if ((ReferenceEquals(a, m) && ReferenceEquals(b, n)) || (ReferenceEquals(a, n) && ReferenceEquals(b, m)))
                {
                    edges.RemoveRange(j, 2);
                    edges.RemoveRange(i, 2);
                    j -= 2;
                    removed = true;
                    break;
                }
            }
            if (removed)
                continue;
        }
    }

    private static List<TinTriangle> Triangulate(List<TinVertex> vertices)
    {
        if (vertices.Count < 3)
            return new List<TinTriangle>();

        // sort DESCENDENTE por x (byX da fonte); estável como o Array.sort do JS
        vertices = vertices.OrderByDescending(v => v.X).ToList();

        var i = vertices.Count - 1;
        var xmin = vertices[i].X;
        var xmax = vertices[0].X;
        var ymin = vertices[i].Y;
        var ymax = ymin;
        const double epsilon = 1e-12;

        while (i-- > 0)
        {
            if (vertices[i].Y < ymin)
                ymin = vertices[i].Y;
            if (vertices[i].Y > ymax)
                ymax = vertices[i].Y;
        }

        var dx = xmax - xmin;
        var dy = ymax - ymin;
        var dmax = dx > dy ? dx : dy;
        var xmid = (xmax + xmin) * 0.5;
        var ymid = (ymax + ymin) * 0.5;

        var open = new List<TinTriangle>
        {
            new(
                new TinVertex(xmid - 20 * dmax, ymid - dmax) { Sentinel = true },
                new TinVertex(xmid, ymid + 20 * dmax) { Sentinel = true },
                new TinVertex(xmid + 20 * dmax, ymid - dmax) { Sentinel = true }
            ),
        };
        var closed = new List<TinTriangle>();
        var edges = new List<TinVertex>();

        i = vertices.Count;
        while (i-- > 0)
        {
            edges.Clear();
            var j = open.Count;
            while (j-- > 0)
            {
                // à direita do circumcírculo? nunca mais será tocado — fecha
                dx = vertices[i].X - open[j].X;
                if (dx > 0 && dx * dx > open[j].RadiusSquared)
                {
                    closed.Add(open[j]);
                    open.RemoveAt(j);
                    continue;
                }

                // fora do circumcírculo? não mexe
                dy = vertices[i].Y - open[j].Y;
                if (dx * dx + dy * dy > open[j].RadiusSquared)
                    continue;

                // dentro: remove o triângulo e guarda as arestas
                edges.Add(open[j].A);
                edges.Add(open[j].B);
                edges.Add(open[j].B);
                edges.Add(open[j].C);
                edges.Add(open[j].C);
                edges.Add(open[j].A);
                open.RemoveAt(j);
            }

            DedupTinEdges(edges);

            j = edges.Count;
            while (j > 0)
            {
                var b = edges[--j];
                var a = edges[--j];
                var c = vertices[i];
                // colinearidade (área ~0) não vira triângulo
                var abX = b.X - a.X;
                var abY = b.Y - a.Y;
                var determinant = 2 * (abX * (c.Y - b.Y) - abY * (c.X - b.X));
                if (Math.Abs(determinant) > epsilon)
                    open.Add(new TinTriangle(a, b, c));
            }
        }

        closed.AddRange(open);
        i = closed.Count;
        while (i-- > 0)
        {
            if (closed[i].A.Sentinel || closed[i].B.Sentinel || closed[i].C.Sentinel)
                closed.RemoveAt(i);
        }
        return closed;
    }
}
