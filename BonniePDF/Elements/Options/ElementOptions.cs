using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BonniePDF;

/// <summary>
/// Shares equal option objects between elements. A table of a thousand cells styled alike holds
/// one set of options rather than a thousand copies; each element keeps a reference.
/// </summary>
/// <remarks>
/// Option objects are immutable: an element changes an option by switching to another object, so
/// changing one element never affects another. The table is process-wide and bounded; past the
/// bound, new values are simply not shared. It holds only option values (sizes, colours, font
/// names), never content.
/// </remarks>
internal static class Interned<T> where T : class, IEquatable<T>
{
    private const int Capacity = 8192;
    private static readonly ConcurrentDictionary<T, T> Values = new();

    internal static T Get(T value)
    {
        if (Values.TryGetValue(value, out T? shared)) return shared;
        return Values.Count < Capacity ? Values.GetOrAdd(value, value) : value;
    }
}

/// <summary>Exact comparisons for option values: floats compare by bits, so -0 and NaN keep their identity.</summary>
internal static class OptionEquality
{
    internal static bool Same(float a, float b) => BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b);
    internal static bool Same(float? a, float? b) => a.HasValue == b.HasValue && (!a.HasValue || Same(a.Value, b!.Value));
    internal static bool Same(Thickness a, Thickness b) => Same(a.Left, b.Left) && Same(a.Top, b.Top) && Same(a.Right, b.Right) && Same(a.Bottom, b.Bottom);
    internal static bool Same(Thickness? a, Thickness? b) => a.HasValue == b.HasValue && (!a.HasValue || Same(a.Value, b!.Value));
    internal static bool Same(Border a, Border b) => Same(a.Left, b.Left) && Same(a.Top, b.Top) && Same(a.Right, b.Right) && Same(a.Bottom, b.Bottom);
    internal static bool Same(BorderColors a, BorderColors b) => a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;
    // Feature overrides compare by content: each element holds its own validated copy.
    internal static bool Same(IReadOnlyDictionary<string, int>? a, IReadOnlyDictionary<string, int>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null || a.Count != b.Count) return false;
        foreach (var pair in a)
            if (!b.TryGetValue(pair.Key, out int value) || value != pair.Value) return false;
        return true;
    }
    // The same comparisons for a value of any option type, used to recognise a repeated change.
    internal static bool SameValue<TValue>(TValue a, TValue b)
    {
        if (typeof(TValue) == typeof(float)) return Same(Unsafe.As<TValue, float>(ref a), Unsafe.As<TValue, float>(ref b));
        if (typeof(TValue) == typeof(float?)) return Same(Unsafe.As<TValue, float?>(ref a), Unsafe.As<TValue, float?>(ref b));
        if (typeof(TValue) == typeof(Thickness)) return Same(Unsafe.As<TValue, Thickness>(ref a), Unsafe.As<TValue, Thickness>(ref b));
        if (typeof(TValue) == typeof(Thickness?)) return Same(Unsafe.As<TValue, Thickness?>(ref a), Unsafe.As<TValue, Thickness?>(ref b));
        if (typeof(TValue) == typeof(Border)) return Same(Unsafe.As<TValue, Border>(ref a), Unsafe.As<TValue, Border>(ref b));
        if (typeof(TValue) == typeof(BorderColors)) return Same(Unsafe.As<TValue, BorderColors>(ref a), Unsafe.As<TValue, BorderColors>(ref b));
        if (a is IReadOnlyDictionary<string, int> x) return b is IReadOnlyDictionary<string, int> y && Same(x, y);
        return EqualityComparer<TValue>.Default.Equals(a, b);
    }

    internal static int Hash(Thickness t) => HashCode.Combine(t.Left, t.Top, t.Right, t.Bottom);
    internal static int Hash(Border b) => HashCode.Combine(b.Left, b.Top, b.Right, b.Bottom);
}

/// <summary>
/// A change recently made to a shared options object and the object it produced. Elements built
/// alike make the same changes in the same order, so remembering a few changes per object answers
/// almost every change without building and comparing a new object.
/// </summary>
internal abstract class OptionChange<T>(Delegate apply, T result) where T : class
{
    private const int Remembered = 4;

    internal readonly Delegate Apply = apply;
    internal readonly T Result = result;

    /// <summary>The result of the remembered change, or null when it is not among <paramref name="changes"/>.</summary>
    internal static T? Find<TValue>(OptionChange<T>[]? changes, Func<T, TValue, T> apply, TValue value)
    {
        if (changes is null) return null;
        foreach (OptionChange<T> change in changes)
            if (ReferenceEquals(change.Apply, apply) && change is OptionChange<T, TValue> typed && OptionEquality.SameValue(typed.Value, value))
                return change.Result;
        return null;
    }

    /// <summary><paramref name="changes"/> with <paramref name="change"/> first, as a new array (it is read without locks).</summary>
    internal static OptionChange<T>[] Remember(OptionChange<T>[]? changes, OptionChange<T> change)
    {
        int kept = changes is null ? 0 : Math.Min(changes.Length, Remembered - 1);
        var result = new OptionChange<T>[kept + 1];
        result[0] = change;
        if (kept > 0) Array.Copy(changes!, 0, result, 1, kept);
        return result;
    }
}

