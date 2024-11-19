namespace HaroohieClub.NitroPacker.Nitro.Gx;

/// <summary>
/// 16-bit animation sequence token for a v0x103 banner icon
/// </summary>
public class IconAnimationSequence
{
    /// <summary>
    /// The duration of the current frame in 60Hz units (0-255)
    /// </summary>
    public byte FrameDuration { get; set; }
    /// <summary>
    /// The index of the animation bitmap to use (0-7)
    /// </summary>
    public byte BitmapIndex { get; set; }
    /// <summary>
    /// The index of the animation palette to use (0-7)
    /// </summary>
    public byte PaletteIndex { get; set; }
    /// <summary>
    /// If true, the animation bitmap will be flipped horizontally
    /// </summary>
    public bool FlipHorizontally { get; set; }
    /// <summary>
    /// If true, the animation bitmap will be flipped vertically
    /// </summary>
    public bool FlipVertically { get; set; }

    /// <summary>
    /// Packed animation
    /// </summary>
    /// <param name="packed"></param>
    public IconAnimationSequence(ushort packed)
    {
        
    }
    
    /// <summary>
    /// Pack the animation sequence into a ushort
    /// </summary>
    /// <returns>An unsigned short represenation of the animation sequence</returns>
    public ushort Pack()
    {
        return (ushort)(FrameDuration | ((BitmapIndex & 0b111) << 8) | ((PaletteIndex & 0b111) << 11)
                        | (FlipHorizontally ? 0b0100_0000_0000_0000 : 0) 
                        | (FlipVertically ? 0b10000_0000_0000_0000 : 0));
    }
}