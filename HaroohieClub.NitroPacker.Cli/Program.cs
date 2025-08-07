using Mono.Options;

namespace HaroohieClub.NitroPacker.Cli;

public class Program
{
    public static void Main(string[] args)
    {
        CommandSet commands = new("NitroPacker")
        {
            new UnpackCommand(),
            new PackCommand(),
            new NinjaLlvmPatchCommand(),
            new PatchArm9Command(),
            new PatchOverlaysCommand(),
            new MigrateProjectCommand(),
        };
        commands.Run(args);
    }
}