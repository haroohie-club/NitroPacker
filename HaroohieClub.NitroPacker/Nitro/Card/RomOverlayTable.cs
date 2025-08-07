using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Represents the ARM9/ARM7 overlay tables in the ROM
/// </summary>
[XmlType("RomOVT")]
public class RomOverlayTable
{
    /// <summary>
    /// Flags indicating properties of the overlay table
    /// </summary>
    [Flags]
    public enum OverlayTableFlag : byte
    {
        /// <summary>
        /// If set, this overlay is compressed with the BLZ algorithm
        /// </summary>
        Compressed = 1,
        /// <summary>
        /// If set, 
        /// </summary>
        AuthenticationCode = 2,
    }

    /// <summary>
    /// Blank constructor, used for serialization
    /// </summary>
    public RomOverlayTable() { }

    /// <summary>
    /// Constructs an overlay table from a stream using an extended endian binary reader
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryReaderEx"/> initialized with a stream</param>
    public RomOverlayTable(EndianBinaryReaderEx er)
    {
        er.ReadObject(this);
        uint tmp = er.Read<uint>();
        Compressed = tmp & 0xFFFFFF;
        Flag = (OverlayTableFlag)(tmp >> 24);
    }

    /// <summary>
    /// Writes the overlay table to a stream using an extended endian binary writer
    /// </summary>
    /// <param name="ew"><see cref="EndianBinaryWriterEx"/> initialized with a stream</param>
    public void Write(EndianBinaryWriterEx ew)
    {
        ew.WriteObject(this);
        ew.Write(((uint)Flag & 0xFF) << 24 | Compressed & 0xFFFFFF);
    }

    /// <summary>
    /// The ID of the overlay
    /// </summary>
    [XmlAttribute("Id")]
    public uint Id { get; set; }
    /// <summary>
    /// The address at which the game loads the overlay
    /// </summary>
    public uint RamAddress { get; set; }
    /// <summary>
    /// The amount of data from the overlay to load into RAM
    /// </summary>
    public uint RamSize { get; set; }
    /// <summary>
    /// Size of the BSS data region
    /// </summary>
    public uint BssSize { get; set; }
    /// <summary>
    /// Static initializer start address
    /// </summary>
    [XmlElement("SinitInit")]
    public uint StaticInitializerStartAddress { get; set; }
    /// <summary>
    /// Static initializer end address
    /// </summary>
    [XmlElement("SinitInitEnd")]
    public uint StaticInitializerEndAddress { get; set; }

    /// <summary>
    /// The file ID of the overlay (corresponds to its location in the file name table)
    /// </summary>
    [JsonIgnore]
    public uint FileId { get; set; }

    /// <summary>
    /// Indicates whether this overlay is compressed
    /// </summary>
    [Ignore]
    public uint Compressed { get; set; } //:24;

    /// <summary>
    /// A set of flags for this overlay
    /// </summary>
    [Ignore]
    [XmlAttribute("Flag")]
    public OverlayTableFlag Flag { get; set; } // :8;
}