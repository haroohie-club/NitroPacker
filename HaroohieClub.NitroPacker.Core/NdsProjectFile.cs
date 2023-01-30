using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.Nitro.Card;
using HaroohieClub.NitroPacker.Nitro.Fs;
using System.IO;

namespace HaroohieClub.NitroPacker.Core
{
    public class NdsProjectFile : ProjectFile
    {
        public NdsRomInfo RomInfo;

        public class NdsRomInfo
        {
            public NdsRomInfo()
            {
            }

            public NdsRomInfo(Rom Rom)
            {
                Header = Rom.Header;
                NitroFooter = Rom.StaticFooter;
                ARM9Ovt = Rom.MainOvt;
                ARM7Ovt = Rom.SubOvt;
                Banner = Rom.Banner;
                RSASignature = Rom.RSASignature;
            }

            public Rom.RomHeader Header;
            public Rom.NitroFooter NitroFooter;
            public Rom.RomOVT[] ARM9Ovt;
            public Rom.RomOVT[] ARM7Ovt;
            public Rom.RomBanner Banner;
            public byte[] RSASignature;
            public string ExternalARM9Path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputRomPath"></param>
        /// <param name="projectFilePath"></param>
        public static void Pack(string outputRomPath, string projectFilePath)
        {
            using (var file = new FileStream(outputRomPath, FileMode.Create))
            {
                var project = FromByteArray<NdsProjectFile>(File.ReadAllBytes(projectFilePath));

                var basePath = new FileInfo(projectFilePath).DirectoryName;

                var fsRoot = new DiskArchive($"{basePath}\\data");

                project.Build(basePath, fsRoot, file);
            }
        }

        /// <summary>
        /// Creates an NDS ROM project folder structure.
        /// </summary>
        /// <param name="name">Name of the project</param>
        /// <param name="romPath">Path of the NDS ROM to extract</param>
        /// <param name="outPath">Path where the project structure is gonna be created</param>
        /// <param name="unpackArc">Choose whether to unpack the archives</param>
        public static void Create(string name, string romPath, string outPath, bool unpackArc = false)
        {
            var ndsFile = new Rom(File.ReadAllBytes(romPath));
            Create(name, ndsFile, outPath, unpackArc);
        }

        /// <summary>
        /// Creates an NDS ROM project folder structure.
        /// </summary>
        /// <param name="name">Name of the project</param>
        /// <param name="rom">NDS ROM Instance</param>
        /// <param name="outPath">Path where the project structure is gonna be created</param>
        /// <param name="unpackArc">Choose whether to unpack the archives</param>
        public static void Create(string name, Rom rom, string outPath, bool unpackArc = false)
        {
            var projectFile = new NdsProjectFile();
            var ndsFile = rom;
            var fs = ndsFile.ToArchive();
            var dir = new DirectoryInfo(outPath);

            fs.Export(dir.CreateSubdirectory("data").FullName, unpackArc);

            dir.CreateSubdirectory("overlay");
            foreach (var vv in ndsFile.MainOvt)
            {
                File.Create(outPath + $"\\overlay\\main_{vv.Id:X4}.bin").Close();
                File.WriteAllBytes(outPath + $"\\overlay\\main_{vv.Id:X4}.bin", ndsFile.FileData[vv.FileId]);
            }
            foreach (var vv in ndsFile.SubOvt)
            {
                File.Create(outPath + $"\\overlay\\sub_{vv.Id:X4}.bin").Close();
                File.WriteAllBytes(outPath + $"\\overlay\\sub_{vv.Id:X4}.bin", ndsFile.FileData[vv.FileId]);
            }

            File.Create(outPath + "\\arm9.bin").Close();
            File.WriteAllBytes(outPath + "\\arm9.bin", ndsFile.MainRom);
            File.Create(outPath + "\\arm7.bin").Close();
            File.WriteAllBytes(outPath + "\\arm7.bin", ndsFile.SubRom);

            projectFile.RomInfo = new NdsRomInfo(ndsFile);

            File.WriteAllBytes(Path.Combine(outPath, $"{name}.xml"), projectFile.Write());
        }

        public void Build(string projectDir, Archive fsRoot, Stream outputStream)
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
                n.FileData[fid] = File.ReadAllBytes(projectDir + "\\overlay\\main_" + vv.Id.ToString("X4") + ".bin");
                fid++;
            }
            foreach (var vv in n.SubOvt)
            {
                vv.FileId = fid;
                n.Fat[fid] = new FatEntry(0, 0);
                n.FileData[fid] = File.ReadAllBytes(projectDir + "\\overlay\\sub_" + vv.Id.ToString("X4") + ".bin");
                fid++;
            }
            n.MainRom = File.ReadAllBytes(projectDir + "\\arm9.bin");
            n.SubRom = File.ReadAllBytes(projectDir + "\\arm7.bin");
            n.FromArchive(fsRoot);

            n.Write(outputStream);
        }
    }
}
