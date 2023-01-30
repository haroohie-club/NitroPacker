using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HaroohiePals.IO;

namespace HaroohiePals.Nitro.Fs
{
    public class DirectoryTableEntry
    {
        public DirectoryTableEntry() { }

        public DirectoryTableEntry(EndianBinaryReaderEx er)
            => er.ReadObject(this);

        public void Write(EndianBinaryWriterEx er)
            => er.WriteObject(this);

        public uint   EntryStart;
        public ushort EntryFileId;
        public ushort ParentId;
    }
}