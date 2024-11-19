using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using HaroohieClub.NitroPacker.IO.Archive;
using HaroohieClub.NitroPacker.IO.Archive.Tree;
using HaroohieClub.NitroPacker.Nitro.Card;

namespace HaroohieClub.NitroPacker.Nitro.Fs;

public class NitroFsArchive : Archive
{
    public DirectoryTableEntry[] DirTable { get; }
    public NameTableEntry[][] NameTable { get; }
    public NameFatWithData[] FileData { get; }
    public ushort FileIdOffset { get; }

    public NitroFsArchive(DirectoryTableEntry[] dirTable, NameTableEntry[][] nameTable,
        byte[][] fileData, ushort fileIdOffset = 0)
    {
        DirTable = dirTable;
        NameTable = nameTable;
        FileData = fileData.Select(t => new NameFatWithData(null, t)).ToArray();
        FileIdOffset = fileIdOffset;
    }
    

    public NitroFsArchive(DirectoryTableEntry[] dirTable, NameTableEntry[][] nameTable,
        NameFatWithData[] fileData, ushort fileIdOffset = 0)
    {
        DirTable = dirTable;
        NameTable = nameTable;
        FileData = fileData;
        FileIdOffset = fileIdOffset;
    }
        
    public NitroFsArchive(Archive archive, ushort fileIdOffset = 0, List<NameEntryWithFatEntry> nameFat = null)
    {
        if (archive is NitroFsArchive nitroFsArc)
        {
            DirTable = nitroFsArc.DirTable;
            NameTable = nitroFsArc.NameTable;
            FileData = nitroFsArc.FileData;
            FileIdOffset = nitroFsArc.FileIdOffset;
            return;
        }

        List<DirectoryTableEntry> dirTabEntries = [];
        List<NameTableEntry[]> nameTabEntries = [];
        List<NameFatWithData> fileDatas = [];
        
        FileTree fileTree = new(archive, 0xF000, fileIdOffset);
        ProcessDirectoryNode(archive, dirTabEntries, nameTabEntries, fileDatas, fileTree.Root, nameFat);

        dirTabEntries[0].ParentId = (ushort)dirTabEntries.Count;

        DirTable = dirTabEntries.ToArray();
        NameTable = nameTabEntries.ToArray();
        FileData = fileDatas.ToArray();
        FileIdOffset = fileIdOffset;
    }

    public static string GetPathFromDir(int idx, string subPath, RomFNT fnt)
    {
        if (fnt.DirectoryTable[idx].ParentId < 0xF000)
        {
            return subPath;
        }
        int parentIdx = fnt.DirectoryTable[idx].ParentId - 0xF000;
        string name = fnt.NameTable[parentIdx].First(e => e.DirectoryId == 0xF000 + idx).Name;
        subPath = JoinPath(name, subPath);
        return GetPathFromDir(parentIdx, subPath, fnt);
    }

    private static void ProcessDirectoryNode(Archive archive, List<DirectoryTableEntry> dirTable, List<NameTableEntry[]> nameTable,
        List<NameFatWithData> fileDatas, DirectoryNode dirNode, List<NameEntryWithFatEntry> nameFat = null)
    {
        if (dirNode.Id < 0xF000)
        {
            throw new DataException("Directory ID out of range");
        }
        DirectoryTableEntry thisDir = new()
        {
            EntryFileId = dirNode.EntryFileId,
            ParentId = dirNode.Parent?.Id ?? 0,
        };
        dirTable.Add(thisDir);

        List<NameTableEntry> entries = [];

        foreach (INode entry in dirNode.Children)
        {
            if (entry is FileNode file)
            {
                if (file.Id >= 0xF000)
                {
                    throw new DataException("File ID out of range");
                }
                entries.Add(NameTableEntry.File(file.Name));
                if (nameFat is null)
                {
                    fileDatas.Add(new(archive.GetFileData(JoinPath(file.Path, file.Name))));
                }
                else
                {
                    string filePath = JoinPath(file.Path, file.Name);
                    fileDatas.Add(new(nameFat.First(f => f.Path.Equals(filePath)),
                        archive.GetFileData(filePath)));
                }
            }
            else if (entry is DirectoryNode dir)
            {
                entries.Add(NameTableEntry.Directory(dir.Name, dir.Id));
            }
        }

        entries.Add(NameTableEntry.EndOfDirectory());
        
        nameTable.Add(entries.ToArray());

        foreach (DirectoryNode dir in dirNode.Children.Where(n => n is DirectoryNode).Cast<DirectoryNode>())
        {
            ProcessDirectoryNode(archive, dirTable, nameTable, fileDatas, dir, nameFat);
        }
    }

