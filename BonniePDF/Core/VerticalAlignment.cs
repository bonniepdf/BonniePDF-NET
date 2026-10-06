namespace BonniePDF;

/// <summary>
/// Vertical alignment options for layout elements.
///
/// The counterpart to <see cref="HorizontalAlignment"/>. Meaningful on a container
/// that has more vertical room than its child needs - typically one given an
/// explicit Height - which is what makes it possible to anchor content to the
/// bottom of a fixed box without hand-computing a top padding.
/// </summary>
public enum VerticalAlignment
{
    /// <summary>Align to the top</summary>
    Start,

    /// <summary>Align to the middle</summary>
    Center,

    /// <summary>Align to the bottom</summary>
    End,

    /// <summary>Stretch to fill available space</summary>
    Stretch
}
