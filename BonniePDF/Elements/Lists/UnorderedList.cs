using System.Collections.Generic;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Lists;

/// <summary>
/// An unordered list element that displays bulleted items with various bullet styles
/// </summary>
public class UnorderedList : ListElementBase
{
    private readonly List<string> _items = [];
    private readonly List<UnorderedList> _nestedLists = [];
    private Color? _bulletColor;

    /// <summary>
    /// Bullet style for the list (Disc, Circle, Square, Dash, Custom)
    /// </summary>
    public BulletStyle BulletStyle { get; set; } = BulletStyle.Disc;

    /// <summary>
    /// Custom bullet character (only used when BulletStyle is Custom)
    /// </summary>
    public string CustomBullet { get; set; } = "•";

    /// <summary>
    /// Color for the bullet character
    /// </summary>
    public Color BulletColor
    {
        get => _bulletColor ?? TextColor;
        set => _bulletColor = value;
    }

    /// <summary>
    /// The items in this list (exposed for collection initializer syntax)
    /// </summary>
    public IList<string> Items => _items;

    private string BulletChar => GetBulletCharacter();

    /// <summary>
    /// Adds a text item to this list
    /// </summary>
    public void AddItem(string item)
    {
        _items.Add(item ?? string.Empty);
    }

    /// <summary>
    /// Adds a nested list to this list
    /// </summary>
    public void AddNestedList(UnorderedList nestedList)
    {
        nestedList.Indent = Indent;
        _nestedLists.Add(nestedList);
    }

    public override IReadOnlyList<ListElementBase> GetNestedLists()
    {
        return _nestedLists;
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        return MeasureListHeight(_items, _nestedLists, contentWidth);
    }

    protected override void RenderItems(RenderContext ctx, float contentWidth)
    {
        float itemWidth = contentWidth - Indent;

        float localY = ctx.CurrentY;

        // Render regular items
        foreach (string item in _items)
        {
            IList<string> lines = ComputeLines(item, itemWidth);
            float bulletWidth = MeasureTextWidth(BulletChar);

            // Render each line with the bullet
            RenderListItem(
                ctx,
                lines,
                ref localY,
                (x, y) =>
                {
                    // Draw bullet
                    ctx.Canvas.DrawText(x, y, BulletChar, BulletColor, FontSize, FontName, TextAlignment.Left);
                },
                bulletWidth
            );
        }

        // Update Y position
        ctx.CurrentY = localY;

        RenderNestedLists(ctx, _nestedLists, contentWidth);
    }

    private string GetBulletCharacter()
    {
        return BulletStyle switch
        {
            BulletStyle.Disc => "•",
            BulletStyle.Circle => "◦",
            BulletStyle.Square => "■",
            BulletStyle.Dash => "-",
            BulletStyle.Custom => CustomBullet,
            _ => "•",
        };
    }
}
