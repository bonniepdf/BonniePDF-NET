// Vendored file: maintained upstream and copied into this folder by tooling.
// Do not edit it here. A hand edit fails the vendored-files check; see VENDORED.json.
using System.Collections.Generic;

namespace BonniePDF.Rendering.Svg.Parsers;

/// <summary>
/// Shared number scanning for SVG attribute grammars.
/// </summary>
/// <remarks>
/// SVG number lists allow forms a simple split would mishandle: <c>1-2</c> is two
/// numbers, <c>.5.5</c> is two numbers, and exponents may carry signs. Path data
/// and <c>points</c> both need this, so it lives in one place.
/// </remarks>
internal sealed class NumberScanner
{
    private readonly string _text;
    private int _index;

    /// <summary>Creates a scanner over <paramref name="text"/>.</summary>
    public NumberScanner(string text) => _text = text;

    /// <summary>Gets the current offset.</summary>
    public int Position => _index;

    /// <summary>True when only separators remain.</summary>
    public bool AtEnd
    {
        get
        {
            SkipSeparators();
            return _index >= _text.Length;
        }
    }

    /// <summary>Consumes whitespace and commas.</summary>
    public void SkipSeparators()
    {
        while (_index < _text.Length)
        {
            char c = _text[_index];
            if (c is ' ' or '\t' or '\n' or '\r' or '\f' or ',') _index++;
            else break;
        }
    }

    /// <summary>Peeks the next significant character.</summary>
    public char? Peek()
    {
        SkipSeparators();
        return _index < _text.Length ? _text[_index] : null;
    }

    /// <summary>Takes the next significant character.</summary>
    public char? Take()
    {
        SkipSeparators();
        return _index < _text.Length ? _text[_index++] : null;
    }

    /// <summary>Scans one number, or throws.</summary>
    public double Number()
    {
        SkipSeparators();
        int start = _index;

        if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-')) _index++;
        while (IsDigit(_index)) _index++;
        if (_index < _text.Length && _text[_index] == '.')
        {
            _index++;
            while (IsDigit(_index)) _index++;
        }

        // Only consume an exponent that is well formed; `10e` in a path is a
        // number followed by junk, not a broken exponent.
        if (_index < _text.Length && (_text[_index] is 'e' or 'E'))
        {
            int save = _index;
            _index++;
            if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-')) _index++;
            if (IsDigit(_index))
            {
                while (IsDigit(_index)) _index++;
            }
            else
            {
                _index = save;
            }
        }

        string slice = _text[start.._index];
        if (slice.Length == 0 || !double.TryParse(slice, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value) || !double.IsFinite(value))
        {
            throw new SvgParseException($"expected a number at offset {start}", start, _text);
        }

        return value;
    }

    /// <summary>Scans an arc flag, which is exactly <c>0</c> or <c>1</c>.</summary>
    public bool Flag()
    {
        SkipSeparators();
        char? c = _index < _text.Length ? _text[_index] : null;
        if (c is not ('0' or '1'))
        {
            throw new SvgParseException($"expected a 0 or 1 flag at offset {_index}", _index, _text);
        }

        _index++;
        return c == '1';
    }

    /// <summary>Scans every number in a list, e.g. a <c>points</c> attribute.</summary>
    public static List<double> ParseList(string text)
    {
        var scanner = new NumberScanner(text);
        var values = new List<double>();
        while (!scanner.AtEnd) values.Add(scanner.Number());
        return values;
    }

    private bool IsDigit(int index) => index < _text.Length && _text[index] is >= '0' and <= '9';
}
