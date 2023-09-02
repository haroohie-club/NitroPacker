using HaroohieClub.NitroPacker.Core;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace HaroohieClub.NitroPacker.Cli
{
    public class UnpackCommand : Command
    {
        private string _projectName, _romPath, _unpackPath;
        private bool _unpackArchives = false, _decompressArm9 = false;
        public UnpackCommand() : base("unpack", "Unpacks a ROM to a directory and project XML file")
        {
            Options = new()
            {
                { "r|rom=", "Input ROM path", r => _romPath = r },
                { "o|u|output|unpack-path=", "Path to unpack ROM to", u => _unpackPath = u },
                { "p|n|project|name|project-name=", "Name of the project file", p => _projectName = p },
                { "d|decompress-arm9", "Indicates ARM9 is compressed and should be decompressed", d => _decompressArm9 = true },
                { "a|unpack-archives", "Flag to unpack NARC archives as well", a => _unpackArchives = true },
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

            NdsProjectFile.Create(_projectName, _romPath, _unpackPath, _decompressArm9, _unpackArchives);
            return 0;
        }
    }
}
