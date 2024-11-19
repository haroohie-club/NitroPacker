using System.Text;
using HaroohieClub.NitroPacker.IO;

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
}