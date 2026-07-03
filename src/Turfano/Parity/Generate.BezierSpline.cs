namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Smooth spline curve passing through the points of a line — `@turf/bezier-spline` (port of
    /// the library's `Spline` class: cubic basis + time-based sampling). `resolution`/`sharpness`
    /// follow the `@turf` defaults (10000 / 0.85).
    /// </summary>
    public static LineString BezierSpline(LineString line, double resolution = 10000, double sharpness = 0.85)
    {
        var points = line.Coordinates.Select(p => new SplinePoint(p.Lon, p.Lat, 0)).ToArray();
        var spline = new BezierState(points, resolution, sharpness);
        var coords = new List<Position>();

        void PushCoord(double time)
        {
            var pos = spline.Pos(time);
            if ((long)Math.Floor(time / 100) % 2 == 0)
                coords.Add(new Position(pos.X, pos.Y));
        }

        for (double i = 0; i < resolution; i += 10)
            PushCoord(i);
        PushCoord(resolution);

        return new LineString(coords.ToArray());
    }

    private readonly record struct SplinePoint(double X, double Y, double Z);

    private sealed class BezierState
    {
        private readonly SplinePoint[] points;
        private readonly (SplinePoint A, SplinePoint B)[] controls;
        private readonly double duration;
        private readonly int length;

        public BezierState(SplinePoint[] pts, double duration, double sharpness)
        {
            points = pts;
            this.duration = duration;
            length = pts.Length;

            var centers = new SplinePoint[length - 1];
            for (var i = 0; i < length - 1; i++)
                centers[i] = new SplinePoint(
                    (pts[i].X + pts[i + 1].X) / 2,
                    (pts[i].Y + pts[i + 1].Y) / 2,
                    (pts[i].Z + pts[i + 1].Z) / 2
                );

            var ctrl = new List<(SplinePoint, SplinePoint)> { (pts[0], pts[0]) };
            for (var i = 0; i < centers.Length - 1; i++)
            {
                var dx = pts[i + 1].X - (centers[i].X + centers[i + 1].X) / 2;
                var dy = pts[i + 1].Y - (centers[i].Y + centers[i + 1].Y) / 2;
                var dz = pts[i + 1].Z - (centers[i].Y + centers[i + 1].Z) / 2; // replica o @turf
                ctrl.Add(
                    (
                        new SplinePoint(
                            (1 - sharpness) * pts[i + 1].X + sharpness * (centers[i].X + dx),
                            (1 - sharpness) * pts[i + 1].Y + sharpness * (centers[i].Y + dy),
                            (1 - sharpness) * pts[i + 1].Z + sharpness * (centers[i].Z + dz)
                        ),
                        new SplinePoint(
                            (1 - sharpness) * pts[i + 1].X + sharpness * (centers[i + 1].X + dx),
                            (1 - sharpness) * pts[i + 1].Y + sharpness * (centers[i + 1].Y + dy),
                            (1 - sharpness) * pts[i + 1].Z + sharpness * (centers[i + 1].Z + dz)
                        )
                    )
                );
            }
            ctrl.Add((pts[length - 1], pts[length - 1]));
            controls = ctrl.ToArray();
        }

        public SplinePoint Pos(double time)
        {
            var t = time;
            if (t < 0)
                t = 0;
            if (t > duration)
                t = duration - 1;
            var t2 = t / duration;
            if (t2 >= 1)
                return points[length - 1];
            var n = (int)Math.Floor((length - 1) * t2);
            var t1 = (length - 1) * t2 - n;
            return Bezier(t1, points[n], controls[n].B, controls[n + 1].A, points[n + 1]);
        }

        private static SplinePoint Bezier(double t, SplinePoint p1, SplinePoint c1, SplinePoint c2, SplinePoint p2)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            var b0 = t3;
            var b1 = 3 * t2 * (1 - t);
            var b2 = 3 * t * (1 - t) * (1 - t);
            var b3 = (1 - t) * (1 - t) * (1 - t);
            return new SplinePoint(
                p2.X * b0 + c2.X * b1 + c1.X * b2 + p1.X * b3,
                p2.Y * b0 + c2.Y * b1 + c1.Y * b2 + p1.Y * b3,
                p2.Z * b0 + c2.Z * b1 + c1.Z * b2 + p1.Z * b3
            );
        }
    }
}
