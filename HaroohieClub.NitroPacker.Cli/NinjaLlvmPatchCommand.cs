using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HaroohieClub.NitroPacker.Patcher;
using HaroohieClub.NitroPacker.Patcher.Nitro;
using Mono.Options;

namespace HaroohieClub.NitroPacker.Cli;

public class NinjaLlvmPatchCommand : Command
{
    private string _inputDir,
        _outputDir,
        _inputOverlaysDirectory,
        _outputOverlaysDirectory,
        _ninja,
        _llvm,
        _symTableHelper,
        _projectFilePath,
        _overrideSuffix;

    private uint _arenaLoOffset = 0;

    public NinjaLlvmPatchCommand() : base("ninja-llvm-patch",
        "Patches the game's arm9.bin and overlays using ninja & LLVM")
    {
        Options = new()
        {
            { "i|input-dir=", "Input directory containing arm9.bin and source", i => _inputDir = i },
            { "o|output-dir=", "Output directory for writing modified arm9.bin", o => _outputDir = o },
            { "input-overlays=", "Directory containing unpatched overlays", i => _inputOverlaysDirectory = i },
            { "output-overlays=", "Directory where patched overlays will be written", o => _outputOverlaysDirectory = o },
            { "n|ninja=", "Path to ninja executable", n => _ninja = n },
            { "l|llvm=", "Path to the LLVM root directory", l => _llvm = l },
            { "s|sym-table-helper=", "Path to NitroPacker.SymTableHelper executable", s => _symTableHelper = s },
            { "a|arena-lo-offset=", "ArenaLoOffset provided as a hex number", a => _arenaLoOffset = uint.Parse(a.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? a[2..] : a, NumberStyles.HexNumber) },
            { "p|project-file=", "An NDS project file from extracting with NitroPacker (can be provided instead of a RAM address)", p => _projectFilePath = p },
            { "override-suffix=", "(Optional) A file extension suffix to indicate that a general file should be overridden, good for using with e.g. locales", o => _overrideSuffix = o },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrEmpty(_inputOverlaysDirectory) || string.IsNullOrEmpty(_outputOverlaysDirectory) ||
            string.IsNullOrEmpty(_inputDir) || string.IsNullOrEmpty(_projectFilePath))
        {
            int returnValue = 0;
            if (string.IsNullOrEmpty(_inputOverlaysDirectory))
            {
                CommandSet.Out.WriteLine("Input overlays directory not provided, please supply -i or --input-overlays");
                returnValue = 1;
            }

            if (string.IsNullOrEmpty(_outputOverlaysDirectory))
            {
                CommandSet.Out.WriteLine(
                    "Output overlays directory not provided, please supply -o or --output-overlays");
                returnValue = 1;
            }

            if (string.IsNullOrEmpty(_inputDir))
            {
                CommandSet.Out.WriteLine("Overlay source directory not provided, please supply -s or --source-dir");
                returnValue = 1;
            }

            if (string.IsNullOrEmpty(_projectFilePath))
            {
                CommandSet.Out.WriteLine("rominfo.xml not provided, please supply -r or --rom-info");
                returnValue = 1;
            }

            if (string.IsNullOrEmpty(_llvm))
            {
                CommandSet.Out.WriteLine("LLVM root directory not provided, please supply -l or --llvm");
                returnValue = 1;
            }

            Options.WriteOptionDescriptions(CommandSet.Out);
            return returnValue;
        }

        if (_arenaLoOffset == 0)
        {
            CommandSet.Out.WriteLine($"ArenaLoOffset must be provided!\n\n{Help}");
            return 1;
        }

        if (string.IsNullOrEmpty(_ninja))
        {
            _ninja = "ninja";
        }

        if (string.IsNullOrEmpty(_inputDir))
        {
            _inputDir = Path.Combine(Environment.CurrentDirectory, "src");
        }

        if (string.IsNullOrEmpty(_outputDir))
        {
            _outputDir = Path.Combine(Environment.CurrentDirectory, "rom");
        }

        if (!Directory.Exists(_inputDir))
        {
            CommandSet.Out.WriteLine($"Input directory {_inputDir} does not exist!\n\n{Help}");
            return 1;
        }

        if (!Directory.Exists(_outputDir))
        {
            Directory.CreateDirectory(_outputDir);
        }

        List<(string, string)> renames = Utilities.RenameOverrideFiles(_inputDir, _overrideSuffix, CommandSet.Out);

        NdsProjectFile project = NdsProjectFile.Deserialize(_projectFilePath);
        ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(_inputDir, "arm9.bin")), project.RomInfo.Header.Arm9RamAddress);
        NinjaLlvmPatch.Patch(_inputDir, arm9, _inputOverlaysDirectory, _ninja, _llvm,
            _projectFilePath, _arenaLoOffset, _symTableHelper,
            (_, e) => Console.WriteLine(e.Data),
            (_, e) => Console.Error.WriteLine(e.Data));
        File.WriteAllBytes(Path.Combine(_outputDir, "arm9.bin"), arm9.GetBytes());

        Utilities.RevertOverrideFiles(renames);

        return 0;
    }
}