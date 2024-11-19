using System;
using System.Buffers.Binary;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Card.Cryptography;
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
        er.Skip(0x800);
        AnimationBitmaps = er.Read<byte>(0x1000);
        AnimationPalettes = er.Read<byte>(0x100);
        AnimationSequences = er.Read<byte>(0x80);
    }

    public override void Write(EndianBinaryWriter ew)
    {
        base.Write(ew);
        ew.Skip(0x800);
        ew.Write(AnimationBitmaps);
        ew.Write(AnimationPalettes);
        ew.Write(AnimationSequences);
    }

    public override ushort[] GetCrcs()
    {
        ushort[] crcs = base.GetCrcs();

        byte[] data = new byte[0x1180];
        Array.Copy(AnimationBitmaps, data, AnimationBitmaps.Length);
        Array.Copy(AnimationPalettes, data, AnimationPalettes.Length);
        Array.Copy(AnimationSequences, data, AnimationSequences.Length);

        crcs[3] = Crc16.GetCrc16(data);
        return crcs;
    }

    /// <summary>
    /// Gets a series of 8 RGBA bitmaps representing the animation frames
    /// </summary>
    /// <returns>The animation frames as 8 RGBA8 bitmaps</returns>
    public Rgba8Bitmap[] GetAnimatedBitmapFrames()
    {
        var animationFrames = new Rgba8Bitmap[8];
        for (int i = 0; i < animationFrames.Length; i++)
        {
            GxUtil.DecodeChar(AnimationBitmaps.AsSpan()[(i * 0x200)..((i + 1) * 0x200)], AnimationPalettes.AsSpan()[(i * 0x20)..((i + 1) * 0x20)], ImageFormat.Pltt16, 32, 32, true);
        }

        return animationFrames;
    }

    /// <summary>
    /// Unpacks the icon animation sequence data into an array of objects
    /// </summary>
    /// <returns>An array of 64 icon animation sequence objects</returns>
    public IconAnimationSequence[] GetAnimationSequences()
    {
        var animationSequences = new IconAnimationSequence[64];
        
        for (int i = 0; i < animationSequences.Length; i++)
        {
            animationSequences[i] = 
                new(BinaryPrimitives.ReadUInt16LittleEndian(AnimationSequences.AsSpan()[(i * 2)..(i * 2 + 1)]));
        }

        return animationSequences;
    }
}