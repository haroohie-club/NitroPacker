using System;
using System.Collections.Generic;
using System.IO;

namespace HaroohieClub.NitroPacker.Cli
{
    internal static class Utilities
    {
        public static List<(string, string)> RenameOverrideFiles(string sourceDir, string overrideSuffix, TextWriter log)
        {
            List<(string, string)> renames = [];
            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(file).Equals(overrideSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    string overridableFile = Path.GetFileNameWithoutExtension(file);
                    string ignoredFile = $"{overridableFile}.ignore";
                    renames.Add((overridableFile, file));
                    if (File.Exists(overridableFile))
                    {
                        renames.Add((ignoredFile, overridableFile));
                        File.Move(overridableFile, ignoredFile);
                    }
                    File.Move(file, overridableFile);
                }
            }
            return renames;
        }

        public static void RevertOverrideFiles(List<(string, string)> renames)
        {
            foreach ((string renamed, string orig) in renames)
            {
                File.Move(renamed, orig);
            }
        }
    }
}
