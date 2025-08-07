using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace HaroohieClub.NitroPacker.SymTableHelper;

partial class Program
{
    static void Main(string[] args)
    {
        string file = args[0];
        string[] newSymLines = File.ReadAllLines(file);
        List<string> newSymbolsFile = new();
        foreach (string line in newSymLines)
        {
            Match match = SymTableRegex().Match(line);
            if (match.Success)
            {
                newSymbolsFile.Add($"{match.Groups["name"].Value} = 0x{match.Groups["address"].Value.ToUpper()};");
            }
        }
        File.WriteAllLines(args[1], newSymbolsFile);
    }

    [GeneratedRegex(@"(?<address>[\da-f]{8}) \w[\w ]+ \.text\s+[\da-f]{8} (?<name>.+)")]
    private static partial Regex SymTableRegex();
}