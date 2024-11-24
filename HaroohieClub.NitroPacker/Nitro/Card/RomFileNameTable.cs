using System.Collections.Generic;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.Nitro.Fs;

namespace HaroohieClub.NitroPacker.Nitro.Card;

/// <summary>
/// Representation of the file name table
/// </summary>
[XmlType("RomFNT")]
public class RomFileNameTable
{
    /// <summary>
    /// Constructs a blank file name table (used for serialization)
    /// </summary>
    public RomFileNameTable()
    {
        DirectoryTable = new[] { new DirectoryTableEntry { ParentId = 1 } };
        NameTable = new[] { new[] { NameTableEntry.EndOfDirectory() } };
    }

    /// <summary>
    /// Constructs a file name table using an extended endian binary reader
    /// </summary>
    /// <param name="er"><see cref="EndianBinaryReaderEx"/> with an initialized stream</param>
    public RomFileNameTable(EndianBinaryReaderEx er)
    {
        er.BeginChunk();
        {
            DirectoryTableEntry root = new(er);
            DirectoryTable = new DirectoryTableEntry[root.ParentId];
            DirectoryTable[0] = root;
            for (int i = 1; i < root.ParentId; i++)
                DirectoryTable[i] = new(er);

            NameTable = new NameTableEntry[root.ParentId][];
            for (int i = 0; i < root.ParentId; i++)
            {
                er.JumpRelative(DirectoryTable[i].EntryStart);
                var entries = new List<NameTableEntry>();

                NameTableEntry entry;
                do
                {
                    entry = new(er);
                    entries.Add(entry);
                } while (entry.Type != NameTableEntryType.EndOfDirectory);

                NameTable[i] = entries.ToArray();
            }
        }
        er.EndChunk();
    }

    /// <summary>
    /// Writes the file name table to a stream using an extended endian binary writer
    /// </summary>
    /// <param name="ew"><see cref="EndianBinaryWriterEx"/> with an initialized stream</param>
    public void Write(EndianBinaryWriterEx ew)
    {
        DirectoryTable[0].ParentId = (ushort)DirectoryTable.Length;
        ew.BeginChunk();
        {
            long dirTabAddr = ew.BaseStream.Position;
            ew.BaseStream.Position += DirectoryTable.Length * 8;
            for (int i = 0; i < DirectoryTable.Length; i++)
            {
                DirectoryTable[i].EntryStart = (uint)ew.GetCurposRelative();
                foreach (NameTableEntry entry in NameTable[i])
                    entry.Write(ew);
            }

            long curPos = ew.BaseStream.Position;
            ew.BaseStream.Position = dirTabAddr;
            foreach (DirectoryTableEntry entry in DirectoryTable)
                entry.Write(ew);
            ew.BaseStream.Position = curPos;
        }
        ew.EndChunk();
    }

    /// <summary>
    /// The directory table portion of the FNT
    /// </summary>
    public DirectoryTableEntry[] DirectoryTable { get; set; }
    /// <summary>
    /// The name table portion of the FNT
    /// </summary>
    public NameTableEntry[][] NameTable { get; set; }
}