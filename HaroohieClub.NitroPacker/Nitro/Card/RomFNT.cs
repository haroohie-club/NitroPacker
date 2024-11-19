using System.Collections.Generic;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.Nitro.Fs;

namespace HaroohieClub.NitroPacker.Nitro.Card;

public class RomFNT
{
    public RomFNT()
    {
        DirectoryTable = new[] { new DirectoryTableEntry { ParentId = 1 } };
        NameTable = new[] { new[] { NameTableEntry.EndOfDirectory() } };
    }

    public RomFNT(EndianBinaryReaderEx er)
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

    public void Write(EndianBinaryWriterEx er)
    {
        DirectoryTable[0].ParentId = (ushort)DirectoryTable.Length;
        er.BeginChunk();
        {
            long dirTabAddr = er.BaseStream.Position;
            er.BaseStream.Position += DirectoryTable.Length * 8;
            for (int i = 0; i < DirectoryTable.Length; i++)
            {
                DirectoryTable[i].EntryStart = (uint)er.GetCurposRelative();
                foreach (NameTableEntry entry in NameTable[i])
                    entry.Write(er);
            }

            long curPos = er.BaseStream.Position;
            er.BaseStream.Position = dirTabAddr;
            foreach (DirectoryTableEntry entry in DirectoryTable)
                entry.Write(er);
            er.BaseStream.Position = curPos;
        }
        er.EndChunk();
    }

    public DirectoryTableEntry[] DirectoryTable { get; set; }
    public NameTableEntry[][] NameTable { get; set; }
}