using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HaroohiePals.IO;

namespace HaroohiePals.Nitro.Fs
{
    public class FatEntry
    {
        public FatEntry(uint offset, uint size)
        {
            FileTop    = offset;
            FileBottom = offset + size;
        }

        public FatEntry(EndianBinaryReaderEx er)
            => er.ReadObject(this);

        public void Write(EndianBinaryWriterEx er)
            => er.WriteObject(this);

        public uint FileTop;
        public uint FileBottom;

        public uint FileSize => FileBottom - FileTop;
    }
}