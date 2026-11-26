// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
namespace BonniePDF;

/// <summary>A fitted trendline: the curve to draw, its equation and its coefficient of determination.</summary>
internal sealed record TrendlineFit(List<(double X, double Y)> Points, string? Equation, double? R2);

/// <summary>Regression models for trendlines, in the same operation order as the TypeScript implementation.</summary>
internal static class ChartTrendlineMath
{
    private static readonly string[] Superscripts = ["", "", "²", "³", "⁴"];

    /// <summary>Four significant digits in plain notation, trailing zeros removed.</summary>
    internal static string FormatSignificant(double value)
    {
        if (value == 0 || !double.IsFinite(value)) return "0";
        int exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        int decimals = Math.Min(12, Math.Max(0, 3 - exponent));
        string text = ChartNumberFormat.Format(ChartTemplate.ToDecimal(value), "F" + decimals);
        string trimmed = text.Contains('.', StringComparison.Ordinal) ? text.TrimEnd('0').TrimEnd('.') : text;
        return string.Equals(trimmed, "-0", StringComparison.Ordinal) ? "0" : trimmed;
    }

    private static double IntPow(double x, int k)
    {
        double r = 1;
        for (int i = 0; i < k; i++) r *= x;
        return r;
    }

    private static (double A, double B) LeastSquares(IReadOnlyList<double> xs, IReadOnlyList<double> ys)
    {
        int n = xs.Count;
        double sx = 0, sy = 0, sxy = 0, sxx = 0;
        for (int i = 0; i < n; i++) { sx += xs[i]; sy += ys[i]; sxy += xs[i] * ys[i]; sxx += xs[i] * xs[i]; }
        double denominator = n * sxx - sx * sx;
        if (denominator == 0) throw new InvalidOperationException("A trendline needs at least two distinct x values.");
        double b = (n * sxy - sx * sy) / denominator;
        return ((sy - b * sx) / n, b);
    }

