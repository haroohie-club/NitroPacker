using System;
using System.Data;
using System.Text;
using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

public enum NameTableEntryType
{
    EndOfDirectory,
    File,
    Directory,
}

public class NameTableEntry
{
    public NameTableEntryType Type { get; set; }
    public string Name { get; set; }
    public ushort DirectoryId { get; set; }

    public NameTableEntry()
    {
    }
    
    private NameTableEntry(NameTableEntryType type, string name = null, ushort directoryId = 0)
    {
        Type = type;
        if (type != NameTableEntryType.EndOfDirectory)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (name.Length == 0 || name.Length > 0x7F)
                throw new ArgumentException("Name length invalid", nameof(name));
            Name = name;
        }

        if (type == NameTableEntryType.Directory)
        {
            if (directoryId < 0xF000)
                throw new ArgumentException("Directory ID invalid", nameof(directoryId));
            DirectoryId = directoryId;
        }
    }

    public NameTableEntry(EndianBinaryReader er)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte length = er.Read<byte>();
        if (length == 0)
            Type = NameTableEntryType.EndOfDirectory;
        else if ((length & 0x80) != 0)
        {
            Type = NameTableEntryType.Directory;
            Name = er.ReadString(Encoding.GetEncoding("Shift-JIS"), length & ~0x80);
            DirectoryId = er.Read<ushort>();
        }
        else
        {
            Type = NameTableEntryType.File;
            Name = er.ReadString(Encoding.GetEncoding("Shift-JIS"), length);
        }
    }

    public void Write(EndianBinaryWriter er)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        switch (Type)
        {
            case NameTableEntryType.EndOfDirectory:
                er.Write((byte)0);
                break;
            case NameTableEntryType.File:
                if (Name.Length > 0x7F)
                    throw new DataException($"File name '{Name}' too long");
                er.Write((byte)Name.Length);
                er.Write(Name, Encoding.GetEncoding("Shift-JIS"), false);
                break;
            case NameTableEntryType.Directory:
                if (Name.Length > 0x7F)
                    throw new DataException($"Directory name '{Name}' too long");
                er.Write((byte)(Name.Length | 0x80));
                er.Write(Name, Encoding.GetEncoding("Shift-JIS"), false);
                er.Write(DirectoryId);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static NameTableEntry EndOfDirectory() => new(NameTableEntryType.EndOfDirectory);
    public static NameTableEntry File(string name) => new(NameTableEntryType.File, name);

    public static NameTableEntry Directory(string name, ushort directoryId) =>
        new(NameTableEntryType.Directory, name, directoryId);
}