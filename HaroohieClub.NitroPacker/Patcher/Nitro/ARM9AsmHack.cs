// This code is heavily based on code Gericom wrote for ErmiiBuild

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace HaroohieClub.NitroPacker.Patcher.Nitro;

/// <summary>
/// Static class for handling ARM9 ASM hack patching
/// </summary>
public static class ARM9AsmHack
{
    /// <summary>
    /// Compiles a directory containing ASM hacks and inserts them into the ARM9 binary
    /// </summary>
    /// <param name="path">The path to the directory containing the ASM hacks laid out in the structure defined in the documentation</param>
    /// <param name="arm9">The ARM9 object to patch</param>
    /// <param name="arenaLoOffset">The ArenaLo offset of the ROM (can be determined as described in the documentation)</param>
    /// <param name="dockerTag">If using Docker to compile the hacks, the tag of the devkitpro/devkitarm to use (e.g. "latest"); leave empty or null if not using Docker</param>
    /// <param name="outputDataReceived">A handler for standard output from make/Docker</param>
    /// <param name="errorDataReceived">A handler for standard error from make/Docker</param>
    /// <param name="makePath">The path to the make executable (if using and not on path)</param>
    /// <param name="dockerPath">The path to the docker executable (if using and not on path)</param>
    /// <param name="devkitArmPath">The path to devkitARM (if not defined by the DEVKITARM environment variable)</param>
    /// <returns>True if patching succeeds, false if patching fails</returns>
    /// <exception cref="Exception"></exception>
    public static bool Insert(string path, ARM9 arm9, uint arenaLoOffset, string dockerTag, DataReceivedEventHandler outputDataReceived = null, DataReceivedEventHandler errorDataReceived = null,
        string makePath = "make", string dockerPath = "docker", string devkitArmPath = "")
    {
        uint arenaLo = arm9.ReadU32LE(arenaLoOffset);
        byte[] newCode;
        if (Directory.GetFiles(Path.Combine(path, "source")).Length > 0)
        {
            if (!Compile(makePath, dockerPath, path, arenaLo, outputDataReceived, errorDataReceived, dockerTag, devkitArmPath))
            {
                return false;
            }
            if (!File.Exists(Path.Combine(path, "newcode.bin")))
            {
                return false;
            }
            newCode = File.ReadAllBytes(Path.Combine(path, "newcode.bin"));
            if (newCode.Length % 4 != 0)
            {
                newCode = [.. newCode, .. new byte[4 - newCode.Length % 4]];
            }
        }
        else
        {
            File.WriteAllText(Path.Combine(path, "newcode.sym"), string.Empty);
            newCode = [];
        }
        
        string[] newSymLines = File.ReadAllLines(Path.Combine(path, "newcode.sym"));
        List<string> newSymbolsFile = [];
        foreach (string line in newSymLines)
        {
            Match match = Regex.Match(line, @"(?<address>[\da-f]{8}) \w[\w ]+ \.text\s+[\da-f]{8} (?<name>.+)");
            if (match.Success)
            {
                newSymbolsFile.Add($"{match.Groups["name"].Value} = 0x{match.Groups["address"].Value.ToUpper()};");
            }
        }
        File.WriteAllLines(Path.Combine(path, "newcode.x"), newSymbolsFile);

        // Each repl should be compiled separately since they all have their own entry points
        // That's why each one lives in its own separate directory
        List<string> replFiles = [];
        if (Directory.Exists(Path.Combine(path, "replSource")))
        {
            foreach (string subdir in Directory.GetDirectories(Path.Combine(path, "replSource")))
            {
                replFiles.Add($"repl_{Path.GetFileNameWithoutExtension(subdir)}");
                if (!CompileReplace(makePath, dockerPath, Path.GetRelativePath(path, subdir), path, outputDataReceived, errorDataReceived, dockerTag, devkitArmPath))
                {
                    return false;
                }
            }
        }

        foreach (string replFile in replFiles)
        {
            if (!File.Exists(Path.Combine(path, $"{replFile}.bin")))
            {
                return false;
            }
        }

        PatchArm9(path, arm9, arenaLoOffset, arenaLo, newCode);
        
        // Perform the replacements for each of the replacement hacks we assembled
        foreach (string replFile in replFiles)
        {
            byte[] replCode = File.ReadAllBytes(Path.Combine(path, $"{replFile}.bin"));
            uint replaceAddress = uint.Parse(replFile.Split('_')[1], NumberStyles.HexNumber);
            arm9.WriteBytes(replaceAddress, replCode);
        }

        File.Delete(Path.Combine(path, "newcode.bin"));
        File.Delete(Path.Combine(path, "newcode.elf"));
        File.Delete(Path.Combine(path, "newcode.sym"));
        foreach (string overlayDirectory in Directory.GetDirectories(Path.Combine(path, "overlays")))
        {
            File.Copy(Path.Combine(path, "newcode.x"), Path.Combine(overlayDirectory, "arm9_newcode.x"), overwrite: true);
        }
        File.Delete(Path.Combine(path, "newcode.x"));
        foreach (string replFile in replFiles)
        {
            File.Delete(Path.Combine(path, $"{replFile}.bin"));
            File.Delete(Path.Combine(path, $"{replFile}.elf"));
            File.Delete(Path.Combine(path, $"{replFile}.sym"));
        }

        if (Directory.Exists(Path.Combine(path, "build")))
        {
            Directory.Delete(Path.Combine(path, "build"), true);
        }
        return true;
    }

