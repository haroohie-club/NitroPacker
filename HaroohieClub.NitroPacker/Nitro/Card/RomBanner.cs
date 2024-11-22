using System;
using System.Data;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Card.Banners;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Representation of the ROM's icon/title/banner
/// </summary>
[Serializable]
public class RomBanner
{
    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public RomBanner() { }

    /// <summary>
    /// Constructs a banner using an extended endian binary reader
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryReaderEx"/> with initialized stream</param>
    public RomBanner(EndianBinaryReaderEx er)
    {
        Header = new(er);
        Banner = Header.Version switch
        {
            0x001 => new BannerV1(er),
            0x002 => new BannerV2(er),
            0x003 => new BannerV3(er),
            0x103 => new BannerV103(er),
            _ => throw new DataException("Unsupported banner version!"),
        };
    }

    /// <summary>
    /// Writes the banner using an extended endian binary writer
    /// </summary>
    /// <param name="ew"><see cref="EndianBinaryWriterEx"/> with an initialized stream</param>
    public void Write(EndianBinaryWriterEx ew)
    {
        Header.Crc16s = Banner is not null ? Banner.GetCrcs() : OldBanner.GetCrcs();
        Header.Write(ew);
        if (Banner is not null)
        {
            switch (Header.Version)
            {
                case 0x001:
                    ((BannerV1)Banner).Write(ew);
                    break;
                case 0x002:
                    ((BannerV2)Banner).Write(ew);
                    break;
                case 0x003:
                    ((BannerV3)Banner).Write(ew);
                    break;
                case 0x103:
                    ((BannerV103)Banner).Write(ew);
                    break;
            }
        }
        else
        {
            OldBanner.Write(ew);
        }
    }

    /// <summary>
    /// The header of the banner
    /// </summary>
    public Header Header { get; set; }
    
    /// <summary>
    /// The actual ROM banner
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public Banner Banner { get; set; }
    
    /// <summary>
    /// Property used for the old NitroPacker project file format
    /// </summary>
    [Ignore]
    [JsonIgnore]
    [XmlElement("Banner")]
    public BannerV1 OldBanner { get; set; }
}