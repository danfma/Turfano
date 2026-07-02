using System.Globalization;
using System.Numerics;

namespace Turfano.GeoJson.Polyclip;

/// <summary>
/// Decimal de precisão arbitrária que substitui o `bignumber.js` no porte do polyclip-ts:
/// mantissa <see cref="BigInteger"/> × 10^expoente, com sentinelas de ±infinito (usadas só
/// em comparações de bbox). `Plus`/`Minus`/`Times` são EXATAS; `Div`/`Sqrt` reproduzem os
/// defaults do bignumber.js (resultado arredondado a 20 casas decimais, half-up — ties
/// para longe do zero). Os nomes espelham a API do bignumber.js de propósito, para o porte
/// ser revisável linha a linha contra a fonte.
/// </summary>
internal readonly struct ExactDecimal : IComparable<ExactDecimal>, IEquatable<ExactDecimal>
{
    private const int DivisionDecimalPlaces = 20; // bignumber.js default DECIMAL_PLACES
    private const int SqrtGuardDigits = 5;

    private enum Kind : byte
    {
        Finite = 0,
        PositiveInfinity = 1,
        NegativeInfinity = 2,
    }

    private readonly BigInteger mantissa;
    private readonly int exponent;
    private readonly Kind kind;

    public static readonly ExactDecimal Zero = new(BigInteger.Zero, 0);
    public static readonly ExactDecimal PositiveInfinity = new(Kind.PositiveInfinity);
    public static readonly ExactDecimal NegativeInfinity = new(Kind.NegativeInfinity);

    private ExactDecimal(Kind infinity)
    {
        mantissa = BigInteger.Zero;
        exponent = 0;
        kind = infinity;
    }

    private ExactDecimal(BigInteger mantissa, int exponent)
    {
        // normaliza: remove zeros à direita para a representação ser canônica
        if (mantissa.IsZero)
        {
            exponent = 0;
        }
        else
        {
            while (true)
            {
                var (quotient, remainder) = BigInteger.DivRem(mantissa, 10);
                if (!remainder.IsZero)
                    break;
                mantissa = quotient;
                exponent++;
            }
        }
        this.mantissa = mantissa;
        this.exponent = exponent;
        kind = Kind.Finite;
    }

    public bool IsFinite => kind == Kind.Finite;

    /// <summary>Converte como o `new BigNumber(number)` do JS: usa a representação decimal
    /// round-trip mais curta do double (que é o `Number.prototype.toString`).</summary>
    public static ExactDecimal FromDouble(double value)
    {
        if (double.IsPositiveInfinity(value))
            return PositiveInfinity;
        if (double.IsNegativeInfinity(value))
            return NegativeInfinity;
        if (double.IsNaN(value))
            throw new ArgumentException("NaN não é suportado", nameof(value));

        return Parse(value.ToString("R", CultureInfo.InvariantCulture));
    }

    public static ExactDecimal FromInt(int value) => new(value, 0);

    internal static ExactDecimal Parse(string text)
    {
        var negative = false;
        var index = 0;
        if (text[0] is '+' or '-')
        {
            negative = text[0] == '-';
            index = 1;
        }

        var digits = new System.Text.StringBuilder();
        var fractionLength = 0;
        var scientificExponent = 0;
        var inFraction = false;

        for (; index < text.Length; index++)
        {
            var c = text[index];
            if (c == '.')
            {
                inFraction = true;
            }
            else if (c is 'e' or 'E')
            {
                scientificExponent = int.Parse(text[(index + 1)..], CultureInfo.InvariantCulture);
                break;
            }
            else
            {
                digits.Append(c);
                if (inFraction)
                    fractionLength++;
            }
        }

        var mantissa = BigInteger.Parse(digits.ToString(), CultureInfo.InvariantCulture);
        if (negative)
            mantissa = -mantissa;
        return new ExactDecimal(mantissa, scientificExponent - fractionLength);
    }

    /// <summary>`toNumber()`: o double mais próximo (parsing IEEE corretamente arredondado).</summary>
    public double ToNumber()
    {
        if (kind == Kind.PositiveInfinity)
            return double.PositiveInfinity;
        if (kind == Kind.NegativeInfinity)
            return double.NegativeInfinity;
        return double.Parse(
            mantissa.ToString(CultureInfo.InvariantCulture) + "E" + exponent.ToString(CultureInfo.InvariantCulture),
            NumberStyles.Float,
            CultureInfo.InvariantCulture
        );
    }

    public bool IsZero() => kind == Kind.Finite && mantissa.IsZero;

    public ExactDecimal Abs()
    {
        AssertFinite();
        return mantissa.Sign < 0 ? new ExactDecimal(-mantissa, exponent) : this;
    }

    public ExactDecimal Plus(ExactDecimal other)
    {
        AssertFinite();
        other.AssertFinite();
        var (a, b, sharedExponent) = Align(this, other);
        return new ExactDecimal(a + b, sharedExponent);
    }

    public ExactDecimal Minus(ExactDecimal other)
    {
        AssertFinite();
        other.AssertFinite();
        var (a, b, sharedExponent) = Align(this, other);
        return new ExactDecimal(a - b, sharedExponent);
    }

    public ExactDecimal Times(ExactDecimal other)
    {
        AssertFinite();
        other.AssertFinite();
        return new ExactDecimal(mantissa * other.mantissa, exponent + other.exponent);
    }

    /// <summary>`exponentiatedBy(2)` no uso do polyclip.</summary>
    public ExactDecimal Square() => Times(this);

    /// <summary>`div`: quociente arredondado a 20 casas decimais, half-up (bignumber.js default).</summary>
    public ExactDecimal Div(ExactDecimal divisor)
    {
        AssertFinite();
        divisor.AssertFinite();
        if (divisor.mantissa.IsZero)
            throw new DivideByZeroException();

        // resultado = round_half_up(ma·10^(ea−eb+20) / mb) · 10^-20
        var shift = exponent - divisor.exponent + DivisionDecimalPlaces;
        BigInteger numerator = mantissa,
            denominator = divisor.mantissa;
        if (shift >= 0)
            numerator *= BigInteger.Pow(10, shift);
        else
            denominator *= BigInteger.Pow(10, -shift);

        return new ExactDecimal(DivideHalfUp(numerator, denominator), -DivisionDecimalPlaces);
    }

    public ExactDecimal Div(int divisor) => Div(FromInt(divisor));

    /// <summary>`sqrt()`: raiz arredondada a 20 casas decimais, half-up (bignumber.js default).</summary>
    public ExactDecimal Sqrt()
    {
        AssertFinite();
        if (mantissa.Sign < 0)
            throw new ArithmeticException("Sqrt de valor negativo");
        if (mantissa.IsZero)
            return Zero;

        // floor(sqrt(v)·10^targetDigits) com dígitos de guarda, depois half-up até 20 casas.
        var targetDigits = DivisionDecimalPlaces + SqrtGuardDigits;
        // sqrt(m·10^e)·10^t = sqrt(m·10^(e+2t)); o expoente precisa ficar não-negativo.
        var scalePower = exponent + 2 * targetDigits;
        if (scalePower < 0)
        {
            var extra = (-scalePower + 1) / 2;
            targetDigits += extra;
            scalePower += 2 * extra;
        }

        var scaled = mantissa * BigInteger.Pow(10, scalePower);
        var floorRoot = IntegerSqrt(scaled); // = floor(sqrt(v)·10^targetDigits)

        // arredonda de targetDigits para 20 casas (half-up)
        var q = DivideHalfUp(floorRoot, BigInteger.Pow(10, targetDigits - DivisionDecimalPlaces));
        return new ExactDecimal(q, -DivisionDecimalPlaces);
    }

    public int ComparedTo(ExactDecimal other) => CompareTo(other);

    public int CompareTo(ExactDecimal other)
    {
        if (kind != Kind.Finite || other.kind != Kind.Finite)
        {
            var selfRank = kind switch { Kind.NegativeInfinity => -1, Kind.PositiveInfinity => 1, _ => 0 };
            var otherRank = other.kind switch { Kind.NegativeInfinity => -1, Kind.PositiveInfinity => 1, _ => 0 };
            return selfRank.CompareTo(otherRank);
        }

        var signComparison = mantissa.Sign.CompareTo(other.mantissa.Sign);
        if (signComparison != 0)
            return signComparison;

        var (a, b, _) = Align(this, other);
        return a.CompareTo(b);
    }

    public bool Eq(ExactDecimal other) => CompareTo(other) == 0;

    public bool IsLessThan(ExactDecimal other) => CompareTo(other) < 0;

    public bool IsGreaterThan(ExactDecimal other) => CompareTo(other) > 0;

    public bool IsLessThanOrEqualTo(ExactDecimal other) => CompareTo(other) <= 0;

    public bool IsGreaterThanOrEqualTo(ExactDecimal other) => CompareTo(other) >= 0;

    public bool IsLessThan(int other) => CompareTo(FromInt(other)) < 0;

    public bool IsGreaterThanOrEqualTo(int other) => CompareTo(FromInt(other)) >= 0;

    public bool Equals(ExactDecimal other) =>
        kind == other.kind && exponent == other.exponent && mantissa.Equals(other.mantissa);

    public override bool Equals(object? obj) => obj is ExactDecimal other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(kind, exponent, mantissa);

    public override string ToString()
    {
        if (kind == Kind.PositiveInfinity)
            return "Infinity";
        if (kind == Kind.NegativeInfinity)
            return "-Infinity";
        return exponent == 0
            ? mantissa.ToString(CultureInfo.InvariantCulture)
            : $"{mantissa.ToString(CultureInfo.InvariantCulture)}E{exponent}";
    }

    private void AssertFinite()
    {
        if (kind != Kind.Finite)
            throw new InvalidOperationException("Aritmética com infinito não é usada pelo polyclip.");
    }

    private static (BigInteger A, BigInteger B, int Exponent) Align(ExactDecimal x, ExactDecimal y)
    {
        if (x.exponent == y.exponent)
            return (x.mantissa, y.mantissa, x.exponent);
        if (x.exponent > y.exponent)
            return (x.mantissa * BigInteger.Pow(10, x.exponent - y.exponent), y.mantissa, y.exponent);
        return (x.mantissa, y.mantissa * BigInteger.Pow(10, y.exponent - x.exponent), x.exponent);
    }

    /// <summary>Divisão inteira com half-up (ties para longe do zero), como o ROUND_HALF_UP.</summary>
    private static BigInteger DivideHalfUp(BigInteger numerator, BigInteger denominator)
    {
        var negative = (numerator.Sign < 0) ^ (denominator.Sign < 0);
        var n = BigInteger.Abs(numerator);
        var d = BigInteger.Abs(denominator);
        var (quotient, remainder) = BigInteger.DivRem(n, d);
        if (remainder * 2 >= d)
            quotient += BigInteger.One;
        return negative ? -quotient : quotient;
    }

    private static BigInteger IntegerSqrt(BigInteger value)
    {
        if (value.Sign <= 0)
            return BigInteger.Zero;

        // Newton: chute inicial por comprimento de bits, refina até convergir.
        var bitLength = (int)value.GetBitLength();
        var guess = BigInteger.One << (bitLength / 2 + 1);
        while (true)
        {
            var next = (guess + value / guess) >> 1;
            if (next >= guess)
                break;
            guess = next;
        }
        // garante floor exato
        while (guess * guess > value)
            guess -= BigInteger.One;
        while ((guess + 1) * (guess + 1) <= value)
            guess += BigInteger.One;
        return guess;
    }
}
