using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HaroohiePals.IO;

namespace HaroohiePals.Nitro.Fs
{
    public enum NameTableEntryType
    {
        EndOfDirectory,
        File,
        Directory
    }

    public class NameTableEntry
    {
        private NameTableEntry(NameTableEntryType type, string name = null, ushort directoryId = 0)
        {
            Type = type;
            if (type != NameTableEntryType.EndOfDirectory)
            {
                if (name == null)
                    throw new ArgumentNullException(nameof(name));
                if (name.Length == 0 || name.Length > 0x7F)
                    throw new ArgumentException(nameof(name));
                Name = name;
            }

            if (type == NameTableEntryType.Directory)
            {
                if (directoryId < 0xF000)
                    throw new ArgumentException(nameof(directoryId));
                DirectoryId = directoryId;
            }
        }

        public NameTableEntry(EndianBinaryReader er)
        {
            byte length = er.Read<byte>();
            if (length == 0)
                Type = NameTableEntryType.EndOfDirectory;
            else if ((length & 0x80) != 0)
            {
                Type        = NameTableEntryType.Directory;
                Name        = er.ReadString(Encoding.ASCII, length & ~0x80);
                DirectoryId = er.Read<ushort>();
            }
            else
            {
                Type = NameTableEntryType.File;
                Name = er.ReadString(Encoding.ASCII, length);
            }
        }

        public void Write(EndianBinaryWriter er)
        {
            switch (Type)
            {
                case NameTableEntryType.EndOfDirectory:
                    er.Write((byte)0);
                    break;
                case NameTableEntryType.File:
                    if (Name.Length > 0x7F)
                        throw new Exception("File name too long");
                    er.Write((byte)Name.Length);
                    er.Write(Name, Encoding.ASCII, false);
                    break;
                case NameTableEntryType.Directory:
                    if (Name.Length > 0x7F)
                        throw new Exception("Directory name too long");
                    er.Write((byte)(Name.Length | 0x80));
                    er.Write(Name, Encoding.ASCII, false);
                    er.Write(DirectoryId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public NameTableEntryType Type;
        public string             Name;
        public ushort             DirectoryId;

        public static NameTableEntry EndOfDirectory() => new(NameTableEntryType.EndOfDirectory);
        public static NameTableEntry File(string name) => new(NameTableEntryType.File, name);

        public static NameTableEntry Directory(string name, ushort directoryId) =>
            new(NameTableEntryType.Directory, name, directoryId);
    }
}