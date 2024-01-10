using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.IO.Compression;
using HaroohieClub.NitroPacker.Nitro.Card;
using HaroohieClub.NitroPacker.Nitro.Fs;
using System.IO;

namespace HaroohieClub.NitroPacker.Core
{
    /// <summary>
    /// Represents an NDS project file which is an abstraction of the NDS ROM itself
    /// </summary>
    public class NdsProjectFile : ProjectFile
    {
        /// <summary>
        /// The ROM info for this project file
        /// </summary>
        public NdsRomInfo RomInfo { get; set; }

        /// <summary>
        /// An object representing the NDS ROM info
        /// </summary>
        public class NdsRomInfo
        {
            /// <summary>
            /// The ROM header of the ROM, containing all the ROM metadata
            /// </summary>
            public Rom.RomHeader Header { get; set; }
            /// <summary>
            /// The Nitro footer
            /// </summary>
            public Rom.NitroFooter NitroFooter { get; set; }
            /// <summary>
            /// Data representation of the ARM9 overlay table
            /// </summary>
            public Rom.RomOVT[] ARM9Ovt { get; set; }
            /// <summary>
            /// Data representation of the ARM7 overlay table
            /// </summary>
            public Rom.RomOVT[] ARM7Ovt { get; set; }
            /// <summary>
            /// The banner of the ROM
            /// </summary>
            public Rom.RomBanner Banner { get; set; }
            /// <summary>
            /// The ROM's RSA signature
            /// </summary>
            public byte[] RSASignature { get; private set; }
            /// <summary>
            /// The path to the ARM9 binary
            /// </summary>
            public string ExternalARM9Path { get; set; }

            /// <summary>
            /// Creates blank NDS ROM info
            /// </summary>
            public NdsRomInfo()
            {
            }

            /// <summary>
            /// Creates NDS ROM info given a ROM
            /// </summary>
            /// <param name="Rom">The ROM object</param>
            public NdsRomInfo(Rom Rom)
            {
                Header = Rom.Header;
                NitroFooter = Rom.StaticFooter;
                ARM9Ovt = Rom.MainOvt;
                ARM7Ovt = Rom.SubOvt;
                Banner = Rom.Banner;
                RSASignature = Rom.RSASignature;
            }
        }

        /// <summary>
        /// Packs an NDS ROM given a project file
        /// </summary>
        /// <param name="outputRomPath">The ROM to be output</param>
        /// <param name="projectFilePath">The path to the project file on disk</param>
        /// <param name="compressArm9">If set, will compress the ARM9 binary</param>
        public static void Pack(string outputRomPath, string projectFilePath, bool compressArm9 = false)
        {
            using var file = new FileStream(outputRomPath, FileMode.Create);
            var project = FromByteArray<NdsProjectFile>(File.ReadAllBytes(projectFilePath));

            var basePath = new FileInfo(projectFilePath).DirectoryName;

            var fsRoot = new DiskArchive(Path.Combine(basePath, "data"));

            project.Build(basePath, fsRoot, file, compressArm9);
        }

        /// <summary>
        /// Creates an NDS ROM project folder structure
        /// </summary>
        /// <param name="name">Name of the project</param>
        /// <param name="romPath">Path of the NDS ROM to extract</param>
        /// <param name="outPath">Path where the project structure is gonna be created</param>
        /// <param name="decompressArm9">Choose whether to decompress the ARM9 executable</param>
        /// <param name="unpackArc">Choose whether to unpack the archives</param>
        public static void Create(string name, string romPath, string outPath, bool decompressArm9 = false, bool unpackArc = false)
        {
            var ndsFile = new Rom(File.ReadAllBytes(romPath));
            Create(name, ndsFile, outPath, decompressArm9, unpackArc);
        }

