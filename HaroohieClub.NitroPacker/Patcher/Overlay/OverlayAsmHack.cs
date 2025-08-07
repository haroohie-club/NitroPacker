using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace HaroohieClub.NitroPacker.Patcher.Overlay;

/// <summary>
/// Static class for handling overlay ASM hack patching
/// </summary>
public static class OverlayAsmHack
{
    /// <summary>
    /// Compiles a directory containing ASM hacks and inserts them into an overlay binary
    /// </summary>
    /// <param name="path">The path to the directory containing the ASM hacks laid out in the structure defined in the documentation</param>
    /// <param name="overlay">The overlay object to patch</param>
    /// <param name="romInfoPath">The path to the rominfo JSON file created by NitroPacker during unpack</param>
    /// <param name="buildType">The build system type to use; specify one of Make, Docker, or Ninja/Clang</param>
    /// <param name="outputDataReceived">A handler for standard output from make/Docker/ninja & clang</param>
    /// <param name="errorDataReceived">A handler for standard error from make/Docker/ninja & clang</param>
    /// <param name="buildSystemPaths">The path to the build system executable(s) (make, docker, or ninja & clang) (only necessary to specify if not on path)</param>
    /// <param name="dockerTag">If using Docker to compile the hacks, the tag of the devkitpro/devkitarm to use (e.g. "latest"); only needs to be specified if using docker</param>
    /// <param name="devkitArmPath">The path to devkitARM (if not defined by the DEVKITARM environment variable)</param>
    /// <returns>True if patching succeeds, false if patching fails</returns>
    public static bool Insert(string path, Overlay overlay, string romInfoPath, BuildType buildType, DataReceivedEventHandler outputDataReceived = null, DataReceivedEventHandler errorDataReceived = null,
        string[] buildSystemPaths = null, string dockerTag = "latest", string devkitArmPath = "")
    {
        if ((buildSystemPaths ?? []).Length == 0)
        {
            buildSystemPaths = buildType switch
            {
                BuildType.Make => ["make"],
                BuildType.Docker => ["docker"],
                BuildType.NinjaClang => ["ninja", "/lib/llvm-19/"],
                _ => ["echo Cannot find build system"],
            };
        }

        if (buildType == BuildType.NinjaClang && buildSystemPaths!.Length == 1)
        {
            buildSystemPaths = [buildSystemPaths[0], "/lib/llvm-19/"];
        }
        
        if (!Compile(buildSystemPaths, buildType, path, overlay, outputDataReceived, errorDataReceived, dockerTag, devkitArmPath))
        {
            return false;
        }

        // Add a new symbols file based on what we just compiled so the replacements can reference the old symbols
        string[] newSym = File.ReadAllLines(Path.Combine(path, overlay.Name, "newcode.sym"));
        List<string> newSymbolsFile = [];
        foreach (string line in newSym)
        {
            Match match = Regex.Match(line, @"(?<address>[\da-f]{8}) \w[\w ]+ \.text\s+[\da-f]{8} (?<name>.+)");
            if (match.Success)
            {
                newSymbolsFile.Add($"{match.Groups["name"].Value} = 0x{match.Groups["address"].Value.ToUpper()};");
            }
        }
        File.WriteAllLines(Path.Combine(path, overlay.Name, "newcode.x"), newSymbolsFile);

        // Each repl should be compiled separately since they all have their own entry points
        // That's why each one lives in its own separate directory
        List<string> replFiles = [];
        if (Directory.Exists(Path.Combine(path, overlay.Name, "replSource")))
        {
            foreach (string subdir in Directory.GetDirectories(Path.Combine(path, overlay.Name, "replSource")))
            {
                replFiles.Add($"repl_{Path.GetFileNameWithoutExtension(subdir)}");
                if (!CompileReplace(buildSystemPaths, buildType, Path.GetRelativePath(path, subdir), path, overlay, outputDataReceived, errorDataReceived, dockerTag, devkitArmPath))
                {
                    return false;
                }
            }
        }
        if (!File.Exists(Path.Combine(path, overlay.Name, "newcode.bin")))
        {
            return false;
        }
        foreach (string replFile in replFiles)
        {
            if (!File.Exists(Path.Combine(path, overlay.Name, $"{replFile}.bin")))
            {
                return false;
            }
        }
        // We'll start by adding in the hook and append codes
        byte[] newCode = File.ReadAllBytes(Path.Combine(path, overlay.Name, "newcode.bin"));

        foreach (string line in newSym)
        {
            Match match = Regex.Match(line, @"(?<address>[\da-f]{8}) \w\s+.text\s+\d{8} (?<name>.+)");
            if (match.Success)
            {
                string[] nameSplit = match.Groups["name"].Value.Split('_');
                switch (nameSplit[0])
                {
                    case "ahook":
                        uint replaceAddress = uint.Parse(nameSplit[1], NumberStyles.HexNumber);
                        uint replace = 0xEB000000; //BL Instruction
                        uint destinationAddress = uint.Parse(match.Groups["address"].Value, NumberStyles.HexNumber);
                        uint relativeDestinationOffset = (destinationAddress / 4) - (replaceAddress / 4) - 2;
                        relativeDestinationOffset &= 0x00FFFFFF;
                        replace |= relativeDestinationOffset;
                        overlay.Patch(replaceAddress, BitConverter.GetBytes(replace));
                        break;
                }
            }
        }

        // Perform the replacements for each of the replacement hacks we assembled
        foreach (string replFile in replFiles)
        {
            byte[] replCode = File.ReadAllBytes(Path.Combine(path, overlay.Name, $"{replFile}.bin"));
            uint replaceAddress = uint.Parse(replFile.Split('_')[1], NumberStyles.HexNumber);
            overlay.Patch(replaceAddress, replCode);
        }

        overlay.Append(newCode, romInfoPath);
            
        // Clean up after ourselves
        File.Delete(Path.Combine(path, overlay.Name, "newcode.bin"));
        File.Delete(Path.Combine(path, overlay.Name, "newcode.elf"));
        File.Delete(Path.Combine(path, overlay.Name, "newcode.sym"));
        File.Delete(Path.Combine(path, overlay.Name, "newcode.x"));
        File.Delete(Path.Combine(path, overlay.Name, "arm9_newcode.x"));
        foreach (string replFile in replFiles)
        {
            File.Delete(Path.Combine(path, overlay.Name, $"{replFile}.bin"));
            File.Delete(Path.Combine(path, overlay.Name, $"{replFile}.elf"));
            File.Delete(Path.Combine(path, overlay.Name, $"{replFile}.sym"));
        }
        Directory.Delete(Path.Combine(path, "build"), true);
        return true;
    }

