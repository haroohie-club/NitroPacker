using System;

namespace HaroohieClub.NitroPacker.Nitro.Gx;

/// <summary>
/// A class representing the Nitro color format
/// </summary>
public class ColorFormat
{
    /// <summary>
    /// Shifts and sizes for the various color components
    /// </summary>
    public readonly int AShift, ASize, RShift, RSize, GShift, GSize, BShift, BSize;

    /// <summary>
    /// Constructs a color format from the various shifts and sizes for each component
    /// </summary>
    /// <param name="aShift">Alpha shit</param>
    /// <param name="aSize">Alpha size</param>
    /// <param name="rShift">Red shift</param>
    /// <param name="rSize">Red size</param>
    /// <param name="gShift">Green shift</param>
    /// <param name="gSize">Green size</param>
    /// <param name="bShift">Blue shift</param>
    /// <param name="bSize">Blue size</param>
    public ColorFormat(int aShift, int aSize, int rShift, int rSize, int gShift, int gSize, int bShift, int bSize)
    {
        AShift = aShift;
        ASize = aSize;
        RShift = rShift;
        RSize = rSize;
        GShift = gShift;
        GSize = gSize;
        BShift = bShift;
        BSize = bSize;
    }

    /// <summary>
    /// Number of bytes
    /// </summary>
    public int NrBytes => (int)Math.Ceiling((ASize + RSize + GSize + BSize) / 8f);

    //The naming is based on the bit order when read out in the correct endianness
    /// <summary>
    /// 32-bit ARBG color
    /// </summary>
    /// 
    public static readonly ColorFormat ARGB8888 = new(24, 8, 16, 8, 8, 8, 0, 8);
    /// <summary>
    /// 15-bit ARBG color
    /// </summary>
    public static readonly ColorFormat ARGB3444 = new(12, 3, 8, 4, 4, 4, 0, 4);
    
    /// <summary>
    /// 32-bit RGBA color
    /// </summary>
    public static readonly ColorFormat RGBA8888 = new(0, 8, 24, 8, 16, 8, 8, 8);

    /// <summary>
    /// 16-bit RGBA color
    /// </summary>
    public static readonly ColorFormat RGBA4444 = new(0, 4, 12, 4, 8, 4, 4, 4);

    /// <summary>
    /// 24-bit RGB color
    /// </summary>
    public static readonly ColorFormat RGB888 = new(0, 0, 16, 8, 8, 8, 0, 8);

    /// <summary>
    /// 16-bit RGB color
    /// </summary>
    public static readonly ColorFormat RGB565 = new(0, 0, 11, 5, 5, 6, 0, 5);

    /// <summary>
    /// 16-bit ARGB color
    /// </summary>
    public static readonly ColorFormat ARGB1555 = new(15, 1, 10, 5, 5, 5, 0, 5);
    /// <summary>
    /// 16-bit XRGB color
    /// </summary>
    public static readonly ColorFormat XRGB1555 = new(0, 0, 10, 5, 5, 5, 0, 5);

    /// <summary>
    /// 16-bit ABGR color
    /// </summary>
    public static readonly ColorFormat ABGR1555 = new(15, 1, 0, 5, 5, 5, 10, 5);
    /// <summary>
    /// 16-bit XBGR color
    /// </summary>
    public static readonly ColorFormat XBGR1555 = new(0, 0, 0, 5, 5, 5, 10, 5);

    /// <summary>
    /// 16-bit RGBA color
    /// </summary>
    public static readonly ColorFormat RGBA5551 = new(0, 1, 11, 5, 6, 5, 1, 5);
}