        /// <summary>
        /// Creates an NDS ROM project folder structure
        /// </summary>
        /// <param name="name">Name of the project</param>
        /// <param name="rom">NDS ROM Instance</param>
        /// <param name="outPath">Path where the project structure is gonna be created</param>
        /// <param name="decompressArm9">Choose whether to decompress the ARM9 executable</param>
        /// <param name="unpackArc">Choose whether to unpack the archives</param>
        public static void Create(string name, Rom rom, string outPath, bool decompressArm9 = false, bool unpackArc = false)
        {
            var projectFile = new NdsProjectFile();
            var ndsFile = rom;
            var fs = ndsFile.ToArchive();
            var dir = new DirectoryInfo(outPath);

            fs.Export(dir.CreateSubdirectory("data").FullName, unpackArc);

            dir.CreateSubdirectory("overlay");
            foreach (var vv in ndsFile.MainOvt)
            {
                if (vv.Compressed > 0)
                {
                    File.WriteAllBytes(Path.Combine(outPath, "overlay", $"main_{vv.Id:X4}.bin"), Blz.Decompress(ndsFile.FileData[vv.FileId]));
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(outPath, "overlay", $"main_{vv.Id:X4}.bin"), ndsFile.FileData[vv.FileId]);
                }
            }
            foreach (var vv in ndsFile.SubOvt)
            {
                if (vv.Compressed > 0)
                {
                    File.WriteAllBytes(Path.Combine(outPath, "overlay", $"sub_{vv.Id:X4}.bin"), Blz.Decompress(ndsFile.FileData[vv.FileId]));
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(outPath, "overlay", $"sub_{vv.Id:X4}.bin"), ndsFile.FileData[vv.FileId]);
                }
            }

            if (decompressArm9)
            {
                File.WriteAllBytes(Path.Combine(outPath, "arm9.bin"), Blz.Decompress(ndsFile.MainRom));
            }
            else
            {
                File.WriteAllBytes(Path.Combine(outPath, "arm9.bin"), ndsFile.MainRom);
            }
            File.WriteAllBytes(Path.Combine(outPath, "arm7.bin"), ndsFile.SubRom);

            projectFile.RomInfo = new NdsRomInfo(ndsFile);

            File.WriteAllBytes(Path.Combine(outPath, $"{name}.xml"), projectFile.Write());
        }

        /// <summary>
        /// Builds a project and writes the resulting ROM to an output stream
        /// </summary>
        /// <param name="projectDir">The directory containing the unpacked NDS project</param>
        /// <param name="fsRoot">The Nitro filesystem root</param>
        /// <param name="outputStream">The stream to which to output the ROM</param>
        /// <param name="compressArm9">If set to true, will compress the ARM9 binary</param>
        public void Build(string projectDir, Archive fsRoot, Stream outputStream, bool compressArm9 = false)
        {
            var n = new Rom
            {
                Header = RomInfo.Header,
                StaticFooter = RomInfo.NitroFooter,
                MainOvt = RomInfo.ARM9Ovt,
                SubOvt = RomInfo.ARM7Ovt,
                Banner = RomInfo.Banner,
                RSASignature = RomInfo.RSASignature,
                Fnt = new Rom.RomFNT()
            };

            n.Fat = new FatEntry[n.MainOvt.Length + n.SubOvt.Length];
            n.FileData = new byte[n.MainOvt.Length + n.SubOvt.Length][];
            uint fid = 0;
            foreach (var vv in n.MainOvt)
            {
                vv.FileId = fid;
                n.Fat[fid] = new FatEntry(0, 0);
                if (vv.Compressed > 0)
                {
                    Blz blz = new();
                    n.FileData[fid] = blz.BLZ_Encode(File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"main_{vv.Id:X4}.bin")), false);
                }
                else
                {
                    n.FileData[fid] = File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"main_{vv.Id:X4}.bin"));
                }
                fid++;
            }
            foreach (var vv in n.SubOvt)
            {
                vv.FileId = fid;
                n.Fat[fid] = new FatEntry(0, 0);
                if (vv.Compressed > 0)
                {
                    Blz blz = new();
                    n.FileData[fid] = blz.BLZ_Encode(File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"sub_{vv.Id:X4}.bin")), false);
                }
                else
                {
                    n.FileData[fid] = File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"sub_{vv.Id:X4}.bin"));
                }
                fid++;
            }
            n.MainRom = File.ReadAllBytes(Path.Combine(projectDir, "arm9.bin"));
            if (compressArm9)
            {
                Blz blz = new();
                n.MainRom = blz.BLZ_Encode(n.MainRom, true);
            }
            n.SubRom = File.ReadAllBytes(Path.Combine(projectDir, "arm7.bin"));
            n.FromArchive(fsRoot);

            n.Write(outputStream);
        }
    }
}
