using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HaroohieClub.NitroPacker.Patcher;
using HaroohieClub.NitroPacker.Patcher.Overlay;
using Mono.Options;

namespace HaroohieClub.NitroPacker.Cli;

public class PatchOverlaysCommand : Command
{
    private string _inputOverlaysDirectory, _outputOverlaysDirectory, _overlaySourceDir, _romInfoPath, _buildSystemPath, _dockerTag = "latest", _devkitArm, _overrideSuffix;
    private BuildType _buildType = BuildType.Ninja;

    public PatchOverlaysCommand() : base("patch-overlays", "Patches the game's overlays")
    {
        Options = new()
        {
            "Patches the game's overlays given a Riivolution-style XML file and a rominfo.xml",
            "Usage: HaruhiChokuretsuCLI patch-overlays -i [inputOverlaysDirectory] -o [outputOverlaysDirectory] -p [overlayPatch] -r [romInfo]",
            "",
            { "i|input-overlays=", "Directory containing unpatched overlays", i => _inputOverlaysDirectory = i },
            { "o|output-overlays=", "Directory where patched overlays will be written", o => _outputOverlaysDirectory = o },
            { "s|source-dir=", "Directory where overlay source code lives", s => _overlaySourceDir = s },
            { "r|project=", "Project JSON file containing the overlay table", r => _romInfoPath = r },
            { "b|build-type=", "The build system to use; specify one of 'make', 'docker', or 'ninja'", b => _buildType = b.ToLower() switch
            {
                "make" => BuildType.Make,
                "docker" => BuildType.Docker,
                "ninja" => BuildType.Ninja,
                _ => BuildType.NotSpecified,
            }},
            { "build-system-path=", "The path to the build system executable; defaults to just using an executable on the path", b => _buildSystemPath = b },
            { "d|docker-tag=", "(Optional) Indicates a docker tag of the devkitpro/devkitarm image to use (defaults to 'latest')", d => _dockerTag = d },
            { "devkitarm=", "(Optional) Location of the devkitARM installation; defaults to the DEVKITARM environment variable", dev => _devkitArm = dev },
            { "override-suffix=", "(Optional) A file extension suffix to indicate that a general file should be overridden, good for using with e.g. locales", o => _overrideSuffix = o },
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrEmpty(_inputOverlaysDirectory) || string.IsNullOrEmpty(_outputOverlaysDirectory) || string.IsNullOrEmpty(_overlaySourceDir) || string.IsNullOrEmpty(_romInfoPath) || _buildType == BuildType.NotSpecified)
        {
            int returnValue = 0;
            if (string.IsNullOrEmpty(_inputOverlaysDirectory))
            {
                CommandSet.Out.WriteLine("Input overlays directory not provided, please supply -i or --input-overlays");
                returnValue = 1;
            }
            if (string.IsNullOrEmpty(_outputOverlaysDirectory))
            {
                CommandSet.Out.WriteLine("Output overlays directory not provided, please supply -o or --output-overlays");
                returnValue = 1;
            }
            if (string.IsNullOrEmpty(_overlaySourceDir))
            {
                CommandSet.Out.WriteLine("Overlay source directory not provided, please supply -s or --source-dir");
                returnValue = 1;
            }
            if (string.IsNullOrEmpty(_romInfoPath))
            {
                CommandSet.Out.WriteLine("rominfo.xml not provided, please supply -r or --rom-info");
                returnValue = 1;
            }
            if (_buildType == BuildType.NotSpecified)
            {
                CommandSet.Out.WriteLine("Build type not specified or not recognized, please specify one of 'make', 'docker', or 'ninja' with -b or --build-type");
                returnValue = 1;
            }
            Options.WriteOptionDescriptions(CommandSet.Out);
            return returnValue;
        }

        if (!Directory.Exists(_outputOverlaysDirectory))
        {
            Directory.CreateDirectory(_outputOverlaysDirectory);
        }

        List<Overlay> overlays = [];
        overlays.AddRange(Directory.GetFiles(_inputOverlaysDirectory).Select(file => new Overlay(file, _romInfoPath)));

        List<(string, string)> renames = Utilities.RenameOverrideFiles(_overlaySourceDir, _overrideSuffix, CommandSet.Out);

        foreach (Overlay overlay in overlays)
        {
            if (Directory.GetDirectories(_overlaySourceDir).Contains(Path.Combine(_overlaySourceDir, overlay.Name)))
            {
                OverlayAsmHack.Insert(_overlaySourceDir, overlay, _romInfoPath, _buildType, 
                    (_, e) => Console.WriteLine(e.Data),
                    (_, e) => Console.Error.WriteLine(e.Data),
                    _buildSystemPath, _dockerTag, _devkitArm);
            }
        }

        Utilities.RevertOverrideFiles(renames);

        foreach (Overlay overlay in overlays)
        {
            overlay.Save(Path.Combine(_outputOverlaysDirectory, $"{overlay.Name}.bin"));
        }

        return 0;
    }
}