    private static bool Compile(string[] buildSystemPaths, BuildType buildType, string path, Overlay overlay, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, string dockerTag, string devkitArmPath)
    {
        ProcessStartInfo psi = buildType switch
        {
            BuildType.Make => new()
            {
                FileName = buildSystemPaths[0],
                Arguments = $"TARGET={overlay.Name}/newcode SOURCES={overlay.Name}/source INCLUDES={overlay.Name}/source BUILD=build CODEADDR=0x{overlay.Address + overlay.Length:X7}",
                WorkingDirectory = path,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
            BuildType.Docker => new()
            {
                FileName = buildSystemPaths[0],
                Arguments = $"run --rm -v \"{Path.GetFullPath(path)}:/src\" -w /src devkitpro/devkitarm:{dockerTag} make TARGET={overlay.Name}/newcode SOURCES={overlay.Name}/source INCLUDES={overlay.Name}/source BUILD=build CODEADDR=0x{overlay.Address + overlay.Length:X7}",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
            BuildType.NinjaClang => new()
            {
                FileName = buildSystemPaths[0],
                Arguments = $"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
            _ => new()
            {
                FileName = "echo",
                Arguments = $"Build system {buildType} not recognized!",
            },
        };
        if (!string.IsNullOrEmpty(devkitArmPath))
        {
            if (psi.EnvironmentVariables.ContainsKey("DEVKITARM"))
            {
                psi.EnvironmentVariables["DEVKITARM"] = devkitArmPath;
            }
            else
            {
                psi.EnvironmentVariables.Add("DEVKITARM", devkitArmPath);
            }
            if (psi.EnvironmentVariables.ContainsKey("DEVKITPRO"))
            {
                psi.EnvironmentVariables["DEVKITPRO"] = Path.GetDirectoryName(devkitArmPath);
            }
            else
            {
                psi.EnvironmentVariables.Add("DEVKITPRO", Path.GetDirectoryName(devkitArmPath));
            }
        }
        Process p = new() { StartInfo = psi };
        p.OutputDataReceived += outputDataReceived ?? Func;
        p.ErrorDataReceived += errorDataReceived ?? Func;
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        return p.ExitCode == 0;

        static void Func(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }

    private static bool CompileReplace(string[] buildSystemPaths, BuildType buildType, string subDirectory, string path, Overlay overlay, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, string dockerTag, string devkitArmPath)
    {
        uint address = uint.Parse(Path.GetFileNameWithoutExtension(subDirectory), NumberStyles.HexNumber);

        ProcessStartInfo psi = buildType switch
        {
            BuildType.Make => new()
            {
                FileName = buildSystemPaths[0],
                Arguments =
                    $"TARGET={overlay.Name}/repl_{Path.GetFileNameWithoutExtension(subDirectory)} SOURCES={subDirectory} INCLUDES={subDirectory} NEWSYM={overlay.Name}/newcode.x BUILD=build CODEADDR=0x{address:X7}",
                WorkingDirectory = path,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
            BuildType.Docker => new()
            {
                FileName = buildSystemPaths[0],
                Arguments =
                    $"run --rm -v \"{Path.GetFullPath(path)}:/src\" -w /src devkitpro/devkitarm:{dockerTag} make TARGET={overlay.Name}/repl_{Path.GetFileNameWithoutExtension(subDirectory.Replace('\\', '/'))} SOURCES={subDirectory.Replace('\\', '/')} INCLUDES={subDirectory.Replace('\\', '/')} NEWSYM={overlay.Name}/newcode.x BUILD=build CODEADDR=0x{address:X7}",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
            BuildType.NinjaClang => new()
            {
                FileName = buildSystemPaths[0],
                Arguments =
                    $"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            },
            _ => new()
            {
                FileName = "echo",
                Arguments = $"Build system {buildType} not recognized!",
            },
        };
        if (!string.IsNullOrEmpty(devkitArmPath))
        {
            if (psi.EnvironmentVariables.ContainsKey("DEVKITARM"))
            {
                psi.EnvironmentVariables["DEVKITARM"] = devkitArmPath;
            }
            else
            {
                psi.EnvironmentVariables.Add("DEVKITARM", devkitArmPath);
            }
            if (psi.EnvironmentVariables.ContainsKey("DEVKITPRO"))
            {
                psi.EnvironmentVariables["DEVKITPRO"] = Path.GetDirectoryName(devkitArmPath);
            }
            else
            {
                psi.EnvironmentVariables.Add("DEVKITPRO", Path.GetDirectoryName(devkitArmPath));
            }
        }
        Process p = new() { StartInfo = psi };
        p.OutputDataReceived += outputDataReceived ?? Func;
        p.ErrorDataReceived += errorDataReceived ?? Func;
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        return p.ExitCode == 0;

        static void Func(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}