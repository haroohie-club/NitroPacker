using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using HaroohieClub.NitroPacker.Core;
using HaroohieClub.NitroPacker.Patcher.Nitro;
using Mono.Options;

namespace HaroohieClub.NitroPacker.Cli;

public class PatchArm9Command : Command
{
    private string _inputDir, _outputDir, _projectFilePath, _dockerTag, _devkitArm, _overrideSuffix;
    private uint _arenaLoOffset = 0, _ramAddress = 0;

    public PatchArm9Command() : base("patch-arm9", "Patches the game's arm9.bin")
    {
        Options = new()
        {
            { "i|input-dir=", "Input directory containing arm9.bin and source", i => _inputDir = i },
            { "o|output-dir=", "Output directory for writing modified arm9.bin", o => _outputDir = o },
            { "a|arena-lo-offset=", "ArenaLoOffset provided as a hex number", a => _arenaLoOffset = uint.Parse(a.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? a[2..] : a, NumberStyles.HexNumber) },
            { "p|project-file=", "An NDS project file from extracting with NitroPacker (can be provided instead of a RAM address)", p => _projectFilePath = p },
            { "r|ram-address=", "The address at which the ROM is loaded into NDS RAM", r => _ramAddress = uint.Parse(r.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? r[2..] : r, NumberStyles.HexNumber) },
            { "d|docker-tag=", "(Optional) Indicates Docker should be used and provides a docker tag of the devkitpro/devkitarm image to use", d => _dockerTag = d },
            { "devkitarm=", "(Optional) Location of the devkitARM installation; defaults to the DEVKITARM environment variable", dev => _devkitArm = dev },
            { "override-suffix=", "(Optional) A file extension suffix to indicate that a general file should be overridden, good for using with e.g. locales", o => _overrideSuffix = o },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (_arenaLoOffset == 0)
        {
            CommandSet.Out.WriteLine($"ArenaLoOffset must be provided!\n\n{Help}");
            return 1;
        }

        if (string.IsNullOrEmpty(_projectFilePath) && _ramAddress == 0)
        {
            CommandSet.Out.WriteLine("Neither project file nor RAM address were specified; assuming ARM9 RAM address is 0x2000000...");
            _ramAddress = 0x2000000;
        }
        else if (_ramAddress == 0)
        {
            NdsProjectFile project = JsonSerializer.Deserialize<NdsProjectFile>(_projectFilePath);
            _ramAddress = project.RomInfo.Header.MainRamAddress;
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

        ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(_inputDir, "arm9.bin")), _ramAddress);
        if (!ARM9AsmHack.Insert(_inputDir, arm9, _arenaLoOffset, _dockerTag,
                (object sender, DataReceivedEventArgs e) => Console.WriteLine(e.Data),
                (object sender, DataReceivedEventArgs e) => Console.Error.WriteLine(e.Data),
                devkitArmPath: _devkitArm))
        {
            Console.WriteLine("ERROR: ASM hack insertion failed!");
            Utilities.RevertOverrideFiles(renames);
            return 1;
        }
        File.WriteAllBytes(Path.Combine(_outputDir, "arm9.bin"), arm9.GetBytes());

        Utilities.RevertOverrideFiles(renames);

        return 0;
    }
}