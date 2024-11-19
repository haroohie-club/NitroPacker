using System.Collections.Generic;
using System.IO;
using HaroohieClub.NitroPacker.Core;
using Mono.Options;

namespace HaroohieClub.NitroPacker.Cli;

public class MigrateProjectCommand : Command
{
    private string _projectXml;
    
    public MigrateProjectCommand() : base("migrate", "Migrates a project from the 2.x format to the 3.x+ format")
    {
        Options = new()
        {
            { "i|p|input|project=", "The project XML to port to the new project format", p => _projectXml = p },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);
        if (string.IsNullOrEmpty(_projectXml) || !File.Exists(_projectXml))
        {
            CommandSet.Error.WriteLine("You must provide an existing project XML file!");
            return 1;
        }
        
        NdsProjectFile.ConvertProjectFile(_projectXml);
        
        return 0;
    }
}