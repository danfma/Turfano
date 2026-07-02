using System.Text.Json.Nodes;
using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Elipse do desvio padrão (distribuição direcional) — `@turf/standard-deviational-ellipse`.
    /// Ajusta uma elipse em torno do centro médio (<see cref="CenterMean"/>) cujos semieixos
    /// são os desvios padrão (~68% dos dados) ao longo dos eixos principal/secundário da
    /// nuvem de pontos. As propriedades trazem `standardDeviationalEllipse` com
    /// `meanCenterCoordinates`, `semiMajorAxis`, `semiMinorAxis`, `numberOfFeatures`, `angle` e
    /// `percentageWithinEllipse` — mesmos campos do @turf.
    /// </summary>
    public static Feature StandardDeviationalEllipse(
        FeatureCollection points,
        string? weightProperty = null,
        int steps = 64,
        JsonObject? properties = null
    )
    {
        var numberOfFeatures = 0;
        foreach (var feature in points.Features)
            if (feature.Geometry is { } g)
                EachPosition(g, excludeWrapCoord: false, _ => numberOfFeatures++);

        var meanCenter = ((Point)CenterMean(points, weightProperty).Geometry!).Coordinates;

        double xDeviationSquaredSum = 0,
            yDeviationSquaredSum = 0,
            xyDeviationSum = 0;
        foreach (var feature in points.Features)
        {
            var weight = WeightOrOneIfFalsy(feature.Properties, weightProperty);
            var coord = ((Point)feature.Geometry!).Coordinates;
            var dx = coord.Lon - meanCenter.Lon;
            var dy = coord.Lat - meanCenter.Lat;
            xDeviationSquaredSum += Math.Pow(dx, 2) * weight;
            yDeviationSquaredSum += Math.Pow(dy, 2) * weight;
            xyDeviationSum += dx * dy * weight;
        }

        var bigA = xDeviationSquaredSum - yDeviationSquaredSum;
        var bigB = Math.Sqrt(Math.Pow(bigA, 2) + 4 * Math.Pow(xyDeviationSum, 2));
        var bigC = 2 * xyDeviationSum;
        var theta = Math.Atan((bigA + bigB) / bigC);
        var thetaDeg = theta * 180 / Math.PI;

        double sigmaXsum = 0,
            sigmaYsum = 0,
            weightSum = 0;
        foreach (var feature in points.Features)
        {
            var weight = WeightOrOneIfFalsy(feature.Properties, weightProperty);
            var coord = ((Point)feature.Geometry!).Coordinates;
            var dx = coord.Lon - meanCenter.Lon;
            var dy = coord.Lat - meanCenter.Lat;
            sigmaXsum += Math.Pow(dx * Math.Cos(theta) - dy * Math.Sin(theta), 2) * weight;
            sigmaYsum += Math.Pow(dx * Math.Sin(theta) + dy * Math.Cos(theta), 2) * weight;
            weightSum += weight;
        }

        var sigmaX = Math.Sqrt(2 * sigmaXsum / weightSum);
        var sigmaY = Math.Sqrt(2 * sigmaYsum / weightSum);

        var theEllipse = Ellipse(
            new Point(meanCenter),
            new Units.Length(sigmaX, Units.LengthUnit.Degrees),
            new Units.Length(sigmaY, Units.LengthUnit.Degrees),
            Units.Angle.FromDegrees(thetaDeg),
            steps
        );

        var withinEllipse = PointsWithinPolygon(points, theEllipse);
        var coordsWithin = 0;
        foreach (var feature in withinEllipse.Features)
            if (feature.Geometry is { } g)
                EachPosition(g, excludeWrapCoord: false, _ => coordsWithin++);

        var sdeStats = new JsonObject
        {
            ["meanCenterCoordinates"] = new JsonArray(meanCenter.Lon, meanCenter.Lat),
            ["semiMajorAxis"] = sigmaX,
            ["semiMinorAxis"] = sigmaY,
            ["numberOfFeatures"] = numberOfFeatures,
            ["angle"] = thetaDeg,
            ["percentageWithinEllipse"] = 100.0 * coordsWithin / numberOfFeatures,
        };

        var finalProperties = properties is null ? new JsonObject() : (JsonObject)properties.DeepClone();
        finalProperties["standardDeviationalEllipse"] = sdeStats;

        return new Feature(theEllipse, finalProperties);
    }

    /// <summary>
    /// Resolve o peso com a regra `|| 1` do @turf (qualquer valor falsy — ausente, `0`,
    /// `NaN` — cai no default 1), distinta da regra `??` usada em `CenterMean`/`CenterMedian`.
    /// </summary>
    private static double WeightOrOneIfFalsy(JsonObject? properties, string? weightProperty)
    {
        if (string.IsNullOrEmpty(weightProperty))
            return 1;
        var value = NumberOrNull(properties?[weightProperty]);
        return value is null or 0 ? 1 : value.Value;
    }
}
