using System.IO;
using System.Threading.Tasks;
using HaroohieClub.NitroPacker.Nitro.Fs;

namespace HaroohieClub.NitroPacker;

internal static class NitroFsArchiveExtensions
{
    public static void Export(this NitroFsArchive root, string outPath, bool unpackArc = false)
        => ExtractDirectories(root, "/", outPath, unpackArc);

    private static void ExtractDirectories(NitroFsArchive dir, string dirPath, string outPath, bool unpackArc = false)
    {
        string path = outPath + dirPath;

        Directory.CreateDirectory(path);

        foreach (string f in dir.EnumerateFiles(dirPath, true))
        {
            File.WriteAllBytes(outPath + f, dir.GetFileData(f));
        }

        Parallel.ForEach(dir.EnumerateDirectories(dirPath, true), d =>
        {
            ExtractDirectories(dir, d, outPath, unpackArc);
        });
    }
}