
namespace BonniePDF.Fluent;

/// <summary>
/// Fluent chaining extension methods for styling containers.
/// Each method creates a Container element and returns it as IContentContainer,
/// enabling continued chaining: .Padding(10).Border(1).Background(Color.Red)
/// </summary>
public static class ContainerStyleExtensions
{
    // A one-sided setter changes only its own side or sides. The others keep what earlier calls set, so
    // .PaddingVertical(7).PaddingHorizontal(3) and .BorderBottom(1).BorderTop(1) accumulate.

    private static void SetPadding(Container c, float? left = null, float? top = null, float? right = null, float? bottom = null) =>
        c.Padding = new Thickness(left ?? c.Padding.Left, top ?? c.Padding.Top, right ?? c.Padding.Right, bottom ?? c.Padding.Bottom);

    private static void SetMargin(Container c, float? left = null, float? top = null, float? right = null, float? bottom = null) =>
        c.Margin = new Thickness(left ?? c.Margin.Left, top ?? c.Margin.Top, right ?? c.Margin.Right, bottom ?? c.Margin.Bottom);

    private static void SetBorderSides(Container c, Color color, float? left = null, float? top = null, float? right = null, float? bottom = null)
    {
        c.Border = new Border(left ?? c.Border.Left, top ?? c.Border.Top, right ?? c.Border.Right, bottom ?? c.Border.Bottom);
        c.BorderColors = new BorderColors(
            left.HasValue ? color : c.BorderColors.Left,
            top.HasValue ? color : c.BorderColors.Top,
            right.HasValue ? color : c.BorderColors.Right,
            bottom.HasValue ? color : c.BorderColors.Bottom);
    }

    private static Container GetOrCreateContainer(IContentContainer c)
    {
        if (c is Container existingContainer && existingContainer.Child == null)
        {
            return existingContainer;
        }

        var newContainer = new Container();
        c.Element(newContainer);
        return newContainer;
    }

    extension(IContentContainer c)
    {
        /// <summary>Sets uniform padding on all sides.</summary>
        public IContentContainer Padding(float value)
        {
            var container = GetOrCreateContainer(c);
            container.Padding = new Thickness(value);
            return container;
        }

        /// <summary>Sets horizontal and vertical padding.</summary>
        public IContentContainer Padding(float horizontal, float vertical)
        {
            var container = GetOrCreateContainer(c);
            container.Padding = new Thickness(horizontal, vertical);
            return container;
        }

        /// <summary>Sets padding for each side individually.</summary>
        public IContentContainer Padding(float left, float top, float right,
            float bottom)
        {
            var container = GetOrCreateContainer(c);
            container.Padding = new Thickness(left, top, right, bottom);
            return container;
        }

        /// <summary>Sets top padding.</summary>
        public IContentContainer PaddingTop(float value)
        {
            var container = GetOrCreateContainer(c);
            SetPadding(container, top: value);
            return container;
        }

        /// <summary>Sets bottom padding.</summary>
        public IContentContainer PaddingBottom(float value)
        {
            var container = GetOrCreateContainer(c);
            SetPadding(container, bottom: value);
            return container;
        }

        /// <summary>Sets left padding.</summary>
        public IContentContainer PaddingLeft(float value)
        {
            var container = GetOrCreateContainer(c);
            SetPadding(container, left: value);
            return container;
        }

        /// <summary>Sets right padding.</summary>
        public IContentContainer PaddingRight(float value)
        {
            var container = GetOrCreateContainer(c);
            SetPadding(container, right: value);
            return container;
        }

        /// <summary>Sets horizontal padding (left and right).</summary>
        public IContentContainer PaddingHorizontal(float value)
        {
            var container = GetOrCreateContainer(c);
            SetPadding(container, left: value, right: value);
            return container;
        }

        /// <summary>Sets vertical padding (top and bottom).</summary>
        public IContentContainer PaddingVertical(float value)
        {
            var container = GetOrCreateContainer(c);
            SetPadding(container, top: value, bottom: value);
            return container;
        }

