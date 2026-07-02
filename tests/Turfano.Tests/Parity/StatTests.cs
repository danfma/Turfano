using System.Text.Json.Nodes;
using G = Turfano.GeoJson.Geo;
using GeoJson = Turfano.GeoJson;
using Pos = Turfano.GeoJson.Position;
using Units = Turfano.Units;

namespace Turfano.Tests;

// US3 (Onda G) — estatística espacial; valores do @turf real (reference/_waveg_us3.mjs).
public class StatTests
{
    private static GeoJson.Feature PointFeature(double lon, double lat, string? key = null, double value = 0) =>
        new(G.Point(lon, lat), key is null ? null : new JsonObject { [key] = value });

    [Test]
    public async Task CenterMean_MatchesTurf()
    {
        // turf.centerMean(fc) = [-97.511094, 35.46526666666667]
        var pts = new GeoJson.FeatureCollection(
            new[]
            {
                PointFeature(-97.522259, 35.4691, "value", 10),
                PointFeature(-97.502754, 35.463455, "value", 3),
                PointFeature(-97.508269, 35.463245, "value", 5),
            }
        );

        var mean = G.CenterMean(pts);
        var coords = ((GeoJson.Point)mean.Geometry!).Coordinates;
        await Assert.That(coords.Lon).IsEqualTo(-97.511094).Within(1e-9);
        await Assert.That(coords.Lat).IsEqualTo(35.46526666666667).Within(1e-9);

        // turf.centerMean(fc, {weight:"value"}) = [-97.51512205555557, 35.46653277777778]
        var weighted = G.CenterMean(pts, "value");
        var wCoords = ((GeoJson.Point)weighted.Geometry!).Coordinates;
        await Assert.That(wCoords.Lon).IsEqualTo(-97.51512205555557).Within(1e-9);
        await Assert.That(wCoords.Lat).IsEqualTo(35.46653277777778).Within(1e-9);
    }

    [Test]
    public async Task CenterMedian_MatchesTurf()
    {
        // turf.centerMedian(turf.points([[0,0],[1,0],[0,1],[5,8]]))
        //   = [0.38387561184491403, 0.616989183081166]; 10 candidatos (counter default = 10, sem convergência)
        var pts = new GeoJson.FeatureCollection(
            new[] { PointFeature(0, 0), PointFeature(1, 0), PointFeature(0, 1), PointFeature(5, 8) }
        );
        var median = G.CenterMedian(pts);
        var coords = ((GeoJson.Point)median.Geometry!).Coordinates;
        await Assert.That(coords.Lon).IsEqualTo(0.38387561184491403).Within(1e-9);
        await Assert.That(coords.Lat).IsEqualTo(0.616989183081166).Within(1e-9);
        await Assert.That(((JsonArray)median.Properties!["medianCandidates"]!).Count).IsEqualTo(10);

        // ponderado (mesmos 3 pontos de CenterMean_MatchesTurf, weight="value")
        //   = [-97.50823139667031, 35.46342025259377]; 4 candidatos (convergiu antes do counter)
        var weightedPts = new GeoJson.FeatureCollection(
            new[]
            {
                PointFeature(-97.522259, 35.4691, "value", 10),
                PointFeature(-97.502754, 35.463455, "value", 3),
                PointFeature(-97.508269, 35.463245, "value", 5),
            }
        );
        var weightedMedian = G.CenterMedian(weightedPts, "value");
        var wCoords = ((GeoJson.Point)weightedMedian.Geometry!).Coordinates;
        await Assert.That(wCoords.Lon).IsEqualTo(-97.50823139667031).Within(1e-9);
        await Assert.That(wCoords.Lat).IsEqualTo(35.46342025259377).Within(1e-9);
        await Assert.That(((JsonArray)weightedMedian.Properties!["medianCandidates"]!).Count).IsEqualTo(4);
    }

