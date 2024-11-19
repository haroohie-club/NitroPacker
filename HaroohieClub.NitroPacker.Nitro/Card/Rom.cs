using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.IO.Serialization;
using HaroohieClub.NitroPacker.Nitro.Fs;
using HaroohieClub.NitroPacker.Nitro.Gx;

namespace HaroohieClub.NitroPacker.Nitro.Card;

public class Rom
{
    public Rom() { }

    public Rom(byte[] data)
        : this(new MemoryStream(data)) { }

    public Rom(Stream stream)
    {
        using (var er = new EndianBinaryReaderEx(stream, Endianness.LittleEndian))
        {
            Header = new RomHeader(er);

            if (er.BaseStream.Length >= 0x4000)
            {
                er.BaseStream.Position = 0x1000;

                KeyPadding0 = er.Read<byte>(0x600);
                PTable = er.Read<uint>(Blowfish.PTableEntryCount);
                KeyPadding1 = er.Read<byte>(0x5B8);

                SBoxes = new uint[Blowfish.SBoxCount][];
                for (int i = 0; i < Blowfish.SBoxCount; i++)
                    SBoxes[i] = er.Read<uint>(Blowfish.SBoxEntryCount);

                KeyPadding2 = er.Read<byte>(0x400);
            }

            er.BaseStream.Position = Header.MainRomOffset;
            MainRom = er.Read<byte>((int)Header.MainSize);
            if (er.Read<uint>() == 0xDEC00621) //Nitro Footer
            {
                er.BaseStream.Position -= 4;
                StaticFooter = new NitroFooter(er);
            }

            er.BaseStream.Position = Header.SubRomOffset;
            SubRom = er.Read<byte>((int)Header.SubSize);

            er.BaseStream.Position = Header.FntOffset;
            Fnt = new RomFNT(er);

            er.BaseStream.Position = Header.MainOvtOffset;
            MainOvt = new RomOVT[Header.MainOvtSize / 32];
            for (int i = 0; i < Header.MainOvtSize / 32; i++) MainOvt[i] = new RomOVT(er);

            er.BaseStream.Position = Header.SubOvtOffset;
            SubOvt = new RomOVT[Header.SubOvtSize / 32];
            for (int i = 0; i < Header.SubOvtSize / 32; i++) SubOvt[i] = new RomOVT(er);

            er.BaseStream.Position = Header.FatOffset;
            Fat = new FatEntry[Header.FatSize / 8];
            for (int i = 0; i < Header.FatSize / 8; i++)
                Fat[i] = new FatEntry(er);

            if (Header.BannerOffset != 0)
            {
                er.BaseStream.Position = Header.BannerOffset;
                Banner = new RomBanner(er);
            }

            byte[][] fileData = new byte[Header.FatSize / 8][];
            for (int i = 0; i < Header.FatSize / 8; i++)
            {
                er.BaseStream.Position = Fat[i].FileTop;
                fileData[i] = er.Read<byte>((int)Fat[i].FileSize);
            }

            FileData = fileData.Select(t => new NameFatWithData(t)).ToArray();

            //RSA Signature
            if (Header.RomSize + 0x88 <= er.BaseStream.Length)
            {
                er.BaseStream.Position = Header.RomSize;
                byte[] rsaSig = er.Read<byte>(0x88);
                if (rsaSig[0] == 'a' && rsaSig[1] == 'c')
                    RSASignature = rsaSig;
            }
        }
    }

    public byte[] Write(bool trimmed = true)
    {
        using (var m = new MemoryStream())
        {
            Write(m, trimmed);

            return m.ToArray();
        }
    }

