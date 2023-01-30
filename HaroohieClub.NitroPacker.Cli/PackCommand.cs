using HaroohieClub.NitroPacker.Core;
using Mono.Options;
using System.Collections.Generic;

namespace HaroohieClub.NitroPacker.Cli
{
    public class PackCommand : Command
    {
        string _projectXml, _outputRom;
        public PackCommand() : base("pack", "Packs a ROM given a project XML file")
        {
            Options = new()
            {
                { "p|i|project|input=", "Input project XML file", p => _projectXml = p },
                { "r|o|rom|output=", "Output ROM path", o => _outputRom = o },
            };
        }

        public override int Invoke(IEnumerable<string> arguments)
        {
            Options.Parse(arguments);
            NdsProjectFile.Pack(_outputRom, _projectXml);
            return 0;
        }
    }
}