        /// <summary>Sets the background color.</summary>
        public IContentContainer Background(Color color)
        {
            var container = GetOrCreateContainer(c);
            container.BackgroundColor = color;
            return container;
        }

        /// <summary>Sets a uniform border on all sides.</summary>
        public IContentContainer Border(float width)
        {
            var container = GetOrCreateContainer(c);
            container.Border = new Border(width);
            container.BorderColors = new BorderColors(Color.Black);
            return container;
        }

        /// <summary>Sets a uniform border with a specific color.</summary>
        public IContentContainer Border(float width, Color color)
        {
            var container = GetOrCreateContainer(c);
            container.Border = new Border(width);
            container.BorderColors = new BorderColors(color);
            return container;
        }

        /// <summary>Sets a bottom border.</summary>
        public IContentContainer BorderBottom(float width, Color? color = null)
        {
            var container = GetOrCreateContainer(c);
            SetBorderSides(container, color ?? Color.Black, bottom: width);
            return container;
        }

        /// <summary>Sets a top border.</summary>
        public IContentContainer BorderTop(float width, Color? color = null)
        {
            var container = GetOrCreateContainer(c);
            SetBorderSides(container, color ?? Color.Black, top: width);
            return container;
        }

        /// <summary>Sets a left border.</summary>
        public IContentContainer BorderLeft(float width, Color? color = null)
        {
            var container = GetOrCreateContainer(c);
            SetBorderSides(container, color ?? Color.Black, left: width);
            return container;
        }

        /// <summary>Sets a right border.</summary>
        public IContentContainer BorderRight(float width, Color? color = null)
        {
            var container = GetOrCreateContainer(c);
            SetBorderSides(container, color ?? Color.Black, right: width);
            return container;
        }

        /// <summary>Sets horizontal borders (left and right), matching the PaddingHorizontal convention.</summary>
        public IContentContainer BorderHorizontal(float width, Color? color = null)
        {
            var container = GetOrCreateContainer(c);
            SetBorderSides(container, color ?? Color.Black, left: width, right: width);
            return container;
        }

        /// <summary>Sets vertical borders (top and bottom), matching the PaddingVertical convention.</summary>
        public IContentContainer BorderVertical(float width, Color? color = null)
        {
            var container = GetOrCreateContainer(c);
            SetBorderSides(container, color ?? Color.Black, top: width, bottom: width);
            return container;
        }

        /// <summary>Sets a fixed width.</summary>
        public IContentContainer Width(float value)
        {
            var container = GetOrCreateContainer(c);
            container.Width = value;
            return container;
        }

        /// <summary>Sets a fixed height.</summary>
        public IContentContainer Height(float value)
        {
            var container = GetOrCreateContainer(c);
            container.Height = value;
            return container;
        }

        /// <summary>
        /// Takes the body content height left on the current page instead of sizing
        /// to content - the middle band of a header / body / footer page.
        /// </summary>
        /// <remarks>
        /// A fixed <see cref="Height(IContentContainer, float)"/> wins outright, and
        /// fill never shrinks content below its natural height. See
        /// <see cref="Container.FillHeight"/>.
        /// </remarks>
        public IContentContainer FillHeight()
        {
            var container = GetOrCreateContainer(c);
            container.FillHeight = true;
            return container;
        }

        /// <summary>Sets a fixed width and height.</summary>
        public IContentContainer Size(float width, float height)
        {
            var container = GetOrCreateContainer(c);
            container.Width = width;
            container.Height = height;
            return container;
        }

        /// <summary>Sets the corner radius for rounded corners.</summary>
        public IContentContainer CornerRadius(float radius)
        {
            var container = GetOrCreateContainer(c);
            container.CornerRadius = radius;
            return container;
        }

