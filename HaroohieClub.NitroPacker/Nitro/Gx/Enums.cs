using System.ComponentModel;

namespace HaroohieClub.NitroPacker.Nitro.Gx;

/// <summary>
/// Enum describing various image formats
/// </summary>
public enum ImageFormat : uint
{
    /// <summary>
    /// No image format
    /// </summary>
    [Description("None")]
    None = 0,

    /// <summary>
    /// A3I5 (3-bit alpha, 5-bit intensity)
    /// </summary>
    [Description("A3I5")]
    A3I5 = 1,

    /// <summary>
    /// 4-color palette image, a.k.a. 2bpp
    /// </summary>
    [Description("Palette 4")]
    Pltt4 = 2,

    /// <summary>
    /// 16-color palette image, a.k.a. 4bpp
    /// </summary>
    [Description("Palette 16")]
    Pltt16 = 3,

    /// <summary>
    /// 256-color palette image, a.k.a. 8bpp
    /// </summary>
    [Description("Palette 256")]
    Pltt256 = 4,

    /// <summary>
    /// 4x4 compressed image (DXT)
    /// </summary>
    [Description("4x4")]
    Comp4x4 = 5,

    /// <summary>
    /// A5I3 (5-bit alpha, 3-bit intensity)
    /// </summary>
    [Description("A5I3")]
    A5I3 = 6,

    /// <summary>
    /// Direct, non-paletted image
    /// </summary>
    [Description("Direct")]
    Direct = 7,
}

/// <summary>
/// Char format
/// </summary>
public enum CharFormat : uint
{
    /// <summary>
    /// Char
    /// </summary>
    Char,
    /// <summary>
    /// Bmp
    /// </summary>
    Bmp,
}

/// <summary>
/// Map format
/// </summary>
public enum MapFormat : uint
{
    /// <summary>
    /// Text
    /// </summary>
    Text,
    /// <summary>
    /// Affine
    /// </summary>
    Affine,
}