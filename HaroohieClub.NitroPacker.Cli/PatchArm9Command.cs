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
        private string _inputDir, _outputDir;
        private uint _arenaLoOffset;
        private bool _useDocker;

        public PatchArm9Command() : base("patch-arm9", "Patches the game's arm9.bin")
        {
            Options = new()
            {
                { "i|input-dir=", "Input directory containing arm9.bin and source", i => _inputDir = i },
                { "o|output-dir=", "Output directory for writing modified arm9.bin", o => _outputDir = o },
                { "a|arena-lo-offset=", "ArenaLoOffset provided as a hex number", a => _arenaLoOffset = uint.Parse(a, NumberStyles.HexNumber) },
                { "d|use-docker", "Use docker to build rather than make directly", d => _useDocker = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);

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
                throw new ArgumentException($"Input directory {_inputDir} does not exist!");
            }
            if (!Directory.Exists(_outputDir))
            {
                Directory.CreateDirectory(_outputDir);
            }

            ARM9 arm9 = new(File.ReadAllBytes(Path.Combine(_inputDir, "arm9.bin")), 0x02000000);
            if (!ARM9AsmHack.Insert(_inputDir, arm9, _arenaLoOffset, _useDocker,
                (object sender, DataReceivedEventArgs e) => Console.WriteLine(e.Data),
                (object sender, DataReceivedEventArgs e) => Console.Error.WriteLine(e.Data)))
            {
                Console.WriteLine("ERROR: ASM hack insertion failed!");
                return 1;
            }
            File.WriteAllBytes(Path.Combine(_outputDir, "arm9.bin"), arm9.GetBytes());

            return 0;
        }
    }
}
