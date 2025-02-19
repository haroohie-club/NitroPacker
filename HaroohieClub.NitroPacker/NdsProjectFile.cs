using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using HaroohieClub.NitroPacker.IO;
using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.IO.Compression;
using HaroohieClub.NitroPacker.Nitro.Card;
using HaroohieClub.NitroPacker.Nitro.Card.Banners;
using HaroohieClub.NitroPacker.Nitro.Card.Headers;
using HaroohieClub.NitroPacker.Nitro.Fs;

namespace HaroohieClub.NitroPacker;

/// <summary>
/// Represents an NDS project file which is an abstraction of the NDS ROM itself
/// </summary>
public class NdsProjectFile
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
        public RomHeader Header { get; set; }
        /// <summary>
        /// The Nitro footer
        /// </summary>
        public NitroFooter NitroFooter { get; set; }
        /// <summary>
        /// Data representation of the ARM9 overlay table
        /// </summary>
        public RomOverlayTable[] ARM9Ovt { get; set; }
        /// <summary>
        /// Data representation of the ARM7 overlay table
        /// </summary>
        public RomOverlayTable[] ARM7Ovt { get; set; }
        /// <summary>
        /// The banner of the ROM
        /// </summary>
        [JsonIgnore]
        public RomBanner Banner { get; set; }
        /// <summary>
        /// The ROM's RSA signature
        /// </summary>
        public byte[] RSASignature { get; set; }
        /// <summary>
        /// The path to the ARM9 binary
        /// </summary>
        public string ExternalARM9Path { get; set; }
        /// <summary>
        /// The list of files as they appear in the ROM (optional)
        /// </summary>
        public NameEntryWithFatEntry[] NameEntryWithFatEntries { get; set; }

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
            ARM9Ovt = Rom.Arm9OverlayTable;
            ARM7Ovt = Rom.Arm7OverlayTable;
            Banner = Rom.Banner;
            RSASignature = Rom.RsaSignature;
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
        using FileStream file = File.Create(outputRomPath);
        NdsProjectFile project = JsonSerializer.Deserialize<NdsProjectFile>(File.ReadAllText(projectFilePath));

        string basePath = new FileInfo(projectFilePath).DirectoryName;

        DiskArchive fsRoot = new(Path.Combine(basePath, "data"));

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
    /// <param name="includeFileOrder">Choose whether to include the order files are specified in the FAT in the project file (necessary for repacking some games, maybe)</param>
    /// <remarks>Note that if <paramref name="includeFileOrder"/> is specified, files cannot be added or removed from the project</remarks>
    public static void Create(string name, string romPath, string outPath, bool decompressArm9 = false, bool unpackArc = false, bool includeFileOrder = false)
    {
        Rom ndsFile = new(File.ReadAllBytes(romPath));
        Create(name, ndsFile, outPath, decompressArm9, unpackArc, includeFileOrder);
    }

    /// <summary>
    /// Creates an NDS ROM project folder structure
    /// </summary>
    /// <param name="name">Name of the project</param>
    /// <param name="rom">NDS ROM Instance</param>
    /// <param name="outPath">Path where the project structure is gonna be created</param>
    /// <param name="decompressArm9">Choose whether to decompress the ARM9 executable</param>
    /// <param name="unpackArc">Choose whether to unpack the archives</param>
    /// <param name="includeFileOrder">Choose whether to include the order files are specified in the FAT in the project file (necessary for repacking some games, maybe)</param>
    /// <remarks>Note that if <paramref name="includeFileOrder"/> is specified, files cannot be added or removed from the project</remarks>
    public static void Create(string name, Rom rom, string outPath, bool decompressArm9 = false, bool unpackArc = false, bool includeFileOrder = false)
    {
        NdsProjectFile projectFile = new();
        Rom ndsFile = rom;
        NitroFsArchive fs = ndsFile.ToArchive();
        DirectoryInfo dir = new(outPath);

        fs.Export(dir.CreateSubdirectory("data").FullName, unpackArc);

        dir.CreateSubdirectory("overlay");
        foreach (RomOverlayTable vv in ndsFile.Arm9OverlayTable)
        {
            File.WriteAllBytes(Path.Combine(outPath, "overlay", $"main_{vv.Id:X4}.bin"),
                vv.Flag.HasFlag(RomOverlayTable.OverlayTableFlag.Compressed)
                    ? Blz.Decompress(ndsFile.FileData[vv.FileId].Data)
                    : ndsFile.FileData[vv.FileId].Data);
        }
        foreach (RomOverlayTable vv in ndsFile.Arm7OverlayTable)
        {
            File.WriteAllBytes(Path.Combine(outPath, "overlay", $"sub_{vv.Id:X4}.bin"),
                vv.Flag.HasFlag(RomOverlayTable.OverlayTableFlag.Compressed)
                    ? Blz.Decompress(ndsFile.FileData[vv.FileId].Data)
                    : ndsFile.FileData[vv.FileId].Data);
        }

        File.WriteAllBytes(Path.Combine(outPath, "arm9.bin"),
            decompressArm9 ? Blz.Decompress(ndsFile.Arm9Binary) : ndsFile.Arm9Binary);
        File.WriteAllBytes(Path.Combine(outPath, "arm7.bin"), ndsFile.Arm7Binary);
        using FileStream bannerStream = File.Create(Path.Combine(outPath, "banner.bin"));
        using EndianBinaryWriterEx bw = new(bannerStream);
        ndsFile.Banner.Write(bw);

        if (ndsFile.DigestSectorHashtableBinary?.Length > 0)
        {
            File.WriteAllBytes(Path.Combine(outPath, "digest_sector_hashtable.bin"),
                ndsFile.DigestSectorHashtableBinary);
        }
        if (ndsFile.DigestBlockHashtableBinary?.Length > 0)
        {
            File.WriteAllBytes(Path.Combine(outPath, "digest_block_hashtable.bin"),
                ndsFile.DigestBlockHashtableBinary);
        }

        if (ndsFile.Arm9iBinary?.Length > 0)
        {
            if (ndsFile.TwlStaticHeader?.Length > 0)
            {
                File.WriteAllBytes(Path.Combine(outPath, "twlheader.bin"), ndsFile.TwlStaticHeader);
            }
            File.WriteAllBytes(Path.Combine(outPath, "arm9i.bin"), ndsFile.Arm9iBinary);
        }
        if (ndsFile.Arm7iBinary?.Length > 0)
        {
            File.WriteAllBytes(Path.Combine(outPath, "arm7i.bin"), ndsFile.Arm7iBinary);
        }

        if (ndsFile.DSiWareExtraData?.Length > 0)
        {
            File.WriteAllBytes(Path.Combine(outPath, "dsiware-extra.bin"), ndsFile.DSiWareExtraData);
        }

        projectFile.RomInfo = new(ndsFile);
        if (includeFileOrder)
        {
            projectFile.RomInfo.NameEntryWithFatEntries =
            [
                .. ndsFile.FileNameTable.NameTable.SelectMany((t, i) => t.Where(e => e.Type == NameTableEntryType.File).Select(e => (i, e)))
                    .Zip(ndsFile.Fat.Skip(ndsFile.Arm9OverlayTable.Length + ndsFile.Arm7OverlayTable.Length), (tuple, entry) => (Tuple: tuple, Entry: entry)).Select(n => new NameEntryWithFatEntry
                    {
                        Path = NitroFsArchive.JoinPath(NitroFsArchive.GetPathFromDir(n.Tuple.i, "/", ndsFile.FileNameTable), n.Tuple.e.Name),
                        FatOffset = n.Entry.FileTop,
                    }),
            ];
        }

        File.WriteAllText(Path.Combine(outPath, $"{name}.json"), JsonSerializer.Serialize(projectFile));
    }

    /// <summary>
    /// Builds a project and writes the resulting ROM to an output stream
    /// </summary>
    /// <param name="projectDir">The directory containing the unpacked NDS project</param>
    /// <param name="fsRoot">The Nitro filesystem root</param>
    /// <param name="outputStream">The stream to which to output the ROM</param>
    /// <param name="compressArm9">If set to true, will compress the ARM9 binary</param>
    /// <remarks>Note that if your project file includes a file name table, no new files will be added and any removed files will cause errors</remarks>
    public void Build(string projectDir, Archive fsRoot, Stream outputStream, bool compressArm9 = false)
    {
        // Read banner from file first to initialize that
        using FileStream bannerStream = File.OpenRead(Path.Combine(projectDir, "banner.bin"));
        EndianBinaryReaderEx br = new(bannerStream);
        RomInfo.Banner = new()
        {
            Header = br.ReadObject<Header>()
        };
        RomInfo.Banner.Banner = RomInfo.Banner.Header.Version switch
        {
            0x001 => new BannerV1(br),
            0x002 => new BannerV2(br),
            0x003 => new BannerV3(br),
            0x103 => new BannerV103(br),
            _ => null,
        };

        Rom n = new()
        {
            Header = RomInfo.Header,
            StaticFooter = RomInfo.NitroFooter,
            Arm9OverlayTable = RomInfo.ARM9Ovt,
            Arm7OverlayTable = RomInfo.ARM7Ovt,
            Banner = RomInfo.Banner,
            RsaSignature = RomInfo.RSASignature,
            FileNameTable = new(),
        };

        n.Fat = new FatEntry[n.Arm9OverlayTable.Length + n.Arm7OverlayTable.Length];
        byte[][] fileData = new byte[n.Arm9OverlayTable.Length + n.Arm7OverlayTable.Length][];
        uint fid = 0;
        foreach (RomOverlayTable vv in n.Arm9OverlayTable)
        {
            vv.FileId = fid;
            n.Fat[fid] = new(0, 0);
            if (vv.Flag.HasFlag(RomOverlayTable.OverlayTableFlag.Compressed))
            {
                Blz blz = new();
                fileData[fid] = blz.BLZ_Encode(File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"main_{vv.Id:X4}.bin")), false);
            }
            else
            {
                fileData[fid] = File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"main_{vv.Id:X4}.bin"));
            }
            fid++;
        }
        foreach (RomOverlayTable vv in n.Arm7OverlayTable)
        {
            vv.FileId = fid;
            n.Fat[fid] = new(0, 0);
            if (vv.Flag.HasFlag(RomOverlayTable.OverlayTableFlag.Compressed))
            {
                Blz blz = new();
                fileData[fid] = blz.BLZ_Encode(File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"sub_{vv.Id:X4}.bin")), false);
            }
            else
            {
                fileData[fid] = File.ReadAllBytes(Path.Combine(projectDir, "overlay", $"sub_{vv.Id:X4}.bin"));
            }
            fid++;
        }
        n.FileData = fileData.Select(t => new NameFatWithData(t)).ToArray();
        n.Arm9Binary = File.ReadAllBytes(Path.Combine(projectDir, "arm9.bin"));
        if (compressArm9)
        {
            Blz blz = new();
            n.Arm9Binary = blz.BLZ_Encode(n.Arm9Binary, true);
        }
        n.Arm7Binary = File.ReadAllBytes(Path.Combine(projectDir, "arm7.bin"));
        n.FromArchive(fsRoot, RomInfo.NameEntryWithFatEntries);

        if (File.Exists(Path.Combine(projectDir, "digest_sector_hashtable.bin")))
        {
            n.DigestSectorHashtableBinary = File.ReadAllBytes(Path.Combine(projectDir, "digest_sector_hashtable.bin"));
        }
        if (File.Exists(Path.Combine(projectDir, "digest_block_hashtable.bin")))
        {
            n.DigestBlockHashtableBinary = File.ReadAllBytes(Path.Combine(projectDir, "digest_block_hashtable.bin"));
        }

        if (File.Exists(Path.Combine(projectDir, "twlheader.bin")))
        {
            n.TwlStaticHeader = File.ReadAllBytes(Path.Combine(projectDir, "twlheader.bin"));
        }
        if (File.Exists(Path.Combine(projectDir, "arm9i.bin")))
        {
            n.Arm9iBinary = File.ReadAllBytes(Path.Combine(projectDir, "arm9i.bin"));
        }
        if (File.Exists(Path.Combine(projectDir, "arm7i.bin")))
        {
            n.Arm7iBinary = File.ReadAllBytes(Path.Combine(projectDir, "arm7i.bin"));
        }

        if (File.Exists(Path.Combine(projectDir, "dsiware-extra.bin")))
        {
            n.DSiWareExtraData = File.ReadAllBytes(Path.Combine(projectDir, "dsiware-extra.bin"));
        }

        // If this is a DSi ROM, we don't want to trim it
        n.Write(outputStream);
    }

    /// <summary>
    /// Converts a NitroPacker 2.x style XML project to a NitroPacker 3.x+ style JSON project
    /// </summary>
    /// <param name="oldXmlProject">Path to the old XML project</param>
    public static void ConvertProjectFile(string oldXmlProject)
    {
        using FileStream oldProjectStream = File.OpenRead(oldXmlProject);
        NdsProjectFile project = (NdsProjectFile)new XmlSerializer(typeof(NdsProjectFile))
            .Deserialize(oldProjectStream);
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(oldXmlProject), $"{Path.GetFileNameWithoutExtension(oldXmlProject)}.json"),
            JsonSerializer.Serialize(project));
        using FileStream bannerStream = File.Create(Path.Combine(Path.GetDirectoryName(oldXmlProject), "banner.bin"));
        using EndianBinaryWriterEx bw = new(bannerStream);
        project.RomInfo.Banner.Write(bw);
    }
}