internal sealed class OptionChange<T, TValue>(Func<T, TValue, T> apply, TValue value, T result) : OptionChange<T>(apply, result) where T : class
{
    internal readonly TValue Value = value;
}

/// <summary>The options every element has: margin, capture name, direction override and break permission.</summary>
internal sealed record ElementOptions : IEquatable<ElementOptions>
{
    internal static readonly ElementOptions Default = Interned<ElementOptions>.Get(new ElementOptions());

    // Never part of the value: cleared on every new object, and ignored by Equals.
    private OptionChange<ElementOptions>[]? _changes;

    /// <summary>These options with one change, <paramref name="apply"/> (a static lambda), made.</summary>
    internal ElementOptions With<TValue>(TValue value, Func<ElementOptions, TValue, ElementOptions> apply)
    {
        if (OptionChange<ElementOptions>.Find(_changes, apply, value) is { } known) return known;
        ElementOptions next = apply(this, value);
        next._changes = null;
        next = Interned<ElementOptions>.Get(next);
        _changes = OptionChange<ElementOptions>.Remember(_changes, new OptionChange<ElementOptions, TValue>(apply, value, next));
        return next;
    }

    internal Thickness Margin { get; init; } = new(0);
    internal string? CaptureName { get; init; }
    internal ContentDirection? ContentDirection { get; init; }
    internal bool BreakAllowed { get; init; } = true;

    public bool Equals(ElementOptions? other) =>
        other is not null && OptionEquality.Same(Margin, other.Margin) && string.Equals(CaptureName, other.CaptureName, StringComparison.Ordinal)
        && ContentDirection == other.ContentDirection && BreakAllowed == other.BreakAllowed;

    public override int GetHashCode() => HashCode.Combine(OptionEquality.Hash(Margin), CaptureName, ContentDirection, BreakAllowed);
}

/// <summary>Border, border colours, background and corner radius of a bordered element.</summary>
internal sealed record BoxOptions : IEquatable<BoxOptions>
{
    internal static readonly BoxOptions Default = Interned<BoxOptions>.Get(new BoxOptions());

    // Never part of the value: cleared on every new object, and ignored by Equals.
    private OptionChange<BoxOptions>[]? _changes;

    /// <summary>These options with one change, <paramref name="apply"/> (a static lambda), made.</summary>
    internal BoxOptions With<TValue>(TValue value, Func<BoxOptions, TValue, BoxOptions> apply)
    {
        if (OptionChange<BoxOptions>.Find(_changes, apply, value) is { } known) return known;
        BoxOptions next = apply(this, value);
        next._changes = null;
        next = Interned<BoxOptions>.Get(next);
        _changes = OptionChange<BoxOptions>.Remember(_changes, new OptionChange<BoxOptions, TValue>(apply, value, next));
        return next;
    }

    internal Border Border { get; init; } = new(0);
    internal BorderColors BorderColors { get; init; }
    internal Color? BackgroundColor { get; init; }
    internal float CornerRadius { get; init; }

    public bool Equals(BoxOptions? other) =>
        other is not null && OptionEquality.Same(Border, other.Border) && OptionEquality.Same(BorderColors, other.BorderColors)
        && BackgroundColor == other.BackgroundColor && OptionEquality.Same(CornerRadius, other.CornerRadius);

    public override int GetHashCode() => HashCode.Combine(OptionEquality.Hash(Border), BorderColors.Left, BorderColors.Top, BackgroundColor, CornerRadius);
}

/// <summary>The text colour and padding a table cell or row sets for its content.</summary>
internal sealed record CellOptions : IEquatable<CellOptions>
{
    internal static readonly CellOptions Default = Interned<CellOptions>.Get(new CellOptions());

    // Never part of the value: cleared on every new object, and ignored by Equals.
    private OptionChange<CellOptions>[]? _changes;

    /// <summary>These options with one change, <paramref name="apply"/> (a static lambda), made.</summary>
    internal CellOptions With<TValue>(TValue value, Func<CellOptions, TValue, CellOptions> apply)
    {
        if (OptionChange<CellOptions>.Find(_changes, apply, value) is { } known) return known;
        CellOptions next = apply(this, value);
        next._changes = null;
        next = Interned<CellOptions>.Get(next);
        _changes = OptionChange<CellOptions>.Remember(_changes, new OptionChange<CellOptions, TValue>(apply, value, next));
        return next;
    }

    internal Color? Color { get; init; }
    internal Thickness? Padding { get; init; }
    internal float? MinHeight { get; init; }
    internal Color? BackgroundColor { get; init; }

    public bool Equals(CellOptions? other) =>
        other is not null && Color == other.Color && OptionEquality.Same(Padding, other.Padding)
        && OptionEquality.Same(MinHeight, other.MinHeight) && BackgroundColor == other.BackgroundColor;

