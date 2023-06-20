using HaroohieClub.NitroPacker.Patcher.Nitro;
using Mono.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace HaroohieClub.NitroPacker.Cli
{
    public class PatchArm9Command : Command
    {
        private string _inputDir, _outputDir, _dockerTag, _devkitArm;
        private uint _arenaLoOffset = 0;

        public PatchArm9Command() : base("patch-arm9", "Patches the game's arm9.bin")
        {
            Options = new()
            {
                { "i|input-dir=", "Input directory containing arm9.bin and source", i => _inputDir = i },
                { "o|output-dir=", "Output directory for writing modified arm9.bin", o => _outputDir = o },
                { "a|arena-lo-offset=", "ArenaLoOffset provided as a hex number", a => _arenaLoOffset = uint.Parse(a, NumberStyles.HexNumber) },
                { "d|docker-tag=", "(Optional) Indicates docker should be used and provides a docker tag of the devkitpro/devkitarm image to use", d => _dockerTag = d },
                { "devkitarm=", "(Optional) Location of the devkitARM installation; defaults to the DEVKITARM environment variable", dev => _devkitArm = dev },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

            if (_arenaLoOffset == 0)
            {
                CommandSet.Out.WriteLine($"ArenaLoOffset must be provided!\n\n{_arenaLoOffset}");
                return 1;
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

            ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(_inputDir, "arm9.bin")), 0x02000000);
            if (!ARM9AsmHack.Insert(_inputDir, arm9, _arenaLoOffset, _dockerTag,
                (object sender, DataReceivedEventArgs e) => Console.WriteLine(e.Data),
                (object sender, DataReceivedEventArgs e) => Console.Error.WriteLine(e.Data),
                devkitArmPath: _devkitArm))
            {
                Console.WriteLine("ERROR: ASM hack insertion failed!");
                return 1;
            }
            File.WriteAllBytes(Path.Combine(_outputDir, "arm9.bin"), arm9.GetBytes());

            return 0;
        }
    }
}
