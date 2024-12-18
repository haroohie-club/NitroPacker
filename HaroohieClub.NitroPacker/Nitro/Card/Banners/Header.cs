using System;
using System.Text.Json.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Card.Banners;

/// <summary>
/// A banner's header
/// </summary>
public class Header
{
    /// <summary>
    /// Blank constructor for serialization
    /// </summary>
    public Header() { }

    /// <summary>
    /// Constructs a banner header with an extended endian binary reader
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryReaderEx"/> with an initialized stream</param>
    public Header(EndianBinaryReaderEx er)
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
    [Ignore]
    [Obsolete("ReservedA is deprecated; please use the Version instead.")]
    public byte ReservedA
    {
        get => BitConverter.GetBytes(Version)[1];
        set => Version = (ushort)((value << 8) | Version);
    }

    /// <summary>
    /// The set of CRC-16 hashes for this banner
    /// </summary>
    [JsonIgnore]
    [ArraySize(4)]
    public ushort[] Crc16s { get; set; }

    private byte[] _reservedB;
    /// <summary>
    /// Reserved
    /// </summary>
    [ArraySize(0x16)]
    public byte[] ReservedB
    {
        get => _reservedB;
        set
        {
            if (value.Length == 28)
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