    private static GeoJson.FeatureCollection TwoLines() =>
        new(
            new[]
            {
                new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(110, 45), new Pos(120, 50) })),
                new GeoJson.Feature(new GeoJson.LineString(new[] { new Pos(100, 50), new Pos(115, 55) })),
            }
        );

    [Test]
    public async Task DirectionalMean_DefaultIsGeodesic_MatchesTurf()
    {
        // turf.directionalMean(lines) SEM options — o JSDoc alega default planar=true, mas o
        // código-fonte real usa `!!options.planar` (default false = geodésico). GT confirma:
        // {averageLength:1043790.21389692..., bearingAngle:52.67667867265686, ...}
        var dm = G.DirectionalMean(TwoLines());
        await Assert.That(dm.Properties!["averageLength"]!.GetValue<double>()).IsEqualTo(1043790.2138969203).Within(1e-3);
        await Assert.That(dm.Properties!["averageX"]!.GetValue<double>()).IsEqualTo(111.25).Within(1e-9);
        await Assert.That(dm.Properties!["averageY"]!.GetValue<double>()).IsEqualTo(50.0).Within(1e-9);
        await Assert.That(dm.Properties!["bearingAngle"]!.GetValue<double>()).IsEqualTo(52.67667867265686).Within(1e-9);
        await Assert.That(dm.Properties!["cartesianAngle"]!.GetValue<double>()).IsEqualTo(37.32332132734314).Within(1e-9);
        await Assert.That(dm.Properties!["circularVariance"]!.GetValue<double>()).IsEqualTo(0.0011916630377352133).Within(1e-9);
        await Assert.That(dm.Properties!["countOfLines"]!.GetValue<int>()).IsEqualTo(2);

        var coords = ((GeoJson.LineString)dm.Geometry!).Coordinates;
        await Assert.That(coords[0].Lon).IsEqualTo(105.77303918492785).Within(1e-6);
        await Assert.That(coords[0].Lat).IsEqualTo(47.01949858569476).Within(1e-6);
        await Assert.That(coords[1].Lon).IsEqualTo(117.41275211539057).Within(1e-6);
        await Assert.That(coords[1].Lat).IsEqualTo(52.68979385473219).Within(1e-6);

        // segment=true, sem quebra de linha (1 segmento por linha) → idêntico ao default
        var dmSegment = G.DirectionalMean(TwoLines(), segment: true);
        await Assert.That(dmSegment.Properties!["bearingAngle"]!.GetValue<double>()).IsEqualTo(52.67667867265686).Within(1e-9);
    }

    [Test]
    public async Task DirectionalMean_Planar_MatchesTurf()
    {
        // turf.directionalMean(lines, {planar:true})
        var dm = G.DirectionalMean(TwoLines(), planar: true);
        await Assert.That(dm.Properties!["averageLength"]!.GetValue<double>()).IsEqualTo(13.495864094170422).Within(1e-9);
        await Assert.That(dm.Properties!["bearingAngle"]!.GetValue<double>()).IsEqualTo(67.5).Within(1e-9);
        await Assert.That(dm.Properties!["cartesianAngle"]!.GetValue<double>()).IsEqualTo(22.5).Within(1e-9);
        await Assert.That(dm.Properties!["circularVariance"]!.GetValue<double>()).IsEqualTo(0.0025157911873574523).Within(1e-9);

        var coords = ((GeoJson.LineString)dm.Geometry!).Coordinates;
        await Assert.That(coords[0].Lon).IsEqualTo(105.01572369492098).Within(1e-9);
        await Assert.That(coords[1].Lat).IsEqualTo(52.58232179714496).Within(1e-9);
    }

    [Test]
    public async Task DirectionalMean_PlanarSegment_MatchesTurf()
    {
        // turf.directionalMean(linesMulti, {planar:true, segment:true}) — 2 linhas de 2
        // segmentos cada = 4 segmentos unitários formando um "V"; média = diagonal 45°.
        var linesMulti = new GeoJson.FeatureCollection(
            new[]
            {
                new GeoJson.Feature(
                    new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(1, 0), new Pos(1, 1) })
                ),
                new GeoJson.Feature(
                    new GeoJson.LineString(new[] { new Pos(0, 0), new Pos(0, 1), new Pos(1, 1) })
                ),
            }
        );
        var dm = G.DirectionalMean(linesMulti, planar: true, segment: true);
        await Assert.That(dm.Properties!["averageLength"]!.GetValue<double>()).IsEqualTo(1.0).Within(1e-9);
        await Assert.That(dm.Properties!["averageX"]!.GetValue<double>()).IsEqualTo(0.5).Within(1e-9);
        await Assert.That(dm.Properties!["averageY"]!.GetValue<double>()).IsEqualTo(0.5).Within(1e-9);
        await Assert.That(dm.Properties!["bearingAngle"]!.GetValue<double>()).IsEqualTo(45.0).Within(1e-9);
        await Assert.That(dm.Properties!["cartesianAngle"]!.GetValue<double>()).IsEqualTo(45.0).Within(1e-9);
        await Assert.That(dm.Properties!["circularVariance"]!.GetValue<double>()).IsEqualTo(0.2928932188134524).Within(1e-9);
        await Assert.That(dm.Properties!["countOfLines"]!.GetValue<int>()).IsEqualTo(4);

        var coords = ((GeoJson.LineString)dm.Geometry!).Coordinates;
        await Assert.That(coords[0].Lon).IsEqualTo(0.1464466094067262).Within(1e-9);
        await Assert.That(coords[1].Lon).IsEqualTo(0.8535533905932737).Within(1e-9);
    }

    private static GeoJson.FeatureCollection FourPoints() =>
        new(
            new[] { PointFeature(0, 0), PointFeature(1, 0), PointFeature(0, 1), PointFeature(5, 5) }
        );

    [Test]
    public async Task DistanceWeight_Default_MatchesTurf()
    {
        // turf.distanceWeight(fc) — p=2 (euclidiano), alpha=-1, threshold=10000, binary=false
        var w = G.DistanceWeight(FourPoints());
        await Assert.That(w[0][1]).IsEqualTo(1.0).Within(1e-12);
        await Assert.That(w[1][2]).IsEqualTo(0.7071067811865475).Within(1e-12);
        await Assert.That(w[0][3]).IsEqualTo(0.1414213562373095).Within(1e-12);
        await Assert.That(w[1][3]).IsEqualTo(0.15617376188860607).Within(1e-12);
        await Assert.That(w[0][0]).IsEqualTo(0.0);
    }

    [Test]
    public async Task DistanceWeight_Binary_MatchesTurf()
    {
        // turf.distanceWeight(fc, {threshold:1, binary:true})
        var w = G.DistanceWeight(FourPoints(), threshold: 1, binary: true);
        await Assert.That(w[0][1]).IsEqualTo(1.0);
        await Assert.That(w[0][2]).IsEqualTo(1.0);
        await Assert.That(w[0][3]).IsEqualTo(0.0);
        await Assert.That(w[1][2]).IsEqualTo(0.0);
    }

    [Test]
    public async Task DistanceWeight_ManhattanAndAlpha_MatchTurf()
    {
        // turf.distanceWeight(fc, {p:1})
        var manhattan = G.DistanceWeight(FourPoints(), p: 1);
        await Assert.That(manhattan[0][3]).IsEqualTo(0.1).Within(1e-12);
        await Assert.That(manhattan[1][2]).IsEqualTo(0.5).Within(1e-12);

        // turf.distanceWeight(fc, {alpha:-2})
        var alpha2 = G.DistanceWeight(FourPoints(), alpha: -2);
        await Assert.That(alpha2[0][3]).IsEqualTo(0.019999999999999997).Within(1e-12);
        await Assert.That(alpha2[1][2]).IsEqualTo(0.49999999999999994).Within(1e-12);
    }

    [Test]
    public async Task DistanceWeight_Standardization_MatchesTurf()
    {
        // turf.distanceWeight(fc, {standardization:true}) — linhas somam 1 (exceto zeros)
        var w = G.DistanceWeight(FourPoints(), standardization: true);
        await Assert.That(w[0][1]).IsEqualTo(0.4669795587343443).Within(1e-9);
        await Assert.That(w[1][0]).IsEqualTo(0.5366878346454488).Within(1e-9);
        await Assert.That(w[3][0]).IsEqualTo(0.311659442650152).Within(1e-9);
    }

    [Test]
    public async Task MoranIndex_MatchesTurf()
    {
        // turf.moranIndex(fc, {inputField:"value"}) — default standardization=true, threshold=1e5
        var pts = new GeoJson.FeatureCollection(
            new[]
            {
                PointFeature(0, 0, "value", 10),
                PointFeature(1, 0, "value", 20),
                PointFeature(0, 1, "value", 30),
                PointFeature(5, 5, "value", 5),
            }
        );
        var result = G.MoranIndex(pts, "value");
        await Assert.That(result.MoranIndex).IsEqualTo(-0.3480501451592129).Within(1e-9);
        await Assert.That(result.ExpectedMoranIndex).IsEqualTo(-0.3333333333333333).Within(1e-9);
        await Assert.That(result.StdNorm).IsEqualTo(0.12411036310588155).Within(1e-9);
        await Assert.That(result.ZNorm).IsEqualTo(-0.11857842856622952).Within(1e-9);

        // standardization:false
        var noStd = G.MoranIndex(pts, "value", standardization: false);
        await Assert.That(noStd.MoranIndex).IsEqualTo(-0.32161978663365665).Within(1e-9);
        await Assert.That(noStd.StdNorm).IsEqualTo(0.2234242222035502).Within(1e-9);
        await Assert.That(noStd.ZNorm).IsEqualTo(0.052427380452084844).Within(1e-9);
    }

    private static GeoJson.FeatureCollection FiveScatteredPoints() =>
        new(
            new[]
            {
                PointFeature(-65.5, 40.2),
                PointFeature(-64.8, 40.9),
                PointFeature(-63.7, 41.5),
                PointFeature(-64.1, 40.5),
                PointFeature(-63.9, 41.1),
            }
        );

    [Test]
    public async Task NearestNeighborAnalysis_MatchesTurf()
    {
        // turf.nearestNeighborAnalysis(dataset) — units default 'kilometers'
        var result = G.NearestNeighborAnalysis(FiveScatteredPoints());
        var stats = result.Properties!["nearestNeighborAnalysis"]!;
        await Assert.That(stats["units"]!.GetValue<string>()).IsEqualTo("kilometers");
        await Assert.That(stats["arealUnits"]!.GetValue<string>()).IsEqualTo("kilometers²");
        await Assert.That(stats["observedMeanDistance"]!.GetValue<double>()).IsEqualTo(67.09686829160036).Within(1e-6);
        await Assert.That(stats["expectedMeanDistance"]!.GetValue<double>()).IsEqualTo(33.079326103733656).Within(1e-6);
        await Assert.That(stats["nearestNeighborIndex"]!.GetValue<double>()).IsEqualTo(2.028362611777244).Within(1e-6);
        await Assert.That(stats["numberOfPoints"]!.GetValue<int>()).IsEqualTo(5);
        await Assert.That(stats["zScore"]!.GetValue<double>()).IsEqualTo(4.399083075935764).Within(1e-6);

        // studyArea default = bbox do dataset: [-65.5, 40.2, -63.7, 41.5]
        var bbox = G.Bbox(result.Geometry!).Values;
        await Assert.That(bbox[0]).IsEqualTo(-65.5).Within(1e-9);
        await Assert.That(bbox[1]).IsEqualTo(40.2).Within(1e-9);
        await Assert.That(bbox[2]).IsEqualTo(-63.7).Within(1e-9);
        await Assert.That(bbox[3]).IsEqualTo(41.5).Within(1e-9);

        // units:'meters' — mesma proporção, 1000x maiores nas distâncias
        var meters = G.NearestNeighborAnalysis(FiveScatteredPoints(), units: Units.LengthUnit.Meters);
        var metersStats = meters.Properties!["nearestNeighborAnalysis"]!;
        await Assert.That(metersStats["units"]!.GetValue<string>()).IsEqualTo("meters");
        await Assert.That(metersStats["observedMeanDistance"]!.GetValue<double>()).IsEqualTo(67096.86829160036).Within(1e-3);
        await Assert.That(metersStats["nearestNeighborIndex"]!.GetValue<double>()).IsEqualTo(2.028362611777244).Within(1e-6);
    }

    private static GeoJson.FeatureCollection EightGridPoints() =>
        new(
            new[]
            {
                PointFeature(-65.3, 40.2),
                PointFeature(-64.9, 40.3),
                PointFeature(-64.7, 40.6),
                PointFeature(-64.2, 40.8),
                PointFeature(-63.9, 41.0),
                PointFeature(-63.7, 41.3),
                PointFeature(-64.5, 40.1),
                PointFeature(-64.0, 40.4),
            }
        );

    [Test]
    public async Task QuadratAnalysis_MatchesTurf()
    {
        // turf.quadratAnalysis(dataset) — confidenceLevel default 20
        var result = G.QuadratAnalysis(EightGridPoints());
        await Assert.That(result.CriticalValue).IsEqualTo(0.7585487995178689).Within(1e-9);
        await Assert.That(result.IsRandom).IsTrue();
        await Assert.That(result.MaxAbsoluteDifference).IsEqualTo(0.1766764161830635).Within(1e-9);
        await Assert.That(result.ObservedDistribution).IsEquivalentTo(new[] { 0.0, 0.5, 0.5, 1.0 });

        // confidenceLevel:5 — mesma distribuição observada, criticalValue maior
        var cl5 = G.QuadratAnalysis(EightGridPoints(), confidenceLevel: 5);
        await Assert.That(cl5.CriticalValue).IsEqualTo(0.9603217195294502).Within(1e-9);
        await Assert.That(cl5.MaxAbsoluteDifference).IsEqualTo(0.1766764161830635).Within(1e-9);
    }

    private static GeoJson.FeatureCollection SdEllipsePoints() =>
        new(
            new[]
            {
                PointFeature(-73.99, 40.72, "weight", 2),
                PointFeature(-73.98, 40.73, "weight", 1),
                PointFeature(-73.97, 40.71, "weight", 3),
                PointFeature(-73.96, 40.74, "weight", 1),
                PointFeature(-73.95, 40.7, "weight", 2),
            }
        );

    [Test]
    public async Task StandardDeviationalEllipse_MatchesTurf()
    {
        // turf.standardDeviationalEllipse(points, {steps:8})
        var ellipse = G.StandardDeviationalEllipse(SdEllipsePoints(), steps: 8);
        var stats = ellipse.Properties!["standardDeviationalEllipse"]!;
        var meanCenter = (JsonArray)stats["meanCenterCoordinates"]!;
        await Assert.That(meanCenter[0]!.GetValue<double>()).IsEqualTo(-73.97).Within(1e-9);
        await Assert.That(meanCenter[1]!.GetValue<double>()).IsEqualTo(40.720000000000006).Within(1e-9);
        await Assert.That(stats["semiMajorAxis"]!.GetValue<double>()).IsEqualTo(0.01673320053068328).Within(1e-9);
        await Assert.That(stats["semiMinorAxis"]!.GetValue<double>()).IsEqualTo(0.02280350850197947).Within(1e-9);
        await Assert.That(stats["numberOfFeatures"]!.GetValue<int>()).IsEqualTo(5);
        await Assert.That(stats["angle"]!.GetValue<double>()).IsEqualTo(-45.0).Within(1e-9);
        await Assert.That(stats["percentageWithinEllipse"]!.GetValue<double>()).IsEqualTo(60.0).Within(1e-9);

        var ring = ((GeoJson.Polygon)ellipse.Geometry!).Coordinates[0];
        await Assert.That(ring.Length).IsEqualTo(9);
        await Assert.That(ring[0].Lon).IsEqualTo(-73.98560885705557).Within(1e-9);
        await Assert.That(ring[0].Lat).IsEqualTo(40.70816678910859).Within(1e-9);

        // weight:"weight" — mesmo centro médio ponderado (distinto do centro não ponderado)
        var weighted = G.StandardDeviationalEllipse(SdEllipsePoints(), weightProperty: "weight", steps: 8);
        var wStats = weighted.Properties!["standardDeviationalEllipse"]!;
        var wMeanCenter = (JsonArray)wStats["meanCenterCoordinates"]!;
        await Assert.That(wMeanCenter[0]!.GetValue<double>()).IsEqualTo(-73.97).Within(1e-9);
        await Assert.That(wMeanCenter[1]!.GetValue<double>()).IsEqualTo(40.71555555555555).Within(1e-9);
        await Assert.That(wStats["semiMajorAxis"]!.GetValue<double>()).IsEqualTo(0.0140322949906796).Within(1e-9);
        await Assert.That(wStats["semiMinorAxis"]!.GetValue<double>()).IsEqualTo(0.02278473348561385).Within(1e-9);
        await Assert.That(wStats["angle"]!.GetValue<double>()).IsEqualTo(-52.55054908069298).Within(1e-9);
        await Assert.That(wStats["percentageWithinEllipse"]!.GetValue<double>()).IsEqualTo(60.0).Within(1e-9);
    }
}
