using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Footer of a DS ROM file
/// </summary>
public class NitroFooter
{
    /// <summary>
    /// Constructs an empty footer, used for serialization
    /// </summary>
    public NitroFooter() { }

    /// <summary>
    /// Constructs a DS ROM footer from an extended endian binary reader
    /// </summary>
    /// <param name="er">An EndianBinaryReaderEx with an initialized stream</param>
    public NitroFooter(EndianBinaryReaderEx er) => er.ReadObject(this);
    /// <summary>
    /// Writes the DS ROM footer to a stream using an extended endian binary writer
    /// </summary>
    /// <param name="er">An EndianBinaryWriterEx with an initialized stream</param>
    public void Write(EndianBinaryWriterEx er) => er.WriteObject(this);

    /// <summary>
    /// The Nitro Code at the ROM footer
    /// </summary>
    public uint NitroCode { get; set; }
    /// <summary>
    /// The start offset for module params
    /// </summary>
    [XmlElement("_start_ModuleParamsOffset")]
    public uint StartModuleParamsOffset { get; set; }
    /// <summary>
    /// Unknown
    /// </summary>
    public uint Unknown { get; set; }
}