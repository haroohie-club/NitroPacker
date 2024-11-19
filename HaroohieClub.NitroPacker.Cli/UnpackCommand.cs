using System.Collections.Generic;
using HaroohieClub.NitroPacker;
using Mono.Options;

namespace HaroohieClub.NitroPacker.Cli;

public class UnpackCommand : Command
{
    private string _projectName, _romPath, _unpackPath;
    private bool _unpackArchives, _decompressArm9, _includeFnt;
    public UnpackCommand() : base("unpack", "Unpacks a ROM to a directory and project XML file")
    {
        Options = new()
        {
            { "r|rom=", "Input ROM path", r => _romPath = r },
            { "o|u|output|unpack-path=", "Path to unpack ROM to", u => _unpackPath = u },
            { "p|n|project|name|project-name=", "Name of the project file", p => _projectName = p },
            { "d|decompress-arm9", "If specified, will attempt to decompress the ARM9 binary", d => _decompressArm9 = true },
            { "f|include-file-order", "If specified, will include the order of the files in the project file (some games might require this to repack); " +
                               "if true, no files can be added or removed after unpacking (though they still can be modified)", f => _includeFnt = true },
            { "a|unpack-archives", "If specified, will unpack NARC archives as well", a => _unpackArchives = true },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrEmpty(_projectName))
        {
            CommandSet.Out.WriteLine($"Must provide project name.");
            Options.WriteOptionDescriptions(CommandSet.Out);
            return 1;
        }
        if (string.IsNullOrEmpty(_romPath))
        {
            CommandSet.Out.WriteLine($"Must provide path to ROM.");
            Options.WriteOptionDescriptions(CommandSet.Out);
            return 1;
        }
        if (string.IsNullOrEmpty(_unpackPath))
        {
            CommandSet.Out.WriteLine($"Must provide path to unpack to.");
            Options.WriteOptionDescriptions(CommandSet.Out);
            return 1;
        }

        NdsProjectFile.Create(_projectName, _romPath, _unpackPath, _decompressArm9, _unpackArchives, _includeFnt);
        return 0;
    }
}