using System.Collections.Generic;
using Mono.Options;

namespace HaroohieClub.NitroPacker.Cli;

public class PackCommand : Command
{
    private string _projectJson, _outputRom;
    private bool _compressArm9;
    public PackCommand() : base("pack", "Packs a ROM given a project JSON file")
    {
        Options = new()
        {
            { "p|i|project|input=", "Input project JSON file", p => _projectJson = p },
            { "r|o|rom|output=", "Output ROM path", o => _outputRom = o },
            { "c|compress-arm9", "Indicates ARM9 should be compressed", _ => _compressArm9 = true },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrEmpty(_projectJson))
        {
            CommandSet.Out.WriteLine($"Must provide path to project JSON file.");
            Options.WriteOptionDescriptions(CommandSet.Out);
            return 1;
        }
        if (string.IsNullOrEmpty(_outputRom))
        {
            CommandSet.Out.WriteLine($"Must provide path to output ROM to.");
            Options.WriteOptionDescriptions(CommandSet.Out);
            return 1;
        }

        NdsProjectFile.Pack(_outputRom, _projectJson, _compressArm9);
        return 0;
    }
}