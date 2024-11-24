﻿using System.Collections.Generic;
using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.IO.Compression;
using HaroohieClub.NitroPacker.Nitro.Card;
using HaroohieClub.NitroPacker.Nitro.Fs;
using System.IO;
using System.Linq;

namespace HaroohieClub.NitroPacker.Core;

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
        using FileStream file = File.Create(outputRomPath);
        NdsProjectFile project = FromByteArray<NdsProjectFile>(File.ReadAllBytes(projectFilePath));

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
        foreach (Rom.RomOVT vv in ndsFile.MainOvt)
        {
            File.WriteAllBytes(Path.Combine(outPath, "overlay", $"main_{vv.Id:X4}.bin"),
                vv.Flag.HasFlag(Rom.RomOVT.OVTFlag.Compressed)
                    ? Blz.Decompress(ndsFile.FileData[vv.FileId].Data)
                    : ndsFile.FileData[vv.FileId].Data);
        }
        foreach (Rom.RomOVT vv in ndsFile.SubOvt)
        {
            File.WriteAllBytes(Path.Combine(outPath, "overlay", $"sub_{vv.Id:X4}.bin"),
                vv.Flag.HasFlag(Rom.RomOVT.OVTFlag.Compressed)
                    ? Blz.Decompress(ndsFile.FileData[vv.FileId].Data)
                    : ndsFile.FileData[vv.FileId].Data);
        }

        File.WriteAllBytes(Path.Combine(outPath, "arm9.bin"),
            decompressArm9 ? Blz.Decompress(ndsFile.MainRom) : ndsFile.MainRom);
        File.WriteAllBytes(Path.Combine(outPath, "arm7.bin"), ndsFile.SubRom);

        projectFile.RomInfo = new(ndsFile);
        if (includeFileOrder)
        {
            projectFile.RomInfo.NameEntryWithFatEntries =
            [
                .. ndsFile.Fnt.NameTable.SelectMany((t, i) => t.Where(e => e.Type == NameTableEntryType.File).Select(e => (i, e)))
                    .Zip(ndsFile.Fat.Skip(ndsFile.MainOvt.Length + ndsFile.SubOvt.Length)).Select(n => new NameEntryWithFatEntry
                    {
                        Path = NitroFsArchive.JoinPath(NitroFsArchive.GetPathFromDir(n.First.i, "/", ndsFile.Fnt), n.First.e.Name),
                        FatOffset = n.Second.FileTop,
                    }),
            ];
        }

        File.WriteAllBytes(Path.Combine(outPath, $"{name}.xml"), projectFile.Write());
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
        Rom n = new()
        {
            Header = RomInfo.Header,
            StaticFooter = RomInfo.NitroFooter,
            MainOvt = RomInfo.ARM9Ovt,
            SubOvt = RomInfo.ARM7Ovt,
            Banner = RomInfo.Banner,
            RSASignature = RomInfo.RSASignature,
            Fnt = new(),
        };

        n.Fat = new FatEntry[n.MainOvt.Length + n.SubOvt.Length];
        byte[][] fileData = new byte[n.MainOvt.Length + n.SubOvt.Length][];
        uint fid = 0;
        foreach (Rom.RomOVT vv in n.MainOvt)
        {
            vv.FileId = fid;
            n.Fat[fid] = new(0, 0);
            if (vv.Flag.HasFlag(Rom.RomOVT.OVTFlag.Compressed))
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
        foreach (Rom.RomOVT vv in n.SubOvt)
        {
            vv.FileId = fid;
            n.Fat[fid] = new(0, 0);
            if (vv.Flag.HasFlag(Rom.RomOVT.OVTFlag.Compressed))
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
        n.MainRom = File.ReadAllBytes(Path.Combine(projectDir, "arm9.bin"));
        if (compressArm9)
        {
            Blz blz = new();
            n.MainRom = blz.BLZ_Encode(n.MainRom, true);
        }
        n.SubRom = File.ReadAllBytes(Path.Combine(projectDir, "arm7.bin"));
        n.FromArchive(fsRoot, RomInfo.NameEntryWithFatEntries);

        n.Write(outputStream);
    }
}