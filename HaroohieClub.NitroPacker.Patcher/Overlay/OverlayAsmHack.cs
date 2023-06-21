using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace HaroohieClub.NitroPacker.Patcher.Overlay
{
    public class OverlayAsmHack
    {
        public static bool Insert(string path, Overlay overlay, string romInfoPath, string dockerTag, DataReceivedEventHandler outputDataReceived = null, DataReceivedEventHandler errorDataReceived = null,
            string makePath = "make", string dockerPath = "docker", string devkitArmPath = "")
        {
            if (!Compile(makePath, dockerPath, path, overlay, outputDataReceived, errorDataReceived, dockerTag, devkitArmPath))
            {
                return false;
            }

            // Add a new symbols file based on what we just compiled so the replacements can reference the old symbols
            string[] newSym = File.ReadAllLines(Path.Combine(path, overlay.Name, "newcode.sym"));
            List<string> newSymbolsFile = new();
            foreach (string line in newSym)
            {
                Match match = Regex.Match(line, @"(?<address>[\da-f]{8}) \w\s+.text\s+\d{8} (?<name>.+)");
                if (match.Success)
                {
                    newSymbolsFile.Add($"{match.Groups["name"].Value} = 0x{match.Groups["address"].Value.ToUpper()};");
                }
            }
            File.WriteAllLines(Path.Combine(path, overlay.Name, "newcode.x"), newSymbolsFile);

            // Each repl should be compiled separately since they all have their own entry points
            // That's why each one lives in its own separate directory
            List<string> replFiles = new();
            if (Directory.Exists(Path.Combine(path, overlay.Name, "replSource")))
            {
                foreach (string subdir in Directory.GetDirectories(Path.Combine(path, overlay.Name, "replSource")))
                {
                    replFiles.Add($"repl_{Path.GetFileNameWithoutExtension(subdir)}");
                    if (!CompileReplace(makePath, dockerPath, Path.GetRelativePath(path, subdir), path, overlay, outputDataReceived, errorDataReceived, dockerTag, devkitArmPath))
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

        private static bool Compile(string makePath, string dockerPath, string path, Overlay overlay, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, string dockerTag, string devkitArmPath)
        {
            ProcessStartInfo psi;
            if (!string.IsNullOrEmpty(dockerTag))
            {
                psi = new()
                {
                    FileName = dockerPath,
                    Arguments = $"run -v \"{Path.GetFullPath(path)}\":/src -w /src devkitpro/devkitarm:{dockerTag} make TARGET={overlay.Name}/newcode SOURCES={overlay.Name}/source INCLUDES={overlay.Name}/source BUILD=build CODEADDR=0x{overlay.Address + overlay.Length:X7}",
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
                    Arguments = $"TARGET={overlay.Name}/newcode SOURCES={overlay.Name}/source INCLUDES={overlay.Name}/source BUILD=build CODEADDR=0x{overlay.Address + overlay.Length:X7}",
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
            static void func(object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine(e.Data);
            }
            p.OutputDataReceived += outputDataReceived is not null ? outputDataReceived : func;
            p.ErrorDataReceived += errorDataReceived is not null ? errorDataReceived : func;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode == 0;
        }

        private static bool CompileReplace(string makePath, string dockerPath, string subdir, string path, Overlay overlay, DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived, string dockerTag, string devkitArmPath)
        {
            uint address = uint.Parse(Path.GetFileNameWithoutExtension(subdir), NumberStyles.HexNumber);
            ProcessStartInfo psi;

            if (!string.IsNullOrEmpty(dockerTag))
            {
                subdir = subdir.Replace('\\', '/');
                psi = new()
                {
                    FileName = dockerPath,
                    Arguments = $"run -v \"{Path.GetFullPath(path)}\":/src -w /src devkitpro/devkitarm:{dockerTag} make TARGET={overlay.Name}/repl_{Path.GetFileNameWithoutExtension(subdir)} SOURCES={subdir} INCLUDES={subdir} NEWSYM={overlay.Name}/newcode.x BUILD=build CODEADDR=0x{address:X7}",
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
                    Arguments = $"TARGET={overlay.Name}/repl_{Path.GetFileNameWithoutExtension(subdir)} SOURCES={subdir} INCLUDES={subdir} NEWSYM={overlay.Name}/newcode.x BUILD=build CODEADDR=0x{address:X7}",
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
            static void func(object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine(e.Data);
            }
            p.OutputDataReceived += outputDataReceived is not null ? outputDataReceived : func;
            p.ErrorDataReceived += errorDataReceived is not null ? errorDataReceived : func;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode == 0;
        }
    }
}
