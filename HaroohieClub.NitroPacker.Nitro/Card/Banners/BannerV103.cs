using System;
using System.Text;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Gx;

namespace HaroohieClub.NitroPacker.Nitro.Card.Banners;

/// <summary>
/// An implementation of banner version 0x0103 (used in DSi games)
/// </summary>
public class BannerV103 : BannerV3
{
    /// <summary>
    /// The individual frames that make up the animated image in the banner
    /// </summary>
    [ArraySize(32 * 32 / 2 * 8)]
    public byte[] AnimationBitmaps { get; set; }

    /// <summary>
    /// The palettes for each of the banner's <see cref="AnimationBitmaps"/>
    /// </summary>
    [ArraySize(16 * 2 * 8)]
    public byte[] AnimationPalettes { get; set; }
    
    /// <summary>
    /// The tokens that define the animation sequence frames
    /// </summary>
    [ArraySize(64 * 2)]
    public byte[] AnimationSequences { get; set; }
    
    /// <inheritdoc />
    public BannerV103()
    {
    }

    /// <inheritdoc />
    public BannerV103(EndianBinaryReader er) : base(er)
    {
        
    }

    public Rgba8Bitmap[] GetAnimatedBitmaps()
    {
        var animationFrames = new Rgba8Bitmap[8];
        for (int i = 0; i < animationFrames.Length; i++)
        {
            GxUtil.DecodeChar(AnimationBitmaps.AsSpan()[(i * 0x200)..((i + 1) * 0x200)], AnimationPalettes.AsSpan()[(i * 0x20)..((i + 1) * 0x20)], ImageFormat.Pltt16, 32, 32, true);
        }

        return animationFrames;
    }
}