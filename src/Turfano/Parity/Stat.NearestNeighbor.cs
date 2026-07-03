using System.Text.Json.Nodes;
using Units = Turfano.Units;

namespace Turfano.GeoJson;

public static partial class Geo
{
    /// <summary>
    /// Nearest neighbor analysis — `@turf/nearest-neighbor-analysis`. Returns the study
    /// area (provided or the bbox of the dataset) with the statistics attached under
    /// `properties.nearestNeighborAnalysis` (same fields as @turf: `units`, `arealUnits`,
    /// `observedMeanDistance`, `expectedMeanDistance`, `nearestNeighborIndex`,
    /// `numberOfPoints`, `zScore`). **Note, faithful to @turf**: if <paramref name="studyArea"/>
    /// is provided, its original `properties` are REPLACED (not merged) with
    /// <paramref name="properties"/> + the statistics — this is what the source does
    /// (`studyArea.properties = properties`).
    /// </summary>
    public static Feature NearestNeighborAnalysis(
        FeatureCollection dataset,
        Feature? studyArea = null,
        Units.LengthUnit units = Units.LengthUnit.Kilometers,
        JsonObject? properties = null
    )
    {
        var area =
            studyArea
            ?? new Feature(
                BboxPolygon(
                    Bbox(
                        new GeometryCollection(
                            dataset.Features.Where(f => f.Geometry is not null).Select(f => f.Geometry!).ToArray()
                        )
                    )
                )
            );

        var centroids = dataset.Features.Where(f => f.Geometry is not null).Select(f => Centroid(f.Geometry!)).ToArray();
        var n = centroids.Length;

        double observedSum = 0;
        for (var i = 0; i < n; i++)
        {
            var others = new FeatureCollection(
                centroids.Where((_, index) => index != i).Select(p => new Feature(p)).ToArray()
            );
            var nearest = (Point)NearestPoint(centroids[i], others).Geometry!;
            observedSum += Distance(centroids[i].Coordinates, nearest.Coordinates).As(units);
        }
        var observedMeanDistance = observedSum / n;

        var areaUnit = LengthUnitToAreaUnit(units);
        var populationDensity = n / ConvertArea(Area(area.Geometry!).SquareMeters, Units.AreaUnit.SquareMeters, areaUnit);
        var expectedMeanDistance = 1 / (2 * Math.Sqrt(populationDensity));
        var variance = 0.26136 / Math.Sqrt(n * populationDensity);

        var unitName = units.ToString().ToLowerInvariant();
        var stats = new JsonObject
        {
            ["units"] = unitName,
            ["arealUnits"] = unitName + "²",
            ["observedMeanDistance"] = observedMeanDistance,
            ["expectedMeanDistance"] = expectedMeanDistance,
            ["nearestNeighborIndex"] = observedMeanDistance / expectedMeanDistance,
            ["numberOfPoints"] = n,
            ["zScore"] = (observedMeanDistance - expectedMeanDistance) / variance,
        };

        var finalProperties = properties is null ? new JsonObject() : (JsonObject)properties.DeepClone();
        finalProperties["nearestNeighborAnalysis"] = stats;

        return new Feature(area.Geometry, finalProperties) { Id = area.Id };
    }

    private static Units.AreaUnit LengthUnitToAreaUnit(Units.LengthUnit unit) =>
        unit switch
        {
            Units.LengthUnit.Meters => Units.AreaUnit.SquareMeters,
            Units.LengthUnit.Kilometers => Units.AreaUnit.SquareKilometers,
            Units.LengthUnit.Miles => Units.AreaUnit.SquareMiles,
            Units.LengthUnit.NauticalMiles => Units.AreaUnit.SquareNauticalMiles,
            Units.LengthUnit.Feet => Units.AreaUnit.SquareFeet,
            Units.LengthUnit.Yards => Units.AreaUnit.SquareYards,
            Units.LengthUnit.Inches => Units.AreaUnit.SquareInches,
            Units.LengthUnit.Centimeters => Units.AreaUnit.SquareCentimeters,
            Units.LengthUnit.Millimeters => Units.AreaUnit.SquareMillimeters,
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unidade sem área correspondente"),
        };
}