        /// <summary>
        /// Rounds the container's corners <em>and</em> clips its content to that
        /// rounded outline. Unlike <see cref="CornerRadius"/> — which only
        /// rounds the background fill and border stroke — this masks the child too, so
        /// a full-bleed image or any overflowing content is cut to the rounded box.
        /// This is the idiomatic way to build rounded image cards and avatars.
        /// <example>
        /// <code>
        /// container.RoundedCorners(12).Image("avatar.png");
        /// container.RoundedCorners(6).Background(ColorPalette.Slate[100]);
        /// </code>
        /// </example>
        /// </summary>
        public IContentContainer RoundedCorners(float radius)
        {
            var container = GetOrCreateContainer(c);
            container.CornerRadius = radius;
            container.ClipContent = true;
            return container;
        }

        /// <summary>
        /// Renders the container's subtree inside a crisp-edge region: on SVG
        /// export, thin axis-aligned strokes/rects (grid/table lines, dividers) snap to
        /// the pixel grid instead of being antialiased. No-op on PDF output. Apply only
        /// to axis-aligned content — rounded shapes, diagonals, and text want
        /// antialiasing.
        /// </summary>
        public IContentContainer CrispEdges()
        {
            var container = GetOrCreateContainer(c);
            container.CrispEdges = true;
            return container;
        }

        /// <summary>Sets uniform margin on all sides.</summary>
        public IContentContainer Margin(float value)
        {
            var container = GetOrCreateContainer(c);
            container.Margin = new Thickness(value);
            return container;
        }

        /// <summary>Sets horizontal and vertical margin.</summary>
        public IContentContainer Margin(float horizontal, float vertical)
        {
            var container = GetOrCreateContainer(c);
            container.Margin = new Thickness(horizontal, vertical);
            return container;
        }

        /// <summary>Sets margin for each side individually.</summary>
        public IContentContainer Margin(float left, float top, float right, float bottom)
        {
            var container = GetOrCreateContainer(c);
            container.Margin = new Thickness(left, top, right, bottom);
            return container;
        }

        /// <summary>Sets top margin.</summary>
        public IContentContainer MarginTop(float value)
        {
            var container = GetOrCreateContainer(c);
            SetMargin(container, top: value);
            return container;
        }

        /// <summary>Sets bottom margin.</summary>
        public IContentContainer MarginBottom(float value)
        {
            var container = GetOrCreateContainer(c);
            SetMargin(container, bottom: value);
            return container;
        }

        /// <summary>Sets left margin.</summary>
        public IContentContainer MarginLeft(float value)
        {
            var container = GetOrCreateContainer(c);
            SetMargin(container, left: value);
            return container;
        }

        /// <summary>Sets right margin.</summary>
        public IContentContainer MarginRight(float value)
        {
            var container = GetOrCreateContainer(c);
            SetMargin(container, right: value);
            return container;
        }

        /// <summary>Sets horizontal margin (left and right).</summary>
        public IContentContainer MarginHorizontal(float value)
        {
            var container = GetOrCreateContainer(c);
            SetMargin(container, left: value, right: value);
            return container;
        }

        /// <summary>Sets vertical margin (top and bottom).</summary>
        public IContentContainer MarginVertical(float value)
        {
            var container = GetOrCreateContainer(c);
            SetMargin(container, top: value, bottom: value);
            return container;
        }

        /// <summary>Centers the content horizontally.</summary>
        public IContentContainer AlignCenter()
        {
            var container = GetOrCreateContainer(c);
            container.HorizontalAlignment = HorizontalAlignment.Center;
            return container;
        }

        /// <summary>Aligns content to the right.</summary>
        public IContentContainer AlignRight()
        {
            var container = GetOrCreateContainer(c);
            container.HorizontalAlignment = HorizontalAlignment.End;
            return container;
        }

        /// <summary>Aligns content to the left (default).</summary>
        public IContentContainer AlignLeft()
        {
            var container = GetOrCreateContainer(c);
            container.HorizontalAlignment = HorizontalAlignment.Start;
            return container;
        }
    }
}
