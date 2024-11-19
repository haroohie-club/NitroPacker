using System;
using System.Text;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.Nitro.Card.Cryptography;

namespace HaroohieClub.NitroPacker.Nitro.Card.Banners;

/// <summary>
/// An implementation of banner version 0x003
/// </summary>
public class BannerV3 : BannerV2
{
    /// <inheritdoc />
    public BannerV3()
    {
    }

    /// <inheritdoc />
    public BannerV3(EndianBinaryReader er)
    {
        Image = er.Read<byte>(32 * 32 / 2);
        Palette = er.Read<byte>(16 * 2);
        GameName = new string[8];
        for (int i = 0; i < GameName.Length; i++)
        {
            GameName[i] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
        }
    }

    /// <inheritdoc />
    public override ushort[] GetCrcs()
    {
        ushort[] crcs = base.GetCrcs();
        
        byte[] data = new byte[0xA20];
        Array.Copy(Image, data, 512);
        Array.Copy(Palette, 0, data, 512, 32);
        for (int i = 0; i < 8; i++)
        {
            Array.Copy(Encoding.Unicode.GetBytes(GameName[i].PadRight(128, '\0')), 0, data, 544 + 256 * i, 256);
        }

        crcs[2] = Crc16.GetCrc16(data);
        return crcs;
    }
}