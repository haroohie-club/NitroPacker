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
    /// The version of this banner (0x001, 0x002, 0x003, 0x103)
    /// </summary>
    public int Version { get; set; }
    
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
    /// <param name="ew"><see cref="EndianBinaryWriter"/> with initialized stream</param>
    public virtual void Write(EndianBinaryWriter ew)
    {
        ew.Write(Image, 0, 32 * 32 / 2);
        ew.Write(Palette, 0, 16 * 2);
        foreach (string s in GameName)
        {
            ew.Write(s.PadRight(128, '\0'), Encoding.Unicode, false);
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
    [XmlElement("Pltt")]
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
        return new ushort[4];
    }

    /// <summary>
    /// Gets an RGBA bitmap representation of the icon
    /// </summary>
    /// <returns>An RGBA8 Bitmap representing the icon</returns>
    public Rgba8Bitmap GetIcon() => GxUtil.DecodeChar(Image, Palette, ImageFormat.Pltt16, 32, 32, true);
}