    private static double[] SolvePolynomial(IReadOnlyList<double> xs, IReadOnlyList<double> ys, int order)
    {
        int size = order + 1;
        double[][] m = Enumerable.Range(0, size).Select(_ => new double[size + 1]).ToArray();
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++) for (int k = 0; k < xs.Count; k++) m[i][j] += IntPow(xs[k], i + j);
            for (int k = 0; k < xs.Count; k++) m[i][size] += ys[k] * IntPow(xs[k], i);
        }
        for (int col = 0; col < size; col++)
        {
            int pivot = col;
            for (int r = col + 1; r < size; r++) if (Math.Abs(m[r][col]) > Math.Abs(m[pivot][col])) pivot = r;
            if (Math.Abs(m[pivot][col]) < 1e-12) throw new InvalidOperationException("The trendline cannot be fitted to these points.");
            (m[col], m[pivot]) = (m[pivot], m[col]);
            for (int r = col + 1; r < size; r++)
            {
                double f = m[r][col] / m[col][col];
                for (int c = col; c <= size; c++) m[r][c] -= f * m[col][c];
            }
        }
        double[] coefficients = new double[size];
        for (int i = size - 1; i >= 0; i--)
        {
            double sum = m[i][size];
            for (int j = i + 1; j < size; j++) sum -= m[i][j] * coefficients[j];
            coefficients[i] = sum / m[i][i];
        }
        return coefficients;
    }

    private static string Signed(double value) => value < 0 ? $" - {FormatSignificant(-value)}" : $" + {FormatSignificant(value)}";

    /// <summary>Fits <paramref name="type"/> to the points. Throws when the data is outside the model's domain.</summary>
    internal static TrendlineFit Fit(string type, IReadOnlyList<double> xs, IReadOnlyList<double> ys, int order = 2, int period = 2)
    {
        int n = xs.Count;
        if (n < 2) throw new InvalidOperationException("A trendline needs at least two points.");
        if (string.Equals(type, "movingAverage", StringComparison.Ordinal))
        {
            if (period < 2) throw new InvalidOperationException("A moving average needs a period of at least 2.");
            List<(double, double)> averaged = [];
            for (int i = period - 1; i < n; i++)
            {
                double sum = 0;
                for (int k = i - period + 1; k <= i; k++) sum += ys[k];
                averaged.Add((xs[i], sum / period));
            }
            return new TrendlineFit(averaged, null, null);
        }
        Func<double, double> predict;
        string equation;
        switch (type)
        {
            case "linear":
            {
                (double a, double b) = LeastSquares(xs, ys);
                predict = x => a + b * x;
                equation = $"y = {FormatSignificant(b)}x{Signed(a)}";
                break;
            }
            case "exponential":
            {
                if (ys.Any(y => !(y > 0))) throw new InvalidOperationException("An exponential trendline needs positive values.");
                (double a, double b) = LeastSquares(xs, ys.Select(v => Math.Log(v)).ToList());
                double scale = Math.Exp(a);
                predict = x => scale * Math.Exp(b * x);
                equation = $"y = {FormatSignificant(scale)}e^({FormatSignificant(b)}x)";
                break;
            }
            case "logarithmic":
            {
                if (xs.Any(x => !(x > 0))) throw new InvalidOperationException("A logarithmic trendline needs positive x values.");
                (double a, double b) = LeastSquares(xs.Select(v => Math.Log(v)).ToList(), ys);
                predict = x => a + b * Math.Log(x);
                equation = $"y = {FormatSignificant(b)}ln(x){Signed(a)}";
                break;
            }
            case "power":
            {
                if (xs.Any(x => !(x > 0)) || ys.Any(y => !(y > 0))) throw new InvalidOperationException("A power trendline needs positive values.");
                (double a, double b) = LeastSquares(xs.Select(v => Math.Log(v)).ToList(), ys.Select(v => Math.Log(v)).ToList());
                double scale = Math.Exp(a);
                predict = x => scale * Math.Pow(x, b);
                equation = $"y = {FormatSignificant(scale)}x^{FormatSignificant(b)}";
                break;
            }
            default:
            {
                if (order < 2 || order > 4) throw new InvalidOperationException("A polynomial trendline needs an order from 2 to 4.");
                if (n <= order) throw new InvalidOperationException("A polynomial trendline needs more points than its order.");
                double[] c = SolvePolynomial(xs, ys, order);
                predict = x => { double y = 0; for (int i = order; i >= 0; i--) y = y * x + c[i]; return y; };
                string text = $"y = {FormatSignificant(c[order])}x{Superscripts[order]}";
                for (int i = order - 1; i >= 2; i--) text += $"{Signed(c[i])}x{Superscripts[i]}";
                text += $"{Signed(c[1])}x{Signed(c[0])}";
                equation = text;
                break;
            }
        }
        double mean = 0;
        foreach (double y in ys) mean += y;
        mean /= n;
        double ssRes = 0, ssTot = 0;
        for (int i = 0; i < n; i++)
        {
            double e = ys[i] - predict(xs[i]);
            double d = ys[i] - mean;
            ssRes += e * e;
            ssTot += d * d;
        }
        double r2 = ssTot == 0 ? 1 : 1 - ssRes / ssTot;
        double first = xs[0], last = xs[n - 1];
        int samples = string.Equals(type, "linear", StringComparison.Ordinal) ? 2 : (n - 1) * 8 + 1;
        List<(double, double)> points = [];
        for (int k = 0; k < samples; k++)
        {
            double x = first + (last - first) * k / (samples - 1);
            points.Add((x, predict(x)));
        }
        return new TrendlineFit(points, equation, r2);
    }

    /// <summary>Sample standard deviation (n - 1).</summary>
    internal static double SampleStandardDeviation(IReadOnlyList<double> values)
    {
        int n = values.Count;
        if (n < 2) return 0;
        double mean = 0;
        foreach (double v in values) mean += v;
        mean /= n;
        double sum = 0;
        foreach (double v in values) sum += (v - mean) * (v - mean);
        return Math.Sqrt(sum / (n - 1));
    }
}
