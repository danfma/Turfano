namespace Turfano;

public static partial class Turf
{
    private static readonly ICoordinateFilter Wgs84ToMercatorConverter =
        new Wgs84ToMercatorConverterFilter();

    private static readonly ICoordinateFilter MercatorToWgs84Converter =
        new MercatorToWgs84ConverterFilter();

    private static TGeometry ApplyConverter<TGeometry>(
        TGeometry geometry,
        ICoordinateFilter converter,
        bool mutate
    )
        where TGeometry : Geometry
    {
        if (geometry.IsEmpty)
        {
            return geometry;
        }

        var target = mutate ? geometry : (TGeometry)geometry.Copy();

        target.Apply(converter);

        return target;
    }

    public static TGeometry ToMercator<TGeometry>(TGeometry geometry, bool mutate = false)
        where TGeometry : Geometry
    {
        return ApplyConverter(geometry, Wgs84ToMercatorConverter, mutate);
    }

    public static TGeometry ToWgs84<TGeometry>(TGeometry geometry, bool mutate = false)
        where TGeometry : Geometry
    {
        return ApplyConverter(geometry, MercatorToWgs84Converter, mutate);
    }

    private sealed class Wgs84ToMercatorConverterFilter : ICoordinateFilter
    {
        private const double RadiansPerDegree = Math.PI / 180.0;
        private const double A = 6378137.0;
        private const double MaxExtent = 20037508.342789244;

        public void Filter(Coordinate coord)
        {
            // NOTE: compensate longitudes passing the 180th meridian
            // from https://github.com/proj4js/proj4js/blob/master/lib/common/adjust_lon.js

            var adjusted =
                Math.Abs(coord.X) <= 180 ? coord.X : coord.X - Math.Sign(coord.X) * 360.0;

            var x = A * adjusted * RadiansPerDegree;
            var y = A * Math.Log(Math.Tan(Math.PI * 0.25 + 0.5 * coord.Y * RadiansPerDegree));

            // if xy value is beyond maxextent (e.g. poles), return maxextent
            if (x > MaxExtent)
                x = MaxExtent;

            if (x < -MaxExtent)
                x = -MaxExtent;

            if (y > MaxExtent)
                y = MaxExtent;

            if (y < -MaxExtent)
                y = -MaxExtent;

            coord.X = x;
            coord.Y = y;
        }
    }

    private sealed class MercatorToWgs84ConverterFilter : ICoordinateFilter
    {
        private const double A = 6378137.0;
        private const double DegreesPerRadian = 180.0 / Math.PI;

        public void Filter(Coordinate coord)
        {
            coord.X = coord.X * DegreesPerRadian / A;
            coord.Y = (Math.PI * 0.5 - 2.0 * Math.Atan(Math.Exp(-coord.Y / A))) * DegreesPerRadian;
        }
    }
}
