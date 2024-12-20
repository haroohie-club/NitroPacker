﻿using HaroohieClub.NitroPacker.IO;

namespace HaroohieClub.NitroPacker.Nitro;

internal class BinaryBlockHeader
{
    public BinaryBlockHeader(uint signature)
    {
        Kind = signature;
    }

    public BinaryBlockHeader(EndianBinaryReader er)
    {
        Kind = er.Read<uint>();
        Size = er.Read<uint>();
    }

    public void Write(EndianBinaryWriterEx er)
    {
        er.Write(Kind);
        er.Write(Size);
    }

    public uint Kind;
    public uint Size;
}