// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BonniePDF.Rendering.Svg.Geometry;

namespace BonniePDF.Rendering.Svg.Parsers;

/// <summary>Parses a <c>transform</c> attribute into a single matrix.</summary>
internal static partial class TransformParser
{
    [GeneratedRegex(@"(?<name>[a-zA-Z]+)[ \t\n\r\f]*\((?<args>[^)]*)\)", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex FunctionPattern();

    /// <summary>
    /// Composes the attribute's functions into one matrix.
    /// </summary>
    /// <remarks>
    /// SVG composes left to right: <c>translate(a) rotate(b)</c> translates the
    /// already-rotated content, so the leftmost function is the outermost.
    /// Reversing this is the classic transform bug and only shows up once two
    /// non-commuting functions appear together.
    /// </remarks>
    public static Matrix Parse(string input)
    {
        string text = input.Trim();
        if (text.Length == 0) return Matrix.Identity;

        Matrix result = Matrix.Identity;
        int consumed = 0;

        foreach (Match match in FunctionPattern().Matches(text))
        {
            string name = match.Groups["name"].Value.ToLowerInvariant();
            List<double> args = ReadNumbers(match.Groups["args"].Value, text, match.Index);
            result = Matrix.Multiply(result, ToMatrix(name, args, text, match.Index));
            consumed = match.Index + match.Length;
        }

        // Trailing junk means the attribute was malformed; ignoring it hides a
        // transform that was meant to apply.
        if (text[consumed..].Trim().Length != 0)
        {
            throw new SvgParseException("unparsed text in transform attribute", consumed, text);
        }

        return result;
    }

    private static List<double> ReadNumbers(string body, string source, int offset)
    {
        var scanner = new NumberScanner(body);
        var values = new List<double>();
        try
        {
            while (!scanner.AtEnd) values.Add(scanner.Number());
        }
        catch (SvgParseException)
        {
            throw new SvgParseException("malformed transform arguments", offset, source);
        }

        return values;
    }

    private static Matrix ToMatrix(string name, List<double> args, string source, int offset)
    {
        void Need(int count)
        {
            if (args.Count < count) throw new SvgParseException($"{name}() needs {count} argument(s)", offset, source);
        }

        switch (name)
        {
            case "translate":
                Need(1);
                // A one-argument translate leaves y at zero.
                return Matrix.Translation(args[0], args.Count > 1 ? args[1] : 0.0);

            case "scale":
                Need(1);
                // A one-argument scale is uniform.
                return Matrix.Scaling(args[0], args.Count > 1 ? args[1] : args[0]);

            case "rotate":
                Need(1);
                if (args.Count >= 3)
                {
                    // Rotation about a point: translate to it, rotate, translate back.
                    return Matrix.Multiply(
                        Matrix.Multiply(Matrix.Translation(args[1], args[2]), Matrix.Rotation(args[0])),
                        Matrix.Translation(-args[1], -args[2]));
                }

                return Matrix.Rotation(args[0]);

            case "skewx":
                Need(1);
                return Matrix.SkewX(args[0]);

            case "skewy":
                Need(1);
                return Matrix.SkewY(args[0]);

            case "matrix":
                Need(6);
                return new Matrix(args[0], args[1], args[2], args[3], args[4], args[5]);

            default:
                throw new SvgParseException($"unknown transform function \"{name}\"", offset, source);
        }
    }
}