    internal static void PatchArm9(string path, ARM9 arm9, uint arenaLoOffset, uint arenaLo, byte[] newCode)
    {
        StreamReader r = new(Path.Combine(path, "newcode.sym"));
        while (r.ReadLine() is { } currentLine)
        {
            string[] lines = currentLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 4)
            {
                if (lines[3].Length < 7) continue;
                switch (lines[3].Remove(6))
                {
                    case "ahook_":
                    {
                        string replaceOffsetString = lines[3].Replace("ahook_", "");
                        uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
                        uint replace = 0xEB000000; //BL Instruction
                        uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
                        uint relativeDestinationOffset = (destinationOffset / 4) - (replaceOffset / 4) - 2;
                        relativeDestinationOffset &= 0x00FFFFFF;
                        replace |= relativeDestinationOffset;
                        if (!arm9.WriteU32LE(replaceOffset, replace))
                        {
                            throw new(
                                $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset."
                            );
                        }
                        break;
                    }
                    case "ansub_":
                    {
                        string replaceOffsetString = lines[3].Replace("ansub_", "");
                        uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
                        uint replace = 0xEA000000;//B Instruction
                        uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
                        uint relativeDestinationOffset = (destinationOffset / 4) - (replaceOffset / 4) - 2;
                        relativeDestinationOffset &= 0x00FFFFFF;
                        replace |= relativeDestinationOffset;
                        if (!arm9.WriteU32LE(replaceOffset, replace))
                        {
                            throw new(
                                $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset."
                            );
                        }
                        break;
                    }
                    case "thook_":
                    {
                        string replaceOffsetString = lines[3].Replace("thook_", "");
                        uint replaceOffset = uint.Parse(replaceOffsetString, NumberStyles.HexNumber);
                        ushort replace1 = 0xF000;//BLX Instruction (Part 1)
                        ushort replace2 = 0xE800;//BLX Instruction (Part 2)
                        uint destinationOffset = uint.Parse(lines[0], NumberStyles.HexNumber);
                        uint relativeDestinationOffset = destinationOffset - replaceOffset - 2;
                        relativeDestinationOffset >>= 1;
                        relativeDestinationOffset &= 0x003FFFFF;
                        replace1 |= (ushort)((relativeDestinationOffset >> 11) & 0x7FF);
                        replace2 |= (ushort)((relativeDestinationOffset >> 0) & 0x7FE);
                        if (!arm9.WriteU16LE(replaceOffset, replace1)) 
                        {
                            throw new(
                                $"The offset of function {lines[3]} is invalid. Maybe your code is inside an overlay or you wrote the wrong offset.\r\nIf your code is inside an overlay, this is an action replay code to let your asm hack still work:\r\n1 {replaceOffset:X7} 0000{replace1:X4}\r\n1{replaceOffset + 2:X7} 0000{replace2:X4})"
                            );
                        }

                        arm9.WriteU16LE(replaceOffset + 2, replace2);
                        break;
                    }
                }
            }
        }
        r.Close();
        arm9.WriteU32LE(arenaLoOffset, arenaLo + (uint)newCode.Length);
        arm9.AddAutoLoadEntry(arenaLo, newCode);
    }

    private static bool Compile(string makePath, string dockerPath, string path, uint arenaLo, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, string dockerTag, string devkitArmPath)
    {
        ProcessStartInfo psi;
        if (!string.IsNullOrEmpty(dockerTag))
        {
            psi = new()
            {
                FileName = dockerPath,
                Arguments = $"run --rm -v \"{Path.GetFullPath(path)}:/src\" -w /src devkitpro/devkitarm:{dockerTag} make TARGET=newcode SOURCES=source BUILD=build CODEADDR=0x{arenaLo:X8}",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
        }
        else
        {
            psi = new()
            {
                FileName = makePath,
                Arguments = $"TARGET=newcode SOURCES=source BUILD=build CODEADDR=0x{arenaLo:X8}",
                WorkingDirectory = path,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
        }
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

    private static bool CompileReplace(string makePath, string dockerPath, string subdir, string path, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, string dockerTag, string devkitArmPath)
    {
        uint address = uint.Parse(Path.GetFileNameWithoutExtension(subdir), NumberStyles.HexNumber);
        ProcessStartInfo psi;
        if (!string.IsNullOrEmpty(dockerTag))
        {
            subdir = subdir.Replace('\\', '/');
            psi = new()
            {
                FileName = dockerPath,
                Arguments = $"run --rm -v \"{Path.GetFullPath(path)}:/src\" -w /src devkitpro/devkitarm:{dockerTag} make TARGET=repl_{Path.GetFileNameWithoutExtension(subdir)} SOURCES={subdir} BUILD=build NEWSYM=newcode.x CODEADDR=0x{address:X7}",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
        }
        else
        {
            psi = new()
            {
                FileName = makePath,
                Arguments = $"TARGET=repl_{Path.GetFileNameWithoutExtension(subdir)} SOURCES={subdir} BUILD=build NEWSYM=newcode.x CODEADDR=0x{address:X7}",
                WorkingDirectory = path,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
        }
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