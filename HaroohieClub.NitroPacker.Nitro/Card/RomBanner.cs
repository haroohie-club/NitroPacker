using System;
using System.Data;
using System.Text.Json.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Card.Banners;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Representation of the ROM's icon/title/banner
/// </summary>
public class RomBanner
{
    /// <summary>
    /// Empty constructor for serialization
    /// </summary>
    public RomBanner() { }

    /// <summary>
    /// Constructs a banner with an extended endian binary reader
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
    /// Writes the banner with an extended endian binary writer
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryWriterEx"/> with an initialized stream</param>
    public void Write(EndianBinaryWriterEx er)
    {
        Header.Crc16s = Banner.GetCrcs();
        Header.Write(er);
        Banner.Write(er);
    }

    /// <summary>
    /// The header of the banner
    /// </summary>
    public BannerHeader Header { get; set; }

    /// <summary>
    /// A banner's header
    /// </summary>
    public class BannerHeader
    {
        /// <summary>
        /// Blank constructor for serialization
        /// </summary>
        public BannerHeader() { }

        /// <summary>
        /// Constructs a banner header with an extended endian binary reader
        /// </summary>
        /// <param name="er"><see cref="EndianBinaryReaderEx"/> with an initialized stream</param>
        public BannerHeader(EndianBinaryReaderEx er)
        {
            er.ReadObject(this);
        }

        /// <summary>
        /// Writes a banner with an extended endian binary writer
        /// </summary>
        /// <param name="er"><see cref="EndianBinaryWriterEx"/> with an initialized stream</param>
        public void Write(EndianBinaryWriterEx er)
        {
            er.WriteObject(this);
        }

        /// <summary>
        /// The version of the banner (0x0001, 0x0002, 0x0003, or 0x0103)
        /// </summary>
        public ushort Version { get; set; }

        /// <summary>
        /// Deprecated. Formerly thought to be reserved but is actually part of the <see cref="Version"/>
        /// </summary>
        [JsonIgnore]
        [Obsolete("ReservedA is deprecated; please use the Version instead.")]
        public byte ReservedA
        {
            get => BitConverter.GetBytes(Version)[1];
            set => Version = (ushort)((value << 8) | Version);
        }

        [JsonIgnore]
        internal ushort[] Crc16s { get; set; }

        private byte[] _reservedB;
        /// <summary>
        /// Reserved
        /// </summary>
        // [ArraySize(22)]
        public byte[] ReservedB
        {
            get => _reservedB;
            set
            {
                if (value.Length == 0x28)
                {
                    // We don't need to set the CRCs here since they're ignored by serialization anyway
                    _reservedB = value[6..];
                }
                else
                {
                    _reservedB = value;
                }
            }
        }
    }
    
    /// <summary>
    /// The actual ROM banner
    /// </summary>
    public Banner Banner { get; set; }
}