    private int FindDirectory(string path)
    {
        path = NormalizePath(path).Trim(PathSeparator);
        string[] parts = path.Split(PathSeparator);

        if (parts.Length == 0 || parts[0].Length == 0)
            return 0;

        int partIdx = 0;
        int dir = 0;
        while (partIdx < parts.Length)
        {
            foreach (NameTableEntry entry in NameTable[dir])
            {
                if (entry.Type == NameTableEntryType.EndOfDirectory)
                    throw new DataException("Invalid path specified");

                if (entry.Type == NameTableEntryType.Directory)
                {
                    if (entry.Name != parts[partIdx])
                        continue;

                    dir = entry.DirectoryId & 0xFFF;

                    if (++partIdx == parts.Length)
                        return dir;

                    break;
                }
            }
        }

        throw new("Invalid path specified");
    }

    public override IEnumerable<string> EnumerateFiles(string path, bool fullPath)
    {
        string normPath = NormalizePath(path);
        int dir = FindDirectory(normPath);

        for (int i = 0; i < NameTable[dir].Length; i++)
        {
            NameTableEntry entry = NameTable[dir][i];

            if (entry.Type == NameTableEntryType.EndOfDirectory)
                break;

            if (entry.Type == NameTableEntryType.File)
            {
                if (fullPath)
                    yield return JoinPath(normPath, entry.Name);
                else
                    yield return entry.Name;
            }
        }
    }

    public override IEnumerable<string> EnumerateDirectories(string path, bool fullPath)
    {
        string normPath = NormalizePath(path);
        int dir = FindDirectory(normPath);

        for (int i = 0; i < NameTable[dir].Length; i++)
        {
            NameTableEntry entry = NameTable[dir][i];

            if (entry.Type == NameTableEntryType.EndOfDirectory)
                break;

            if (entry.Type == NameTableEntryType.Directory)
            {
                if (fullPath)
                    yield return JoinPath(normPath, entry.Name);
                else
                    yield return entry.Name;
            }
        }
    }

    public override bool ExistsFile(string path)
    {
        if (path.EndsWith(PathSeparator))
            return false;

        string normPath = NormalizePath(path).Trim(PathSeparator);

        int val = normPath.LastIndexOf(PathSeparator);
        string dirPath = val < 0 ? RootPath : normPath.Substring(0, val);
        int dir = FindDirectory(dirPath);

        string fileName = normPath.Substring(val + 1);

        for (int i = 0; i < NameTable[dir].Length; i++)
        {
            NameTableEntry entry = NameTable[dir][i];

            if (entry.Type == NameTableEntryType.EndOfDirectory)
                break;

            if (entry.Type == NameTableEntryType.File && entry.Name == fileName)
                return true;
        }

        return false;
    }

    public override bool ExistsDirectory(string path)
    {
        try
        {
            FindDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override byte[] GetFileData(string path)
    {
        if (path.EndsWith(PathSeparator))
            throw new("Invalid path specified");

        string normPath = NormalizePath(path).Trim(PathSeparator);

        int val = normPath.LastIndexOf(PathSeparator);
        string dirPath = val < 0 ? RootPath : normPath[..val];
        int dir = FindDirectory(dirPath);

        string fileName = normPath[(val + 1)..];

        int fileId = DirTable[dir].EntryFileId;
        for (int i = 0; i < NameTable[dir].Length; i++)
        {
            NameTableEntry entry = NameTable[dir][i];

            if (entry.Type == NameTableEntryType.EndOfDirectory)
                break;

            if (entry.Type == NameTableEntryType.File)
            {
                if (entry.Name == fileName)
                    return FileData[fileId - FileIdOffset].Data;
                fileId++;
            }
        }

        throw new ArgumentException("Invalid path specified", nameof(path));
    }

    public override Stream OpenFileReadStream(string path)
        => new MemoryStream(GetFileData(path), false);
}