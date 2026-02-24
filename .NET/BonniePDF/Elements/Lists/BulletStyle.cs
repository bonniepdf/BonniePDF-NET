namespace BonniePDF.Models;

/// <summary>
/// Bullet style options for unordered lists
/// </summary>
public enum BulletStyle
{
    /// <summary>Standard disc bullet: •</summary>
    Disc,

    /// <summary>Hollow circle bullet: ◦</summary>
    Circle,

    /// <summary>Square bullet: ■</summary>
    Square,

    /// <summary>Dash bullet: -</summary>
    Dash,

    /// <summary>Custom bullet character</summary>
    Custom
}