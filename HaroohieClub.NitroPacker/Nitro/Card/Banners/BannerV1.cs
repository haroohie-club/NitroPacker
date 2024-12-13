using System;
using System.Text;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.Nitro.Card.Cryptography;

namespace HaroohieClub.NitroPacker.Nitro.Card.Banners;

/// <summary>
/// An implementation of banner version 0x001
/// </summary>
public class BannerV1 : Banner
{
    /// <inheritdoc/>
    public BannerV1()
    {
    }
    
    /// <inheritdoc/>
    public BannerV1(EndianBinaryReader er) : base(er)
    {
        Image = er.Read<byte>(32 * 32 / 2);
        Palette = er.Read<byte>(16 * 2);
        GameName = new string[6];
        for (int i = 0; i < GameName.Length; i++)
        {
            GameName[i] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
        }
    }

    /// <inheritdoc />
    public override ushort[] GetCrcs()
    {
        byte[] data = new byte[0x820];
        Array.Copy(Image, data, 512);
        Array.Copy(Palette, 0, data, 512, 32);
        for (int i = 0; i < 6; i++)
        {
            Array.Copy(Encoding.Unicode.GetBytes(GameName[i].PadRight(128, '\0')), 0, data, 544 + 256 * i, 256);
        }
        return [Crc16.GetCrc16(data), 0, 0, 0];
    }
}