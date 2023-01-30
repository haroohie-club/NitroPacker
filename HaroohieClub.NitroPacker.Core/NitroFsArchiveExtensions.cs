using HaroohiePals.Nitro.Fs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaroohieClub.NitroPacker.Core
{
    internal static class NitroFsArchiveExtensions
    {
        public static void Export(this NitroFsArchive root, string outPath, bool unpackArc = false)
           => ExtractDirectories(root, "/", outPath, unpackArc);

        private static void ExtractDirectories(NitroFsArchive dir, string dirPath, string outPath, bool unpackArc = false)
        {
            var path = outPath + dirPath;

            Directory.CreateDirectory(path);

            foreach (var f in dir.EnumerateFiles(dirPath, true))
            {
                File.WriteAllBytes(outPath + f, dir.GetFileData(f));
            }

            Parallel.ForEach(dir.EnumerateDirectories(dirPath, true), d =>
            {
                ExtractDirectories(dir, d, outPath, unpackArc);
            });
        }
    }
}
