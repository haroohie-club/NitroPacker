using System;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Gx;

namespace HaroohieClub.NitroPacker.Nitro.Card.Banners;

/// <summary>
/// Abstract class for banners used in the ROM header
/// </summary>
public abstract class Banner
{
    /// <summary>
    /// Constructs an empty banner, used for serialization
    /// </summary>
    public Banner() { }

    /// <summary>
    /// Constructs a banner using an endian binary reader
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryReader"/> with initialized stream</param>
    public Banner(EndianBinaryReader er)
    {
    }

    /// <summary>
    /// Writes the banner using an endian binary writer
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryWriter"/> with initialized stream</param>
    public virtual void Write(EndianBinaryWriter er)
    {
        er.Write(Image, 0, 32 * 32 / 2);
        er.Write(Palette, 0, 16 * 2);
        foreach (string s in GameName)
        {
            er.Write(GameName[0].PadRight(128, '\0'), Encoding.Unicode, false);
        }
    }

    /// <summary>
    /// The image contained in the banner
    /// </summary>
    [ArraySize(32 * 32 / 2)]
    public byte[] Image { get; set; }

    /// <summary>
    /// The palette for the banner's <see cref="Image"/>
    /// </summary>
    [ArraySize(16 * 2)]
    [XmlAttribute("Pltt")]
    public byte[] Palette { get; set; }

    /// <summary>
    /// The array of game titles that appear in the banner depending on region
    /// The languages are:
    /// 0: Japanese
    /// 1: English
    /// 2: French
    /// 3: German
    /// 4: Italian
    /// 5: Spanish
    /// 6: Chinese (<see cref="BannerV2"/> and up)
    /// 7: Korean (v0x003 and up)
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public string[] GameName { get; set; }

    /// <summary>
    /// Base 64 encoded versions of <see cref="GameName"/>
    /// </summary>
    [XmlElement("GameName")]
    [JsonPropertyName("GameNames")]
    public virtual string[] Base64GameName
    {
        get
        {
            string[] b = new string[GameName.Length];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = Convert.ToBase64String(Encoding.Unicode.GetBytes(GameName[i]));
            }

            return b;
        }
        set
        {
            GameName = new string[value.Length];
            for (int i = 0; i < GameName.Length; i++)
            {
                GameName[i] = Encoding.Unicode.GetString(Convert.FromBase64String(value[i]));
            }
        }
    }

    /// <summary>
    /// Gets the CRC16 hashes associated with this banner
    /// </summary>
    /// <returns>An array of up to four CRC16 hashes</returns>
    public virtual ushort[] GetCrcs()
    {
        byte[] data = new byte[0x820];
        Array.Copy(Image, data, 512);
        Array.Copy(Palette, 0, data, 512, 32);
        for (int i = 0; i < GameName.Length; i++)
        {
            Array.Copy(Encoding.Unicode.GetBytes(GameName[i].PadRight(128, '\0')), 0, data, 544 + 256 * i, 256);
        }
        return [Crc16.GetCrc16(data), 0, 0, 0];
    }

    public Rgba8Bitmap GetIcon() => GxUtil.DecodeChar(Image, Palette, ImageFormat.Pltt16, 32, 32, true);
}