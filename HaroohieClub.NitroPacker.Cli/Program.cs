using HaroohieClub.NitroPacker.Core;
using Mono.Options;
using System.IO;

namespace HaroohieClub.NitroPacker.Cli;

public class Program
{
    public static void Main(string[] args)
    {
        CommandSet commands = new("NitroPacker")
        {
            new UnpackCommand(),
            new PackCommand(),
            new PatchArm9Command(),
            new PatchOverlaysCommand(),
        };
        commands.Run(args);
    }
}