    public override int GetHashCode() => HashCode.Combine(Color, Padding.HasValue ? OptionEquality.Hash(Padding.Value) : 0, MinHeight, BackgroundColor);
}

/// <summary>
/// The text style and text-block options of a text element, including which values were set
/// explicitly (the document's default text style fills in only the others).
/// </summary>
internal sealed record TextOptions : IEquatable<TextOptions>
{
    internal static readonly TextOptions Default = Interned<TextOptions>.Get(new TextOptions());

    // Never part of the value: cleared on every new object, and ignored by Equals.
    private OptionChange<TextOptions>[]? _changes;

    /// <summary>These options with one change, <paramref name="apply"/> (a static lambda), made.</summary>
    internal TextOptions With<TValue>(TValue value, Func<TextOptions, TValue, TextOptions> apply)
    {
        if (OptionChange<TextOptions>.Find(_changes, apply, value) is { } known) return known;
        TextOptions next = apply(this, value);
        next._changes = null;
        next = Interned<TextOptions>.Get(next);
        _changes = OptionChange<TextOptions>.Remember(_changes, new OptionChange<TextOptions, TValue>(apply, value, next));
        return next;
    }

    internal IReadOnlyDictionary<string, int>? FontFeatures { get; init; }
    internal string? FontFamily { get; init; }
    internal FontWeight Weight { get; init; } = FontWeight.Normal;
    internal bool Italic { get; init; }
    internal string FontName { get; init; } = StandardFonts.Helvetica;
    internal bool FontExplicit { get; init; }
    internal bool FontFamilyExplicit { get; init; }
    internal bool WeightExplicit { get; init; }
    internal bool ItalicExplicit { get; init; }
    internal float FontSize { get; init; } = 12f;
    internal bool FontSizeExplicit { get; init; }
    internal Color TextColor { get; init; } = Color.Black;
    internal bool TextColorExplicit { get; init; }
    internal float LineHeight { get; init; } = 1.2f;
    internal bool LineHeightExplicit { get; init; }
    internal TextDecoration Decoration { get; init; } = TextDecoration.None;
    internal bool DecorationExplicit { get; init; }
    internal Color? DecorationColor { get; init; }
    internal float? DecorationThickness { get; init; }
    internal DecorationStyle DecorationStyle { get; init; } = DecorationStyle.Solid;
    internal float LetterSpacing { get; init; }
    internal float WordSpacing { get; init; }

    // TextBlock options.
    internal TextAlignment Alignment { get; init; } = TextAlignment.Left;
    internal bool AlignmentExplicit { get; init; }
    internal int? MaxLines { get; init; }
    internal string Ellipsis { get; init; } = "…";
    internal float FirstLineIndent { get; init; }
    internal float ParagraphSpacing { get; init; }
    internal bool PreserveWhitespace { get; init; }
    internal int MinLinesBeforeBreak { get; init; } = 2;
    internal int MinLinesAfterBreak { get; init; } = 2;

    public bool Equals(TextOptions? other) =>
        other is not null && OptionEquality.Same(FontFeatures, other.FontFeatures)
        && string.Equals(FontFamily, other.FontFamily, StringComparison.Ordinal) && Weight == other.Weight && Italic == other.Italic
        && string.Equals(FontName, other.FontName, StringComparison.Ordinal) && FontExplicit == other.FontExplicit
        && FontFamilyExplicit == other.FontFamilyExplicit && WeightExplicit == other.WeightExplicit && ItalicExplicit == other.ItalicExplicit
        && OptionEquality.Same(FontSize, other.FontSize) && FontSizeExplicit == other.FontSizeExplicit
        && TextColor == other.TextColor && TextColorExplicit == other.TextColorExplicit
        && OptionEquality.Same(LineHeight, other.LineHeight) && LineHeightExplicit == other.LineHeightExplicit
        && Decoration == other.Decoration && DecorationExplicit == other.DecorationExplicit && DecorationColor == other.DecorationColor
        && OptionEquality.Same(DecorationThickness, other.DecorationThickness) && DecorationStyle == other.DecorationStyle
        && OptionEquality.Same(LetterSpacing, other.LetterSpacing) && OptionEquality.Same(WordSpacing, other.WordSpacing)
        && Alignment == other.Alignment && AlignmentExplicit == other.AlignmentExplicit && MaxLines == other.MaxLines
        && string.Equals(Ellipsis, other.Ellipsis, StringComparison.Ordinal) && OptionEquality.Same(FirstLineIndent, other.FirstLineIndent)
        && OptionEquality.Same(ParagraphSpacing, other.ParagraphSpacing) && PreserveWhitespace == other.PreserveWhitespace
        && MinLinesBeforeBreak == other.MinLinesBeforeBreak && MinLinesAfterBreak == other.MinLinesAfterBreak;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FontFamily); hash.Add(FontName); hash.Add(Weight); hash.Add(FontSize); hash.Add(TextColor);
        hash.Add(LineHeight); hash.Add(Alignment); hash.Add(FontExplicit); hash.Add(FontSizeExplicit); hash.Add(TextColorExplicit);
        return hash.ToHashCode();
    }
}
