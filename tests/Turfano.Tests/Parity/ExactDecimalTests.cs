using Turfano.GeoJson.Polyclip;

namespace Turfano.Tests;

// T002 — fundação do porte do polyclip: aritmética idêntica à do bignumber.js.
public class ExactDecimalTests
{
    [Test]
    public async Task PlusMinusTimes_AreExact()
    {
        // 0.1 + 0.2 == 0.3 exato (em decimal; em double seria 0.30000000000000004)
        var sum = ExactDecimal.FromDouble(0.1).Plus(ExactDecimal.FromDouble(0.2));
        await Assert.That(sum.Eq(ExactDecimal.Parse("0.3"))).IsTrue();

        // produto de 17 dígitos é exato (onde o decimal do C# arredondaria)
        var product = ExactDecimal.Parse("1.2345678901234567").Times(ExactDecimal.Parse("9.8765432109876543"));
        await Assert.That(product.Eq(ExactDecimal.Parse("12.19326311370217861743636654061881"))).IsTrue();

        var difference = ExactDecimal.FromDouble(1.5).Minus(ExactDecimal.FromDouble(2.25));
        await Assert.That(difference.Eq(ExactDecimal.Parse("-0.75"))).IsTrue();
    }

    [Test]
    public async Task Div_RoundsTo20DecimalPlaces_HalfUp()
    {
        // 1/3 = 0.33333333333333333333 (trunca — dígito seguinte 3 < 5)
        var oneThird = ExactDecimal.FromInt(1).Div(ExactDecimal.FromInt(3));
        await Assert.That(oneThird.Eq(ExactDecimal.Parse("0.33333333333333333333"))).IsTrue();

        // 2/3 = 0.66666666666666666667 (arredonda — dígito seguinte 6 ≥ 5)
        var twoThirds = ExactDecimal.FromInt(2).Div(ExactDecimal.FromInt(3));
        await Assert.That(twoThirds.Eq(ExactDecimal.Parse("0.66666666666666666667"))).IsTrue();

        // divisão exata não sofre arredondamento
        var half = ExactDecimal.FromInt(1).Div(2);
        await Assert.That(half.Eq(ExactDecimal.Parse("0.5"))).IsTrue();

        // sinal: -2/3 half-up para longe do zero
        var negative = ExactDecimal.FromInt(-2).Div(ExactDecimal.FromInt(3));
        await Assert.That(negative.Eq(ExactDecimal.Parse("-0.66666666666666666667"))).IsTrue();
    }

    [Test]
    public async Task Sqrt_RoundsTo20DecimalPlaces()
    {
        // sqrt(2) = 1.41421356237309504880168... → 20 casas: ...4880 (dígito seguinte 1)
        var rootOfTwo = ExactDecimal.FromInt(2).Sqrt();
        await Assert.That(rootOfTwo.Eq(ExactDecimal.Parse("1.4142135623730950488"))).IsTrue();

        var rootOfNine = ExactDecimal.FromInt(9).Sqrt();
        await Assert.That(rootOfNine.Eq(ExactDecimal.FromInt(3))).IsTrue();

        // valor < 1 (expoente negativo no caminho de escala)
        var rootOfQuarter = ExactDecimal.Parse("0.25").Sqrt();
        await Assert.That(rootOfQuarter.Eq(ExactDecimal.Parse("0.5"))).IsTrue();
    }

    [Test]
    public async Task Compare_AcrossExponents_AndInfinity()
    {
        await Assert.That(ExactDecimal.Parse("0.1").IsLessThan(ExactDecimal.Parse("1E2"))).IsTrue();
        await Assert.That(ExactDecimal.Parse("100").Eq(ExactDecimal.Parse("1E2"))).IsTrue();
        await Assert.That(ExactDecimal.Parse("-5").IsLessThan(ExactDecimal.Parse("0.001"))).IsTrue();

        await Assert.That(ExactDecimal.NegativeInfinity.IsLessThan(ExactDecimal.Parse("-1E30"))).IsTrue();
        await Assert.That(ExactDecimal.PositiveInfinity.IsGreaterThan(ExactDecimal.Parse("1E30"))).IsTrue();
    }

    [Test]
    public async Task FromDouble_ToNumber_RoundTrips()
    {
        double[] samples = { 0.1, -75.343, 39.984, 1e-9, 12345.6789, -0.0000001 };
        foreach (var sample in samples)
            await Assert.That(ExactDecimal.FromDouble(sample).ToNumber()).IsEqualTo(sample);
    }
}