    public void Write(Stream stream, bool trimmed = true)
    {
        using (var er = new EndianBinaryWriterEx(stream, Endianness.LittleEndian))
        {
            //Header
            //skip the header, and write it afterwards
            if (PTable != null && SBoxes != null && PTable.Any(p => p != 0))
            {
                er.BaseStream.Position = 0x1000;
                if (KeyPadding0 != null)
                {
                    if (KeyPadding0.Length != 0x600)
                        throw new Exception();
                    er.Write(KeyPadding0);
                }
                else
                    er.BaseStream.Position += 0x600;

                if (PTable.Length != Blowfish.PTableEntryCount)
                    throw new Exception();
                er.Write(PTable);

                if (KeyPadding1 != null)
                {
                    if (KeyPadding1.Length != 0x5B8)
                        throw new Exception();
                    er.Write(KeyPadding1);
                }
                else
                    er.BaseStream.Position += 0x5B8;

                if (SBoxes.Length != Blowfish.SBoxCount)
                    throw new Exception();

                for (int i = 0; i < Blowfish.SBoxCount; i++)
                {
                    if (SBoxes[i] == null || SBoxes[i].Length != Blowfish.SBoxEntryCount)
                        throw new Exception();
                    er.Write(SBoxes[i]);
                }

                if (KeyPadding2 != null)
                {
                    if (KeyPadding2.Length != 0x400)
                        throw new Exception();
                    er.Write(KeyPadding2);
                }
                else
                    er.BaseStream.Position += 0x400;

                // test patterns
                er.Write(new byte[] { 0xFF, 0x00, 0xFF, 0x00, 0xAA, 0x55, 0xAA, 0x55 });

                for (int i = 8; i < 0x200; i++)
                    er.Write((byte)(i & 0xFF));

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)(0xFF - (i & 0xFF)));

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0xFF);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0x0F);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0xF0);

                for (int i = 0; i < 0x200; i++)
                    er.Write((byte)0x55);

                for (int i = 0; i < 0x1FF; i++)
                    er.Write((byte)0xAA);

                er.Write((byte)0);
            }

            er.BaseStream.Position = 0x4000;
            Header.HeaderSize = (uint)er.BaseStream.Position;
            //MainRom
            Header.MainRomOffset = (uint)er.BaseStream.Position;
            Header.MainSize = (uint)MainRom.Length;
            er.Write(MainRom, 0, MainRom.Length);
            //Static Footer
            StaticFooter?.Write(er);
            if (MainOvt.Length != 0)
            {
                er.WritePadding(0x200, 0xFF);
                //Main Ovt
                Header.MainOvtOffset = (uint)er.BaseStream.Position;
                Header.MainOvtSize = (uint)MainOvt.Length * 0x20;
                foreach (var v in MainOvt)
                {
                    v.Compressed = (uint)FileData[v.FileId].Data.Length;
                    v.Write(er);
                }
                foreach (var v in MainOvt)
                {
                    er.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)er.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)er.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    er.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.MainOvtOffset = 0;
                Header.MainOvtSize = 0;
            }

            er.WritePadding(0x200, 0xFF);
            //SubRom
            Header.SubRomOffset = (uint)er.BaseStream.Position;
            Header.SubSize = (uint)SubRom.Length;
            er.Write(SubRom, 0, SubRom.Length);
            //I assume this works the same as the main ovt?
            if (SubOvt.Length != 0)
            {
                er.WritePadding(0x200, 0xFF);
                //Sub Ovt
                Header.SubOvtOffset = (uint)er.BaseStream.Position;
                Header.SubOvtSize = (uint)SubOvt.Length * 0x20;
                foreach (var v in SubOvt)
                    v.Write(er);
                foreach (var v in SubOvt)
                {
                    er.WritePadding(0x200, 0xFF);
                    Fat[v.FileId].FileTop = (uint)er.BaseStream.Position;
                    Fat[v.FileId].FileBottom = (uint)er.BaseStream.Position + (uint)FileData[v.FileId].Data.Length;
                    er.Write(FileData[v.FileId].Data, 0, FileData[v.FileId].Data.Length);
                }
            }
            else
            {
                Header.SubOvtOffset = 0;
                Header.SubOvtSize = 0;
            }

            er.WritePadding(0x200, 0xFF);
            //FNT
            Header.FntOffset = (uint)er.BaseStream.Position;
            Fnt.Write(er);
            Header.FntSize = (uint)er.BaseStream.Position - Header.FntOffset;
            er.WritePadding(0x200, 0xFF);
            //FAT
            Header.FatOffset = (uint)er.BaseStream.Position;
            Header.FatSize = (uint)Fat.Length * 8;
            //Skip the fat, and write it after writing the data itself
            er.BaseStream.Position += Header.FatSize;
            //Banner
            if (Banner != null)
            {
                er.WritePadding(0x200, 0xFF);
                Header.BannerOffset = (uint)er.BaseStream.Position;
                Banner.Write(er);
            }
            else
                Header.BannerOffset = 0;

            //Files
            if (FileData.All(f => f.NameFat is null))
            {
                for (int i = (int)(Header.MainOvtSize / 32 + Header.SubOvtSize / 32); i < FileData.Length; i++)
                {
                    er.WritePadding(0x200, 0xFF);
                    Fat[i].FileTop = (uint)er.BaseStream.Position;
                    Fat[i].FileBottom = (uint)er.BaseStream.Position + (uint)FileData[i].Data.Length;
                    er.Write(FileData[i].Data, 0, FileData[i].Data.Length);
                }
            }
            else
            {
                foreach ((int i, byte[] data) in FileData.Skip((int)(Header.MainOvtSize / 32 + Header.SubOvtSize / 32))
                             .Select((f, i) => (f, i))
                             .OrderBy(t => t.f.NameFat?.FatOffset ?? 0).Select(t => (t.i, t.f.Data)))
                {
                    int idx = i + (int)(Header.MainOvtSize / 32 + Header.SubOvtSize / 32);
                    er.WritePadding(0x200, 0xFF);
                    Fat[idx].FileTop = (uint)er.BaseStream.Position;
                    Fat[idx].FileBottom = (uint)er.BaseStream.Position + (uint)data.Length;
                    er.Write(data, 0, data.Length);
                }
            }

            er.WritePadding(4);
            Header.RomSize = (uint)er.BaseStream.Position;
            uint capacitySize = Header.RomSize;
            capacitySize |= capacitySize >> 16;
            capacitySize |= capacitySize >> 8;
            capacitySize |= capacitySize >> 4;
            capacitySize |= capacitySize >> 2;
            capacitySize |= capacitySize >> 1;
            capacitySize++;
            if (capacitySize < 0x20000)
                capacitySize = 0x20000;
            uint capacitySize2 = capacitySize;
            int capacity = -18;
            while (capacitySize2 != 0)
            {
                capacitySize2 >>= 1;
                capacity++;
            }

            Header.DeviceSize = (byte)(capacity < 0 ? 0 : capacity);
            //RSA
            if (RSASignature != null)
                er.Write(RSASignature, 0, 0x88);

            //if writing untrimmed write padding up to the power of 2 size of the rom
            if (!trimmed)
                er.WritePadding((int)capacitySize, 0xFF);

            //Fat
            er.BaseStream.Position = Header.FatOffset;
            foreach (var v in Fat)
                v.Write(er);
            //Header
            er.BaseStream.Position = 0;
            Header.Write(er);
        }
    }

    public RomHeader Header { get; set; }

    public class RomHeader
    {
        public RomHeader() { }

        public RomHeader(EndianBinaryReader er)
        {
            GameName = er.ReadString(Encoding.ASCII, 12).TrimEnd('\0');
            GameCode = er.ReadString(Encoding.ASCII, 4).TrimEnd('\0');
            MakerCode = er.ReadString(Encoding.ASCII, 2).TrimEnd('\0');
            ProductId = er.Read<byte>();
            DeviceType = er.Read<byte>();
            DeviceSize = er.Read<byte>();
            ReservedA = er.Read<byte>(9);
            GameVersion = er.Read<byte>();
            Property = er.Read<byte>();

            MainRomOffset = er.Read<uint>();
            MainEntryAddress = er.Read<uint>();
            MainRamAddress = er.Read<uint>();
            MainSize = er.Read<uint>();
            SubRomOffset = er.Read<uint>();
            SubEntryAddress = er.Read<uint>();
            SubRamAddress = er.Read<uint>();
            SubSize = er.Read<uint>();

            FntOffset = er.Read<uint>();
            FntSize = er.Read<uint>();

            FatOffset = er.Read<uint>();
            FatSize = er.Read<uint>();

            MainOvtOffset = er.Read<uint>();
            MainOvtSize = er.Read<uint>();

            SubOvtOffset = er.Read<uint>();
            SubOvtSize = er.Read<uint>();

            RomParamA = er.Read<byte>(8);
            BannerOffset = er.Read<uint>();
            SecureCRC = er.Read<ushort>();
            RomParamB = er.Read<byte>(2);

            MainAutoloadDone = er.Read<uint>();
            SubAutoloadDone = er.Read<uint>();

            RomParamC = er.Read<byte>(8);
            RomSize = er.Read<uint>();
            HeaderSize = er.Read<uint>();
            ReservedB = er.Read<byte>(0x38);

            LogoData = er.Read<byte>(0x9C);
            LogoCRC = er.Read<ushort>();
            HeaderCRC = er.Read<ushort>();
        }

        public void Write(EndianBinaryWriter er)
        {
            var m = new MemoryStream();
            byte[] header;
            using (var ew = new EndianBinaryWriter(m, Endianness.LittleEndian))
            {
                ew.Write(GameName.PadRight(12, '\0')[..12], Encoding.ASCII, false);
                ew.Write(GameCode.PadRight(4, '\0')[..4], Encoding.ASCII, false);
                ew.Write(MakerCode.PadRight(2, '\0')[..2], Encoding.ASCII, false);
                ew.Write(ProductId);
                ew.Write(DeviceType);
                ew.Write(DeviceSize);
                ew.Write(ReservedA, 0, 9);
                ew.Write(GameVersion);
                ew.Write(Property);

                ew.Write(MainRomOffset);
                ew.Write(MainEntryAddress);
                ew.Write(MainRamAddress);
                ew.Write(MainSize);
                ew.Write(SubRomOffset);
                ew.Write(SubEntryAddress);
                ew.Write(SubRamAddress);
                ew.Write(SubSize);

                ew.Write(FntOffset);
                ew.Write(FntSize);

                ew.Write(FatOffset);
                ew.Write(FatSize);

                ew.Write(MainOvtOffset);
                ew.Write(MainOvtSize);

                ew.Write(SubOvtOffset);
                ew.Write(SubOvtSize);

                ew.Write(RomParamA, 0, 8);
                ew.Write(BannerOffset);
                ew.Write(SecureCRC);
                ew.Write(RomParamB, 0, 2);

                ew.Write(MainAutoloadDone);
                ew.Write(SubAutoloadDone);

                ew.Write(RomParamC, 0, 8);
                ew.Write(RomSize);
                ew.Write(HeaderSize);
                ew.Write(ReservedB, 0, 0x38);

                ew.Write(LogoData, 0, 0x9C);
                LogoCRC = Crc16.GetCrc16(LogoData);
                ew.Write(LogoCRC);

                header = m.ToArray();
            }

            HeaderCRC = Crc16.GetCrc16(header);

            er.Write(header);
            er.Write(HeaderCRC);
        }

        public string GameName { get; set; }  //12
        public string GameCode { get; set; }  //4
        public string MakerCode { get; set; } //2
        public byte ProductId { get; set; }
        public byte DeviceType { get; set; }
        public byte DeviceSize { get; set; }

        [ArraySize(9)]
        public byte[] ReservedA { get; set; }

        public byte GameVersion { get; set; }
        public byte Property { get; set; }

        [JsonIgnore]
        public uint MainRomOffset { get; set; }

        public uint MainEntryAddress { get; set; }
        public uint MainRamAddress { get; set; }

        [JsonIgnore]
        public uint MainSize { get; set; }

        [JsonIgnore]
        public uint SubRomOffset { get; set; }

        public uint SubEntryAddress { get; set; }
        public uint SubRamAddress { get; set; }

        [JsonIgnore]
        public uint SubSize { get; set; }

        [JsonIgnore]
        public uint FntOffset { get; set; }

        [JsonIgnore]
        public uint FntSize { get; set; }

        [JsonIgnore]
        public uint FatOffset { get; set; }

        [JsonIgnore]
        public uint FatSize { get; set; }

        [JsonIgnore]
        public uint MainOvtOffset { get; set; }

        [JsonIgnore]
        public uint MainOvtSize { get; set; }

        [JsonIgnore]
        public uint SubOvtOffset { get; set; }

        [JsonIgnore]
        public uint SubOvtSize { get; set; }

        [ArraySize(8)]
        public byte[] RomParamA { get; set; }

        [JsonIgnore]
        public uint BannerOffset { get; set; }

        public ushort SecureCRC { get; set; }

        [ArraySize(2)]
        public byte[] RomParamB { get; set; }

        public uint MainAutoloadDone { get; set; }
        public uint SubAutoloadDone { get; set; }

        [ArraySize(8)]
        public byte[] RomParamC { get; set; } //8

        [JsonIgnore]
        public uint RomSize { get; set; }

        [JsonIgnore]
        public uint HeaderSize { get; set; }
        
        [ArraySize(0x38)]
        public byte[] ReservedB { get; set; }

        [ArraySize(0x9C)]
        public byte[] LogoData { get; set; }

        [JsonIgnore]
        public ushort LogoCRC { get; set; }

        [JsonIgnore]
        public ushort HeaderCRC { get; set; }
    }

    public byte[] KeyPadding0 { get; set; }
    public uint[] PTable { get; set; }
    public byte[] KeyPadding1 { get; set; }
    public uint[][] SBoxes { get; set; }
    public byte[] KeyPadding2 { get; set; }

    public byte[] MainRom { get; set; }
    public NitroFooter StaticFooter { get; set; }

    public class NitroFooter
    {
        public NitroFooter() { }

        public NitroFooter(EndianBinaryReaderEx er) => er.ReadObject(this);
        public void Write(EndianBinaryWriterEx er) => er.WriteObject(this);

        public uint NitroCode { get; set; }
        public uint _start_ModuleParamsOffset { get; set; }
        public uint Unknown { get; set; }
    }


    public byte[] SubRom { get; set; }
    public RomFNT Fnt { get; set; }

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

    public RomOVT[] MainOvt { get; set; }
    public RomOVT[] SubOvt { get; set; }

    public class RomOVT
    {
        [Flags]
        public enum OVTFlag : byte
        {
            Compressed = 1,
            AuthenticationCode = 2
        }

        public RomOVT() { }

        public RomOVT(EndianBinaryReaderEx er)
        {
            er.ReadObject(this);
            uint tmp = er.Read<uint>();
            Compressed = tmp & 0xFFFFFF;
            Flag = (OVTFlag)(tmp >> 24);
        }

        public void Write(EndianBinaryWriterEx er)
        {
            er.WriteObject(this);
            er.Write(((uint)Flag & 0xFF) << 24 | Compressed & 0xFFFFFF);
        }

        public uint Id { get; set; }

        public uint RamAddress { get; set; }
        public uint RamSize { get; set; }
        public uint BssSize { get; set; }
        public uint SinitInit { get; set; }
        public uint SinitInitEnd { get; set; }

        [JsonIgnore]
        public uint FileId { get; set; }

        [Ignore]
        public uint Compressed { get; set; } //:24;

        [Ignore]
        public OVTFlag Flag { get; set; } // :8;
    }

    public FatEntry[] Fat { get; set; }
    public RomBanner Banner { get; set; }

    public class RomBanner
    {
        public RomBanner() { }

        public RomBanner(EndianBinaryReaderEx er)
        {
            Header = new BannerHeader(er);
            Banner = new BannerV1(er);
        }

        public void Write(EndianBinaryWriterEx er)
        {
            Header.CRC16_v1 = Banner.GetCrc();
            Header.Write(er);
            Banner.Write(er);
        }

        public BannerHeader Header { get; set; }

        [Serializable]
        public class BannerHeader
        {
            public BannerHeader() { }

            public BannerHeader(EndianBinaryReaderEx er)
            {
                er.ReadObject(this);
            }

            public void Write(EndianBinaryWriterEx er)
            {
                er.WriteObject(this);
            }

            public byte Version { get; set; }
            public byte ReservedA { get; set; }

            [JsonIgnore]
            public ushort CRC16_v1 { get; set; }

            [ArraySize(28)]
            public byte[] ReservedB { get; set; }
        }

        public BannerV1 Banner { get; set; }

        [Serializable]
        public class BannerV1
        {
            public BannerV1() { }

            public BannerV1(EndianBinaryReader er)
            {
                Image = er.Read<byte>(32 * 32 / 2);
                Pltt = er.Read<byte>(16 * 2);
                GameName = new string[6];
                GameName[0] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[1] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[2] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[3] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[4] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
                GameName[5] = er.ReadString(Encoding.Unicode, 128).Replace("\0", "");
            }

            public void Write(EndianBinaryWriter er)
            {
                er.Write(Image, 0, 32 * 32 / 2);
                er.Write(Pltt, 0, 16 * 2);
                foreach (string s in GameName) er.Write(GameName[0].PadRight(128, '\0'), Encoding.Unicode, false);
            }

            [ArraySize(32 * 32 / 2)]
            public byte[] Image { get; set; }

            [ArraySize(16 * 2)]
            public byte[] Pltt { get; set; }

            [JsonIgnore]
            public string[] GameName { get; set; } //6, 128 chars (UTF16-LE)

            [JsonPropertyName("GameName")]
            public string[] Base64GameName
            {
                get
                {
                    string[] b = new string[6];
                    for (int i = 0; i < 6; i++)
                    {
                        b[i] = Convert.ToBase64String(Encoding.Unicode.GetBytes(GameName[i]));
                    }

                    return b;
                }
                set
                {
                    GameName = new string[6];
                    for (int i = 0; i < 6; i++)
                    {
                        GameName[i] = Encoding.Unicode.GetString(Convert.FromBase64String(value[i]));
                    }
                }
            }

            public ushort GetCrc()
            {
                byte[] data = new byte[2080];
                Array.Copy(Image, data, 512);
                Array.Copy(Pltt, 0, data, 512, 32);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[0].PadRight(128, '\0')), 0, data, 544, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[1].PadRight(128, '\0')), 0, data, 544 + 256, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[2].PadRight(128, '\0')), 0, data, 544 + 256 * 2, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[3].PadRight(128, '\0')), 0, data, 544 + 256 * 3, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[4].PadRight(128, '\0')), 0, data, 544 + 256 * 4, 256);
                Array.Copy(Encoding.Unicode.GetBytes(GameName[5].PadRight(128, '\0')), 0, data, 544 + 256 * 5, 256);
                return Crc16.GetCrc16(data);
            }

            public Rgba8Bitmap GetIcon() => GxUtil.DecodeChar(Image, Pltt, ImageFormat.Pltt16, 32, 32, true);
        }
    }

    public NameFatWithData[] FileData { get; set; }

    public byte[] RSASignature { get; set; }

    public NitroFsArchive ToArchive()
    {
        return new(Fnt.DirectoryTable, Fnt.NameTable, FileData);
    }

    public void FromArchive(Archive archive, NameEntryWithFatEntry[] nameFat = null)
    {
        int nrOverlays = MainOvt.Length + SubOvt.Length;
        
        NitroFsArchive nitroArc = new(archive, (ushort)nrOverlays, nameFat?.ToList());
        Fnt.DirectoryTable = nitroArc.DirTable;
        Fnt.NameTable = nitroArc.NameTable;

        int nrFiles = nitroArc.FileData.Length;

        var fat = new FatEntry[nrOverlays + nrFiles];
        Array.Copy(Fat, fat, nrOverlays);
        Fat = fat;

        var fileData = new byte[nrOverlays + nrFiles][];
        Array.Copy(FileData.Select(f => f.Data).ToArray(), fileData, nrOverlays);
        FileData = fileData.Select(t => new NameFatWithData(t)).ToArray();

        for (int i = nrOverlays; i < nrFiles + nrOverlays; i++)
        {
            Fat[i] = new(0, 0);
            FileData[i] = nitroArc.FileData[i - nitroArc.FileIdOffset];
        }
    }
}