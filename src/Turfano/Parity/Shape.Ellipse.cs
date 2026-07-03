namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Geodesic ellipse — faithful port of `@turf/ellipse`: the quadrants' angular
    /// parameters are spaced by approximate arc length (the source's incremental
    /// integration), and each vertex is derived via `destination(r(θ), θ)`.
    /// </summary>
    public static Polygon Ellipse(
        Point center,
        Units.Length xSemiAxis,
        Units.Length ySemiAxis,
        Units.Angle? angle = null,
        int steps = 64
    )
    {
        var angleDegrees = angle?.Degrees ?? 0;

        // a fonte rotaciona o centro em torno do pivô (default: o próprio centro → no-op)
        var centerCoords = ((Point)TransformRotate(center, Units.Angle.FromDegrees(angleDegrees), center.Coordinates)).Coordinates;

        var bearingBase = -90 + angleDegrees;
        var quarterSteps = (int)Math.Ceiling(steps / 4.0);

        var a = xSemiAxis.Kilometers;
        var b = ySemiAxis.Kilometers;
        var c = b;
        var m = (a - b) / (Math.PI / 2);
        var arcQuarter = (a + b) * Math.PI / 4;
        const double v = 0.5;
        double k = quarterSteps;

        var quadrantParameters = new List<double>();
        var w = 0.0;
        var x = 0.0;
        for (var i = 0; i < quarterSteps; i++)
        {
            x += w;
            if (m == 0)
                w = arcQuarter / k / c;
            else
                w = (-(m * x + c) + Math.Sqrt(Math.Pow(m * x + c, 2) - 4 * (v * m) * -(arcQuarter / k))) / (2 * (v * m));
            if (x != 0)
                quadrantParameters.Add(x);
        }

        var parameters = new List<double> { 0 };
        parameters.AddRange(quadrantParameters);
        parameters.Add(Math.PI / 2);
        for (var i = 0; i < quadrantParameters.Count; i++)
            parameters.Add(Math.PI - quadrantParameters[quadrantParameters.Count - i - 1]);
        parameters.Add(Math.PI);
        parameters.AddRange(quadrantParameters.Select(q => Math.PI + q));
        parameters.Add(3 * Math.PI / 2);
        for (var i = 0; i < quadrantParameters.Count; i++)
            parameters.Add(2 * Math.PI - quadrantParameters[quadrantParameters.Count - i - 1]);
        parameters.Add(0);

        var coords = new List<Position>();
        foreach (var parameter in parameters)
        {
            var theta = Math.Atan2(b * Math.Sin(parameter), a * Math.Cos(parameter));
            var r = Math.Sqrt(
                Math.Pow(a, 2) * Math.Pow(b, 2)
                    / (Math.Pow(a * Math.Sin(theta), 2) + Math.Pow(b * Math.Cos(theta), 2))
            );
            coords.Add(
                Destination(
                    centerCoords,
                    Units.Length.FromKilometers(r),
                    Units.Angle.FromDegrees(bearingBase + theta / RadiansPerDegree)
                ).Coordinates
            );
        }

        return new Polygon(new[] { coords.ToArray() });
    }
}
