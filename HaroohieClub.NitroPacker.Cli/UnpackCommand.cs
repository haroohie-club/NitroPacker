using HaroohieClub.NitroPacker.Core;
using Mono.Options;
using System.Collections.Generic;

namespace HaroohieClub.NitroPacker.Cli
{
    public class UnpackCommand : Command
    {
        private string _projectName, _romPath, _unpackPath;
        private bool _unpackArchives = false;
        public UnpackCommand() : base("unpack", "Unpacks a ROM to a directory and project XML file")
        {
            Options = new()
            {
                { "r|rom=", "Input ROM path", r => _romPath = r },
                { "o|u|output|unpack-path=", "Path to unpack ROM to", u => _unpackPath = u },
                { "p|n|project|name|project-name=", "Name of the project file", p => _projectName = p },
                { "a|unpack-archives", "Flag to unpack NARC archives as well", a => _unpackArchives = true },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            NdsProjectFile.Create(_projectName, _romPath, _unpackPath, _unpackArchives);
            return 0;
        }
    }
}
